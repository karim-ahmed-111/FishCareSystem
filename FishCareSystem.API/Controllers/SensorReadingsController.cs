using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/sensor-readings")]
    [Authorize]
    public class SensorReadingsController : ControllerBase
    {
        private readonly FishCareDbContext _context;

        public SensorReadingsController(FishCareDbContext context)
        {
            _context = context;
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

            // Simulate AI analysis with simple rules
            bool isAbnormal = false;
            string alertMessage = null;
            string severity = "Warning";

            if (createDto.Type == "Temperature" && createDto.Value > 30)
            {
                isAbnormal = true;
                alertMessage = $"High temperature: {createDto.Value}°C";
                // Simulate AI auto-adjust: Turn on cooler
                await UpdateDeviceStatus(createDto.TankId, "Cooler", "On");
            }
            else if (createDto.Type == "pH" && (createDto.Value < 6.5 || createDto.Value > 8.5))
            {
                isAbnormal = true;
                alertMessage = $"Abnormal pH: {createDto.Value}";
                severity = "Critical";
            }
            else if (createDto.Type == "Oxygen" && createDto.Value < 5)
            {
                isAbnormal = true;
                alertMessage = $"Low oxygen: {createDto.Value} mg/L";
                // Simulate AI auto-adjust: Turn on aerator
                await UpdateDeviceStatus(createDto.TankId, "Aerator", "On");
            }

            if (isAbnormal)
            {
                var alert = new Alert
                {
                    TankId = createDto.TankId,
                    Message = alertMessage,
                    Severity = severity,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Alerts.Add(alert);
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
            }
        }
    }
}
