using Domain.Entities.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SentTradeConfiguration : IEntityTypeConfiguration<SentTrade>
{
    public void Configure(EntityTypeBuilder<SentTrade> builder)
    {
        builder.HasKey(st => st.TradeId);

        builder.HasOne(st => st.Trade)
            .WithOne(t => t.SentTrade)
            .HasForeignKey<SentTrade>(st => st.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
