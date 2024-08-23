namespace MiddleBooth.Models
{
    public class PaymentModel
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}