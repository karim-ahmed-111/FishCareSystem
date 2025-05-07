using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
namespace FishCareSystem.API.Services.Interface
{
    public interface IMqttClientService
    {
        Task StartAsync();
        Task StopAsync();
        Task SubscribeAsync(string topic, Func<string, Task> messageHandler);
        Task PublishAsync(string topic, string message);
    }
}
