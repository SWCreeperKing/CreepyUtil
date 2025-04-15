namespace CreepyUtil.BackBone;

public class LimitedQueue<T>
{
    public static int Limit = 200;
    private Queue<T> Queue = [];

    public bool Add(T t)
    {
        Queue.Enqueue(t);
        var removed = Queue.Count > Limit;
        while (Queue.Count > Limit)
        {
            Queue.Dequeue();
        }

        return removed;
    }

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