using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FishCareSystem.API.Services.Interface;

namespace FishCareSystem.API.Services.Service
{
    public class MqttClientService : IMqttClientService
    {
        private readonly IManagedMqttClient _mqttClient;
        private readonly string _brokerHost;
        private readonly int _brokerPort;
        private readonly ILogger<MqttClientService> _logger;
        private bool _isStarted;

        public MqttClientService(IConfiguration configuration, ILogger<MqttClientService> logger)
        {
            _brokerHost = configuration["Mqtt:Host"] ?? "localhost";
            _brokerPort = int.Parse(configuration["Mqtt:Port"] ?? "1883");
            _logger = logger;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();
            _isStarted = false;
        }

        public async Task StartAsync()
        {
            var options = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(_brokerHost, _brokerPort)
                    .Build())
                .Build();

            await _mqttClient.StartAsync(options);
            _isStarted = true;
            _logger.LogInformation("MQTT client started.");
        }

        public async Task StopAsync()
        {
            if (_isStarted)
            {
                await _mqttClient.StopAsync();
                _isStarted = false;
                _logger.LogInformation("MQTT client stopped.");
            }
        }

        public async Task SubscribeAsync(string topic, Func<string, Task> messageHandler)
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("MQTT client must be started before subscribing. Call StartAsync first.");
            }

            await _mqttClient.SubscribeAsync(new[] { new MqttTopicFilterBuilder().WithTopic(topic).Build() });

            _mqttClient.ConnectedAsync += async e =>
            {
                await _mqttClient.SubscribeAsync(new[] { new MqttTopicFilterBuilder().WithTopic(topic).Build() });
                _logger.LogInformation($"Subscribed to topic: {topic}");
            };

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                _logger.LogInformation($"Received message on topic {e.ApplicationMessage.Topic}: {payload}");
                await messageHandler(payload);
            };
        }

        public async Task PublishAsync(string topic, string message)
        {
            //StartAsync().Wait(); // Ensure the client is started before publishing
            if (!_isStarted)
            {
                throw new InvalidOperationException("MQTT client must be started before publishing. Call StartAsync first.");
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(message))
                .Build();

            await _mqttClient.EnqueueAsync(mqttMessage);
            _logger.LogInformation($"Published message to topic {topic}: {message}");
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
        }
    }
}