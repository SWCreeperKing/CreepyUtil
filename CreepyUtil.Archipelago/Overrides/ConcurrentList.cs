using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CreepyUtil.Archipelago.Overrides;

public interface IConcurrentList<T>
{
    int Count { get; }
    void Add(T item);
    void Clear();
    ReadOnlyCollection<T> AsReadOnlyCollection();
}

public class ConcurrentList<T> : IConcurrentList<T>
{
    readonly ConcurrentDictionary<int, T> list = new();

    public int Count => list.Count;

    public void Add(T item) => list.TryAdd(list.Count, item);

    public void Clear() => list.Clear();

    public ReadOnlyCollection<T> AsReadOnlyCollection() => (ReadOnlyCollection<T>)list.Values;
}