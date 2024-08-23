using System.Linq;
using MiddleBooth.Models;
using MiddleBooth.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiddleBooth.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;

        // Flag untuk menghindari duplikasi permintaan
        private bool _isRequestInProgress;

        public event Action<string> OnPaymentNotificationReceived = delegate { };

        public PaymentService(ISettingsService settingsService, INavigationService navigationService)
        {
            _settingsService = settingsService;
            _navigationService = navigationService;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_settingsService.GetMidtransBaseUrl())
            };

            Log.Information("PaymentService initialized with base URL: {BaseUrl}", _httpClient.BaseAddress);
        }

        public async Task<string> GenerateQRCode(decimal amount)
        {
            // Cek apakah permintaan sedang berlangsung
            if (_isRequestInProgress)
            {
                Log.Warning("QR code generation request is already in progress.");
                return string.Empty;
            }

            _isRequestInProgress = true;

            Log.Information("Generating QR code for amount: {Amount}", amount);

            var serverKey = _settingsService.GetMidtransServerKey();
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{serverKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var orderId = $"order-{Guid.NewGuid()}";
            Log.Information("Generated order ID: {OrderId}", orderId);

            var request = new
            {
                payment_type = "qris",
                transaction_details = new
                {
                    order_id = orderId,
                    gross_amount = (int)amount
                }
            };

            var notificationUrl = _settingsService.GetPaymentGatewayUrl();
            _httpClient.DefaultRequestHeaders.Add("X-Override-Notification", notificationUrl);
            Log.Information("Notification URL set to: {NotificationUrl}", notificationUrl);

            try
            {
                var response = await _httpClient.PostAsync(
                    "charge",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                );

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<MidtransResponse>(content);

                    if (result == null || string.IsNullOrEmpty(result.StatusCode) || result.StatusCode != "201")
                    {
                        Log.Error("Invalid response from Midtrans: {Content}", content);
                        _navigationService.NavigateTo("PaymentOptionsPage");
                        _isRequestInProgress = false;
                        return string.Empty;
                    }

                    var qrCodeUrl = result.Actions.FirstOrDefault(a => a.Name == "generate-qr-code")?.Url ?? string.Empty;
                    Log.Information("QR code generated successfully: {QRCodeUrl}", qrCodeUrl);
                    _isRequestInProgress = false;
                    return qrCodeUrl;
                }
                else
                {
                    Log.Error("Failed to generate QR code. Status code: {StatusCode}, Reason: {ReasonPhrase}, Response: {Content}",
                              response.StatusCode, response.ReasonPhrase, content);

                    var errorResponse = JsonSerializer.Deserialize<MidtransErrorResponse>(content);
                    Log.Error("Midtrans error: {StatusCode} - {StatusMessage} (ID: {Id})", errorResponse?.StatusCode, errorResponse?.StatusMessage, errorResponse?.Id);

                    _navigationService.NavigateTo("PaymentOptionsPage");
                    _isRequestInProgress = false;
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while generating QR code");
                _navigationService.NavigateTo("PaymentOptionsPage");
                _isRequestInProgress = false;
                return string.Empty;
            }
        }

        public async Task<bool> ValidateVoucher(string voucherCode)
        {
            Log.Information("Validating voucher code: {VoucherCode}", voucherCode);
            await Task.Delay(1000); // Simulasi delay jaringan
            var isValid = voucherCode.Length == 8;
            Log.Information("Voucher validation result for {VoucherCode}: {IsValid}", voucherCode, isValid);
            return isValid;
        }

        public async Task<PaymentModel> ProcessPayment(string paymentMethod, string paymentData)
        {
            Log.Information("Processing payment with method: {PaymentMethod}", paymentMethod);
            await Task.Delay(2000); // Simulasi delay jaringan

            var paymentModel = new PaymentModel
            {
                Amount = _settingsService.GetServicePrice(),
                PaymentMethod = paymentMethod,
                Status = "Success"
            };

            Log.Information("Payment processed successfully. Method: {PaymentMethod}, Amount: {Amount}", paymentMethod, paymentModel.Amount);
            return paymentModel;
        }

        public void ProcessPaymentNotification(string notificationJson)
        {
            Log.Information("Processing payment notification: {NotificationJson}", notificationJson);
            var notification = JsonSerializer.Deserialize<MidtransNotification>(notificationJson);
            if (notification != null)
            {
                Log.Information("Payment notification received. Status: {Status}, Order ID: {OrderId}", notification.TransactionStatus, notification.OrderId);
                OnPaymentNotificationReceived?.Invoke(notification.TransactionStatus);
            }
            else
            {
                Log.Warning("Failed to deserialize payment notification.");
            }
        }

        public decimal GetServicePrice()
        {
            var price = _settingsService.GetServicePrice();
            Log.Information("Retrieved service price: {ServicePrice}", price);
            return price;
        }
    }

    public class MidtransErrorResponse
    {
        public string StatusCode { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }

    public class MidtransResponse
    {
        public string StatusCode { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string GrossAmount { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public List<MidtransAction> Actions { get; set; } = [];
    }

    public class MidtransAction
    {
        public string Name { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class MidtransNotification
    {
        public string TransactionStatus { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string GrossAmount { get; set; } = string.Empty;
        public string TransactionTime { get; set; } = string.Empty;
    }
}
