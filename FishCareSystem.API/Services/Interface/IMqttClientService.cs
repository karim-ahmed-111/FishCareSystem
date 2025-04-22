using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
namespace FishCareSystem.API.Services.Interface
{
    public interface IMqttClientService
    {
        Task PublishAsync(string topic, string payload);
    }
}
