using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IOdooService
    {
        Task<(bool success, int? machineId, int? partnerId, bool isNew, string message)> ActivateMachine(
            string clientMachineId,
            string name,
            string partnerName,
            string? partnerStreet = null,
            string? partnerCity = null,
            int? partnerStateId = null,
            int? partnerCountryId = null,
            string? partnerZip = null,
            string? partnerPhone = null,
            string? partnerEmail = null,
            float latitude = 0,
            float longitude = 0);

        Task<VoucherDetails> CheckVoucher(string voucherCode, string clientMachineId);
        Task<MachineInfo> GetMachineInfo(string clientMachineId);
        Task<(bool success, int? orderId, string message)> CreateBoothOrder(string clientMachineId, string? voucherCode = null);
    }

    public class VoucherDetails
    {
        public string VoucherCode { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public float Value { get; set; }
        public float TotalDiscount { get; set; }
        public bool IsValid { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MachineInfo
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Name { get; set; }
        public string? ClientMachineId { get; set; }
        public string? ApplicationPin { get; set; }
        public string? MidtransServerKey { get; set; }
        public bool IsProduction { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public float DefaultBoothPrice { get; set; }
        public string? PaymentGatewayUrl { get; set; }
        public string? MqttHost { get; set; }
        public int MqttPort { get; set; }
        public string? MqttUsername { get; set; }
        public string? MqttPassword { get; set; }
        public string? ProductImage { get; set; }
        public string? MainBackgroundImage { get; set; }
    }
}