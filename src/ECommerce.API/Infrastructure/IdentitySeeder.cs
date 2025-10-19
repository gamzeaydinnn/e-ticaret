using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Constants;

namespace ECommerce.API.Infrastructure
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var config = services.GetRequiredService<IConfiguration>();

            // Rolleri oluştur
            string[] roles = new[]
            {
                Roles.SuperAdmin,
                Roles.Admin,
                Roles.Manager,
                Roles.Editor,
                Roles.User
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }

            // Varsayılan admin kullanıcı
            var adminEmail = config["Admin:Email"] ?? "admin@local";
            var adminPassword = config["Admin:Password"] ?? "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    IsActive = true,
                    Role = Roles.Admin
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    await userManager.AddToRoleAsync(adminUser, Roles.SuperAdmin);
                }
                else
                {
                    // Hata durumunda sessiz geçmek yerine loglanması önerilir
                }
            }
        }
    }
}

