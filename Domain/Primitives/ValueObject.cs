namespace Domain.Primitives;

public abstract class ValueObject : IEquatable<ValueObject>
{
	// returns the components of the value object (its values)
	public abstract IEnumerable<object> GetAtomicValues();
	
	public bool Equals(ValueObject? other)
	{
		return other is not null && ValuesAreEqual(other);
	}
	
	public override bool Equals(object? obj)
	{
		return obj is ValueObject other && ValuesAreEqual(other);
	}
	
	public override int GetHashCode()
	{
		return GetAtomicValues()
			.Aggregate(
				default(int),
				HashCode.Combine);
	}
	
	private bool ValuesAreEqual(ValueObject other)
	{
		return GetAtomicValues()
            .SequenceEqual(other.GetAtomicValues());
		    // SequenceEqual - calls Equal method for each element from the collection
	}
}