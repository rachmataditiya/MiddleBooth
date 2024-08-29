using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System.Windows.Input;
using System.Windows;
using Serilog;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.IO;

namespace MiddleBooth.ViewModels
{
    public class PaymentOptionsPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingsService _settingsService;

        public ICommand QrisPaymentCommand { get; }
        public ICommand VoucherPaymentCommand { get; }
        public ICommand BackCommand { get; }

        private ImageSource? _productImageSource;
        public ImageSource? ProductImageSource
        {
            get => _productImageSource;
            set => SetProperty(ref _productImageSource, value);
        }


        private decimal _servicePrice;
        public decimal ServicePrice
        {
            get => _servicePrice;
            set => SetProperty(ref _servicePrice, value);
        }

        public string FormattedServicePrice => $"IDR {ServicePrice:N0}";

        public PaymentOptionsPageViewModel(INavigationService navigationService, IPaymentService paymentService, ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;
            _settingsService = settingsService;

            QrisPaymentCommand = new RelayCommand(_ => StartQrisPayment());
            VoucherPaymentCommand = new RelayCommand(_ => StartVoucherPayment());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));

            LoadProductImage();
            LoadServicePrice();

            Log.Information("PaymentOptionsPageViewModel initialized");
        }

        private void LoadProductImage()
        {
            string imagePath = _settingsService.GetProductImagePath();
            if (File.Exists(imagePath))
            {
                ProductImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                Log.Information($"Product image loaded from {imagePath}");
            }
            else
            {
                Log.Warning($"Product image not found at {imagePath}");
                // Optionally, set a default image here
                // ProductImageSource = new BitmapImage(new Uri("/Resources/default_product_image.png", UriKind.Relative));
            }
        }

        private void LoadServicePrice()
        {
            ServicePrice = _paymentService.GetServicePrice();
            Log.Information($"Service price loaded: {FormattedServicePrice}");
        }

        private void StartQrisPayment()
        {
            Log.Information("Starting QRIS payment process");
            // Clear any existing voucher data
            if (Application.Current.Properties.Contains("VoucherCode"))
            {
                Application.Current.Properties.Remove("VoucherCode");
            }
            if (Application.Current.Properties.Contains("DiscountedPrice"))
            {
                Application.Current.Properties.Remove("DiscountedPrice");
            }
            // Set full price for QRIS payment
            Application.Current.Properties["QrisPaymentAmount"] = ServicePrice;
            _navigationService.NavigateTo("QrisPaymentPage");
        }

        private void StartVoucherPayment()
        {
            Log.Information("Starting voucher payment process");
            _navigationService.NavigateTo("VoucherPaymentPage");
        }
    }
}