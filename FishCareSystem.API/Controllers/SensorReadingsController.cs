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
        public async Task<IActionResult> GetSensorReadings([FromQuery] int? tankId, [FromQuery] string? sensorType, [FromQuery] int? hours)
        {
            var query = _context.SensorReadings.AsQueryable();

            if (tankId.HasValue)
            {
                query = query.Where(r => r.TankId == tankId.Value);
            }

            if (!string.IsNullOrEmpty(sensorType))
            {
                query = query.Where(r => r.Type == sensorType);
            }

            if (hours.HasValue)
            {
                var fromDate = DateTime.UtcNow.AddHours(-hours.Value);
                query = query.Where(r => r.Timestamp >= fromDate);
            }

            var readings = await query
                .OrderByDescending(r => r.Timestamp)
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

        // New endpoint for current sensor readings (like in image 1)
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentSensorReadings([FromQuery] int? tankId)
        {
            var query = _context.Tanks.AsQueryable();
            
            if (tankId.HasValue)
            {
                query = query.Where(t => t.Id == tankId.Value);
            }

            var result = await query
                .Select(t => new CurrentSensorReadingsDto
                {
                    TankId = t.Id,
                    TankName = t.Name,
                    Sensors = t.SensorReadings
                        .GroupBy(sr => sr.Type)
                        .Select(g => g.OrderByDescending(sr => sr.Timestamp).First())
                        .Select(sr => new CurrentSensorValueDto
                        {
                            Type = sr.Type,
                            Value = sr.Value,
                            Unit = sr.Unit,
                            LastUpdated = sr.Timestamp,
                            Status = GetSensorStatus(sr.Type, sr.Value)
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(result);
        }

        // New endpoint for sensor statistics (Average, Min, Max)
        [HttpGet("statistics")]
        public async Task<IActionResult> GetSensorStatistics([FromQuery] SensorHistoryFilterDto filter)
        {
            if (filter.TankId == 0 || string.IsNullOrEmpty(filter.SensorType))
            {
                return BadRequest("TankId and SensorType are required");
            }

            var (fromDate, toDate) = GetDateRange(filter.TimePeriod, filter.FromDate, filter.ToDate);

            var readings = await _context.SensorReadings
                .Where(r => r.TankId == filter.TankId 
                           && r.Type == filter.SensorType 
                           && r.Timestamp >= fromDate 
                           && r.Timestamp <= toDate)
                .ToListAsync();

            if (!readings.Any())
            {
                return NotFound("No sensor readings found for the specified criteria");
            }

            var statistics = new SensorStatisticsDto
            {
                Average = Math.Round(readings.Average(r => r.Value), 2),
                Minimum = readings.Min(r => r.Value),
                Maximum = readings.Max(r => r.Value),
                Unit = readings.First().Unit,
                DataPoints = readings.Count,
                FromDate = fromDate,
                ToDate = toDate
            };

            return Ok(statistics);
        }

        // New endpoint for chart data (like in image 2)
        [HttpGet("chart")]
        public async Task<IActionResult> GetChartData([FromQuery] SensorHistoryFilterDto filter)
        {
            if (filter.TankId == 0 || string.IsNullOrEmpty(filter.SensorType))
            {
                return BadRequest("TankId and SensorType are required");
            }

            var (fromDate, toDate) = GetDateRange(filter.TimePeriod, filter.FromDate, filter.ToDate);

            var readings = await _context.SensorReadings
                .Where(r => r.TankId == filter.TankId 
                           && r.Type == filter.SensorType 
                           && r.Timestamp >= fromDate 
                           && r.Timestamp <= toDate)
                .OrderBy(r => r.Timestamp)
                .ToListAsync();

            if (!readings.Any())
            {
                return NotFound("No sensor readings found for the specified criteria");
            }

            var statistics = new SensorStatisticsDto
            {
                Average = Math.Round(readings.Average(r => r.Value), 2),
                Minimum = readings.Min(r => r.Value),
                Maximum = readings.Max(r => r.Value),
                Unit = readings.First().Unit,
                DataPoints = readings.Count,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Group data for chart points (to reduce data points for better visualization)
            var groupedData = GroupDataForChart(readings, filter.TimePeriod);

            var chartData = new ChartDataDto
            {
                Statistics = statistics,
                SensorType = filter.SensorType,
                Unit = readings.First().Unit,
                DataPoints = groupedData.Select(r => new ChartPointDto
                {
                    Timestamp = r.Timestamp,
                    Value = Math.Round(r.Value, 2),
                    FormattedTime = r.Timestamp.ToString("HH:mm")
                }).ToList()
            };

            return Ok(chartData);
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

        // Helper methods
        private (DateTime fromDate, DateTime toDate) GetDateRange(string timePeriod, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                return (fromDate.Value, toDate.Value);
            }

            var now = DateTime.UtcNow;
            return timePeriod?.ToLower() switch
            {
                "1h" => (now.AddHours(-1), now),
                "6h" => (now.AddHours(-6), now),
                "10h" => (now.AddHours(-10), now),
                "24h" => (now.AddHours(-24), now),
                "7d" => (now.AddDays(-7), now),
                "30d" => (now.AddDays(-30), now),
                _ => (now.AddHours(-10), now) // Default to 10 hours like in the image
            };
        }

        private List<SensorReading> GroupDataForChart(List<SensorReading> readings, string timePeriod)
        {
            // For chart visualization, we might want to group data to reduce points
            var groupMinutes = timePeriod?.ToLower() switch
            {
                "1h" => 5,   // Group by 5 minutes for 1 hour
                "6h" => 30,  // Group by 30 minutes for 6 hours
                "10h" => 60, // Group by 1 hour for 10 hours
                "24h" => 120, // Group by 2 hours for 24 hours
                "7d" => 360,  // Group by 6 hours for 7 days
                "30d" => 1440, // Group by 1 day for 30 days
                _ => 60
            };

            return readings
                .GroupBy(r => new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, 
                                         r.Timestamp.Hour, (r.Timestamp.Minute / groupMinutes) * groupMinutes, 0))
                .Select(g => new SensorReading
                {
                    Timestamp = g.Key,
                    Value = g.Average(x => x.Value),
                    Type = g.First().Type,
                    Unit = g.First().Unit
                })
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        private string GetSensorStatus(string sensorType, double value)
        {
            // Simple status logic - you can enhance this based on your thresholds
            return sensorType.ToLower() switch
            {
                "temperature" => value > 30 || value < 20 ? "Warning" : "Normal",
                "ph" => value > 8.5 || value < 6.5 ? "Warning" : "Normal",
                "oxygen" => value < 5 ? "Critical" : "Normal",
                "waterlevel" => value < 50 ? "Warning" : "Normal",
                _ => "Normal"
            };
        }
    }
}