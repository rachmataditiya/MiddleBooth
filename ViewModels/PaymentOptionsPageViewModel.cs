// File: ViewModels/PaymentOptionsPageViewModel.cs

using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System.Windows.Input;
using Serilog;

namespace MiddleBooth.ViewModels
{
    public class PaymentOptionsPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;

        public ICommand QrisPaymentCommand { get; }
        public ICommand VoucherPaymentCommand { get; }
        public ICommand BackCommand { get; }

        public PaymentOptionsPageViewModel(INavigationService navigationService, IPaymentService paymentService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;

            QrisPaymentCommand = new RelayCommand(_ => StartQrisPayment());
            VoucherPaymentCommand = new RelayCommand(_ => StartVoucherPayment());
            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));

            Log.Information("PaymentOptionsPageViewModel initialized");
        }

        private void StartQrisPayment()
        {
            Log.Information("Starting QRIS payment process");
            _navigationService.NavigateTo("QrisPaymentPage");
        }

        private void StartVoucherPayment()
        {
            Log.Information("Starting voucher payment process");
            _navigationService.NavigateTo("VoucherPaymentPage");
        }
    }
}