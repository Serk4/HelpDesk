using HelpDesk.Domain.Tickets;

namespace HelpDesk.Application.Tickets;

public interface ITicketService
{
    Task<List<Ticket>> GetAllAsync();
    Task<List<Ticket>> GetVisibleAsync(TicketListQuery query, string userId, bool isAgent, bool isAdmin);
    Task<TicketDashboardSummary> GetDashboardAsync(string userId, bool isAgent, bool isAdmin);
    Task<IReadOnlyList<TicketCategory>> GetCategoriesAsync();
    Task<Ticket?> GetByIdAsync(int id, string userId, bool isAgent, bool isAdmin);
    Task<List<TicketNote>> GetNotesAsync(int ticketId, bool includeInternal);
    Task<List<TicketStatusHistory>> GetStatusHistoryAsync(int ticketId);
    Task<Ticket> CreateAsync(string title, string description, int categoryId, TicketPriority priority, string requesterUserId);
    Task<Ticket?> UpdateDetailsAsync(int id, string title, string description, int categoryId, TicketPriority priority, string actingUserId, bool canEditAll);
    Task<Ticket?> AssignAsync(int id, string actingUserId, string assigneeUserId, bool canManageAssignments, string? note);
    Task<Ticket?> StartWorkAsync(int id, string actingUserId, string? note);
    Task<Ticket?> AbandonAsync(int id, string actingUserId, string? note);
    Task<Ticket?> ResolveAsync(int id, string actingUserId, string resolutionNote);
    Task<Ticket?> CloseAsync(int id, string actingUserId, string? closeNote);
    Task<Ticket?> ReopenAsync(int id, string actingUserId, string? note, bool canManageAll);
    Task<TicketNote?> AddCommentAsync(int ticketId, string actingUserId, string body, bool isInternal, bool canAddInternal, bool canAccessAll);
    Task<bool> SoftDeleteAsync(int id, string actingUserId, bool canDeleteAll);
}
