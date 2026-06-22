namespace Statki_Game;

public class Statek
{
    public enum Orientacja
    {
        Pozioma,
        Pionowa
    }

    private readonly List<SegmentStatku> _segmenty;

    public int X { get; }
    public int Y { get; }
    public int Wielkosc { get; }
    public Orientacja OrientacjaStatku { get; }
    public IReadOnlyList<SegmentStatku> Segmenty => _segmenty;

    public Statek(int x, int y, int wielkosc, Orientacja orientacjaStatku)
    {
        if (wielkosc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wielkosc), "Statek musi miec przynajmniej jeden segment.");
        }

        X = x;
        Y = y;
        Wielkosc = wielkosc;
        OrientacjaStatku = orientacjaStatku;
        _segmenty = new List<SegmentStatku>();
        UtworzSegmenty();
    }

    private void UtworzSegmenty()
    {
        for (int i = 0; i < Wielkosc; i++)
        {
            int segmentX = OrientacjaStatku == Orientacja.Pozioma ? X + i : X;
            int segmentY = OrientacjaStatku == Orientacja.Pionowa ? Y + i : Y;
            _segmenty.Add(new SegmentStatku(segmentX, segmentY));
        }
    }

    public bool CzyZajmujePole(int x, int y)
    {
        return _segmenty.Any(segment => segment.X == x && segment.Y == y);
    }

    public void TrafionyStatek(int x, int y)
    {
        SegmentStatku? segment = _segmenty.FirstOrDefault(segment => segment.X == x && segment.Y == y);
        segment?.Traf();
    }

    public bool CzyZatopiony()
    {
        return _segmenty.All(segment => segment.CzyTrafiony);
    }
}
