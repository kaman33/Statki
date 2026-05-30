namespace Statki_Game;

public class Statek
{

    public enum Orientacja
    {
        Pozioma,
        Pionowa
    }
    
    private int _x;
    private int _y;
    private Orientacja _orientacjaStatku;
    private int _wielkosc;
    private List<SegmentStatku> _segmenty;
    public IReadOnlyList<SegmentStatku> Segmenty => _segmenty;
    public Statek(int x, int y, int wielkosc, Orientacja orientacjaStatku)
    {
        this._x = x;
        this._y = y;
        this._wielkosc = wielkosc;
        this._orientacjaStatku = orientacjaStatku;
        _segmenty = new List<SegmentStatku>();
        UtworzSegement();
    }
    private void UtworzSegement()
    {
        switch (_orientacjaStatku)
        {
            case Orientacja.Pozioma:
                for (int i = 0; i < _wielkosc; i++)
                {
                    _segmenty.Add(new SegmentStatku(_x + i, _y));
                }
                break;
            case Orientacja.Pionowa:
                for (int i = 0; i < _wielkosc; i++)
                {
                    _segmenty.Add(new SegmentStatku(_x, _y + i));
                }
                break;

        }
    }


    public bool CzyZajmujePole(int strzalX, int strzalY)
    {
        return _segmenty.Any(segment => segment.X == strzalX && segment.Y == strzalY);
    }

    public void TrafionyStatek(int x, int y)
    {
        foreach (SegmentStatku segment in _segmenty)
        {
            if (segment.X == x && segment.Y == y)
            {
                segment.Traf();
                return;
            }
        }
    }

    
    public bool CzyZatopiony()
    {
        return _segmenty.All(segment => segment.CzyTrafiony);
    }
    
}