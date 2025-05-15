using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    [Authorize(Roles = "Manager,IoT,User")]
    public class AlertsController : ControllerBase
    {
        private readonly FishCareDbContext _context;

        public AlertsController(FishCareDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlerts()
        {
            var alerts = await _context.Alerts
                .Select(a => new AlertDto
                {
                    Id = a.Id,
                    TankId = a.TankId,
                    Message = a.Message,
                    Severity = a.Severity,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
            return Ok(alerts);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlert(int id)
        {
            var alert = await _context.Alerts
                .Where(a => a.Id == id)
                .Select(a => new AlertDto
                {
                    Id = a.Id,
                    TankId = a.TankId,
                    Message = a.Message,
                    Severity = a.Severity,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();
            if (alert == null)
            {
                return NotFound();
            }
            return Ok(alert);
        }
        [HttpPost]
        [Authorize(Roles = "Manager,IoT,User")]
        public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto createDto)
        {
            var tank = await _context.Tanks.FindAsync(createDto.TankId);
            if (tank == null)
            {
                return BadRequest("Tank not found");
            }

            var alert = new Alert
            {
                TankId = createDto.TankId,
                Message = createDto.Message,
                Severity = createDto.Severity,
                CreatedAt = DateTime.UtcNow
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            return Ok(new AlertDto
            {
                Id = alert.Id,
                TankId = alert.TankId,
                Message = alert.Message,
                Severity = alert.Severity,
                CreatedAt = alert.CreatedAt
            });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return NotFound();
            }
            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        
    }
}
