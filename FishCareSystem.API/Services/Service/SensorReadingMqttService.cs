using FishCareSystem.API.Services.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FishCareSystem.API.DTOs;

namespace FishCareSystem.API.Services.Service
{
    public class SensorReadingMqttService : BackgroundService
    {
        private readonly IMqttClientService _mqttService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SensorReadingMqttService> _logger;

        public SensorReadingMqttService(
            IMqttClientService mqttService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SensorReadingMqttService> logger)
        {
            _mqttService = mqttService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _mqttService.SubscribeAsync("fishcare/sensor/readings", async message =>
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var sensorReadingService = scope.ServiceProvider.GetRequiredService<SensorReadingService>();
                        try
                        {
                            var createDto = JsonSerializer.Deserialize<CreateSensorReadingDto>(message);
                            if (createDto != null)
                            {
                                await sensorReadingService.ProcessSensorReadingAsync(createDto);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error processing sensor reading: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to subscribe to MQTT topic: {ex.Message}");
            }
        }
    }
}