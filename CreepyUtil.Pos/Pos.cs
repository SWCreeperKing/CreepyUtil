using static CreepyUtil.NodeDirection;

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
    public Pos RotationReflection(NodeDirection direction) => this * direction.Positional();
    public Pos Move(NodeDirection direction) => this + direction.Positional();
    public Pos Mirror(NodeDirection direction) => this + direction.Positional().Flip();
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

    public static implicit operator Pos(NodeDirection dir) => dir.Positional();
    public static implicit operator Pos((int x, int y) pos) => new(pos.x, pos.y);
    public static implicit operator (int x, int y)(Pos pos) => (pos.X, pos.Y);

    public override string ToString() => $"({X}, {Y})";
}

[Flags]
public enum NodeDirection
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
    public static NodeDirection Rotate(this NodeDirection dir, bool useCorners = false)
    {
        if (!useCorners)
        {
            return dir switch
            {
                Center => Center,
                Left => Up,
                _ => (NodeDirection) ((int) dir << 1)
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

    public static NodeDirection RotateCC(this NodeDirection dir, bool useCorners = false)
    {
        if (!useCorners)
        {
            return dir switch
            {
                Center => Center,
                Up => Left,
                _ => (NodeDirection) ((int) dir >> 1)
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

    public static NodeDirection Rotate180(this NodeDirection dir)
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

    public static Pos Positional(this NodeDirection dir, bool leftRightReversed = false, bool upDownReversed = false)
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
    public static NodeDirection Mirror(this NodeDirection dir)
        => dir switch
        {
            Up => Left, Right => Down, Down => Right, Left => Up,
        };

    //     ^ b
    // a   |
    // --> \ <-- b
    //     |
    //     v a
    public static NodeDirection MirrorOther(this NodeDirection dir)
        => dir switch
        {
            Up => Right, Right => Up, Down => Left, Left => Down,
        };

    public static NodeDirection ToDir(this (int x, int y) dir)
        => dir switch
        {
            (0, -1) => Up,
            (1, 0) => Right,
            (0, 1) => Down,
            (-1, 0) => Left,
            _ => Center
        };
}