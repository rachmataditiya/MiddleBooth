using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> GenerateQRCode(decimal amount);
        public void ProcessPaymentNotification(string notificationJson);

        event Action<string, string> OnPaymentNotificationReceived;
        decimal GetServicePrice();

    }
}
