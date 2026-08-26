using HelpDesk.Application.Security;
using HelpDesk.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Users;

public sealed class UserAdministrationService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IUserAdministrationService
{
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync()
    {
        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync();

        var results = new List<UserSummary>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            results.Add(new UserSummary(user.Id, user.Email ?? user.UserName ?? user.Id, roles.OrderBy(role => role).ToList()));
        }

        return results;
    }

    public async Task<IReadOnlyList<UserSummary>> GetAssignableAgentsAsync()
    {
        var results = new List<UserSummary>();
        var users = await userManager.Users.OrderBy(user => user.Email).ToListAsync();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains(ApplicationRoles.Agent) || roles.Contains(ApplicationRoles.Admin))
            {
                results.Add(new UserSummary(user.Id, user.Email ?? user.UserName ?? user.Id, roles.OrderBy(role => role).ToList()));
            }
        }

        return results;
    }

    public Task<IReadOnlyList<string>> GetAvailableRolesAsync()
        => Task.FromResult<IReadOnlyList<string>>(ApplicationRoles.All);

    public async Task UpdateRolesAsync(string userId, IReadOnlyCollection<string> roles)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId)
            ?? throw new InvalidOperationException("User not found.");

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var targetRoles = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var toAdd = targetRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (toAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", addResult.Errors.Select(error => error.Description)));
            }
        }

        var toRemove = currentRoles.Except(targetRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (toRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", removeResult.Errors.Select(error => error.Description)));
            }
        }
    }
}
