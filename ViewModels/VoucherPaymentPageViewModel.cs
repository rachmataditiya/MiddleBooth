using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Serilog;

namespace MiddleBooth.ViewModels
{
    public class VoucherPaymentPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;
        private readonly IOdooService _odooService;

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

        public VoucherPaymentPageViewModel(INavigationService navigationService, IPaymentService paymentService, IOdooService odooService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;
            _odooService = odooService;

            ValidateVoucherCommand = new RelayCommand(async _ => await ValidateVoucherAsync());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentOptionsPage"));

            Log.Information("VoucherPaymentPageViewModel initialized");
        }

        private async Task ValidateVoucherAsync()
        {
            Log.Information($"Validating voucher: {VoucherCode}");
            try
            {
                bool isValid = await _odooService.CheckVoucher(VoucherCode);
                if (isValid)
                {
                    Log.Information($"Voucher {VoucherCode} is valid");
                    ValidationResult = "Voucher Valid!";
                    ValidationResultColor = "Green";
                    await CreateBoothOrder();
                }
                else
                {
                    Log.Warning($"Voucher {VoucherCode} is not valid");
                    ValidationResult = "Kode Voucher Tidak Valid!";
                    ValidationResultColor = "Red";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error validating voucher {VoucherCode}");
                ValidationResult = $"Error: {ex.Message}";
                ValidationResultColor = "Red";
            }
        }

        private async Task CreateBoothOrder()
        {
            try
            {
                string name = $"BO{DateTime.Now:yyyyMMddHHmmss}";
                decimal price = _paymentService.GetServicePrice();
                string saleType = "Voucher"; // Tipe penjualan untuk voucher
                Log.Information($"Creating booth order: {name}, Price: {price}, Sale Type: {saleType}");

                bool success = await _odooService.CreateBoothOrder(name, DateTime.Now, price, saleType);
                if (success)
                {
                    Log.Information($"Booth order {name} created successfully");
                    ValidationResult = "Order berhasil dibuat!";
                    ValidationResultColor = "Green";
                    await Task.Delay(3000); // Memberikan waktu untuk user melihat pesan sukses
                    _navigationService.NavigateTo("MainView");
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

    }
}