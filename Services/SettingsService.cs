// File: Services/SettingsService.cs

using MiddleBooth.Services.Interfaces;
using MiddleBooth.Data;
using MiddleBooth.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using Serilog;
using System.Collections.Generic;

namespace MiddleBooth.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly MiddleBoothContext _context;

        public SettingsService()
        {
            _context = new MiddleBoothContext();
            _context.Database.EnsureCreated();
            InitializeDefaultSettings();
        }

        private string GetSetting(string key, string defaultValue = "")
        {
            var setting = _context.Settings.FirstOrDefault(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        private void SetSetting(string key, string value)
        {
            var setting = _context.Settings.FirstOrDefault(s => s.Key == key);
            if (setting == null)
            {
                _context.Settings.Add(new Setting { Key = key, Value = value });
            }
            else
            {
                setting.Value = value;
            }
            _context.SaveChanges();
        }

        public void InitializeDefaultSettings()
        {
            var defaultSettings = new Dictionary<string, string>
            {
                {"IsProduction", "false"},
                {"DSLRBoothPath", ""},
                {"DSLRBoothPassword", ""},
                {"PaymentGatewayUrl", ""},
                {"ServicePrice", "0"},
                {"ApplicationPin", "1234"},
                {"MidtransServerKey", ""},
                {"OdooServer", ""},
                {"OdooUsername", ""},
                {"OdooPassword", ""},
                {"OdooDatabase", ""},
                {"MqttHost", "y1c4d1ec.ala.asia-southeast1.emqxsl.com"},
                {"MqttPort", "8883"},
                {"MqttUsername", "sedari"},
                {"MqttPassword", "@mpera50A"},
                {"Activated", "false"},
                {"ProductImage", ""},
                {"MainBackgroundImage", ""}
            };

            foreach (var setting in defaultSettings)
            {
                if (!_context.Settings.Any(s => s.Key == setting.Key))
                {
                    _context.Settings.Add(new Setting { Key = setting.Key, Value = setting.Value });
                }
            }
            _context.SaveChanges();
        }

        public string GetDSLRBoothPath()
        {
            return GetSetting("DSLRBoothPath");
        }

        public void SetDSLRBoothPath(string path)
        {
            SetSetting("DSLRBoothPath", path);
            Log.Information($"DSLRBooth path updated: {path}");
        }

        public string GetDSLRBoothPassword()
        {
            return GetSetting("DSLRBoothPassword");
        }

        public void SetDSLRBoothPassword(string password)
        {
            SetSetting("DSLRBoothPassword", password);
            Log.Information("DSLRBooth password updated");
        }

        public string GetPaymentGatewayUrl()
        {
            return GetSetting("PaymentGatewayUrl");
        }

        public void SetPaymentGatewayUrl(string url)
        {
            SetSetting("PaymentGatewayUrl", url);
            Log.Information($"Payment Gateway URL updated: {url}");
        }

        public decimal GetServicePrice()
        {
            if (decimal.TryParse(GetSetting("ServicePrice"), out decimal price))
            {
                return price;
            }
            return 0;
        }

        public void SetServicePrice(decimal price)
        {
            SetSetting("ServicePrice", price.ToString());
            Log.Information($"Service Price updated: {price}");
        }

        public string GetApplicationPin()
        {
            return GetSetting("ApplicationPin");
        }

        public void SetApplicationPin(string pin)
        {
            SetSetting("ApplicationPin", pin);
            Log.Information("Application PIN updated");
        }

        public string GetMidtransServerKey()
        {
            return GetSetting("MidtransServerKey");
        }

        public void SetMidtransServerKey(string key)
        {
            SetSetting("MidtransServerKey", key);
            Log.Information("Midtrans Server Key updated");
        }

        public bool IsProduction()
        {
            string value = GetSetting("IsProduction", "false");
            return bool.Parse(value);
        }

        public void SetProduction(bool isProduction)
        {
            SetSetting("IsProduction", isProduction.ToString());
            Log.Information($"Production mode set to: {isProduction}");
        }

        public string GetMidtransBaseUrl()
        {
            return IsProduction()
                ? "https://api.midtrans.com/v2/"
                : "https://api.sandbox.midtrans.com/v2/";
        }

        public string GetOdooServer()
        {
            return GetSetting("OdooServer");
        }

        public void SetOdooServer(string server)
        {
            SetSetting("OdooServer", server);
            Log.Information($"Odoo Server updated: {server}");
        }

        public string GetOdooUsername()
        {
            return GetSetting("OdooUsername");
        }

        public void SetOdooUsername(string username)
        {
            SetSetting("OdooUsername", username);
            Log.Information($"Odoo Username updated: {username}");
        }

        public string GetOdooPassword()
        {
            return GetSetting("OdooPassword");
        }

        public void SetOdooPassword(string password)
        {
            SetSetting("OdooPassword", password);
            Log.Information("Odoo Password updated");
        }

        public string GetOdooDatabase()
        {
            return GetSetting("OdooDatabase");
        }

        public void SetOdooDatabase(string database)
        {
            SetSetting("OdooDatabase", database);
            Log.Information($"Odoo Database updated: {database}");
        }

        public string GetMqttHost()
        {
            return GetSetting("MqttHost", "y1c4d1ec.ala.asia-southeast1.emqxsl.com");
        }

        public void SetMqttHost(string host)
        {
            SetSetting("MqttHost", host);
            Log.Information($"MQTT Host updated: {host}");
        }

        public int GetMqttPort()
        {
            if (int.TryParse(GetSetting("MqttPort", "8883"), out int port))
            {
                return port;
            }
            return 8883;
        }

        public void SetMqttPort(int port)
        {
            SetSetting("MqttPort", port.ToString());
            Log.Information($"MQTT Port updated: {port}");
        }

        public string GetMqttUsername()
        {
            return GetSetting("MqttUsername", "sedari");
        }

        public void SetMqttUsername(string username)
        {
            SetSetting("MqttUsername", username);
            Log.Information($"MQTT Username updated: {username}");
        }

        public string GetMqttPassword()
        {
            return GetSetting("MqttPassword", "@mpera50A");
        }

        public void SetMqttPassword(string password)
        {
            SetSetting("MqttPassword", password);
            Log.Information("MQTT Password updated");
        }

        public string GetMachineId()
        {
            return GetSetting("MachineId");
        }

        public void SetMachineId(string machineId)
        {
            SetSetting("MachineId", machineId);
            Log.Information($"Machine ID updated: {machineId}");
        }

        public bool MachineActivated()
        {
            string value = GetSetting("Activated", "false");
            return bool.Parse(value);
        }

        public void SetMachineActivated(bool activated)
        {
            SetSetting("Activated", activated.ToString());
            Log.Information($"Machine activation status updated: {activated}");
        }

        public string GetProductImage()
        {
            return GetSetting("ProductImage");
        }

        public void SetProductImage(string path)
        {
            SetSetting("ProductImage", path);
            Log.Information($"Product Image path updated: {path}");
        }

        public string GetMainBackgroundImage()
        {
            return GetSetting("MainBackgroundImage");
        }

        public void SetMainBackgroundImage(string path)
        {
            SetSetting("MainBackgroundImage", path);
            Log.Information($"Main Background Image path updated: {path}");
        }
    }
}