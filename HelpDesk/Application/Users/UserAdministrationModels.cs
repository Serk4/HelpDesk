namespace HelpDesk.Application.Users;

public sealed record UserSummary(
    string UserId,
    string Email,
    IReadOnlyList<string> Roles);
