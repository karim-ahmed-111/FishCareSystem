using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using FishCareSystem.API.Services;
using FishCareSystem.API.Services.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/sensor-readings")]
    [Authorize(Roles = "Manager,IoT,User")]
    public class SensorReadingsController : ControllerBase
    {
        private readonly SensorReadingService _sensorReadingService;
        private readonly FishCareDbContext _context;
        private readonly ILogger<SensorReadingsController> _logger;

        public SensorReadingsController(
            SensorReadingService sensorReadingService,
            FishCareDbContext context,
            ILogger<SensorReadingsController> logger)
        {
            _sensorReadingService = sensorReadingService;
            _context = context;
            _logger = logger;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _sensorReadingService.ProcessSensorReadingAsync(createDto);

            var reading = await _context.SensorReadings
                .Where(r => r.TankId == createDto.TankId && r.Type == createDto.Type && r.Value == createDto.Value)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

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
    }
}