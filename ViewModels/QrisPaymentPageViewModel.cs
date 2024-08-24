// File: ViewModels/QrisPaymentPageViewModel.cs

using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using Serilog;

namespace MiddleBooth.ViewModels
{
    public class QrisPaymentPageViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly IDSLRBoothService _dslrBoothService;
        private readonly IOdooService _odooService;

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

        public QrisPaymentPageViewModel(INavigationService navigationService, IPaymentService paymentService, IDSLRBoothService dslrBoothService, IOdooService odooService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;
            _dslrBoothService = dslrBoothService;
            _odooService = odooService;

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

        private async void HandlePaymentNotification(string status)
        {
            Log.Information($"Payment notification received in ViewModel: {status}");

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                Log.Information("Memperbarui status pembayaran di UI.");
                UpdatePaymentStatus(status);
                ShowNotification($"Status pembayaran: {PaymentStatus}");

                if (status.ToLower().Trim() == "settlement")
                {
                    await HandleSuccessfulPayment();
                }
            });
        }

        private async Task HandleSuccessfulPayment()
        {
            try
            {
                if (_dslrBoothService.IsDSLRBoothRunning())
                {
                    await _dslrBoothService.SetDSLRBoothVisibility(true);
                    Log.Information("DSLRBooth set to topmost after successful payment");
                }
                else
                {
                    bool launched = await _dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        await _dslrBoothService.SetDSLRBoothVisibility(true);
                        Log.Information("DSLRBooth launched and set to topmost after successful payment");
                    }
                    else
                    {
                        Log.Warning("Failed to launch DSLRBooth after successful payment");
                        ShowNotification("Pembayaran berhasil, tapi gagal menjalankan DSLRBooth. Silakan cek pengaturan.");
                        return;
                    }
                }

                // Set MiddleBooth window to not topmost
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.SetVisibility(true);
                }

                // Buat order QRIS secara asinkron
                await CreateQRISOrder();

                _navigationService.NavigateTo("MainView");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while handling successful payment");
                ShowNotification("Terjadi kesalahan saat memproses pembayaran. Silakan hubungi administrator.");
            }
        }

        private async Task CreateQRISOrder()
        {
            try
            {
                string name = $"BO{DateTime.Now:yyyyMMddHHmmss}";
                decimal price = _paymentService.GetServicePrice();
                string saleType = "QRIS";
                Log.Information($"Creating QRIS booth order: {name}, Price: {price}, Sale Type: {saleType}");

                bool success = await _odooService.CreateBoothOrder(name, DateTime.Now, price, saleType);
                if (success)
                {
                    Log.Information($"QRIS booth order {name} created successfully");
                }
                else
                {
                    Log.Warning($"Failed to create QRIS booth order {name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating QRIS booth order");
            }
        }

        private void UpdatePaymentStatus(string status)
        {
            Log.Information($"Memperbarui status pembayaran menjadi: {status}");

            switch (status.ToLower().Trim())
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
            Log.Information($"Menampilkan notifikasi: {message}");
            NotificationMessage = message;
            IsNotificationVisible = true;

            // Sembunyikan notifikasi setelah 3 detik
            Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsNotificationVisible = false;
                    NotificationMessage = string.Empty;
                    Log.Information("Notifikasi disembunyikan.");
                });
            });
        }

        public void Dispose()
        {
            _paymentService.OnPaymentNotificationReceived -= HandlePaymentNotification;
        }
    }
}