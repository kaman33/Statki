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
    private int _trafioneSegmenty;

    public Statek(int x, int y, int wielkosc, Orientacja orientacjaStatku)
    {
        this._x = x;
        this._y = y;
        this._wielkosc = wielkosc;
        this._orientacjaStatku = orientacjaStatku;
        this._trafioneSegmenty = 0;
    }

    public bool CzyZatopiony()
    {
        return _trafioneSegmenty == _wielkosc;
    }

    public bool CzyZajmujePole(int strzalX, int strzalY)
    {
        switch (_orientacjaStatku)
        {
            case Orientacja.Pozioma:
                return strzalY == _y && strzalX >= _x && strzalX < _x + _wielkosc;
            case Orientacja.Pionowa:
                return strzalX == _x && strzalY >= _y && strzalY < _y + _wielkosc;
            default:
                return false;
        }
    }

    public void TrafionyStatek()
    {
        if (this._trafioneSegmenty < _wielkosc)
        {
            this._trafioneSegmenty++;
        }
    }
    
}