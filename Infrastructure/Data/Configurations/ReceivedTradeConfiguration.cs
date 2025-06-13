using Domain.Entities.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ReceivedTradeConfiguration : IEntityTypeConfiguration<ReceivedTrade>
{
    public void Configure(EntityTypeBuilder<ReceivedTrade> builder)
    {
        builder.HasKey(st => st.TradeId);

        builder.HasOne(st => st.Trade)
            .WithOne(t => t.ReceivedTrade)
            .HasForeignKey<ReceivedTrade>(st => st.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
