using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Inventories;
using Domain.Entities.Identity;
using Domain.Entities.Items;
using Domain.Aggregates.Inventories;

namespace Infrastructure.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    private static readonly string tableName = nameof(Inventory);

    private static readonly string ownedItemsName = nameof(Inventory.OwnedItems);

    private static readonly string userIdColumnName = nameof(Inventory.UserId);

    private static readonly string itemIdColumnName = nameof(InventoryItem.ItemId);

    private static readonly string itemQuantityColumnName = nameof(InventoryItem.Quantity);

    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.HasKey(i => i.UserId);

        builder.HasOne<User>()
           .WithOne()
           .HasForeignKey<Inventory>(i => i.UserId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(i => i.OwnedItems, owned =>
        {
            owned.WithOwner().HasForeignKey(userIdColumnName);

            owned.Property(o => o.ItemId).IsRequired();
            owned.Property(o => o.Quantity).IsRequired();
            owned.Property(o => o.LockedAmount);

            owned.HasKey(userIdColumnName, itemIdColumnName);

            owned.HasOne<Item>()
                .WithMany()
                .HasForeignKey(nameof(InventoryItem.ItemId))
                .OnDelete(DeleteBehavior.Cascade);

            owned.ToTable($"{nameof(InventoryItem)}s");

        });

        builder.ToTable(nameof(DatabaseContext.Inventories));
    }
}
