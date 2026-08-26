using HelpDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Data.Configurations;

public sealed class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        builder.ToTable("TicketStatusHistory");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(history => history.ChangedAt)
            .IsRequired();

        builder.Property(history => history.Note)
            .HasMaxLength(1000);

        builder.HasQueryFilter(history => !history.Ticket.IsDeleted);

        builder.HasIndex(history => history.TicketId);
        builder.HasIndex(history => history.ChangedAt);
    }
}
