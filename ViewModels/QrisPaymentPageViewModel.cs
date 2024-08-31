using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Serilog;
using System.Net.Http;

namespace MiddleBooth.ViewModels
{
    public class QrisPaymentPageViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly IOdooService _odooService;
        private readonly IDSLRBoothService _dslrBoothService;
        private readonly IWebServerService _webServerService;
        private readonly ISettingsService _settingsService;
        private readonly string _machineId;
        private bool _printed = false;

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

        private string? _voucherCode;
        public string? VoucherCode
        {
            get => _voucherCode;
            set => SetProperty(ref _voucherCode, value);
        }

        private bool _orderCreated = false;

        public QrisPaymentPageViewModel(
            INavigationService navigationService,
            IPaymentService paymentService,
            IOdooService odooService,
            IDSLRBoothService dslrBoothService,
            IWebServerService webServerService,
            ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;
            _odooService = odooService;
            _dslrBoothService = dslrBoothService;
            _webServerService = webServerService;
            _settingsService = settingsService;
            _machineId = _settingsService.GetMachineId();

            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));

            if (Application.Current.Properties.Contains("QrisPaymentAmount"))
            {
                var qrisAmountObj = Application.Current.Properties["QrisPaymentAmount"];
                if (qrisAmountObj is decimal qrisAmount)
                {
                    Amount = qrisAmount;
                }
                Application.Current.Properties.Remove("QrisPaymentAmount");
            }
            else if (Application.Current.Properties.Contains("DiscountedPrice"))
            {
                var discountedPriceObj = Application.Current.Properties["DiscountedPrice"];
                if (discountedPriceObj is decimal discountedPrice)
                {
                    Amount = discountedPrice;
                }
                Application.Current.Properties.Remove("DiscountedPrice");
            }

            if (Amount == 0)
            {
                Amount = _paymentService.GetServicePrice();
                Log.Warning("No valid payment amount found. Using default service price.");
            }

            if (Application.Current.Properties.Contains("VoucherCode"))
            {
                var voucherCodeObj = Application.Current.Properties["VoucherCode"];
                if (voucherCodeObj is string voucherCode)
                {
                    VoucherCode = voucherCode;
                }
                Log.Information($"Voucher Code is Available: {VoucherCode}");
                Application.Current.Properties.Remove("VoucherCode");
            }

            _paymentService.OnPaymentNotificationReceived += HandlePaymentNotification;
            _webServerService.TriggerReceived += OnTriggerReceived;
            Task.Run(StartQrisPayment);
        }

        private async Task StartQrisPayment()
        {
            try
            {
                string qrCodeUrl = await _paymentService.GenerateQRCode(Amount);

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

                if (status.ToLower().Trim() == "settlement" && !_orderCreated)
                {
                    await CreateBoothOrder();
                    await LaunchDSLRBooth();
                }
            });
        }

        private async Task CreateBoothOrder()
        {
            if (_orderCreated) return;

            try
            {
                Log.Information($"Attempting to create booth order. Voucher Code: {VoucherCode}");
                var (success, orderId, message) = VoucherCode == null
                    ? await _odooService.CreateBoothOrder(_machineId)
                    : await _odooService.CreateBoothOrder(_machineId, VoucherCode);

                if (success && orderId.HasValue)
                {
                    Log.Information($"Booth order created successfully with ID: {orderId}");
                    ShowNotification($"Order berhasil dibuat dengan ID: {orderId}");
                    _orderCreated = true;
                }
                else
                {
                    Log.Warning($"Failed to create booth order: {message}");
                    ShowNotification($"Gagal membuat order: {message}");
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
                    await _dslrBoothService.CallStartApi();
                    Log.Information("DSLRBooth set to visible after successful payment");
                }
                else
                {
                    bool launched = await _dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        await _dslrBoothService.SetDSLRBoothVisibility(true);
                        await _dslrBoothService.CallStartApi();
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
            if (e.EventType == "printing")
            {
                _printed = true;
                Log.Information("Printing event received.");
            }
            else if (e.EventType == "session_end" && _printed)
            {
                Log.Information("DSLRBooth session ended after printing. Navigating to MainView.");
                await _dslrBoothService.SetDSLRBoothVisibility(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _navigationService.NavigateTo("MainView");
                });
                _printed = false; // Reset for next session
            }
            else if (e.EventType == "session_end" && !_printed)
            {
                Log.Information("DSLRBooth session ended without printing. Ignoring.");
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
                Log.Information("QrisPaymentPageViewModel is being disposed.");
                _paymentService.OnPaymentNotificationReceived -= HandlePaymentNotification;
                _webServerService.TriggerReceived -= OnTriggerReceived;
            }
        }
    }
}