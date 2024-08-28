using System.Configuration;

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
        string GetOdooServer();
        void SetOdooServer(string server);
        string GetOdooUsername();
        void SetOdooUsername(string username);
        string GetOdooPassword();
        void SetOdooPassword(string password);
        string GetOdooDatabase();
        void SetOdooDatabase(string database);
        string GetMqttHost();
        void SetMqttHost(string host);
        int GetMqttPort();
        void SetMqttPort(int port);
        string GetMqttUsername();
        void SetMqttUsername(string username);
        string GetMqttPassword();
        void SetMqttPassword(string password);
        string GetMachineId();
        void SetMachineId(string machineId);
        public bool MachineActivated();
        public void SetMachineActivated(bool activated);
    }
}