using MiddleBooth.Services.Interfaces;
using Serilog;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiddleBooth.Services
{
    public class WebServerService : IWebServerService
    {
        private HttpListener? _listener;
        private bool _isListening = false;

        public event EventHandler<DSLRBoothEvent>? TriggerReceived;
        public event EventHandler<string>? PaymentNotificationReceived;
        private readonly IPaymentService _paymentService;

        public WebServerService(IPaymentService paymentService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        }

        public async Task StartServerAsync()
        {
            if (_isListening) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://+:8080/");
            _listener.Start();
            _isListening = true;

            Log.Information("Web server started on http://+:8080/");

            while (_isListening)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException ex)
                {
                    Log.Error(ex, "HttpListenerException occurred while listening for requests.");
                    break;
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            string response = "OK";

            string originalHost = context.Request.Url?.Host ?? "unknown";
            string forwardedHost = context.Request.Headers["X-Forwarded-Host"] ?? "not set";
            Log.Information($"Request received. Original Host: {originalHost}, Forwarded Host: {forwardedHost}");

            if (context.Request.HttpMethod == "GET" && context.Request.Url?.PathAndQuery.StartsWith("/trigger") == true)
            {
                string eventType = context.Request.QueryString["event_type"] ?? string.Empty;
                string param1 = context.Request.QueryString["param1"] ?? string.Empty;
                string param2 = context.Request.QueryString["param2"] ?? string.Empty;
                string param3 = context.Request.QueryString["param3"] ?? string.Empty;
                string param4 = context.Request.QueryString["param4"] ?? string.Empty;

                var dslrBoothEvent = new DSLRBoothEvent
                {
                    EventType = eventType,
                    Param1 = param1,
                    Param2 = param2,
                    Param3 = param3,
                    Param4 = param4
                };

                //Log.Information($"DSLRBooth event received: {eventType}, Param1: {param1}, Param2: {param2}, Param3: {param3}, Param4: {param4}");
                TriggerReceived?.Invoke(this, dslrBoothEvent);
            }
            else if (context.Request.HttpMethod == "POST" && context.Request.Url?.PathAndQuery == "/payment")
            {
                using var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string paymentData = await reader.ReadToEndAsync();
                Log.Information($"Payment notification received: {paymentData}");
                PaymentNotificationReceived?.Invoke(this, paymentData);
                _paymentService.ProcessPaymentNotification(paymentData);
            }
            else
            {
                response = "Not Found";
                context.Response.StatusCode = 404;
                Log.Warning($"Unrecognized request: {context.Request.HttpMethod} {context.Request.Url}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
        }

        public void StopServer()
        {
            _isListening = false;
            _listener?.Stop();
            Log.Information("Web server stopped.");
        }
    }
}