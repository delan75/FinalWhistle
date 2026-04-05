using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("Fan"))
        {
            await roleManager.CreateAsync(new IdentityRole("Fan"));
        }

        if (!await userManager.Users.AnyAsync())
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@finalwhistle.com",
                Email = "admin@finalwhistle.com",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await context.Tournaments.AnyAsync())
        {
            var tournament = new Tournament
            {
                Name = "COPA ELITE WORLD CUP 2026",
                Season = 2026,
                Status = TournamentStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };
            context.Tournaments.Add(tournament);
            await context.SaveChangesAsync();

            var groups = new[]
            {
                new Group { TournamentId = tournament.Id, Name = "Group A", DisplayOrder = 1 },
                new Group { TournamentId = tournament.Id, Name = "Group B", DisplayOrder = 2 },
                new Group { TournamentId = tournament.Id, Name = "Group C", DisplayOrder = 3 },
                new Group { TournamentId = tournament.Id, Name = "Group D", DisplayOrder = 4 },
                new Group { TournamentId = tournament.Id, Name = "Group E", DisplayOrder = 5 },
                new Group { TournamentId = tournament.Id, Name = "Group F", DisplayOrder = 6 },
                new Group { TournamentId = tournament.Id, Name = "Group G", DisplayOrder = 7 },
                new Group { TournamentId = tournament.Id, Name = "Group H", DisplayOrder = 8 }
            };
            context.Groups.AddRange(groups);
            await context.SaveChangesAsync();
        }
    }
}
