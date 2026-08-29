namespace CreepyUtil.Archipelago;

public class LimitedCollection<T>(int limit, Func<List<T>, T?>? findToDelete = null)
{
    public int Limit { get; private set; } = limit;
    private List<T> Queue = [];
    private int LocalLimit => Limit == -1 ? 200 : Limit;

    public bool Add(T t, Action<T>? removedObj = null)
    {
        lock (Queue) Queue.Add(t);
        return Update(removedObj);
    }

    public void Remove(T t, Action<T>? removedObj = null)
    {
        lock (Queue)
        {
            Queue.Remove(t);
            removedObj?.Invoke(t);
        }
        Update(removedObj);
    }

    public bool Update(Action<T>? removedObj = null)
    {
        lock (Queue)
        {
            var removed = Queue.Count > LocalLimit;
            while (Queue.Count > LocalLimit)
            {
                if (findToDelete is not null)
                {
                    var find = findToDelete(Queue);
                    if (find is not null)
                    {
                        lock (Queue)
                        {
                            Queue.Remove(find);
                            removedObj?.Invoke(find);
                        }
                        continue;
                    } 
                }
                
                var objRemoved = Dequeue();
                removedObj?.Invoke(objRemoved!);
            }

            return removed;
        }
    }

    public void SetLimit(int newLimit, Action<T>? removedObj = null)
    {
        Limit = Math.Max(newLimit, 10);
        Update(removedObj);
    }

    public bool Enqueue(T t) => Add(t);

    public T? Dequeue()
    {
        lock (Queue)
        {
            if (Queue.Count == 0) return default;
            var item = Queue[0];
            Queue.RemoveAt(0);
            return item;
        }
    }

    public int Count() => Queue.Count;
    public T[] GetCollection => [.. Queue];

    public void Clear()
    {
        lock (Queue) Queue.Clear();
    }

    public void ForEach(Action<T> action)
    {
        lock (Queue)
        {
            foreach (var item in Queue) action(item);
        }
    }

    public T this[int index]
    {
        get
        {
            lock (Queue) return Queue[index];
        }
    }
}