using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Domain.Entities.Items;
using Domain.Entities.Identity;
using Domain.Entities.Inventory;
using Domain.Entities.Trades;
using Domain.Aggregates.Inventory;
using Domain.Aggregates.Trades;

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

    public DbSet<TradeItem> TradeContent { get; set; }

    public DbSet<TradeItemHistory> TradeContentHistory { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => x.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region Identity

        modelBuilder.Entity<User>()
            .Property(u => u.Cash)
            .HasDefaultValue(100); // starting cash value

        modelBuilder.Entity<RefreshToken>()
            .HasKey(rt => rt.Token);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion Identity

        #region Item

        modelBuilder.Entity<OwnedItem>()
            .HasKey(oi => new { oi.ItemId, oi.UserId });

        modelBuilder.Entity<OwnedItem>()
            .HasOne(oi => oi.Item)
            .WithMany()
            .HasForeignKey(oi => oi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OwnedItem>()
            .HasOne(oi => oi.User)
            .WithMany()
            .HasForeignKey(oi => oi.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LockedItem>()
            .HasKey(li => new { li.ItemId, li.UserId });

        modelBuilder.Entity<LockedItem>()
            .HasOne(li => li.OwnedItem)
            .WithOne(oi => oi.LockedItem)
            .HasForeignKey<LockedItem>(li => new { li.ItemId, li.UserId })
            .OnDelete(DeleteBehavior.Cascade);

        #endregion Item

        #region Trade

        modelBuilder.Entity<Trade>()
            .HasKey(t => t.TradeId);

        modelBuilder.Entity<Trade>()
            .HasMany(t => t.TradeContents)
            .WithOne(x => x.Trade)
            .HasForeignKey(tc => tc.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TradeItem>()
            .HasKey(tc => new { tc.ItemId, tc.TradeId });

        modelBuilder.Entity<TradeItem>()
            .HasOne(tc => tc.Item)
            .WithMany()
            .HasForeignKey(tc => tc.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TradeItemHistory>()
            .HasKey(tch => new { tch.ItemId, tch.TradeId });

        modelBuilder.Entity<TradeItemHistory>()
            .HasOne(tch => tch.Trade)
            .WithMany()
            .HasForeignKey(tch => tch.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SentTrade>()
            .HasKey(st => st.TradeId);

        modelBuilder.Entity<SentTrade>()
            .HasOne(st => st.Trade)
            .WithOne(t => t.SentTrade)
            .HasForeignKey<SentTrade>(st => st.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SentTrade>()
            .HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReceivedTrade>()
            .HasKey(st => st.TradeId);

        modelBuilder.Entity<ReceivedTrade>()
            .HasOne(st => st.Trade)
            .WithOne(t => t.ReceivedTrade)
            .HasForeignKey<ReceivedTrade>(st => st.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReceivedTrade>()
            .HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion Trade
    }
}
