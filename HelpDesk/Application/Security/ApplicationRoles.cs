namespace HelpDesk.Application.Security;

public static class ApplicationRoles
{
    public const string Requester = "Requester";
    public const string Agent = "Agent";
    public const string Admin = "Admin";

    public static readonly string[] All = [Requester, Agent, Admin];
}
