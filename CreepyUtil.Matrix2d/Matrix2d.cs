using System.Text;
using CreepyUtil.Pos;
using static CreepyUtil.Pos.Direction;
using static CreepyUtil.Pos.Pos;

namespace CreepyUtil.Matrix2d;

public class Matrix2d<T>
{
    public readonly T[] Array;
    public readonly (int w, int h) Size;
    public readonly int TrueSize;

    public Matrix2d(int wh) : this(wh, wh) { }

    public Matrix2d(int w, int h)
    {
        Size = (w, h);
        Array = new T[TrueSize = w * h];
    }

    public Matrix2d((int w, int h) size)
    {
        Size = size;
        Array = new T[TrueSize = size.w * size.h];
    }

    public Matrix2d(IReadOnlyList<T[]> inArray)
    {
        Size = (inArray.Max(t => t.Length), inArray.Count);
        Array = new T[TrueSize = Size.w * Size.h];

        for (var y = 0; y < inArray.Count; y++)
        for (var x = 0; x < inArray[y].Length; x++)
            this[x, y] = inArray[y][x];
    }

    public Matrix2d(IReadOnlyCollection<T> inArray, int w, int h)
    {
        if (inArray.Count != w * h) throw new ArgumentException("width and height of array does not match");
        Size = (w, h);
        TrueSize = inArray.Count;
        Array = inArray.ToArray();
    }

    public Matrix2d(IReadOnlyCollection<T> inArray, (int w, int h) size) : this(inArray, size.w, size.h) { }

    public T this[int index]
    {
        get => Array[index];
        set => Array[index] = value;
    }

    public T this[int x, int y]
    {
        get => Array[TranslatePosition(x, y)];
        set => Array[TranslatePosition(x, y)] = value;
    }

    public T this[(int x, int y) pos]
    {
        get => Array[TranslatePosition(pos)];
        set => Array[TranslatePosition(pos)] = value;
    }

    public T this[Pos.Pos pos]
    {
        get => Array[TranslatePosition(pos.X, pos.Y)];
        set => Array[TranslatePosition(pos.X, pos.Y)] = value;
    }

    public IEnumerable<(int, int, T)> Iterate()
    {
        for (var y = 0; y < Size.h; y++)
        for (var x = 0; x < Size.w; x++)
            yield return (x, y, this[x, y]);
    }

    public Matrix2d<T> Iterate(Action<Matrix2d<T>, T, int> action)
    {
        for (var i = 0; i < TrueSize; i++) action(this, this[i], i);

        return this;
    }

    public Matrix2d<T> Iterate(Action<Matrix2d<T>, T, int, int> action)
    {
        for (var y = 0; y < Size.h; y++)
        for (var x = 0; x < Size.w; x++)
            action(this, this[x, y], x, y);

        return this;
    }

    public Matrix2d<TO> MatrixSelect<TO>(Func<T, TO> select)
    {
        return new Matrix2d<TO>(Array.Select(select).ToArray(), Size.w, Size.h);
    }

    public Matrix2d<TO> MatrixSelect<TO>(Func<Matrix2d<T>, T, int, TO> select)
    {
        return new Matrix2d<TO>(Array.Select((t, i) => select(this, t, i)).ToArray(), Size.w, Size.h);
    }

    public Matrix2d<TO> MatrixSelect<TO>(Func<Matrix2d<T>, T, Pos.Pos, TO> select)
    {
        return new Matrix2d<TO>(Array.Select((t, i) => select(this, t, TranslatePosition(i))).ToArray(), Size.w,
            Size.h);
    }

    public IEnumerable<TO> Select<TO>(Func<Matrix2d<T>, T, int, int, TO> select)
    {
        return Array.Select((t, i) =>
        {
            var (x, y) = TranslatePosition(i);
            return select(this, t, x, y);
        });
    }

    public bool PositionExists(Pos.Pos pos) { return PositionExists(pos.X, pos.Y); }

    public bool PositionExists(int x, int y) { return x >= 0 && y >= 0 && x < Size.w && y < Size.h; }

    public List<Pos.Pos> WhereInCircle(Pos.Pos pos, Predicate<T> condition, bool corners = true)
    {
        List<Pos.Pos> posList = [];
        foreach (var dxy in corners ? SurroundDiagonal : Surround)
        {
            var next = pos + dxy;
            if (!PositionExists(next) || !condition(this[next])) continue;
            posList.Add(next);
        }

        return posList;
    }

    public bool[] MatchInCircle(Pos.Pos pos, Predicate<T> condition, bool corners = true)
    {
        List<bool> bools = [];

        foreach (var dxy in corners ? SurroundDiagonal : Surround)
        {
            var next = pos + dxy;
            bools.Add(PositionExists(next) && condition(this[next]));
        }

        return bools.ToArray();
    }

    public bool AnyTrueMatchInCircle(Pos.Pos pos, Predicate<T> condition, bool corners = true)
    {
        return MatchInCircle(pos, condition, corners).Any(b => b);
    }

