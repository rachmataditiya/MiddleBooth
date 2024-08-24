using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IOdooService
    {
        Task<bool> CheckVoucher(string voucherCode);
        Task<bool> CreateBoothOrder(string name, DateTime saleDate, decimal price, string saleType);
    }
}