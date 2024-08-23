namespace MiddleBooth.Services.Interfaces
{
    public interface ISettingsService
    {
        string GetDSLRBoothPath();
        void SetDSLRBoothPath(string path);
        string GetPaymentGatewayUrl();
        void SetPaymentGatewayUrl(string url);
        decimal GetServicePrice();
        void SetServicePrice(decimal price);
        string GetApplicationPin();
        void SetApplicationPin(string pin);
        string GetMidtransServerKey();
        void SetMidtransServerKey(string key);
        bool IsProduction();
        void SetProduction(bool isProduction);
        string GetMidtransBaseUrl();
    }
}