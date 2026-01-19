using Domain.Aggregates.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.HasKey(t => t.TradeId);

        builder.HasMany(t => t.TradeContents)
            .WithOne(x => x.Trade)
            .HasForeignKey(tc => tc.TradeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
