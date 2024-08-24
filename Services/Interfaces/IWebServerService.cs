using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IWebServerService
    {
        event EventHandler<DSLRBoothEvent> TriggerReceived;
        event EventHandler<string> PaymentNotificationReceived;
        Task StartServerAsync();
        void StopServer();
    }

    public class DSLRBoothEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string Param1 { get; set; } = string.Empty;
        public string Param2 { get; set; } = string.Empty;
        public string Param3 { get; set; } = string.Empty;
        public string Param4 { get; set; } = string.Empty;
    }
}