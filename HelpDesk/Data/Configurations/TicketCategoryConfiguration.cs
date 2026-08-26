using HelpDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Data.Configurations;

public sealed class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("TicketCategories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(category => category.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(category => category.IsActive)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.HasData(
            new TicketCategory { Id = 1, Name = "Account", Description = "Login, access, or account management issues.", IsActive = true },
            new TicketCategory { Id = 2, Name = "Hardware", Description = "Laptop, monitor, printer, and other physical device issues.", IsActive = true },
            new TicketCategory { Id = 3, Name = "Software", Description = "Application support, installations, and errors.", IsActive = true },
            new TicketCategory { Id = 4, Name = "Network", Description = "Connectivity, VPN, and network performance issues.", IsActive = true },
            new TicketCategory { Id = 5, Name = "Security", Description = "Security concerns, suspicious activity, or policy issues.", IsActive = true },
            new TicketCategory { Id = 6, Name = "Other", Description = "General requests that do not fit another category.", IsActive = true });
    }
}
