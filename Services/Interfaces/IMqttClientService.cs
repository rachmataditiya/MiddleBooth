using System.Threading;
using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IMqttClientService
    {
        /// <summary>
        /// Starts the MQTT client service and connects to the broker.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the MQTT client service and disconnects from the broker.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}