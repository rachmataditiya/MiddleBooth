using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Serilog;

namespace MiddleBooth.ViewModels
{
    public class VoucherPaymentPageViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly IOdooService _odooService;
        private readonly IDSLRBoothService _dslrBoothService;
        private readonly IWebServerService _webServerService;
        private readonly ISettingsService _settingsService;
        private readonly string _machineId;
        private bool _printed = false;
        private bool _disposed;

        public ICommand ValidateVoucherCommand { get; }
        public ICommand BackCommand { get; }

        private string _voucherCode = string.Empty;
        public string VoucherCode
        {
            get => _voucherCode;
            set => SetProperty(ref _voucherCode, value);
        }

        private string _validationResult = string.Empty;
        public string ValidationResult
        {
            get => _validationResult;
            set => SetProperty(ref _validationResult, value);
        }

        private string _validationResultColor = "Gray";
        public string ValidationResultColor
        {
            get => _validationResultColor;
            set => SetProperty(ref _validationResultColor, value);
        }

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => SetProperty(ref _discountAmount, value);
        }

        private decimal _discountedPrice;
        public decimal DiscountedPrice
        {
            get => _discountedPrice;
            set => SetProperty(ref _discountedPrice, value);
        }

        private bool _isFullVoucher;
        public bool IsFullVoucher
        {
            get => _isFullVoucher;
            set => SetProperty(ref _isFullVoucher, value);
        }

        private bool _isPartialVoucher;
        public bool IsPartialVoucher
        {
            get => _isPartialVoucher;
            set => SetProperty(ref _isPartialVoucher, value);
        }

        public VoucherPaymentPageViewModel(
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

            ValidateVoucherCommand = new RelayCommand(async _ => await ValidateVoucherAsync());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentOptionsPage"));

            _webServerService.TriggerReceived += OnTriggerReceived;

            Log.Information("VoucherPaymentPageViewModel initialized");
        }

        private async Task ValidateVoucherAsync()
        {
            Log.Information($"Validating voucher: {VoucherCode}");
            try
            {
                var voucherDetails = await _odooService.CheckVoucher(VoucherCode, _machineId);
                if (voucherDetails.IsValid)
                {
                    Log.Information($"Voucher {VoucherCode} is valid");

                    decimal fullPrice = _paymentService.GetServicePrice();
                    IsFullVoucher = string.Equals(voucherDetails.VoucherType, "full", StringComparison.OrdinalIgnoreCase);
                    IsPartialVoucher = !IsFullVoucher;

                    switch (voucherDetails.VoucherType.ToLowerInvariant())
                    {
                        case "full":
                            DiscountAmount = fullPrice;
                            DiscountedPrice = 0;
                            ValidationResult = "Voucher Valid! Memulai sesi foto...";
                            await CreateBoothOrder();
                            await LaunchDSLRBooth();
                            break;
                        case "percentage":
                            DiscountAmount = fullPrice * (decimal)(voucherDetails.Value / 100f);
                            DiscountedPrice = Math.Max(fullPrice - DiscountAmount, 0);
                            ValidationResult = $"Voucher Valid! Diskon: {voucherDetails.Value}% (Rp{DiscountAmount:N0}). Melanjutkan ke pembayaran...";
                            ProceedToPayment();
                            break;
                        case "nominal":
                            DiscountAmount = (decimal)voucherDetails.Value;
                            DiscountedPrice = Math.Max(fullPrice - DiscountAmount, 0);
                            ValidationResult = $"Voucher Valid! Diskon: Rp{DiscountAmount:N0}. Melanjutkan ke pembayaran...";
                            ProceedToPayment();
                            break;
                        default:
                            throw new Exception($"Tipe voucher tidak dikenal: {voucherDetails.VoucherType}");
                    }

                    ValidationResultColor = "Green";
                }
                else
                {
                    Log.Warning($"Voucher {VoucherCode} is not valid");
                    ValidationResult = "Kode Voucher Tidak Valid!";
                    ValidationResultColor = "Red";
                    ResetVoucherState();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error validating voucher {VoucherCode}");
                ValidationResult = $"Error: {ex.Message}";
                ValidationResultColor = "Red";
                ResetVoucherState();
            }
        }

        private void ResetVoucherState()
        {
            IsFullVoucher = false;
            IsPartialVoucher = false;
            DiscountAmount = 0;
            DiscountedPrice = 0;
        }

        private void ProceedToPayment()
        {
            if (IsPartialVoucher)
            {
                Log.Information($"Proceeding to QRIS payment for discounted amount: {DiscountedPrice}");
                Application.Current.Properties["DiscountedPrice"] = DiscountedPrice;
                Application.Current.Properties["VoucherCode"] = VoucherCode;
                _navigationService.NavigateTo("QrisPaymentPage");
                
            }
        }

        private async Task CreateBoothOrder()
        {
            try
            {
                var (success, orderId, message) = await _odooService.CreateBoothOrder(_machineId, VoucherCode);
                if (success && orderId.HasValue)
                {
                    Log.Information($"Booth order created successfully with ID: {orderId}");
                    ValidationResult = $"Order berhasil dibuat dengan ID: {orderId}";
                    ValidationResultColor = "Green";
                }
                else
                {
                    Log.Warning($"Failed to create booth order: {message}");
                    ValidationResult = $"Gagal membuat order: {message}";
                    ValidationResultColor = "Red";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating booth order");
                ValidationResult = $"Error membuat order: {ex.Message}";
                ValidationResultColor = "Red";
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
                    Log.Information("DSLRBooth set to visible after successful voucher validation");
                }
                else
                {
                    bool launched = await _dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        await _dslrBoothService.SetDSLRBoothVisibility(true);
                        await _dslrBoothService.CallStartApi();
                        Log.Information("DSLRBooth launched and set to visible after successful voucher validation");
                    }
                    else
                    {
                        Log.Warning("Failed to launch DSLRBooth after successful voucher validation");
                        ValidationResult = "Voucher valid, tapi gagal menjalankan DSLRBooth. Silakan cek pengaturan.";
                        ValidationResultColor = "Orange";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while launching DSLRBooth");
                ValidationResult = "Terjadi kesalahan saat menjalankan DSLRBooth. Silakan hubungi administrator.";
                ValidationResultColor = "Red";
            }
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Log.Information("VoucherPaymentPageViewModel is being disposed.");
                    _webServerService.TriggerReceived -= OnTriggerReceived;
                    // Dispose other resources here
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}