using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using FishCareSystem.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/devices")]
    [Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly FishCareDbContext _context;
        private readonly IMqttClientService _mqttService;

        public DevicesController(FishCareDbContext context, IMqttClientService mqttService)
        {
            _context = context;
            _mqttService = mqttService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDevices([FromQuery] int? tankId)
        {
            var query = _context.Devices.AsQueryable();
            if (tankId.HasValue)
            {
                query = query.Where(d => d.TankId == tankId.Value);
            }

            var devices = await query
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    TankId = d.TankId,
                    Name = d.Name,
                    Type = d.Type,
                    Status = d.Status
                })
                .ToListAsync();
            return Ok(devices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDevice(int id)
        {
            var devices = await _context.Devices
                .Where(d=>d.Id == id)
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    TankId = d.TankId,
                    Name = d.Name,
                    Type = d.Type,
                    Status = d.Status
                })
                .ToListAsync();
            return Ok(devices);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto createDto)
        {
            var tank = await _context.Tanks.FindAsync(createDto.TankId);
            if (tank == null)
            {
                return BadRequest("Tank not found");
            }

            var device = new Device
            {
                TankId = createDto.TankId,
                Name = createDto.Name,
                Type = createDto.Type,
                Status = createDto.Status
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            // Notify IoT device via MQTT
            await _mqttService.PublishAsync($"fishcare/tank/{device.TankId}/device/{device.Id}", device.Status);

            return Ok(new DeviceDto
            {
                Id = device.Id,
                TankId = device.TankId,
                Name = device.Name,
                Type = device.Type,
                Status = device.Status
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto updateDto)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound("Device not found");
            }

            device.Status = updateDto.Status;
            await _context.SaveChangesAsync();

            // Notify IoT device via MQTT
            await _mqttService.PublishAsync($"fishcare/tank/{device.TankId}/device/{device.Id}", device.Status);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound("Device not found");
            }
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        
    }
}
