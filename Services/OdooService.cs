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

        public async Task<(bool success, int? machineId, int? partnerId, bool isNew, string message)> ActivateMachine(
            string clientMachineId,
            string name,
            string partnerName,
            string partnerStreet = null,
            string partnerCity = null,
            int? partnerStateId = null,
            int? partnerCountryId = null,
            string partnerZip = null,
            string partnerPhone = null,
            string partnerEmail = null,
            float latitude = 0,
            float longitude = 0)
        {
            Log.Information($"Activating machine: {clientMachineId}");
            var args = new JArray
            {
                clientMachineId,
                new JObject
                {
                    ["name"] = name,
                    ["partner_name"] = partnerName,
                    ["partner_street"] = partnerStreet,
                    ["partner_city"] = partnerCity,
                    ["partner_state_id"] = partnerStateId,
                    ["partner_country_id"] = partnerCountryId,
                    ["partner_zip"] = partnerZip,
                    ["partner_phone"] = partnerPhone,
                    ["partner_email"] = partnerEmail,
                    ["latitude"] = latitude,
                    ["longitude"] = longitude
                }
            };

            try
            {
                var result = await ExecuteKw<JObject>("booth.machine", "activate_machine", args);
                bool success = result["success"].Value<bool>();
                int? machineId = success ? result["machine_id"].Value<int>() : null;
                int? partnerId = success ? result["partner_id"].Value<int>() : null;
                bool isNew = result["is_new"].Value<bool>();
                string message = result["message"].Value<string>();
                Log.Information($"Machine activation result: Success={success}, MachineId={machineId}, PartnerId={partnerId}, IsNew={isNew}, Message={message}");
                return (success, machineId, partnerId, isNew, message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error activating machine {clientMachineId}");
                return (false, null, null, false, ex.Message);
            }
        }

        public async Task<VoucherDetails> CheckVoucher(string voucherCode, string clientMachineId)
        {
            Log.Information($"Checking voucher: {voucherCode} for machine: {clientMachineId}");
            var args = new JArray { voucherCode, clientMachineId };
            try
            {
                var result = await ExecuteKw<JObject>("booth.voucher", "check_voucher", args);
                var voucherDetails = new VoucherDetails
                {
                    VoucherCode = voucherCode,
                    IsValid = result["is_valid"].Value<bool>(),
                    Message = result["message"]?.Value<string>() ?? "",
                    VoucherType = result["voucher_type"]?.Value<string>() ?? "",
                    Value = result["value"]?.Value<float>() ?? 0,
                    TotalDiscount = result["total_discount"]?.Value<float>() ?? 0,
                    ExpiryDate = result["expiry_date"]?.Value<DateTime>()
                };
                Log.Information($"Voucher check result: {voucherDetails.IsValid}, Message={voucherDetails.Message}, Type={voucherDetails.VoucherType}, Value={voucherDetails.Value}, TotalDiscount={voucherDetails.TotalDiscount}, ExpiryDate={voucherDetails.ExpiryDate}");
                return voucherDetails;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error checking voucher {voucherCode}");
                return new VoucherDetails
                {
                    VoucherCode = voucherCode,
                    IsValid = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<(bool success, int? orderId, string message)> CreateBoothOrder(string orderType, string clientMachineId, string voucherCode = null)
        {
            Log.Information($"Creating booth order: Type={orderType}, Machine={clientMachineId}, Voucher={voucherCode ?? "None"}");
            var args = new JArray { orderType, clientMachineId };
            if (!string.IsNullOrEmpty(voucherCode))
            {
                args.Add(voucherCode);
            }

            try
            {
                var result = await ExecuteKw<int>("booth.order", "create_booth_order", args);
                Log.Information($"Booth order created successfully: OrderId={result}");
                return (true, result, "Order created successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating booth order");
                return (false, null, ex.Message);
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