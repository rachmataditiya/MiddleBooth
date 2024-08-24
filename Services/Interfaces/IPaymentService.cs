using MiddleBooth.Models;
using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> GenerateQRCode(decimal amount);
        Task<bool> ValidateVoucher(string voucherCode);
        Task<PaymentModel> ProcessPayment(string paymentMethod, string paymentData);
        public void ProcessPaymentNotification(string notificationJson);

        event Action<string> OnPaymentNotificationReceived;
        decimal GetServicePrice();
    }
}
