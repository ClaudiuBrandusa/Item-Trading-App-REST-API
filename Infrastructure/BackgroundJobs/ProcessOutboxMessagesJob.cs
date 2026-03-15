using System.Text.Json;
using System.Text.Json.Nodes;
using Domain.Primitives;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution] // ensures that this job will run `as a singleton`
public sealed class ProcessOutboxMessagesJob : IJob
{
	private readonly DatabaseContext _dbContext;
    private readonly IPublisher _publisher;

    public ProcessOutboxMessagesJob(DatabaseContext dbContext, IPublisher publisher)
    {
        _dbContext = dbContext;
        _publisher = publisher;
    }

    public async Task Execute(IJobExecutionContext context)
	{
		var messages = await _dbContext
			.OutboxMessages
			//.Set<OutboxMessage>()
			.Where(m => m.ProcessedOnUtc == null)
			.Take(20)
			.ToListAsync(context.CancellationToken);
			
		foreach (var outboxMessage in messages)
		{
			var json = JsonNode.Parse(outboxMessage.Content);
			
			if (!json.AsObject().TryGetPropertyValue("$type", out var typeJson))
			{
				continue;
			}
			var typeName = typeJson!.GetValue<string>();
			var type = Type.GetType(typeName);
			var domainEvent = JsonSerializer.Deserialize(outboxMessage.Content, type) as IDomainEvent;
				
			if (domainEvent is null)
			{
				continue;
			}
			
			await _publisher.Publish(domainEvent, context.CancellationToken);
			
			outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
		}
		
		await _dbContext.SaveChangesAsync();
	}
}
