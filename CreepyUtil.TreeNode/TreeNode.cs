namespace CreepyUtil.TreeNode;

public abstract class TreeNode<T>(string id) where T : TreeNode<T>
{
    public readonly string Id = id;

    public HashSet<T> Children;
    private Dictionary<string, T> NodeMap;
    public T Parent;
    public int Layer { get; private set; }

    public static T CreateTree(Dictionary<string, string[]> map, Func<string, T> createNew)
    {
        var treeMap = map.ToDictionary(kv => kv.Key, kv => createNew(kv.Key));

        foreach (var (id, t) in treeMap)
        {
            t.NodeMap = treeMap;
            if (map[id].Length == 0) continue;
            t.Children = map[id].Select(k => treeMap[k]).ToHashSet();

            foreach (var treeNode in t.Children) treeNode.Parent = t;
        }

        var parents = treeMap.Values.Where(n => n.Parent is null);
        if (parents.Count() > 1) throw new ArgumentException("MULTIPLE PARENTS ARE NOT ALLOWED");
        var parent = parents.ElementAt(0);
        parent.UpdateLayers();
        return parent;
    }

    public static void ParseMaps<TData>(IEnumerable<string> list, out Dictionary<string, string[]> childMap,
        out Dictionary<string, TData> dataMap, Func<string, (string, string[], TData)> parse)
    {
        childMap = [];
        dataMap = [];

        foreach (var line in list)
        {
            var (key, children, data) = parse(line);
            childMap[key] = children;
            dataMap[key] = data;
        }
    }

    public void UpdateLayers()
    {
        if (Children is null) return;

        foreach (var child in Children)
        {
            if (child.Layer != 0) throw new ArgumentException("A node can not have multiple parents");
            child.Layer = Layer + 1;
            child.UpdateLayers();
        }
    }

    public T Climb(Func<HashSet<T>, T> search, Func<HashSet<T>, bool> earlyExit = null)
    {
        var res = search(Children);
        var e = res.Children is null;
        // var e1 = (earlyExit is not null && earlyExit(res.Children));
        return res.Children is null || (earlyExit is not null && earlyExit(res.Children))
            ? res
            : res.Climb(search, earlyExit);
    }

    public IEnumerable<T> Iterate() { return NodeMap.Values; }

    public IEnumerable<T> IterateDeepestFirst()
    {
        for (var i = NodeMap.Values.Max(n => n.Layer); i > 0; i--)
            foreach (var node in NodeMap.Values.Where(n => n.Layer == i))
                yield return node;
    }
}