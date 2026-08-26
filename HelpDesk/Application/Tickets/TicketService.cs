using HelpDesk.Data;
using HelpDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Tickets;

public sealed class TicketService(ApplicationDbContext dbContext) : ITicketService
{
    public Task<List<Ticket>> GetAllAsync()
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetVisibleAsync(TicketListQuery query, string userId, bool isAgent, bool isAdmin)
    {
        var tickets = BuildVisibilityQuery(userId, isAgent, isAdmin)
            .Include(ticket => ticket.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            tickets = tickets.Where(ticket => ticket.Title.Contains(query.SearchText) || ticket.Description.Contains(query.SearchText));
        }

        if (query.CategoryId.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.CategoryId == query.CategoryId.Value);
        }

        if (query.Status.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.Priority == query.Priority.Value);
        }

        if (query.UpdatedFromUtc.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.UpdatedAt >= query.UpdatedFromUtc.Value);
        }

        if (query.UpdatedToUtc.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.UpdatedAt <= query.UpdatedToUtc.Value);
        }

        if (isAgent || isAdmin)
        {
            if (!string.IsNullOrWhiteSpace(query.AssigneeUserId))
            {
                tickets = tickets.Where(ticket => ticket.AssignedUserId == query.AssigneeUserId);
            }

            if (query.OnlyRequesterTickets)
            {
                tickets = tickets.Where(ticket => ticket.RequesterUserId == userId);
            }

            if (query.OnlyAssignedToMe)
            {
                tickets = tickets.Where(ticket => ticket.AssignedUserId == userId);
            }
        }
        else
        {
            tickets = tickets.Where(ticket => ticket.RequesterUserId == userId);
        }

        return await tickets
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .ToListAsync();
    }

    public async Task<TicketDashboardSummary> GetDashboardAsync(string userId, bool isAgent, bool isAdmin)
    {
        var visibleTickets = BuildVisibilityQuery(userId, isAgent, isAdmin);
        var list = await visibleTickets
            .Include(ticket => ticket.Category)
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .ToListAsync();

        return new TicketDashboardSummary(
            list.Count,
            list.Count(ticket => ticket.Status is TicketStatus.New or TicketStatus.Assigned or TicketStatus.InProgress),
            list.Count(ticket => ticket.RequesterUserId == userId),
            list.Count(ticket => ticket.AssignedUserId == userId),
            list.Count(ticket => ticket.Priority == TicketPriority.Critical),
            Enum.GetValues<TicketStatus>().ToDictionary(status => status, status => list.Count(ticket => ticket.Status == status)),
            Enum.GetValues<TicketPriority>().ToDictionary(priority => priority, priority => list.Count(ticket => ticket.Priority == priority)),
            list.Take(5).ToList());
    }

    public async Task<IReadOnlyList<TicketCategory>> GetCategoriesAsync()
    {
        return await dbContext.Set<TicketCategory>()
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<Ticket?> GetByIdAsync(int id, string userId, bool isAgent, bool isAdmin)
    {
        var ticket = await dbContext.Tickets
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (ticket is null)
        {
            return null;
        }

        return CanAccessTicket(ticket, userId, isAgent, isAdmin) ? ticket : null;
    }

    public Task<List<TicketNote>> GetNotesAsync(int ticketId, bool includeInternal)
    {
        var notes = dbContext.TicketNotes
            .AsNoTracking()
            .Where(note => note.TicketId == ticketId);

        if (!includeInternal)
        {
            notes = notes.Where(note => !note.IsInternal);
        }

        return notes.OrderByDescending(note => note.CreatedAt).ToListAsync();
    }

    public Task<List<TicketStatusHistory>> GetStatusHistoryAsync(int ticketId)
    {
        return dbContext.Set<TicketStatusHistory>()
            .AsNoTracking()
            .Where(history => history.TicketId == ticketId)
            .OrderByDescending(history => history.ChangedAt)
            .ToListAsync();
    }

    public async Task<Ticket> CreateAsync(string title, string description, int categoryId, TicketPriority priority, string requesterUserId)
    {
        await EnsureCategoryExistsAsync(categoryId);

        var ticket = new Ticket
        {
            Title = title.Trim(),
            Description = description.Trim(),
            CategoryId = categoryId,
            Priority = priority,
            Status = TicketStatus.New,
            RequesterUserId = requesterUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Tickets.Add(ticket);
        await SaveChangesAsync();

        dbContext.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            FromStatus = TicketStatus.New,
            ToStatus = TicketStatus.New,
            ChangedByUserId = requesterUserId,
            ChangedAt = DateTime.UtcNow,
            Note = "Ticket created."
        });

        await AddNoteCoreAsync(ticket, requesterUserId, "Ticket created by requester.", TicketNoteType.System, true);
        await SaveChangesAsync();

        return await dbContext.Tickets
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstAsync(item => item.Id == ticket.Id);
    }

    public async Task<Ticket?> UpdateDetailsAsync(int id, string title, string description, int categoryId, TicketPriority priority, string actingUserId, bool canEditAll)
    {
        await EnsureCategoryExistsAsync(categoryId);

        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        var canEdit = canEditAll || ticket.RequesterUserId == actingUserId || ticket.AssignedUserId == actingUserId;
        if (!canEdit)
        {
            throw new InvalidOperationException("You do not have permission to edit this ticket.");
        }

        ticket.UpdateDetails(title, description, categoryId, priority);
        await SaveChangesAsync();

        return await dbContext.Tickets
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstAsync(item => item.Id == id);
    }

    public async Task<Ticket?> AssignAsync(int id, string actingUserId, string assigneeUserId, bool canManageAssignments, string? note)
    {
        if (!canManageAssignments)
        {
            throw new InvalidOperationException("Only agents or admins can assign tickets.");
        }

        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        var history = ticket.AssignTo(assigneeUserId.Trim(), actingUserId, note);
        dbContext.Add(history);

        if (!string.IsNullOrWhiteSpace(note))
        {
            await AddNoteCoreAsync(ticket, actingUserId, note, TicketNoteType.InternalNote, true);
        }

        await SaveChangesAsync();
        return await ReloadTicketAsync(id);
    }

    public async Task<Ticket?> StartWorkAsync(int id, string actingUserId, string? note)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        var history = ticket.StartWork(actingUserId, note);
        dbContext.Add(history);

        if (!string.IsNullOrWhiteSpace(note))
        {
            await AddNoteCoreAsync(ticket, actingUserId, note, TicketNoteType.InternalNote, true);
        }

        await SaveChangesAsync();
        return await ReloadTicketAsync(id);
    }

    public async Task<Ticket?> AbandonAsync(int id, string actingUserId, string? note)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        var history = ticket.Abandon(actingUserId, note);
        dbContext.Add(history);

        if (!string.IsNullOrWhiteSpace(note))
        {
            await AddNoteCoreAsync(ticket, actingUserId, note, TicketNoteType.InternalNote, true);
        }

        await SaveChangesAsync();
        return await ReloadTicketAsync(id);
    }

    public async Task<Ticket?> ResolveAsync(int id, string actingUserId, string resolutionNote)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        var history = ticket.Resolve(actingUserId, resolutionNote.Trim());
        dbContext.Add(history);
        await AddNoteCoreAsync(ticket, actingUserId, resolutionNote, TicketNoteType.Resolution, false);

        await SaveChangesAsync();
        return await ReloadTicketAsync(id);
    }

    public async Task<Ticket?> CloseAsync(int id, string actingUserId, string? closeNote)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        var history = ticket.Close(actingUserId, closeNote);
        dbContext.Add(history);

        if (!string.IsNullOrWhiteSpace(closeNote))
        {
            await AddNoteCoreAsync(ticket, actingUserId, closeNote, TicketNoteType.PublicComment, false);
        }

        await SaveChangesAsync();
        return await ReloadTicketAsync(id);
    }

    public async Task<Ticket?> ReopenAsync(int id, string actingUserId, string? note, bool canManageAll)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return null;
        }

        if (!canManageAll && ticket.RequesterUserId != actingUserId)
        {
            throw new InvalidOperationException("You do not have permission to reopen this ticket.");
        }

        var history = ticket.Reopen(actingUserId, note);
        dbContext.Add(history);

        if (!string.IsNullOrWhiteSpace(note))
        {
            await AddNoteCoreAsync(ticket, actingUserId, note, TicketNoteType.PublicComment, false);
        }

        await SaveChangesAsync();
        return await ReloadTicketAsync(id);
    }

    public async Task<TicketNote?> AddCommentAsync(int ticketId, string actingUserId, string body, bool isInternal, bool canAddInternal, bool canAccessAll)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("A comment is required.");
        }

        if (isInternal && !canAddInternal)
        {
            throw new InvalidOperationException("Only agents or admins can add internal notes.");
        }

        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == ticketId);
        if (ticket is null)
        {
            return null;
        }

        if (!canAccessAll && ticket.RequesterUserId != actingUserId)
        {
            throw new InvalidOperationException("You do not have access to this ticket.");
        }

        var noteType = isInternal ? TicketNoteType.InternalNote : TicketNoteType.PublicComment;
        await AddNoteCoreAsync(ticket, actingUserId, body, noteType, isInternal);
        await SaveChangesAsync();

        return await dbContext.TicketNotes
            .AsNoTracking()
            .OrderByDescending(note => note.Id)
            .FirstAsync(note => note.TicketId == ticketId);
    }

    public async Task<bool> SoftDeleteAsync(int id, string actingUserId, bool canDeleteAll)
    {
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return false;
        }

        if (!canDeleteAll && ticket.RequesterUserId != actingUserId)
        {
            throw new InvalidOperationException("You do not have permission to delete this ticket.");
        }

        ticket.SoftDelete(actingUserId);
        await SaveChangesAsync();
        return true;
    }

    private IQueryable<Ticket> BuildVisibilityQuery(string userId, bool isAgent, bool isAdmin)
    {
        var tickets = dbContext.Tickets.AsNoTracking().AsQueryable();
        if (isAgent || isAdmin)
        {
            return tickets;
        }

        return tickets.Where(ticket => ticket.RequesterUserId == userId);
    }

    private static bool CanAccessTicket(Ticket ticket, string userId, bool isAgent, bool isAdmin)
        => isAgent || isAdmin || ticket.RequesterUserId == userId;

    private async Task<Ticket> ReloadTicketAsync(int id)
    {
        return await dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .FirstAsync(ticket => ticket.Id == id);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var exists = await dbContext.Set<TicketCategory>().AnyAsync(category => category.Id == categoryId && category.IsActive);
        if (!exists)
        {
            throw new InvalidOperationException("A valid ticket category is required.");
        }
    }

    private static async Task SaveChangesAsync(ApplicationDbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The ticket was updated by another user. Please refresh and try again.");
        }
    }

    private Task SaveChangesAsync() => SaveChangesAsync(dbContext);

    private Task AddNoteCoreAsync(Ticket ticket, string? userId, string body, TicketNoteType noteType, bool isInternal)
    {
        dbContext.TicketNotes.Add(new TicketNote
        {
            TicketId = ticket.Id,
            CreatedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            Body = body.Trim(),
            CreatedAt = DateTime.UtcNow,
            NoteType = noteType,
            IsInternal = isInternal
        });

        ticket.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
