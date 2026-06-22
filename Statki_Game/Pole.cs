namespace Statki_Game;

public class Pole : IEquatable<Pole>
{
    public int X { get; }
    public int Y { get; }

    public Pole(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Pole? other)
    {
        return other is not null && X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Pole);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}
