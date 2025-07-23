using Domain.Aggregates.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class OwnedItemConfiguration : IEntityTypeConfiguration<OwnedItem>
{
    public void Configure(EntityTypeBuilder<OwnedItem> builder)
    {
        builder.HasKey(oi => new { oi.ItemId, oi.UserId });

        builder.HasOne(oi => oi.Item)
            .WithMany()
            .HasForeignKey(oi => oi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(oi => oi.User)
            .WithMany()
            .HasForeignKey(oi => oi.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
