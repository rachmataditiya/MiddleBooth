using MiddleBooth.Services.Interfaces;
using System.Configuration;

namespace MiddleBooth.Services
{
    public class SettingsService : ISettingsService
    {
        public string GetDSLRBoothPath()
        {
            return ConfigurationManager.AppSettings["DSLRBoothPath"] ?? string.Empty;
        }

        public void SetDSLRBoothPath(string path)
        {
            UpdateSetting("DSLRBoothPath", path);
        }

        public string GetPaymentGatewayUrl()
        {
            return ConfigurationManager.AppSettings["PaymentGatewayUrl"] ?? string.Empty;
        }

        public void SetPaymentGatewayUrl(string url)
        {
            UpdateSetting("PaymentGatewayUrl", url);
        }

        public decimal GetServicePrice()
        {
            if (decimal.TryParse(ConfigurationManager.AppSettings["ServicePrice"], out decimal price))
            {
                return price;
            }
            return 0;
        }

        public void SetServicePrice(decimal price)
        {
            UpdateSetting("ServicePrice", price.ToString());
        }

        public string GetApplicationPin()
        {
            return ConfigurationManager.AppSettings["ApplicationPin"] ?? string.Empty;
        }

        public void SetApplicationPin(string pin)
        {
            UpdateSetting("ApplicationPin", pin);
        }

        public string GetMidtransServerKey()
        {
            return ConfigurationManager.AppSettings["MidtransServerKey"] ?? string.Empty;
        }

        public void SetMidtransServerKey(string key)
        {
            UpdateSetting("MidtransServerKey", key);
        }

        public bool IsProduction()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsProduction"] ?? "false");
        }

        public void SetProduction(bool isProduction)
        {
            UpdateSetting("IsProduction", isProduction.ToString());
        }

        public string GetMidtransBaseUrl()
        {
            return IsProduction()
                ? "https://api.midtrans.com/v2/"
                : "https://api.sandbox.midtrans.com/v2/";
        }

        public string GetOdooServer()
        {
            return ConfigurationManager.AppSettings["OdooServer"] ?? string.Empty;
        }

        public void SetOdooServer(string server)
        {
            UpdateSetting("OdooServer", server);
        }

        public string GetOdooUsername()
        {
            return ConfigurationManager.AppSettings["OdooUsername"] ?? string.Empty;
        }

        public void SetOdooUsername(string username)
        {
            UpdateSetting("OdooUsername", username);
        }

        public string GetOdooPassword()
        {
            return ConfigurationManager.AppSettings["OdooPassword"] ?? string.Empty;
        }

        public void SetOdooPassword(string password)
        {
            UpdateSetting("OdooPassword", password);
        }
        public string GetOdooDatabase()
        {
            return ConfigurationManager.AppSettings["OdooDatabase"] ?? string.Empty;
        }

        public void SetOdooDatabase(string database)
        {
            UpdateSetting("OdooDatabase", database);
        }

        private static void UpdateSetting(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}