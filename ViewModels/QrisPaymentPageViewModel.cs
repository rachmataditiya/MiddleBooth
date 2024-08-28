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
        private readonly IWebServerService _webServerService;
        private readonly ISettingsService _settingsService;
        private readonly string _machineId;

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

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public QrisPaymentPageViewModel(
            INavigationService navigationService,
            IPaymentService paymentService,
            IDSLRBoothService dslrBoothService,
            IOdooService odooService,
            IWebServerService webServerService,
            ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;
            _dslrBoothService = dslrBoothService;
            _odooService = odooService;
            _webServerService = webServerService;
            _settingsService = settingsService;
            _machineId = _settingsService.GetMachineId();

            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));

            _paymentService.OnPaymentNotificationReceived += HandlePaymentNotification;
            _webServerService.TriggerReceived += OnTriggerReceived;
            Task.Run(StartQrisPayment);
        }

        private async Task StartQrisPayment()
        {
            try
            {
                decimal amount = _paymentService.GetServicePrice();

                if (Application.Current.Properties.Contains("DiscountedPrice"))
                {
                    var discountedPrice = Application.Current.Properties["DiscountedPrice"];
                    if (discountedPrice is decimal decimalPrice)
                    {
                        amount = decimalPrice;
                    }
                }

                Amount = amount;
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
                    await CreateBoothOrder();
                    await LaunchDSLRBooth();
                }
            });
        }

        private async Task CreateBoothOrder()
        {
            try
            {
                var (success, orderId, message) = await _odooService.CreateBoothOrder(_machineId);
                if (success && orderId.HasValue)
                {
                    Log.Information($"Booth order created successfully with ID: {orderId}");
                    ShowNotification($"Order berhasil dibuat dengan ID: {orderId}");
                }
                else
                {
                    Log.Warning($"Failed to create booth order: {message}");
                    ShowNotification($"Gagal membuat order: {message}");
                }

                if (Application.Current.Properties.Contains("DiscountedPrice"))
                {
                    Application.Current.Properties.Remove("DiscountedPrice");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating booth order");
                ShowNotification($"Error membuat order: {ex.Message}");
            }
        }

        private async Task LaunchDSLRBooth()
        {
            try
            {
                if (_dslrBoothService.IsDSLRBoothRunning())
                {
                    await _dslrBoothService.SetDSLRBoothVisibility(true);
                    Log.Information("DSLRBooth set to visible after successful payment");
                }
                else
                {
                    bool launched = await _dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        await _dslrBoothService.SetDSLRBoothVisibility(true);
                        Log.Information("DSLRBooth launched and set to visible after successful payment");
                    }
                    else
                    {
                        Log.Warning("Failed to launch DSLRBooth after successful payment");
                        ShowNotification("Pembayaran berhasil, tapi gagal menjalankan DSLRBooth. Silakan cek pengaturan.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while launching DSLRBooth");
                ShowNotification("Terjadi kesalahan saat menjalankan DSLRBooth. Silakan hubungi administrator.");
            }
        }

        private void UpdatePaymentStatus(string status)
        {
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
            NotificationMessage = message;
            IsNotificationVisible = true;

            Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsNotificationVisible = false;
                    NotificationMessage = string.Empty;
                });
            });
        }

        private async void OnTriggerReceived(object? sender, DSLRBoothEvent e)
        {
            if (e.EventType == "session_end")
            {
                Log.Information("DSLRBooth session ended. Navigating to MainView.");
                await _dslrBoothService.SetDSLRBoothVisibility(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _navigationService.NavigateTo("MainView");
                });
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _paymentService.OnPaymentNotificationReceived -= HandlePaymentNotification;
                _webServerService.TriggerReceived -= OnTriggerReceived;
            }
        }
    }
}