    public bool AnyAllCircularMarch(int x, int y, Func<T, bool> allConditional, int ring = 1)
    {
        return March(x, y - ring, Direction.Up).All(allConditional)
               || March(x + ring, y, Direction.Right).All(allConditional)
               || March(x, y + ring, Direction.Down).All(allConditional)
               || March(x - ring, y, Direction.Left).All(allConditional);
    }

    public long[] CircularMarchAndCountWhile(int x, int y, Func<T, bool> count, int ring = 1)
    {
        return
        [
            MarchAndCountWhile(x, y - ring, Direction.Up, count),
            MarchAndCountWhile(x + ring, y, Direction.Right, count),
            MarchAndCountWhile(x, y + ring, Direction.Down, count),
            MarchAndCountWhile(x - ring, y, Direction.Left, count)
        ];
    }

    public long MarchAndCountWhile(int x, int y, Direction direction, Func<T, bool> count)
    {
        long counter = 0;
        foreach (var iterT in March(x, y, direction))
        {
            counter++;
            if (!count(iterT)) break;
        }

        return counter;
    }

    public IEnumerable<T> MarchRange(Pos.Pos pos, int length, Direction direction)
    {
        return MarchRange(pos, length, direction.Positional());
    }

    public IEnumerable<T> MarchRange(Pos.Pos pos, int length, Pos.Pos delta)
    {
        var i = 0;

        foreach (var t in March(pos, delta))
        {
            if (i++ >= length) yield break;
            yield return t;
        }
    }

    public T[]? MarchRangeArr(Pos.Pos pos, int length, Pos.Pos delta)
    {
        var next = pos;
        var arr = new T[length];
        for (var i = 0; i < length; i++)
        {
            next += delta;
            if (!PositionExists(next)) return null;
            arr[i] = this[next];
        }

        return arr;
    }

    public IEnumerable<T> March(Pos.Pos pos, Direction direction) { return March(pos, direction.Positional()); }
    public IEnumerable<T> March(int x, int y, Direction direction) { return March((x, y), direction.Positional()); }

    public IEnumerable<T> March(Pos.Pos pos, Pos.Pos delta)
    {
        var next = pos;
        while (PositionExists(next += delta)) yield return this[next];
    }

    public Pos.Pos Find(T t) { return Find(tt => tt!.Equals(t)); }

    public Pos.Pos Find(Func<T, bool> find)
    {
        for (var y = 0; y < Size.h; y++)
        for (var x = 0; x < Size.w; x++)
        {
            var t = this[x, y];
            if (!find(t)) continue;
            return new Pos.Pos(x, y);
        }
        
        throw new ArgumentException("Could not find element");
    }


    public Pos.Pos[] FindAll(Func<T, bool> find)
    {
        List<Pos.Pos> posList = [];
        for (var y = 0; y < Size.h; y++)
        for (var x = 0; x < Size.w; x++)
        {
            var t = this[x, y];
            if (!find(t)) continue;
            posList.Add(new Pos.Pos(x, y));
        }

        return posList.ToArray();
    }

    public Pos.Pos[] FindAll(T find) { return Array.FindAllIndexesOf(find).Select(i => TranslatePosition(i)).ToArray(); }

    public Pos.Pos TranslatePosition(int index) { return new Pos.Pos(index % Size.w, (int)Math.Floor((float)index / Size.w)); }

    public int TranslatePosition((int x, int y) pos) { return TranslatePosition(pos.x, pos.y); }

    public int TranslatePosition(Pos.Pos pos) { return TranslatePosition(pos.X, pos.Y); }

    public int TranslatePosition(int x, int y)
    {
        if (!PositionExists(x, y)) throw new ArgumentException("X, Y results in out of bounds!");
        return y * Size.w + x;
    }

    public static implicit operator T[](Matrix2d<T> matrix2d) { return matrix2d.Array; }

    public override string ToString()
    {
        StringBuilder sb = new();
        for (var y = 0; y < Size.h; y++)
        {
            for (var x = 0; x < Size.w; x++) sb.Append(this[x, y]);

            sb.Append('\n');
        }

        return sb.ToString();
    }

    public string ToString<TO>(Func<T, TO> map)
    {
        StringBuilder sb = new();
        for (var y = 0; y < Size.h; y++)
        {
            for (var x = 0; x < Size.w; x++) sb.Append(map(this[x, y]));

            sb.Append('\n');
        }

        return sb.ToString();
    }

    public Matrix2d<T> Duplicate() { return MatrixSelect(t => t); }

    public Matrix2d<T> Duplicate(Func<T, T> dupeFunc) { return MatrixSelect(dupeFunc); }
}

public static class Ext
{
    public static int[] FindAllIndexesOf<T>(this IEnumerable<T> arr, T search)
    {
        return arr.Select((o, i) => Equals(search, o) ? i : -1).Where(i => i != -1).ToArray();
    }
}