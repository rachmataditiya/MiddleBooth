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

        private int _discountAmount;
        public int DiscountAmount
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

        public VoucherPaymentPageViewModel(INavigationService navigationService, IPaymentService paymentService, IOdooService odooService, IDSLRBoothService dslrBoothService, IWebServerService webServerService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;
            _odooService = odooService;
            _dslrBoothService = dslrBoothService;
            _webServerService = webServerService;

            ValidateVoucherCommand = new RelayCommand(async _ => await ValidateVoucherAsync());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentOptionsPage"));

            _webServerService.TriggerReceived += OnTriggerReceived;

            Log.Information("VoucherPaymentPageViewModel initialized");
        }

        private async void OnTriggerReceived(object? sender, DSLRBoothEvent e)
        {
            if (e.EventType == "session_end")
            {
                Log.Information("DSLRBooth session ended received from voucher payment. Navigating to MainView.");
                await _dslrBoothService.SetDSLRBoothVisibility(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _navigationService.NavigateTo("MainView");
                });
            }
        }

        private async Task ValidateVoucherAsync()
        {
            Log.Information($"Validating voucher: {VoucherCode}");
            try
            {
                var voucherDetails = await _odooService.GetVoucherDetails(VoucherCode);
                if (voucherDetails.IsValid)
                {
                    Log.Information($"Voucher {VoucherCode} is valid");

                    decimal fullPrice = _paymentService.GetServicePrice();
                    IsFullVoucher = voucherDetails.VoucherType.ToLower() == "full";
                    IsPartialVoucher = !IsFullVoucher;

                    switch (voucherDetails.VoucherType.ToLower())
                    {
                        case "full":
                            DiscountAmount = (int)fullPrice;
                            DiscountedPrice = 0;
                            ValidationResult = "Voucher Valid! Memulai sesi foto...";
                            await CreateBoothOrder();
                            await LaunchDSLRBooth();
                            break;
                        case "percentage":
                            decimal percentage = voucherDetails.TotalDiscount / 100m;
                            DiscountAmount = (int)(fullPrice * percentage);
                            DiscountedPrice = Math.Max(fullPrice - DiscountAmount, 0);
                            ValidationResult = $"Voucher Valid! Diskon: {voucherDetails.TotalDiscount}% (Rp{DiscountAmount:N0}). Melanjutkan ke pembayaran...";
                            ProceedToPayment();
                            break;
                        case "nominal":
                            DiscountAmount = voucherDetails.TotalDiscount;
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
                _navigationService.NavigateTo("QrisPaymentPage");

                // Menyimpan DiscountedPrice ke properti statis sementara
                App.Current.Properties["DiscountedPrice"] = DiscountedPrice;
            }
        }

        private async Task CreateBoothOrder()
        {
            try
            {
                string name = $"BO{DateTime.Now:yyyyMMddHHmmss}";
                decimal price = _paymentService.GetServicePrice();
                string saleType = "Voucher";
                Log.Information($"Creating booth order: {name}, Price: {price}, Sale Type: {saleType}");

                bool success = await _odooService.CreateBoothOrder(name, DateTime.Now, price, saleType);
                if (success)
                {
                    Log.Information($"Booth order {name} created successfully");
                    ValidationResult = "Order berhasil dibuat!";
                    ValidationResultColor = "Green";
                }
                else
                {
                    Log.Warning($"Failed to create booth order {name}");
                    ValidationResult = "Gagal membuat order.";
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
                    Log.Information("DSLRBooth set to visible after successful voucher validation");
                }
                else
                {
                    bool launched = await _dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        await _dslrBoothService.SetDSLRBoothVisibility(true);
                        Log.Information("DSLRBooth launched and set to visible after successful voucher validation");
                    }
                    else
                    {
                        Log.Warning("Failed to launch DSLRBooth after successful voucher validation");
                        ValidationResult = "Voucher valid, tapi gagal menjalankan DSLRBooth. Silakan cek pengaturan.";
                        ValidationResultColor = "Orange";
                        return;
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

        public void Dispose()
        {
            _webServerService.TriggerReceived -= OnTriggerReceived;
        }
    }
}