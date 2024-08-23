using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System.Windows.Input;

namespace MiddleBooth.ViewModels
{
    public class PaymentOptionsPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;

        public ICommand QrisPaymentCommand { get; }
        public ICommand VoucherPaymentCommand { get; }
        public ICommand BackCommand { get; }

        private bool _isQrisPaymentInProgress;

        public PaymentOptionsPageViewModel(INavigationService navigationService, IPaymentService paymentService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;

            QrisPaymentCommand = new RelayCommand(_ => StartQrisPayment());
            VoucherPaymentCommand = new RelayCommand(_ => StartVoucherPayment());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));
        }

        private async void StartQrisPayment()
        {
            if (_isQrisPaymentInProgress) return;
            _isQrisPaymentInProgress = true;
            _navigationService.NavigateTo("QrisPaymentPage");
            _isQrisPaymentInProgress = false;
        }

        private void StartVoucherPayment()
        {
            _navigationService.NavigateTo("VoucherPaymentPage");
        }
    }
}
