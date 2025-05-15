using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/tanks")]
    [Authorize(Roles = "Manager,IoT,User")]
    public class TanksController : ControllerBase
    {
        private readonly FishCareDbContext _context;

        public TanksController(FishCareDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTanks()
        {
            var tanks = await _context.Tanks
                .Select(t => new TankDto
                {
                    Id = t.Id,
                    FarmId = t.FarmId,
                    Name = t.Name,
                    Capacity = t.Capacity,
                    FishSpecies = t.FishSpecies
                })
                .ToListAsync();
            return Ok(tanks);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTank(int id)
        {
            var tanks = await _context.Tanks
                .Where(t=>t.Id == id)
                .Select(t => new TankDto
                {
                    Id = t.Id,
                    FarmId = t.FarmId,
                    Name = t.Name,
                    Capacity = t.Capacity,
                    FishSpecies = t.FishSpecies
                })
                .ToListAsync();
            return Ok(tanks);
        }
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateTank([FromBody] CreateTankDto createTankDto)
        {
            var farm = await _context.Farms.FindAsync(createTankDto.FarmId);
            if (farm == null)
            {
                return BadRequest("Farm not found");
            }

            var tank = new Tank
            {
                FarmId = createTankDto.FarmId,
                Name = createTankDto.Name,
                Capacity = createTankDto.Capacity,
                FishSpecies = createTankDto.FishSpecies
            };

            _context.Tanks.Add(tank);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTanks), new { id = tank.Id }, new TankDto
            {
                Id = tank.Id,
                FarmId = tank.FarmId,
                Name = tank.Name,
                Capacity = tank.Capacity,
                FishSpecies = tank.FishSpecies
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateTank(int id, [FromBody] CreateTankDto updateTankDto)
        {
            var tank = await _context.Tanks.FindAsync(id);
            if (tank == null)
            {
                return NotFound("Tank not found");
            }

            var farm = await _context.Farms.FindAsync(updateTankDto.FarmId);
            if (farm == null)
            {
                return BadRequest("Farm not found");
            }

            tank.FarmId = updateTankDto.FarmId;
            tank.Name = updateTankDto.Name;
            tank.Capacity = updateTankDto.Capacity;
            tank.FishSpecies = updateTankDto.FishSpecies;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteTank(int id)
        {
            var tank = await _context.Tanks.FindAsync(id);
            if (tank == null)
            {
                return NotFound("Tank not found");
            }

            _context.Tanks.Remove(tank);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

