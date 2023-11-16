using Item_Trading_App_REST_API.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Data;

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

        modelBuilder.Entity<User>()
            .Property(u => u.Cash)
            .HasDefaultValue(100); // starting cash value
    }

    public async Task<bool> AddEntityAsync<T>(T entity) where T : class
    {
        await AddAsync(entity);
        var added = await SaveChangesAsync();
        Entry(entity).State = EntityState.Detached;
        return added > 0;
    }

    public async Task<bool> UpdateEntityAsync<T>(T entity) where T : class
    {
        Update(entity);
        var updated = await SaveChangesAsync();
        Entry(entity).State = EntityState.Detached;
        return updated > 0;
    }

    public async Task<bool> RemoveEntityAsync<T>(T entity) where T : class
    {
        Remove(entity);
        var removed = await SaveChangesAsync();
        return removed > 0;
    }
}
