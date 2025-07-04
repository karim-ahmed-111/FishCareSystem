using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FishCareSystem.API.Data
{
    public static class SeedData
    {
        public static async Task Initialize(FishCareDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await context.Database.MigrateAsync();

            // Seed roles
            if (!await roleManager.RoleExistsAsync("Manager"))
            {
                await roleManager.CreateAsync(new IdentityRole("Manager"));
            }
            if (!await roleManager.RoleExistsAsync("IoT"))
            {
                await roleManager.CreateAsync(new IdentityRole("IoT"));
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Seed default manager user
            var managerEmail = "manager@fishcare.com";
            ApplicationUser manager;
            if (await userManager.FindByEmailAsync(managerEmail) == null)
            {
                manager = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FirstName = "Fish",
                    LastName = "Manager"
                };
                await userManager.CreateAsync(manager, "Manager@123");
                await userManager.AddToRoleAsync(manager, "Manager");
            }
            else
            {
                manager = await userManager.FindByEmailAsync(managerEmail);
            }

            // Seed IoT user
            var iotEmail = "iot@fishcare.com";
            if (await userManager.FindByEmailAsync(iotEmail) == null)
            {
                var iotUser = new ApplicationUser
                {
                    UserName = iotEmail,
                    Email = iotEmail,
                    FirstName = "IoT",
                    LastName = "Device"
                };
                await userManager.CreateAsync(iotUser, "IoT@123");
                await userManager.AddToRoleAsync(iotUser, "IoT");
            }

            // Seed sample farms and tanks
            await SeedFarmsAndTanks(context, manager);
            
            // Seed sample sensor readings for testing
            await SeedSensorReadings(context);
        }

        private static async Task SeedFarmsAndTanks(FishCareDbContext context, ApplicationUser manager)
        {
            // Check if we already have farms
            if (await context.Farms.AnyAsync())
                return;

            // Create sample farm
            var farm = new Farm
            {
                Name = "Main Fish Farm",
                Location = "Location A",
                OwnerId = manager.Id
            };
            context.Farms.Add(farm);
            await context.SaveChangesAsync();

            // Create sample tanks (matching the mobile app images - "Pond A", etc.)
            var tanks = new List<Tank>
            {
                new Tank
                {
                    FarmId = farm.Id,
                    Name = "Pond A",
                    Capacity = 1000.0,
                    FishSpecies = "Tilapia"
                },
                new Tank
                {
                    FarmId = farm.Id,
                    Name = "Pond B",
                    Capacity = 1500.0,
                    FishSpecies = "Catfish"
                },
                new Tank
                {
                    FarmId = farm.Id,
                    Name = "Pond C",
                    Capacity = 800.0,
                    FishSpecies = "Salmon"
                }
            };

            context.Tanks.AddRange(tanks);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSensorReadings(FishCareDbContext context)
        {
            // Check if we already have sensor readings
            if (await context.SensorReadings.AnyAsync())
                return;

            var tanks = await context.Tanks.ToListAsync();
            if (!tanks.Any())
                return;

            var sensorReadings = new List<SensorReading>();
            var random = new Random();
            var now = DateTime.UtcNow;

            // Generate sample sensor readings for the last 24 hours
            foreach (var tank in tanks)
            {
                // Generate readings for the last 24 hours, every 30 minutes
                for (int i = 48; i >= 0; i--)
                {
                    var timestamp = now.AddMinutes(-i * 30);

                    // Temperature readings (varying throughout the day)
                    var baseTemp = 25.0;
                    var tempVariation = Math.Sin((i * 30) / 720.0 * Math.PI) * 5; // Sine wave for daily variation
                    var temperature = baseTemp + tempVariation + random.NextDouble() * 2 - 1; // Add some noise

                    sensorReadings.Add(new SensorReading
                    {
                        TankId = tank.Id,
                        Type = "Temperature",
                        Value = Math.Round(temperature, 2),
                        Unit = "°C",
                        Timestamp = timestamp
                    });

                    // pH readings (more stable)
                    var ph = 7.0 + random.NextDouble() * 0.6 - 0.3; // pH between 6.7 and 7.3
                    sensorReadings.Add(new SensorReading
                    {
                        TankId = tank.Id,
                        Type = "pH",
                        Value = Math.Round(ph, 2),
                        Unit = "pH",
                        Timestamp = timestamp
                    });

                    // Oxygen readings
                    var oxygen = 6.0 + random.NextDouble() * 2; // Oxygen between 6 and 8 ppm
                    sensorReadings.Add(new SensorReading
                    {
                        TankId = tank.Id,
                        Type = "Oxygen",
                        Value = Math.Round(oxygen, 2),
                        Unit = "ppm",
                        Timestamp = timestamp
                    });

                    // Water Level readings
                    var waterLevel = 85.0 + random.NextDouble() * 10; // Water level between 85-95%
                    sensorReadings.Add(new SensorReading
                    {
                        TankId = tank.Id,
                        Type = "WaterLevel",
                        Value = Math.Round(waterLevel, 2),
                        Unit = "%",
                        Timestamp = timestamp
                    });
                }
            }

            context.SensorReadings.AddRange(sensorReadings);
            await context.SaveChangesAsync();
        }
    }
}
