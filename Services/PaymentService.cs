using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MiddleBooth.Services.Interfaces;
using Serilog;
using System.Security.Cryptography;

namespace MiddleBooth.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsService _settingsService;
        private bool _isRequestInProgress;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public event Action<string, string> OnPaymentNotificationReceived = delegate { };

        public PaymentService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_settingsService.GetMidtransBaseUrl())
            };

            Log.Information("PaymentService initialized with base URL: {BaseUrl}", _httpClient.BaseAddress);
        }

        public async Task<string> GenerateQRCode(decimal amount)
        {
            if (_isRequestInProgress)
            {
                Log.Warning("QR code generation request is already in progress.");
                return string.Empty;
            }

            _isRequestInProgress = true;

            try
            {
                Log.Information("Generating QR code for amount: {Amount}", amount);

                var serverKey = _settingsService.GetMidtransServerKey();
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{serverKey}:"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

                var orderId = $"order-{Guid.NewGuid()}";
                Log.Information("Generated order ID: {OrderId}", orderId);

                var request = new
                {
                    payment_type = "gopay",
                    transaction_details = new
                    {
                        order_id = orderId,
                        gross_amount = (int)amount
                    }
                };

                var notificationUrl = _settingsService.GetPaymentGatewayUrl();
                _httpClient.DefaultRequestHeaders.Add("X-Override-Notification", notificationUrl);
                Log.Information("Notification URL set to: {NotificationUrl}", notificationUrl);

                var response = await _httpClient.PostAsync(
                    "charge",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                );

                var content = await response.Content.ReadAsStringAsync();
                Log.Information("Received response: {Content}", content);

                var result = JsonSerializer.Deserialize<MidtransResponse>(content, _jsonSerializerOptions);

                if (result == null)
                {
                    Log.Error("Failed to deserialize Midtrans response");
                    return string.Empty;
                }

                Log.Information("Deserialized response: {@Result}", result);

                if (result.StatusCode == "201")
                {
                    var qrCodeUrl = result.Actions?.FirstOrDefault(a => a.Name == "generate-qr-code")?.Url ?? string.Empty;
                    if (!string.IsNullOrEmpty(qrCodeUrl))
                    {
                        Log.Information("QR code generated successfully: {QRCodeUrl}", qrCodeUrl);
                        return qrCodeUrl;
                    }
                }

                Log.Error("Unexpected status code from Midtrans: {StatusCode}. Message: {StatusMessage}",
                          result.StatusCode, result.StatusMessage);

                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while generating QR code");
                return string.Empty;
            }
            finally
            {
                _isRequestInProgress = false;
            }
        }

        public void ProcessPaymentNotification(string notificationJson)
        {
            Log.Information("Processing payment notification: {NotificationJson}", notificationJson);

            var notification = JsonSerializer.Deserialize<MidtransNotification>(notificationJson, _jsonSerializerOptions);

            if (notification != null)
            {
                var computedSignatureKey = ComputeSignature(notification);
                if (computedSignatureKey != notification.SignatureKey)
                {
                    Log.Warning("Signature key verification failed.");
                    return;
                }

                switch (notification.TransactionStatus)
                {
                    case "settlement":
                        Log.Information("Payment settled for Order ID: {OrderId}", notification.OrderId);
                        OnPaymentNotificationReceived?.Invoke(notification.TransactionId, "settlement");
                        break;
                    case "pending":
                        Log.Information("Payment pending for Order ID: {OrderId}", notification.OrderId);
                        OnPaymentNotificationReceived?.Invoke(notification.TransactionId, "pending");
                        break;
                    case "expire":
                        Log.Information("Payment expired for Order ID: {OrderId}", notification.OrderId);
                        OnPaymentNotificationReceived?.Invoke(notification.TransactionId, "expire");
                        break;
                    default:
                        Log.Warning("Unknown transaction status: {TransactionStatus}", notification.TransactionStatus);
                        break;
                }
            }
            else
            {
                Log.Warning("Failed to deserialize payment notification.");
            }
        }

        private string ComputeSignature(MidtransNotification notification)
        {
            var merchantServerKey = _settingsService.GetMidtransServerKey();
            var signatureFields = $"{notification.OrderId}{notification.StatusCode}{notification.GrossAmount}{merchantServerKey}";

            var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(signatureFields));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        public decimal GetServicePrice()
        {
            var price = _settingsService.GetServicePrice();
            Log.Information("Retrieved service price: {ServicePrice}", price);
            return price;
        }
    }

    public class MidtransResponse
    {
        [JsonPropertyName("status_code")]
        public string StatusCode { get; set; } = string.Empty;

        [JsonPropertyName("status_message")]
        public string StatusMessage { get; set; } = string.Empty;

        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("gross_amount")]
        public string GrossAmount { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("payment_type")]
        public string PaymentType { get; set; } = string.Empty;

        [JsonPropertyName("transaction_time")]
        public string TransactionTime { get; set; } = string.Empty;

        [JsonPropertyName("transaction_status")]
        public string TransactionStatus { get; set; } = string.Empty;

        [JsonPropertyName("fraud_status")]
        public string FraudStatus { get; set; } = string.Empty;

        [JsonPropertyName("actions")]
        public List<MidtransAction> Actions { get; set; } = [];

        [JsonPropertyName("expiry_time")]
        public string ExpiryTime { get; set; } = string.Empty;
    }

    public class MidtransAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class MidtransNotification
    {
        [JsonPropertyName("transaction_status")]
        public string TransactionStatus { get; set; } = string.Empty;

        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("payment_type")]
        public string PaymentType { get; set; } = string.Empty;

        [JsonPropertyName("gross_amount")]
        public string GrossAmount { get; set; } = string.Empty;

        [JsonPropertyName("transaction_time")]
        public string TransactionTime { get; set; } = string.Empty;

        [JsonPropertyName("status_code")]
        public string StatusCode { get; set; } = string.Empty;

        [JsonPropertyName("status_message")]
        public string StatusMessage { get; set; } = string.Empty;

        [JsonPropertyName("settlement_time")]
        public string SettlementTime { get; set; } = string.Empty;

        [JsonPropertyName("signature_key")]
        public string SignatureKey { get; set; } = string.Empty;

        [JsonPropertyName("fraud_status")]
        public string FraudStatus { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }
}