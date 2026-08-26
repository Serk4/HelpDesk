namespace HelpDesk.Domain.Tickets;

public enum TicketNoteType
{
    PublicComment = 0,
    InternalNote = 1,
    Resolution = 2,
    System = 3
}

public class TicketNote
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string Body { get; set; } = string.Empty;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TicketNoteType NoteType { get; set; } = TicketNoteType.PublicComment;
    public bool IsInternal { get; set; }
}
