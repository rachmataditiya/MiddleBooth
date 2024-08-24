// File: MainWindow.xaml.cs

using System.Windows;
using MiddleBooth.Views;
using MiddleBooth.Services;
using MiddleBooth.Services.Interfaces;
using MiddleBooth.ViewModels;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace MiddleBooth
{
    public partial class MainWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly IDSLRBoothService _dslrBoothService;

        public MainWindow()
        {
            InitializeComponent();

            var serviceProvider = ((App)Application.Current).ServiceProvider
                                  ?? throw new InvalidOperationException("ServiceProvider is not initialized.");

            _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
            _navigationService = serviceProvider.GetRequiredService<INavigationService>();
            _paymentService = serviceProvider.GetRequiredService<IPaymentService>();
            _dslrBoothService = serviceProvider.GetRequiredService<IDSLRBoothService>();

            _navigationService.NavigationRequested += OnNavigationRequested;
            _navigationService.OverlayRequested += OnOverlayRequested;

            Content = new MainView(_settingsService, _navigationService, _dslrBoothService);
        }

        private void OnNavigationRequested(object? sender, string viewName)
        {
            Content = viewName switch
            {
                "SettingsView" => new SettingsView(new SettingsViewModel(_settingsService, _navigationService)),
                "PaymentOptionsPage" => new PaymentOptionsPage(new PaymentOptionsPageViewModel(_navigationService, _paymentService)),
                "QrisPaymentPage" => new QrisPaymentPage(new QrisPaymentPageViewModel(_navigationService, _paymentService, _dslrBoothService)),
                "VoucherPaymentPage" => new VoucherPaymentPage(new VoucherPaymentPageViewModel(_navigationService, _paymentService)),
                _ => new MainView(_settingsService, _navigationService, _dslrBoothService)
            };
        }

        private void OnOverlayRequested(object? sender, object overlayView)
        {
            if (overlayView is UserControl overlayControl)
            {
                OverlayContainer.Children.Clear();
                OverlayContainer.Children.Add(overlayControl);
            }
        }

        public void SetTopmost(bool isTopmost)
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = isTopmost;
            });
        }
    }
}