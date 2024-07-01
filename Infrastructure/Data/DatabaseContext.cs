using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Domain.Items;
using Domain.Trades;
using Domain.Inventory;
using Domain.Identity;

namespace Infrastructure.Data;

public class DatabaseContext : IdentityDbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    { }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Item> Items { get; set; }

    public DbSet<OwnedItem> OwnedItems { get; set; }

    public DbSet<LockedItem> LockedItems { get; set; }

    public DbSet<Trade> Trades { get; set; }

    public DbSet<SentTrade> SentTrades { get; set; }

    public DbSet<ReceivedTrade> ReceivedTrades { get; set; }

    public DbSet<TradeContent> TradeContent { get; set; }

    public DbSet<TradeContentHistory> TradeContentHistory { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => x.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OwnedItem>()
            .HasKey(li => new { li.ItemId, li.UserId });

        modelBuilder.Entity<LockedItem>()
            .HasKey(li => new { li.ItemId, li.UserId });

        modelBuilder.Entity<LockedItem>()
            .HasOne(li => li.OwnedItem)
            .WithOne(oi => oi.LockedItem)
            .HasForeignKey<LockedItem>(li => new { li.ItemId, li.UserId })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TradeContent>()
            .HasKey(li => new { li.ItemId, li.TradeId });

        modelBuilder.Entity<TradeContentHistory>()
            .HasKey(tch => new { tch.ItemId, tch.TradeId });

        modelBuilder.Entity<User>()
            .Property(u => u.Cash)
            .HasDefaultValue(100); // starting cash value
    }
}
