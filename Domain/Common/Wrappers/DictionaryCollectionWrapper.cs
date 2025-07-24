using System.Collections;

namespace Domain.Common.Wrappers;

/// <summary>
/// A dictionary wrapper that allows the EF core to manipulate its values
/// </summary>
public class DictionaryCollectionWrapper<TKey, TEntity>
    : ICollection<TEntity>, IReadOnlyCollection<TEntity>
    where TEntity : class
{
    private readonly Dictionary<TKey, TEntity> _dict;
    private readonly Func<TEntity, TKey> _keySelector;

    public DictionaryCollectionWrapper(Func<TEntity, TKey> keySelector)
    {
        _keySelector = keySelector
            ?? throw new ArgumentNullException(nameof(keySelector));
        _dict = new Dictionary<TKey, TEntity>();
    }

    public int Count => _dict.Count;

    bool ICollection<TEntity>.IsReadOnly => false;

    public void Add(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var key = _keySelector(entity);
        _dict.Add(key, entity);
    }

    public bool Remove(TEntity entity)
    {
        if (entity == null) return false;
        var key = _keySelector(entity);
        return _dict.Remove(key);
    }

    public bool Remove(TKey key)
    {
        return _dict.Remove(key);
    }

    public bool Contains(TEntity entity)
    {
        if (entity == null) return false;
        return _dict.Values.Contains(entity);
    }

    public bool ContainsKey(TKey key)
    {
        return _dict.ContainsKey(key);
    }

    public void Clear() => _dict.Clear();

    public void CopyTo(TEntity[] array, int arrayIndex)
    {
        _dict.Values.CopyTo(array, arrayIndex);
    }

    public IEnumerator<TEntity> GetEnumerator()
        => _dict.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public TEntity this[TKey key] => _dict[key];

    public bool TryGetValue(TKey key, out TEntity value)
        => _dict.TryGetValue(key, out value!);
}
