using HelpDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Data.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ticket => ticket.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(ticket => ticket.RequesterUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(ticket => ticket.AssignedUserId)
            .HasMaxLength(450);

        builder.Property(ticket => ticket.Status)
            .IsRequired();

        builder.Property(ticket => ticket.Priority)
            .IsRequired();

        builder.Property(ticket => ticket.CreatedAt)
            .IsRequired();

        builder.Property(ticket => ticket.UpdatedAt)
            .IsRequired();

        builder.Property(ticket => ticket.DeletedByUserId)
            .HasMaxLength(450);

        builder.Property(ticket => ticket.RowVersion)
            .IsRowVersion();

        builder.HasQueryFilter(ticket => !ticket.IsDeleted);

        builder.HasIndex(ticket => ticket.Status);
        builder.HasIndex(ticket => ticket.Priority);
        builder.HasIndex(ticket => ticket.AssignedUserId);
        builder.HasIndex(ticket => ticket.RequesterUserId);
        builder.HasIndex(ticket => ticket.CategoryId);
        builder.HasIndex(ticket => new { ticket.Status, ticket.Priority, ticket.CategoryId });
        builder.HasIndex(ticket => ticket.UpdatedAt);

        builder.HasOne(ticket => ticket.Category)
            .WithMany(category => category.Tickets)
            .HasForeignKey(ticket => ticket.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ticket => ticket.Notes)
            .WithOne(note => note.Ticket)
            .HasForeignKey(note => note.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ticket => ticket.StatusHistory)
            .WithOne(history => history.Ticket)
            .HasForeignKey(history => history.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
