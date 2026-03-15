namespace Domain.Primitives;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();

    public void ClearDomainEvents() => _domainEvents.Clear();

	protected void RaiseDomainEvent(IDomainEvent domainEvent)
	{
		_domainEvents.Add(domainEvent);
	}
}
