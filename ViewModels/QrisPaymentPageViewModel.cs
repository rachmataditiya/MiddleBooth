using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace MiddleBooth.ViewModels
{
    public class QrisPaymentPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;

        public ICommand BackCommand { get; }

        private BitmapImage _qrCodeImageSource = new();
        public BitmapImage QrCodeImageSource
        {
            get => _qrCodeImageSource;
            set => SetProperty(ref _qrCodeImageSource, value);
        }

        private string _paymentStatus = string.Empty;
        public string PaymentStatus
        {
            get => _paymentStatus;
            set => SetProperty(ref _paymentStatus, value);
        }

        private string _paymentStatusColor = "Gray";
        public string PaymentStatusColor
        {
            get => _paymentStatusColor;
            set => SetProperty(ref _paymentStatusColor, value);
        }

        private bool _isNotificationVisible;
        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            set => SetProperty(ref _isNotificationVisible, value);
        }

        private string _notificationMessage = string.Empty;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }

        public QrisPaymentPageViewModel(INavigationService navigationService, IPaymentService paymentService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;

            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));

            _paymentService.OnPaymentNotificationReceived += HandlePaymentNotification;

            Task.Run(StartQrisPayment);
        }

        private async Task StartQrisPayment()
        {
            try
            {
                decimal amount = _paymentService.GetServicePrice();
                string qrCodeUrl = await _paymentService.GenerateQRCode(amount);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(qrCodeUrl))
                    {
                        QrCodeImageSource = new BitmapImage(new Uri(qrCodeUrl));
                        UpdatePaymentStatus("waiting");
                        ShowNotification("QR Code berhasil dibuat. Silakan scan untuk melakukan pembayaran.");
                    }
                    else
                    {
                        UpdatePaymentStatus("error");
                        ShowNotification("Gagal menghasilkan QR code. Silakan coba lagi.");
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdatePaymentStatus("error");
                    ShowNotification($"Error: {ex.Message}");
                });
            }
        }

        private void HandlePaymentNotification(string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePaymentStatus(status);
                ShowNotification($"Status pembayaran: {PaymentStatus}");
            });
        }

        private void UpdatePaymentStatus(string status)
        {
            switch (status.ToLower())
            {
                case "settlement":
                    PaymentStatus = "Pembayaran Berhasil!";
                    PaymentStatusColor = "Green";
                    break;
                case "deny":
                case "cancel":
                case "expire":
                    PaymentStatus = "Pembayaran Gagal!";
                    PaymentStatusColor = "Red";
                    break;
                case "waiting":
                    PaymentStatus = "Menunggu pembayaran...";
                    PaymentStatusColor = "Orange";
                    break;
                case "error":
                    PaymentStatus = "Terjadi kesalahan";
                    PaymentStatusColor = "Red";
                    break;
                default:
                    PaymentStatus = $"Status: {status}";
                    PaymentStatusColor = "Gray";
                    break;
            }
        }

        private void ShowNotification(string message)
        {
            NotificationMessage = message;
            IsNotificationVisible = true;

            // Hide notification after 3 seconds
            Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsNotificationVisible = false;
                    NotificationMessage = string.Empty;
                });
            });
        }

        ~QrisPaymentPageViewModel()
        {
            _paymentService.OnPaymentNotificationReceived -= HandlePaymentNotification;
        }
    }
}