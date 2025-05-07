using FishCareSystem.API.AI;
using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using FishCareSystem.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FishCareSystem.API.Services.Service
{
    public class SensorReadingService
    {
        private readonly FishCareDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMqttClientService _mqttService;
        private readonly ILogger<SensorReadingService> _logger;
        private readonly string _aiServiceUrl;

        public SensorReadingService(
            FishCareDbContext context,
            IHttpClientFactory httpClientFactory,
            IMqttClientService mqttService,
            IConfiguration configuration,
            ILogger<SensorReadingService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _mqttService = mqttService;
            _logger = logger;
            _aiServiceUrl = configuration["AI:ServiceUrl"] ?? throw new ArgumentNullException("AI:ServiceUrl is not configured");
        }

        public async Task ProcessSensorReadingAsync(CreateSensorReadingDto createDto)
        {
            // Validate tank exists
            var tank = await _context.Tanks.FindAsync(createDto.TankId);
            if (tank == null)
            {
                throw new InvalidOperationException("Tank not found");
            }

            // Save sensor reading
            var reading = new SensorReading
            {
                TankId = createDto.TankId,
                Type = createDto.Type,
                Value = createDto.Value,
                Unit = createDto.Unit,
                Timestamp = DateTime.UtcNow
            };
            _context.SensorReadings.Add(reading);
            await _context.SaveChangesAsync();

            // Call AI service
            var aiRequest = new
            {
                temperature = createDto.Type == "Temperature" ? createDto.Value : 0,
                pH = createDto.Type == "pH" ? createDto.Value : 0,
                oxygen = createDto.Type == "Oxygen" ? createDto.Value : 0
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(_aiServiceUrl + "/predict",
                    new StringContent(JsonSerializer.Serialize(aiRequest), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var aiResult = await response.Content.ReadFromJsonAsync<AIPredictionResponse>();
                if (aiResult.IsAbnormal)
                {
                    var alert = new Alert
                    {
                        TankId = createDto.TankId,
                        Message = $"Abnormal condition detected: {createDto.Type} = {createDto.Value}{createDto.Unit}",
                        Severity = "Warning",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Alerts.Add(alert);
                    await _context.SaveChangesAsync();

                    if (aiResult.Action != null)
                    {
                        await UpdateDeviceStatus(createDto.TankId, aiResult.Action.Device, aiResult.Action.Status);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"AI service call failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during AI call: {ex.Message}");
            }
        }

        private async Task UpdateDeviceStatus(int tankId, string deviceType, string status)
        {
            
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.TankId == tankId && d.Type == deviceType);
            if (device != null)
            {
                device.Status = status;
                await _context.SaveChangesAsync();

                try
                {
                    await _mqttService.PublishAsync($"fishcare/tank/{tankId}/device/{device.Id}", status);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to publish MQTT message: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning($"Device {deviceType} not found for TankId {tankId}");
            }
        }
    }


}