using Domain.Entities.Inventories;
using Domain.Entities.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(ii => new { ii.ItemId, ii.UserId });
        
        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(nameof(InventoryItem.ItemId))
            .OnDelete(DeleteBehavior.Cascade);
    }
}