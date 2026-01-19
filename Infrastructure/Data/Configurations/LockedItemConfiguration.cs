using Domain.Entities.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LockedItemConfiguration : IEntityTypeConfiguration<LockedItem>
{
    public void Configure(EntityTypeBuilder<LockedItem> builder)
    {
        builder.HasKey(li => new { li.ItemId, li.UserId });

        /*builder.HasOne(li => li.ItemId)
            .WithOne(oi => oi.)
            .HasForeignKey<LockedItem>(li => new { li.ItemId, li.UserId })
            .OnDelete(DeleteBehavior.Cascade);*/
    }
}
