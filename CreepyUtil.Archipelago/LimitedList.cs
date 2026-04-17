namespace CreepyUtil.Archipelago;

public class LimitedList<T>(int limit)
{
    public int Limit = limit; 
    private List<T> List = [];
    private int LocalLimit => Limit == -1 ? 200 : Limit;
    public int Count => List.Count;

    public bool Add(T t, bool removeFront = true, Action<T>? removedObj = null)
    {
        List.Add(t);
        return Update(removeFront, removedObj);
    }

    public bool Update(bool removeFront = true, Action<T>? removedObj = null)
    {
        var removed = List.Count > LocalLimit;
        while (List.Count > LocalLimit)
        {
            var index = removeFront ? 0 : List.Count - 1;
            removedObj?.Invoke(List[index]);
            List.RemoveAt(index);
        }

        return removed;
    }

    public List<T> GetList => List;
    public void Clear() => List.Clear();

    public void ForEach(Action<T> action)
    {
        try
        {
            foreach (var item in List) { action(item); }
        }
        catch (InvalidOperationException)
        {
            //ignored
        }
    }

    public T this[int index] => List[index];
}