using HelpDesk.Domain.Tickets;

namespace HelpDesk.Application.Tickets;

public sealed record TicketListQuery(
    string? SearchText,
    int? CategoryId,
    TicketStatus? Status,
    TicketPriority? Priority,
    string? AssigneeUserId,
    DateTime? UpdatedFromUtc,
    DateTime? UpdatedToUtc,
    bool OnlyRequesterTickets,
    bool OnlyAssignedToMe);

public sealed record TicketDashboardSummary(
    int TotalTickets,
    int OpenTickets,
    int MyRequestedTickets,
    int MyAssignedTickets,
    int CriticalTickets,
    IReadOnlyDictionary<TicketStatus, int> ByStatus,
    IReadOnlyDictionary<TicketPriority, int> ByPriority,
    IReadOnlyList<Ticket> RecentlyUpdated);
