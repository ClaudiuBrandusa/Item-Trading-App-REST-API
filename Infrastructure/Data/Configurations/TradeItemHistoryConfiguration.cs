using Domain.Entities.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TradeItemHistoryConfiguration : IEntityTypeConfiguration<TradeItemHistory>
{
    public void Configure(EntityTypeBuilder<TradeItemHistory> builder)
    {
        builder.HasKey(tch => new { tch.ItemId, tch.TradeId });

        builder.HasOne(tch => tch.Trade)
            .WithMany()
            .HasForeignKey(tch => tch.TradeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
