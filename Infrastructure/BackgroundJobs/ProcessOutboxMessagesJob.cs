using System.Text.Json;
using System.Text.Json.Nodes;
using Domain.Primitives;
using Infrastructure.Services.DatabaseContextWrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution] // ensures that this job will run `as a singleton`
public sealed class ProcessOutboxMessagesJob : IJob
{
	private readonly IDatabaseContextWrapper _dbContextWrapper;
    private readonly IPublisher _publisher;

    public ProcessOutboxMessagesJob(IDatabaseContextWrapper dbContextWrapper, IPublisher publisher)
    {
		_dbContextWrapper = dbContextWrapper;
        _publisher = publisher;
    }

    public async Task Execute(IJobExecutionContext context)
	{
		var dbContext = await _dbContextWrapper.ProvideDatabaseContextAsync();

		var messages = await dbContext
			.OutboxMessages
			.Where(m => m.ProcessedOnUtc == null)
			.Take(20)
			.ToListAsync(context.CancellationToken);
			
		foreach (var outboxMessage in messages)
		{
			var json = JsonNode.Parse(outboxMessage.Content);

			if (json is null)
			{
				continue;
			}
			
			if (!json.AsObject().TryGetPropertyValue("$type", out var typeJson))
			{
				continue;
			}
			var typeName = typeJson!.GetValue<string>();
			var type = Type.GetType(typeName);

			if (type is null)
			{
				continue;
			}

			var domainEvent = JsonSerializer.Deserialize(outboxMessage.Content, type) as IDomainEvent;
				
			if (domainEvent is null)
			{
				continue;
			}
			
			await _publisher.Publish(domainEvent, context.CancellationToken);
			
			outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
		}
		
		await dbContext.SaveChangesAsync();

		_dbContextWrapper.DisposeDatabaseContext(dbContext);
	}
}
