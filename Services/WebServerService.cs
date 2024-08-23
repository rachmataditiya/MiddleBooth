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

        public event EventHandler<string>? TriggerReceived;
        public event EventHandler<string>? PaymentNotificationReceived;

        public async Task StartServerAsync()
        {
            if (_isListening) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8080/");
            _listener.Start();
            _isListening = true;

            Log.Information("Web server started on http://localhost:8080/");

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

        public void StopServer()
        {
            _isListening = false;
            _listener?.Stop();
            Log.Information("Web server stopped.");
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            string response = "OK";

            if (context.Request.HttpMethod == "GET" && context.Request.Url?.PathAndQuery.StartsWith("/trigger") == true)
            {
                string triggerData = context.Request.QueryString["event"] ?? string.Empty;
                Log.Information($"Trigger received: {triggerData}");
                TriggerReceived?.Invoke(this, triggerData);
            }
            else if (context.Request.HttpMethod == "POST" && context.Request.Url?.PathAndQuery == "/payment")
            {
                using var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string paymentData = await reader.ReadToEndAsync();
                Log.Information($"Payment notification received: {paymentData}");
                PaymentNotificationReceived?.Invoke(this, paymentData);
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
    }
}
