namespace CreepyUtil.Archipelago;

public class LimitedQueue<T>(int limit)
{
    public int Limit = limit; 
    private Queue<T> Queue = [];
    private int LocalLimit => Limit == -1 ? 200 : Limit;

    public bool Add(T t, Action<T>? removedObj = null)
    {
        Queue.Enqueue(t);
        return Update(removedObj);
    }

    public bool Update(Action<T>? removedObj = null)
    {
        var removed = Queue.Count > LocalLimit;
        while (Queue.Count > LocalLimit)
        {
            var objRemoved = Queue.Dequeue();
            removedObj?.Invoke(objRemoved);
        }

        return removed;
    }
    
    public bool Enqueue(T t) => Add(t);
    public T Dequeue() => Queue.Dequeue();
    public int Count() => Queue.Count;
    public Queue<T> GetQueue => Queue;
    public void Clear() => Queue.Clear();
    
    public void ForEach(Action<T> action)
    {
        try
        {
            foreach (var item in Queue)
            {
                action(item);
            }
        }
        catch (InvalidOperationException)
        {
            //ignored
        }
    }
}