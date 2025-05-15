using FishCareSystem.API.Data;
using FishCareSystem.API.DTOs;
using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FishCareSystem.API.Controllers
{
    [ApiController]
    [Route("api/farms")]
    [Authorize(Roles = "Manager,IoT,User")]
    public class FarmsController : ControllerBase
    {
        private readonly FishCareDbContext _context;

        public FarmsController(FishCareDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFarms()
        {
            var farms = await _context.Farms
                .Include(f => f.Owner)
                .Select(f => new FarmDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Location = f.Location
                })
                .ToListAsync();
            return Ok(farms);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFarm(int id)
        {
            var farms = await _context.Farms
                .Include(f => f.Owner)
                .Where(f => f.Id == id)
                .Select(f => new FarmDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Location = f.Location
                })
                .ToListAsync();
            return Ok(farms);
        }
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateFarm([FromBody] CreateFarmDto createFarmDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var farm = new Farm
            {
                Name = createFarmDto.Name,
                Location = createFarmDto.Location,
                OwnerId = userId
            };

            _context.Farms.Add(farm);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFarms), new { id = farm.Id }, new FarmDto
            {
                Id = farm.Id,
                Name = farm.Name,
                Location = farm.Location
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateFarm(int id, [FromBody] CreateFarmDto updateFarmDto)
        {
            var farm = await _context.Farms.FindAsync(id);
            if (farm == null)
            {
                return NotFound("Farm not found");
            }

            farm.Name = updateFarmDto.Name;
            farm.Location = updateFarmDto.Location;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteFarm(int id)
        {
            var farm = await _context.Farms.FindAsync(id);
            if (farm == null)
            {
                return NotFound("Farm not found");
            }

            _context.Farms.Remove(farm);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        
    }
}
