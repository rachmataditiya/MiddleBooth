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
            // Inisialisasi logger sebelum layanan diinisialisasi
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
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IWebServerService, WebServerService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPaymentService, PaymentService>();

            // Tambahkan logging ke DI
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(dispose: true);
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application Exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
