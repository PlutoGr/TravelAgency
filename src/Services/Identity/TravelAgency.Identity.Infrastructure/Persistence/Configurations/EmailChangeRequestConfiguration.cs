using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Infrastructure.Persistence.Configurations;

public class EmailChangeRequestConfiguration : IEntityTypeConfiguration<EmailChangeRequest>
{
    public void Configure(EntityTypeBuilder<EmailChangeRequest> builder)
    {
        builder.ToTable("EmailChangeRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.NewEmail).IsRequired().HasMaxLength(EmailChangeRequest.MaxEmailLength);
        builder.Property(request => request.ConfirmTokenHash).IsRequired().HasMaxLength(RefreshToken.HashLength);
        builder.Property(request => request.CancelTokenHash).IsRequired().HasMaxLength(RefreshToken.HashLength);
        builder.HasIndex(request => request.ConfirmTokenHash).IsUnique();
        builder.HasIndex(request => request.CancelTokenHash).IsUnique();
        builder.HasIndex(request => request.UserId)
            .IsUnique()
            .HasFilter("\"ConsumedAt\" IS NULL")
            .HasDatabaseName("IX_EmailChangeRequests_OneActive");
        builder.Property(request => request.ExpiresAt).IsRequired();
        builder.Property(request => request.CreatedAt).IsRequired();
        builder.HasOne(request => request.User)
            .WithMany()
            .HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
