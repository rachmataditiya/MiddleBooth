using System.Windows;
using MiddleBooth.Views;
using MiddleBooth.Services;
using MiddleBooth.Services.Interfaces;
using MiddleBooth.ViewModels;
using System.Windows.Controls;

namespace MiddleBooth
{
    public partial class MainWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;

        public MainWindow()
        {
            InitializeComponent();

            _settingsService = new SettingsService();
            _navigationService = new NavigationService();
            _paymentService = new PaymentService(_settingsService, _navigationService);

            _navigationService.NavigationRequested += OnNavigationRequested;
            _navigationService.OverlayRequested += OnOverlayRequested;

            Content = new MainView(_settingsService, _navigationService);
        }

        private void OnNavigationRequested(object? sender, string viewName)
        {
            Content = viewName switch
            {
                "SettingsView" => new SettingsView(new SettingsViewModel(_settingsService, _navigationService)),
                "PaymentOptionsPage" => new PaymentOptionsPage(new PaymentOptionsPageViewModel(_navigationService, _paymentService)),
                "QrisPaymentPage" => new QrisPaymentPage(new QrisPaymentPageViewModel(_navigationService, _paymentService)),
                "VoucherPaymentPage" => new VoucherPaymentPage(new VoucherPaymentPageViewModel(_navigationService, _paymentService)),
                _ => new MainView(_settingsService, _navigationService)
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
    }
}
