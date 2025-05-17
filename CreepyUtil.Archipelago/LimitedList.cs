namespace CreepyUtil.Archipelago;

public class LimitedList<T>(int limit = -1)
{
    public static int Limit = 200;
    private List<T> List = [];
    private int LocalLimit = limit == -1 ? Limit : limit;
    public int Count => List.Count;

    public bool Add(T t, bool removeFront = true)
    {
        List.Add(t);
        var removed = List.Count > LocalLimit;
        while (List.Count > LocalLimit)
        {
            List.RemoveAt(removeFront ? 0 : List.Count - 1);
        }

        return removed;
    }

    public List<T> GetList => List;
    public void Clear() => List.Clear();

    public void ForEach(Action<T> action)
    {
        try
        {
            foreach (var item in List)
            {
                action(item);
            }
        }
        catch (InvalidOperationException)
        {
            //ignored
        }
    }

    public T this[int index] => List[index];
}