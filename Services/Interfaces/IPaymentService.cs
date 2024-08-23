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

        // Tambahkan event untuk notifikasi pembayaran
        event Action<string> OnPaymentNotificationReceived;

        // Tambahkan metode untuk mendapatkan harga layanan
        decimal GetServicePrice();
    }
}
