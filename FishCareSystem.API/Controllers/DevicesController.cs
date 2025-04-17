using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
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

        public DevicesController(FishCareDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var devices = await _context.Devices
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

            return Ok(new DeviceDto
            {
                Id = device.Id,
                TankId = device.TankId,
                Name = device.Name,
                Type = device.Type,
                Status = device.Status
            });
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto updateDto)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound("Device not found");
            }

            device.Status = updateDto.Status;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
