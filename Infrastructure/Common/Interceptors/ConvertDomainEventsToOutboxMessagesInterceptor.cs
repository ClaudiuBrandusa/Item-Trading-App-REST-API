using Domain.Primitives;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;

namespace Infrastructure.Common.Outbox;

public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
       DbContextEventData eventData,
       InterceptionResult<int> result,
       CancellationToken cancellationToken = default)
    {
        DatabaseContext? dbContext = eventData.Context as DatabaseContext;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Access all of the entries that are of type aggregate root,
        // because only the domain objects of this type can raise domain events
        var outboxMessages = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .SelectMany(aggregateRoot =>
            {
                var domainEvents = aggregateRoot.GetDomainEvents();
            
                aggregateRoot.ClearDomainEvents();

                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = DateTime.UtcNow,
                Type = domainEvent.GetType().Name,
                Content = JsonConvert.SerializeObject(
                    domainEvent,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    })
            })
            .ToList();

        dbContext.OutboxMessages.AddRange(outboxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
