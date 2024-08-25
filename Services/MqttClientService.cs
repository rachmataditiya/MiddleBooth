using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MiddleBooth.Services.Interfaces;
using System.Security.Authentication;
using System.Net.Http;
using Serilog;

namespace MiddleBooth.Services
{
    public class MqttClientService : IMqttClientService
    {
        private readonly ISettingsService _settingsService;
        private IMqttClient _mqttClient;
        private readonly HttpClient _httpClient;

        public MqttClientService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _mqttClient = new MqttFactory().CreateMqttClient();
            _httpClient = new HttpClient();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_settingsService.GetMqttHost(), _settingsService.GetMqttPort())
                    .WithCredentials(_settingsService.GetMqttUsername(), _settingsService.GetMqttPassword())
                    .WithTlsOptions(tlsOptions =>
                    {
                        tlsOptions.UseTls();
                    })
                    .WithCleanSession()
                    .Build();

                _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;

                await _mqttClient.ConnectAsync(options, cancellationToken);

                if (_mqttClient.IsConnected)
                {
                    Log.Information("Connected to MQTT broker successfully");
                    await SubscribeToTopic("payment/notification");
                }
                else
                {
                    Log.Error("Failed to connect to MQTT broker");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while connecting to MQTT broker");
            }
        }

        private async Task SubscribeToTopic(string topic)
        {
            try
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
                Log.Information($"Subscribed to topic: {topic}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error occurred while subscribing to topic: {topic}");
            }
        }

        private async Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            Log.Information($"Received message: {payload}");

            try
            {
                // Forward the received message to the local /payment route
                var response = await _httpClient.PostAsync("http://localhost:8080/payment", new StringContent(payload, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Successfully forwarded MQTT message to local /payment route");
                }
                else
                {
                    Log.Warning($"Failed to forward MQTT message. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while forwarding MQTT message to local /payment route");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), cancellationToken);
                Log.Information("Disconnected from MQTT broker");
            }
        }
    }
}