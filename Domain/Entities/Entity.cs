namespace Domain.Entities;

public abstract class Entity
{
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        return Compare(obj);
    }

    public override int GetHashCode()
    {
        return GetId().GetHashCode();
    }

    protected abstract bool Compare(object obj);

    protected abstract object GetId();

    public static bool operator ==(Entity obj1, Entity obj2)
    {
        if (obj1 is null && obj2 is null) return true;

        return obj1?.Equals(obj2) ?? false;
    }

    public static bool operator !=(Entity obj1, Entity obj2) => !(obj1 == obj2);
}
