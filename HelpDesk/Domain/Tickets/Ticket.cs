namespace HelpDesk.Domain.Tickets;

public enum TicketStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public TicketCategory Category { get; set; } = null!;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public string RequesterUserId { get; set; } = string.Empty;
    public string? AssignedUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<TicketNote> Notes { get; set; } = new List<TicketNote>();
    public ICollection<TicketStatusHistory> StatusHistory { get; set; } = new List<TicketStatusHistory>();

    public void UpdateDetails(string title, string description, int categoryId, TicketPriority priority)
    {
        EnsureActive();
        Title = title.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        Priority = priority;
        Touch();
    }

    public TicketStatusHistory AssignTo(string assigneeUserId, string changedByUserId, string? note = null)
    {
        EnsureActive();
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new InvalidOperationException("Resolved or closed tickets cannot be assigned.");
        }

        var previous = Status;
        AssignedUserId = assigneeUserId;
        Status = TicketStatus.Assigned;
        Touch();
        return CreateHistory(previous, TicketStatus.Assigned, changedByUserId, note ?? $"Assigned to '{assigneeUserId}'.");
    }

    public TicketStatusHistory StartWork(string changedByUserId, string? note = null)
    {
        EnsureActive();
        EnsureAssignedTo(changedByUserId);
        if (Status != TicketStatus.Assigned)
        {
            throw new InvalidOperationException("Only assigned tickets can be moved to in progress.");
        }

        var previous = Status;
        Status = TicketStatus.InProgress;
        Touch();
        return CreateHistory(previous, TicketStatus.InProgress, changedByUserId, note ?? "Work started.");
    }

    public TicketStatusHistory Abandon(string changedByUserId, string? note = null)
    {
        EnsureActive();
        EnsureAssignedTo(changedByUserId);
        if (Status is not TicketStatus.Assigned and not TicketStatus.InProgress)
        {
            throw new InvalidOperationException("Only assigned or in-progress tickets can be abandoned.");
        }

        var previous = Status;
        AssignedUserId = null;
        Status = TicketStatus.New;
        Touch();
        return CreateHistory(previous, TicketStatus.New, changedByUserId, note ?? "Ticket returned to the pool.");
    }

    public TicketStatusHistory Resolve(string changedByUserId, string resolutionNote)
    {
        EnsureActive();
        EnsureAssignedTo(changedByUserId);
        if (Status != TicketStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress tickets can be resolved.");
        }

        var previous = Status;
        Status = TicketStatus.Resolved;
        Touch();
        return CreateHistory(previous, TicketStatus.Resolved, changedByUserId, resolutionNote);
    }

    public TicketStatusHistory Close(string changedByUserId, string? note = null)
    {
        EnsureActive();
        EnsureAssignedTo(changedByUserId);
        if (Status != TicketStatus.Resolved)
        {
            throw new InvalidOperationException("Only resolved tickets can be closed.");
        }

        var previous = Status;
        Status = TicketStatus.Closed;
        Touch();
        return CreateHistory(previous, TicketStatus.Closed, changedByUserId, note ?? "Ticket closed.");
    }

    public TicketStatusHistory Reopen(string changedByUserId, string? note = null)
    {
        EnsureActive();
        if (Status is not TicketStatus.Resolved and not TicketStatus.Closed)
        {
            throw new InvalidOperationException("Only resolved or closed tickets can be reopened.");
        }

        var previous = Status;
        Status = string.IsNullOrWhiteSpace(AssignedUserId) ? TicketStatus.New : TicketStatus.Assigned;
        Touch();
        return CreateHistory(previous, Status, changedByUserId, note ?? "Ticket reopened.");
    }

    public void SoftDelete(string changedByUserId)
    {
        EnsureActive();
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = changedByUserId;
        Touch();
    }

    private TicketStatusHistory CreateHistory(TicketStatus fromStatus, TicketStatus toStatus, string changedByUserId, string? note)
    {
        return new TicketStatusHistory
        {
            TicketId = Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Note = note
        };
    }

    private void EnsureAssignedTo(string userId)
    {
        if (!string.Equals(AssignedUserId, userId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only the assigned user can perform this action.");
        }
    }

    private void EnsureActive()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Deleted tickets cannot be modified.");
        }
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
