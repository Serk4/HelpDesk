namespace HelpDesk.Application.Users;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserSummary>> GetUsersAsync();
    Task<IReadOnlyList<UserSummary>> GetAssignableAgentsAsync();
    Task<IReadOnlyList<string>> GetAvailableRolesAsync();
    Task UpdateRolesAsync(string userId, IReadOnlyCollection<string> roles);
}
