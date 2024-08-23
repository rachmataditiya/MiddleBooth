namespace MiddleBooth.ViewModels
{
    using MiddleBooth.Services.Interfaces;
    using MiddleBooth.Utilities;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class VoucherPaymentPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;

        public ICommand ValidateVoucherCommand { get; }
        public ICommand BackCommand { get; }

        private string _voucherCode = "1234";
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

        public VoucherPaymentPageViewModel(INavigationService navigationService, IPaymentService paymentService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;

            ValidateVoucherCommand = new RelayCommand(async _ => await ValidateVoucherAsync());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentOptionsPage"));
        }

        private async Task ValidateVoucherAsync()
        {
            // Simulasi validasi dengan kode voucher sementara "1234"
            if (VoucherCode == "1234")
            {
                ValidationResult = "Voucher Valid!";
                ValidationResultColor = "Green";
            }
            else
            {
                ValidationResult = "Kode Voucher Tidak Valid!";
                ValidationResultColor = "Red";
            }

            await Task.Delay(500); // Simulasi penundaan (misalnya, menunggu respons server)
        }
    }
}
