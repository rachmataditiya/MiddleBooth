using System;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IWebServerService
    {
        Task StartServerAsync();
        void StopServer();
        event EventHandler<string> TriggerReceived;
        event EventHandler<string> PaymentNotificationReceived;
    }
}