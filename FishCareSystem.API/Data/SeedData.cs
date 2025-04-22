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

            // Seed default manager user
            var managerEmail = "manager@fishcare.com";
            if (await userManager.FindByEmailAsync(managerEmail) == null)
            {
                var manager = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FirstName = "Fish",
                    LastName = "Manager"
                };
                await userManager.CreateAsync(manager, "Manager@123");
                await userManager.AddToRoleAsync(manager, "Manager");
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
        }
    }
}
