using FishCareSystem.API.Services.Interface;
using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
namespace FishCareSystem.API.Services.Service
{
    public class MqttClientService : IMqttClientService
    {
        private readonly IMqttClient _mqttClient;

        public MqttClientService(IConfiguration configuration)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(configuration["MQTT:Broker"], int.Parse(configuration["MQTT:Port"]))
                .Build();
            _mqttClient.ConnectAsync(options).Wait();
        }

        public async Task PublishAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(System.Text.Encoding.UTF8.GetBytes(payload))
                .Build();
            await _mqttClient.PublishAsync(message);
        }
    }
}
