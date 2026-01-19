using Domain.Entities.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TradeItemConfiguration : IEntityTypeConfiguration<TradeItem>
{
    public void Configure(EntityTypeBuilder<TradeItem> builder)
    {
        builder.HasKey(tc => new { tc.ItemId, tc.TradeId });

        builder.HasOne(tc => tc.Item)
            .WithMany()
            .HasForeignKey(tc => tc.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
