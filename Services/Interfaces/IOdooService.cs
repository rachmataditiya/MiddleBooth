using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IOdooService
    {
        Task<bool> CheckVoucher(string voucherCode);
        Task<bool> CreateBoothOrder(string name, DateTime saleDate, decimal price, string saleType);
        Task<VoucherDetails> GetVoucherDetails(string voucherCode);
    }

    public class VoucherDetails
    {
        public string VoucherCode { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public int TotalDiscount { get; set; }
        public bool IsValid { get; set; }
    }
}