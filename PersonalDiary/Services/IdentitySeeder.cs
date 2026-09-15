using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace PersonalDiary.Services;

public static class IdentitySeeder
{
    private static readonly SeedUser[] Users =
    [
        new("Kishalaya", "Test@123", "Kishalaya", "Bhattacharjee"),
        new("Aarav", "Aarav@2026!", "Aarav", "Sharma"),
        new("Ananya", "Ananya@2026!", "Ananya", "Sen"),
        new("Rohan", "Rohan@2026!", "Rohan", "Das"),
        new("Priya", "Priya@2026!", "Priya", "Roy")
    ];

    public static async Task SeedAsync(UserManager<IdentityUser> userManager)
    {
        foreach (var seedUser in Users)
        {
            var user = await userManager.FindByNameAsync(seedUser.UserName);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = seedUser.UserName,
                    Email = $"{seedUser.UserName.ToLowerInvariant()}@personaldiary.local",
                    EmailConfirmed = true
                };

                var creationResult = await userManager.CreateAsync(user, seedUser.Password);
                if (!creationResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not seed user {seedUser.UserName}: " +
                        string.Join("; ", creationResult.Errors.Select(error => error.Description)));
                }
            }

            var claims = await userManager.GetClaimsAsync(user);
            var nameClaims = new[]
            {
                new Claim(ClaimTypes.GivenName, seedUser.FirstName),
                new Claim(ClaimTypes.Surname, seedUser.LastName)
            };

            var missingClaims = nameClaims.Where(claim =>
                !claims.Any(existing => existing.Type == claim.Type && existing.Value == claim.Value));

            if (missingClaims.Any())
            {
                var claimResult = await userManager.AddClaimsAsync(user, missingClaims);
                if (!claimResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not seed profile claims for {seedUser.UserName}.");
                }
            }
        }
    }

    private sealed record SeedUser(
        string UserName,
        string Password,
        string FirstName,
        string LastName);
}
