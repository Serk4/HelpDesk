using HelpDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Data.Configurations;

public sealed class TicketNoteConfiguration : IEntityTypeConfiguration<TicketNote>
{
    public void Configure(EntityTypeBuilder<TicketNote> builder)
    {
        builder.ToTable("TicketNotes");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.Body)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(note => note.CreatedByUserId)
            .HasMaxLength(450);

        builder.Property(note => note.CreatedAt)
            .IsRequired();

        builder.Property(note => note.NoteType)
            .IsRequired();

        builder.Property(note => note.IsInternal)
            .IsRequired();

        builder.HasQueryFilter(note => !note.Ticket.IsDeleted);

        builder.HasIndex(note => note.TicketId);
        builder.HasIndex(note => note.CreatedAt);
        builder.HasIndex(note => new { note.TicketId, note.IsInternal });
    }
}
