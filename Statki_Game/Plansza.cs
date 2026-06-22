namespace Statki_Game;

public class Plansza
{
    public const int WymiarX = 10;
    public const int WymiarY = 10;

    private readonly List<Statek> _statki;
    private readonly HashSet<Pole> _strzelonePola;

    public IReadOnlyList<Statek> Statki => _statki;
    public IReadOnlyCollection<Pole> StrzelonePola => _strzelonePola;

    public Plansza()
    {
        _statki = new List<Statek>();
        _strzelonePola = new HashSet<Pole>();
    }

    public enum WynikStrzalu
    {
        Pudlo,
        Trafiony,
        Zatopiony,
        JuzStrzelono,
        PozaPlansza,
        NieTwojaTura,
        GraZakonczona
    }

    public enum StanPola
    {
        Puste,
        Statek,
        Pudlo,
        Trafiony,
        Zatopiony
    }

    public bool DodajStatek(Statek statek)
    {
        if (!CzyStatekMiesciSieNaPlanszy(statek))
        {
            return false;
        }

        if (CzyStatekDotykaInnego(statek))
        {
            return false;
        }

        _statki.Add(statek);
        return true;
    }

    public WynikStrzalu OddajStrzal(int x, int y)
    {
        if (!CzyPoleNaPlanszy(x, y))
        {
            return WynikStrzalu.PozaPlansza;
        }

        Pole pole = new Pole(x, y);
        if (_strzelonePola.Contains(pole))
        {
            return WynikStrzalu.JuzStrzelono;
        }

        _strzelonePola.Add(pole);

        Statek? trafionyStatek = ZnajdzStatekNaPolu(x, y);
        if (trafionyStatek is null)
        {
            return WynikStrzalu.Pudlo;
        }

        trafionyStatek.TrafionyStatek(x, y);
        if (trafionyStatek.CzyZatopiony())
        {
            OznaczPolaDookolaZatopionegoStatku(trafionyStatek);
            return WynikStrzalu.Zatopiony;
        }

        return WynikStrzalu.Trafiony;
    }

    public bool CzyWszystkieStatkiZatopione()
    {
        return _statki.Count > 0 && _statki.All(statek => statek.CzyZatopiony());
    }

    public Statek? ZnajdzStatekNaPolu(int x, int y)
    {
        return _statki.FirstOrDefault(statek => statek.CzyZajmujePole(x, y));
    }

    public Statek? ZnajdzsStatekNaPolu(int x, int y)
    {
        return ZnajdzStatekNaPolu(x, y);
    }

    public StanPola PobierzStanPola(int x, int y, bool pokazNieodkryteStatki = false)
    {
        if (!CzyPoleNaPlanszy(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Pole znajduje sie poza plansza.");
        }

        bool byloStrzelone = _strzelonePola.Contains(new Pole(x, y));
        Statek? statek = ZnajdzStatekNaPolu(x, y);

        if (!byloStrzelone)
        {
            return statek is not null && pokazNieodkryteStatki ? StanPola.Statek : StanPola.Puste;
        }

        if (statek is null)
        {
            return StanPola.Pudlo;
        }

        return statek.CzyZatopiony() ? StanPola.Zatopiony : StanPola.Trafiony;
    }

    public static bool CzyPoleNaPlanszy(int x, int y)
    {
        return x >= 0 && x < WymiarX && y >= 0 && y < WymiarY;
    }

    private bool CzyStatekMiesciSieNaPlanszy(Statek statek)
    {
        return statek.Segmenty.All(segment => CzyPoleNaPlanszy(segment.X, segment.Y));
    }

    private static bool CzySegmentySieDotykaja(SegmentStatku pierwszy, SegmentStatku drugi)
    {
        int roznicaX = Math.Abs(pierwszy.X - drugi.X);
        int roznicaY = Math.Abs(pierwszy.Y - drugi.Y);

        return roznicaX <= 1 && roznicaY <= 1;
    }

    private void OznaczPolaDookolaZatopionegoStatku(Statek statek)
    {
        foreach (SegmentStatku segment in statek.Segmenty)
        {
            for (int y = segment.Y - 1; y <= segment.Y + 1; y++)
            {
                for (int x = segment.X - 1; x <= segment.X + 1; x++)
                {
                    if (CzyPoleNaPlanszy(x, y))
                    {
                        _strzelonePola.Add(new Pole(x, y));
                    }
                }
            }
        }
    }

    private bool CzyStatekDotykaInnego(Statek nowyStatek)
    {
        foreach (Statek istniejacyStatek in _statki)
        {
            foreach (SegmentStatku nowySegment in nowyStatek.Segmenty)
            {
                foreach (SegmentStatku istniejacySegment in istniejacyStatek.Segmenty)
                {
                    if (CzySegmentySieDotykaja(nowySegment, istniejacySegment))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
