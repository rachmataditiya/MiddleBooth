// File: MainWindow.xaml.cs

using System;
using System.Windows;
using MiddleBooth.Views;
using MiddleBooth.Services;
using MiddleBooth.Services.Interfaces;
using MiddleBooth.ViewModels;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Serilog;

namespace MiddleBooth
{
    public partial class MainWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly IDSLRBoothService _dslrBoothService;
        private readonly IOdooService _odooService;
        private readonly IWebServerService _webServerService;

        public MainWindow()
        {
            InitializeComponent();

            var serviceProvider = ((App)Application.Current).ServiceProvider
                                  ?? throw new InvalidOperationException("ServiceProvider is not initialized.");

            _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
            _navigationService = serviceProvider.GetRequiredService<INavigationService>();
            _paymentService = serviceProvider.GetRequiredService<IPaymentService>();
            _dslrBoothService = serviceProvider.GetRequiredService<IDSLRBoothService>();
            _odooService = serviceProvider.GetRequiredService<IOdooService>();
            _webServerService = serviceProvider.GetRequiredService<IWebServerService>();

            _navigationService.NavigationRequested += OnNavigationRequested;
            _navigationService.OverlayRequested += OnOverlayRequested;

            Content = new MainView(_settingsService, _navigationService, _dslrBoothService, _odooService);

            Loaded += MainWindow_Loaded;

            Log.Information("MainWindow initialized");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("MainWindow loaded, initializing components");
                Topmost = true;
                await Task.Delay(500);

                if (Content is MainView mainView && mainView.DataContext is MainViewModel viewModel)
                {
                    await viewModel.SetInitialVisibility();
                    Log.Information("Initial visibility state set");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during MainWindow_Loaded");
            }
        }

        private async void OnNavigationRequested(object? sender, string viewName)
        {
            try
            {
                Log.Information($"Navigation requested to: {viewName}");

                Content = viewName switch
                {
                    "SettingsView" => new SettingsView(new SettingsViewModel(_settingsService, _navigationService)),
                    "PaymentOptionsPage" => new PaymentOptionsPage(new PaymentOptionsPageViewModel(_navigationService,_paymentService)),
                    "QrisPaymentPage" => new QrisPaymentPage(new QrisPaymentPageViewModel(_navigationService, _paymentService, _odooService, _dslrBoothService, _webServerService, _settingsService)),
                    "VoucherPaymentPage" => new VoucherPaymentPage(new VoucherPaymentPageViewModel(_navigationService, _paymentService, _odooService, _dslrBoothService, _webServerService, _settingsService)),
                    _ => new MainView(_settingsService, _navigationService, _dslrBoothService, _odooService)
                };

                if (Content is MainView mainView && mainView.DataContext is MainViewModel viewModel)
                {
                    await viewModel.SetInitialVisibility();
                    Log.Information("Initial visibility state set after navigation");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error during navigation to {viewName}");
            }
        }

        private void OnOverlayRequested(object? sender, object overlayView)
        {
            try
            {
                Log.Information("Overlay requested");

                if (overlayView is UserControl overlayControl)
                {
                    OverlayContainer.Children.Clear();
                    OverlayContainer.Children.Add(overlayControl);
                    Log.Information("Overlay added to container");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding overlay");
            }
        }

        public void SetVisibility(bool isVisible)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    Topmost = isVisible;
                    Log.Information($"MainWindow visibility set to: {Visibility}, Topmost: {Topmost}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error setting MainWindow visibility to {isVisible}");
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                Log.Information("MainWindow closing");

                base.OnClosed(e);
                if (Content is MainView mainView && mainView.DataContext is MainViewModel viewModel)
                {
                    viewModel.Dispose();
                    Log.Information("MainViewModel disposed");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during MainWindow closure");
            }
        }
    }
}