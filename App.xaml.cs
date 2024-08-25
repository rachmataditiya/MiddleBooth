// File: App.xaml.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiddleBooth.Services.Interfaces;
using MiddleBooth.Services;
using Serilog;
using System;
using System.Windows;

namespace MiddleBooth
{
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs\\app-log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Application Starting");

            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            // Start the web server
            var webServerService = ServiceProvider.GetRequiredService<IWebServerService>();
            _ = webServerService.StartServerAsync();

            // Check and launch DSLRBooth if necessary
            var dslrBoothService = ServiceProvider.GetRequiredService<IDSLRBoothService>();
            CheckAndLaunchDSLRBooth(dslrBoothService);

            var mqttClientService = ServiceProvider.GetRequiredService<IMqttClientService>();
            _ = mqttClientService.StartAsync(CancellationToken.None);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IWebServerService, WebServerService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPaymentService, PaymentService>();
            services.AddSingleton<IDSLRBoothService, DSLRBoothService>();
            services.AddSingleton<IOdooService, OdooService>();
            services.AddSingleton<IMqttClientService, MqttClientService>();

            // Tambahkan logging ke DI
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(dispose: true);
            });
        }

        private static async void CheckAndLaunchDSLRBooth(IDSLRBoothService dslrBoothService)
        {
            if (dslrBoothService.CheckDSLRBoothPath())
            {
                if (!dslrBoothService.IsDSLRBoothRunning())
                {
                    bool launched = await dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        Log.Information("DSLRBooth launched successfully");
                        await dslrBoothService.SetDSLRBoothVisibility(false);
                    }
                    else
                    {
                        Log.Warning("Failed to launch DSLRBooth");
                    }
                }
                else
                {
                    Log.Information("DSLRBooth is already running");
                    await dslrBoothService.SetDSLRBoothVisibility(false);
                }
            }
            else
            {
                Log.Warning("DSLRBooth path is not set or invalid");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (ServiceProvider != null)
            {
                var mqttClientService = ServiceProvider.GetRequiredService<IMqttClientService>();
                _ = mqttClientService.StopAsync(CancellationToken.None);
            }
            Log.Information("Application Exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}