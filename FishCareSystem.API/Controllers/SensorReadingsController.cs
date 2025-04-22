using FishCareSystem.API.AI;
using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using FishCareSystem.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/sensor-readings")]
    [Authorize]
    public class SensorReadingsController : ControllerBase
    {
        private readonly FishCareDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IMqttClientService _mqttService;
        private readonly ILogger<SensorReadingsController> _logger;

        public SensorReadingsController(
            FishCareDbContext context,
            HttpClient httpClient,
            IMqttClientService mqttService,
            IConfiguration configuration,
            ILogger<SensorReadingsController> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _mqttService = mqttService;
            _logger = logger;

            // Set the HttpClient BaseAddress from configuration
            var aiServiceUrl = configuration["AI:ServiceUrl"] ?? throw new ArgumentNullException("AI:ServiceUrl is not configured");
            _httpClient.BaseAddress = new Uri(aiServiceUrl);
            _logger.LogInformation($"HttpClient BaseAddress set to: {_httpClient.BaseAddress}");
        }

        [HttpGet]
        public async Task<IActionResult> GetSensorReadings()
        {
            var readings = await _context.SensorReadings
                .Select(r => new SensorReadingDto
                {
                    Id = r.Id,
                    TankId = r.TankId,
                    Type = r.Type,
                    Value = r.Value,
                    Unit = r.Unit,
                    Timestamp = r.Timestamp
                })
                .ToListAsync();
            return Ok(readings);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSensorReading(int id)
        {
            var reading = await _context.SensorReadings
                .Where(r => r.Id == id)
                .Select(r => new SensorReadingDto
                {
                    Id = r.Id,
                    TankId = r.TankId,
                    Type = r.Type,
                    Value = r.Value,
                    Unit = r.Unit,
                    Timestamp = r.Timestamp
                })
                .FirstOrDefaultAsync();
            if (reading == null)
            {
                return NotFound();
            }
            return Ok(reading);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSensorReading(int id)
        {
            var reading = await _context.SensorReadings.FindAsync(id);
            if (reading == null)
            {
                return NotFound();
            }
            _context.SensorReadings.Remove(reading);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "IoT,Manager")]
        public async Task<IActionResult> CreateSensorReading([FromBody] CreateSensorReadingDto createDto)
        {
            var tank = await _context.Tanks.FindAsync(createDto.TankId);
            if (tank == null)
            {
                return BadRequest("Tank not found");
            }

            var reading = new SensorReading
            {
                TankId = createDto.TankId,
                Type = createDto.Type,
                Value = createDto.Value,
                Unit = createDto.Unit,
                Timestamp = DateTime.UtcNow
            };

            // Call AI service (Python FastAPI)
            var aiRequest = new
            {
                temperature = createDto.Type == "Temperature" ? createDto.Value : 0,
                pH = createDto.Type == "pH" ? createDto.Value : 0,
                oxygen = createDto.Type == "Oxygen" ? createDto.Value : 0
            };

            _logger.LogInformation($"Calling AI service at: {_httpClient.BaseAddress}/predict with request: {System.Text.Json.JsonSerializer.Serialize(aiRequest)}");
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/predict", aiRequest);
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

                    if (aiResult.Action != null)
                    {
                        await UpdateDeviceStatus(createDto.TankId, aiResult.Action.Device, aiResult.Action.Status);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"AI service call failed: {ex.Message}");
                // Continue saving the reading even if AI fails
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during AI call: {ex.Message}");
                // Continue saving the reading
            }

            _context.SensorReadings.Add(reading);
            await _context.SaveChangesAsync();

            return Ok(new SensorReadingDto
            {
                Id = reading.Id,
                TankId = reading.TankId,
                Type = reading.Type,
                Value = reading.Value,
                Unit = reading.Unit,
                Timestamp = reading.Timestamp
            });
        }

        private async Task UpdateDeviceStatus(int tankId, string deviceType, string status)
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.TankId == tankId && d.Type == deviceType);
            if (device != null)
            {
                device.Status = status;
                await _context.SaveChangesAsync();

                // Publish to MQTT for IoT devices
                await _mqttService.PublishAsync($"fishcare/tank/{tankId}/device/{device.Id}", status);
            }
        }
    }
}