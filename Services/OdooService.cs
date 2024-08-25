using MiddleBooth.Services.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace MiddleBooth.Services
{
    public class OdooService : IOdooService
    {
        private readonly OdooConnection _connection;
        private readonly HttpClient _httpClient;
        private int? _uid;

        public OdooService(ISettingsService settingsService)
        {
            _connection = new OdooConnection(
                settingsService.GetOdooServer(),
                settingsService.GetOdooDatabase(),
                settingsService.GetOdooUsername(),
                settingsService.GetOdooPassword()
            );
            _httpClient = new HttpClient();
            Log.Information("OdooService initialized");
        }

        public async Task<bool> CheckVoucher(string voucherCode)
        {
            const string VOUCHER_MODEL = "x_voucher_management";
            Log.Information($"Checking voucher: {voucherCode}");
            var searchArgs = new JArray
            {
                new JArray
                {
                    new JArray("x_studio_voucher_code", "=", voucherCode),
                    new JArray("x_studio_used", "=", false)
                }
            };

            try
            {
                // Mencari voucher yang valid
                var searchResult = await ExecuteKw<JArray>(VOUCHER_MODEL, "search", searchArgs);
                if (searchResult != null && searchResult.Count > 0)
                {
                    var voucherId = searchResult[0].Value<int>();
                    Log.Information($"Valid voucher found with ID: {voucherId}");

                    // Memperbarui voucher menjadi used
                    var updateArgs = new JArray
                    {
                        voucherId,
                        new JObject
                        {
                            ["x_studio_used"] = true
                        }
                    };
                    var updateResult = await ExecuteKw<bool>(VOUCHER_MODEL, "write", updateArgs);

                    if (updateResult)
                    {
                        Log.Information($"Voucher {voucherCode} marked as used");
                        return true;
                    }
                    else
                    {
                        Log.Warning($"Failed to mark voucher {voucherCode} as used");
                        return false;
                    }
                }
                else
                {
                    Log.Information($"Voucher {voucherCode} is invalid or already used");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error checking voucher {voucherCode}");
                throw;
            }
        }

        public async Task<bool> CreateBoothOrder(string name, DateTime saleDate, decimal price, string saleType)
        {
            var gmtSaleDate = saleDate.ToUniversalTime();

            Log.Information($"Creating booth order: {name}, Local Date: {saleDate}, GMT Date: {gmtSaleDate}, Price: {price}, Sale Type: {saleType}");
            var args = new JArray
    {
        new JObject
        {
            ["x_name"] = name,
            ["x_studio_tanggal_penjualan"] = gmtSaleDate.ToString("yyyy-MM-dd HH:mm:ss"),
            ["x_studio_harga"] = price,
            ["x_studio_tipe_penjualan"] = saleType
        }
    };

            try
            {
                Log.Debug($"Create booth order arguments: {args}");
                var result = await ExecuteKw<int>("x_booth_order", "create", args);
                Log.Debug($"Create booth order result: {result}");
                var success = result > 0;
                Log.Information($"Booth order {name} creation {(success ? "successful" : "failed")}");
                return success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error creating booth order {name}");
                throw;
            }
        }


        private async Task<T?> ExecuteKw<T>(string model, string method, JArray args)
        {
            Log.Debug($"Executing Odoo method: {model}.{method}");
            var uid = await GetUid();
            var jsonRpc = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = "call",
                ["params"] = new JObject
                {
                    ["service"] = "object",
                    ["method"] = "execute_kw",
                    ["args"] = new JArray
            {
                _connection.Database,
                uid,
                _connection.Password,
                model,
                method,
                args
            }
                }
            };

            try
            {
                var response = await SendRequest(_connection.ServerUrl + "/jsonrpc", jsonRpc);
                Log.Debug($"Full response from Odoo: {response}");

                if (response.ContainsKey("error"))
                {
                    var error = response["error"];
                    Log.Error($"Odoo error: {error?["message"]}, Data: {error?["data"]?["message"]}");
                    throw new Exception($"Odoo error: {error?["message"]}");
                }

                var result = response["result"];
                if (result != null)
                {
                    Log.Debug($"Odoo method {model}.{method} executed successfully. Result: {result}");
                    return result.Value<T>();
                }
                Log.Warning($"Odoo method {model}.{method} returned null result");
                return default;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error executing Odoo method {model}.{method}");
                throw;
            }
        }
        private async Task<int> GetUid()
        {
            if (_uid.HasValue)
            {
                return _uid.Value;
            }

            Log.Debug("Getting UID from Odoo server");
            var jsonRpc = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = "call",
                ["params"] = new JObject
                {
                    ["service"] = "common",
                    ["method"] = "login",
                    ["args"] = new JArray
                    {
                        _connection.Database,
                        _connection.Username,
                        _connection.Password
                    }
                }
            };

            try
            {
                var response = await SendRequest(_connection.ServerUrl + "/jsonrpc", jsonRpc);
                var result = response["result"];
                if (result != null && result.Type != JTokenType.Null)
                {
                    _uid = result.Value<int>();
                    Log.Information($"UID obtained: {_uid}");
                    return _uid.Value;
                }
                Log.Error("Failed to get UID from Odoo server");
                throw new Exception("Failed to get UID from Odoo server");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting UID from Odoo server");
                throw;
            }
        }

        private async Task<JObject> SendRequest(string url, JObject jsonRpc)
        {
            Log.Debug($"Sending request to Odoo server: {url}");
            var content = new StringContent(jsonRpc.ToString(), Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();
                Log.Debug("Received response from Odoo server");
                return JObject.Parse(responseString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error sending request to Odoo server: {url}");
                throw;
            }
        }
    }
}