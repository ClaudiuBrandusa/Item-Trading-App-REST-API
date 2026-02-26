using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Identity;
using Domain.Aggregates.Inventories;

namespace Infrastructure.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.HasKey(i => i.UserId);

        builder.HasOne<User>()
           .WithOne()
           .HasForeignKey<Inventory>(i => i.UserId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Inventory.OwnedItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(i => i.OwnedItems)
            .WithOne()
            .HasForeignKey(ii => ii.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(nameof(DatabaseContext.Inventories));
    }
}
