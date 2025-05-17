namespace CreepyUtil.Archipelago;

public class LimitedQueue<T>(int limit = -1)
{
    public static int Limit = 200;
    private Queue<T> Queue = [];
    private int LocalLimit = limit == -1 ? Limit : limit;

    public bool Add(T t)
    {
        Queue.Enqueue(t);
        var removed = Queue.Count > LocalLimit;
        while (Queue.Count > LocalLimit)
        {
            Queue.Dequeue();
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