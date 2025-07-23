using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Items;
using Domain.Entities.Identity;
using Domain.Entities.Inventories;
using Domain.Entities.Trades;
using Domain.Aggregates.Inventories;
using Domain.Aggregates.Trades;

namespace Infrastructure.Data;

public class DatabaseContext : IdentityDbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Item> Items { get; set; }

    public DbSet<OwnedItem> OwnedItems { get; set; }

    public DbSet<LockedItem> LockedItems { get; set; }

    public DbSet<Trade> Trades { get; set; }

    public DbSet<SentTrade> SentTrades { get; set; }

    public DbSet<ReceivedTrade> ReceivedTrades { get; set; }

    public DbSet<TradeItem> TradeContent { get; set; }

    public DbSet<TradeItemHistory> TradeContentHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DatabaseContext).Assembly);
    }
}
