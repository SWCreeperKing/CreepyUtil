using static CreepyUtil.Direction;

namespace CreepyUtil;

public readonly struct Pos(int x = 0, int y = 0)
{
    public static readonly Pos Zero = new();
    public static readonly Pos One = new(1, 1);
    public static readonly Pos Up = new(0, -1);
    public static readonly Pos Right = new(1, 0);
    public static readonly Pos Down = new(0, 1);
    public static readonly Pos Left = new(-1, 0);

    public readonly int X = x;
    public readonly int Y = y;

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    public Pos Flip() => new(-Y, -X);
    public Pos RotationReflection(Direction direction) => this * direction.Positional();
    public Pos Move(Direction direction) => this + direction.Positional();
    public Pos Mirror(Direction direction) => this + direction.Positional().Flip();
    public int ManhattanDistance(Pos pos2) => Math.Abs(pos2.X - X) + Math.Abs(pos2.Y - Y);
    public long Shoelace(Pos pos2) => X * pos2.Y - Y * pos2.X;
    public double Distance(Pos pos2) => Math.Sqrt(Math.Pow(pos2.X - X, 2) + Math.Pow(pos2.Y - Y, 2));

    public static Pos operator +(Pos p1, Pos p2) => new(p1.X + p2.X, p1.Y + p2.Y);
    public static Pos operator -(Pos p1, Pos p2) => new(p1.X - p2.X, p1.Y - p2.Y);
    public static Pos operator *(Pos p1, Pos p2) => new(p1.X * p2.X, p1.Y * p2.Y);
    public static Pos operator /(Pos p1, Pos p2) => new(p1.X / p2.X, p1.Y / p2.Y);

    public static bool operator ==(Pos p1, Pos p2) => p1.X == p2.X && p1.Y == p2.Y;
    public static bool operator !=(Pos p1, Pos p2) => p1.X != p2.X || p1.Y != p2.Y;

    public bool Equals(Pos other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is Pos other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static implicit operator Pos(Direction dir) => dir.Positional();
    public static implicit operator Pos((int x, int y) pos) => new(pos.x, pos.y);
    public static implicit operator (int x, int y)(Pos pos) => (pos.X, pos.Y);

    public override string ToString() => $"({X}, {Y})";
}

[Flags]
public enum Direction
{
    Center = 0,
    Up = 0b_0000_0001, // 0, 1
    Right = 0b_0000_0010, // 1, 0
    Down = 0b_0000_0100, // 0, -1
    Left = 0b_0000_1000, // -1, 0
    UpRight = Up | Right,
    UpLeft = Up | Left,
    DownRight = Down | Right,
    DownLeft = Down | Left,
}

public static partial class Ext
{
    public static bool IsVertical(this Direction dir) => dir is Up or Down;
    public static bool IsHorizontal(this Direction dir) => dir is Left or Right;
    
    public static Direction Rotate(this Direction dir, bool useCorners = false)
    {
        if (!useCorners)
        {
            return dir switch
            {
                Center => Center,
                Left => Up,
                _ => (Direction) ((int) dir << 1)
            };
        }

        return dir switch
        {
            Center => Center,
            Up => UpRight,
            Right => DownRight,
            Down => DownLeft,
            Left => UpLeft,
            UpRight => Right,
            UpLeft => Up,
            DownRight => Down,
            DownLeft => Left
        };
    }

    public static Direction RotateCC(this Direction dir, bool useCorners = false)
    {
        if (!useCorners)
        {
            return dir switch
            {
                Center => Center,
                Up => Left,
                _ => (Direction) ((int) dir >> 1)
            };
        }

        return dir switch
        {
            Center => Center,
            Up => UpLeft,
            Right => UpRight,
            Down => DownRight,
            Left => DownLeft,
            UpRight => Up,
            UpLeft => Left,
            DownRight => Right,
            DownLeft => Down,
        };
    }

    public static Direction Rotate180(this Direction dir)
        => dir switch
        {
            Center => Center,
            Up => Down,
            Right => Left,
            Down => Up,
            Left => Right,
            UpRight => DownLeft,
            UpLeft => DownRight,
            DownRight => UpLeft,
            DownLeft => UpRight,
        };

    public static Pos Positional(this Direction dir, bool leftRightReversed = false, bool upDownReversed = false)
    {
        var dx = 0;
        if (dir.HasFlag(Right))
        {
            dx = leftRightReversed ? -1 : 1;
        }
        else if (dir.HasFlag(Left))
        {
            dx = leftRightReversed ? 1 : -1;
        }

        var dy = 0;
        if (dir.HasFlag(Down))
        {
            dy = upDownReversed ? -1 : 1;
        }
        else if (dir.HasFlag(Up))
        {
            dy = upDownReversed ? 1 : -1;
        }

        return new Pos(dx, dy);
    }

    //     ^ a
    // a   |
    // --> / <-- b
    //     |
    //     v b
    public static Direction Mirror(this Direction dir)
        => dir switch
        {
            Up => Left, Right => Down, Down => Right, Left => Up,
        };

    //     ^ b
    // a   |
    // --> \ <-- b
    //     |
    //     v a
    public static Direction MirrorOther(this Direction dir)
        => dir switch
        {
            Up => Right, Right => Up, Down => Left, Left => Down,
        };

    public static Direction ToDir(this (int x, int y) dir)
        => dir switch
        {
            (0, -1) => Up,
            (1, 0) => Right,
            (0, 1) => Down,
            (-1, 0) => Left,
            _ => Center
        };
    
    //https://www.wikihow.com/Calculate-the-Area-of-a-Polygon
    //https://en.wikipedia.org/wiki/Shoelace_formula
    public static long Shoelace(this IEnumerable<(int amount, Direction dir)> list)
    {
        long x = 0, y = 0, area = 0, perimeter = 0;
        foreach (var (amount, dir) in list)
        {
            long lx = x, ly = y;
            if (dir is Up or Down)
            {
                y += amount * (dir is Up ? -1 : 1);
            }
            else
            {
                x += amount * (dir is Left ? -1 : 1);
            }

            perimeter += amount;
            area += lx * y - ly * x;
        }

        return Math.Abs(area / 2) + perimeter / 2 + 1;
    }
}