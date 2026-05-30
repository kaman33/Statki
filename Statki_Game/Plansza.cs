namespace Statki_Game;

public class Plansza
{
    private const int WYMIAR_X = 10;
    private const int WYMIAR_Y = 10;


    private List<Statek> Statki;
    private List<Pole> StrzelonePole;

    public Plansza()
    {
        Statki = new List<Statek>();
        StrzelonePole = new List<Pole>();
    }

    private bool CzyStatekMiesciSieNaPlanszy(Statek statek)
    {
        foreach (SegmentStatku segment in statek.Segmenty)
        {
            if (segment.X < 0 || segment.X >= WYMIAR_X)
            {
                return false;
            }
            if (segment.Y < 0 || segment.Y >= WYMIAR_Y)
            {
                return false;
            }
        }
        return true;
    }

    private bool CzySegmentySieDotykaja(SegmentStatku pierwszy, SegmentStatku drugi)
    {
        int roznicaX = Math.Abs(pierwszy.X - drugi.X);
        int roznicaY = Math.Abs(pierwszy.Y - drugi.Y);
        
        return roznicaX <= 1 &&  roznicaY <= 1;
    }
    
    private bool CzyStatekDotykaInnego(Statek nowyStatek)
    {
        foreach (Statek istniejacyStatek in Statki)
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
        
        Statki.Add(statek);
        return true;
    }

    public enum WynikStrzalu
    {
        Pudlo, Trafiony, Zatopiony, JuzStrzelono, PozaPlansza
    }

    private bool CzyPoleNaPlanszy(int x, int y)
    {
        return x >= 0 && x < WYMIAR_X && y >= 0 && y < WYMIAR_Y;
    }

    private bool CzyPoleByloStrzelone(int x, int y)
    {
        foreach (Pole pole in StrzelonePole)
        {
            if (pole.X == x && pole.Y == y)
            {
                return true;
            }
        }

        return false;
    }
    
    public WynikStrzalu OddajStrzal(int x, int y)
    {
        if (!CzyPoleNaPlanszy(x, y))
        {
            return WynikStrzalu.PozaPlansza;
        }

        if (CzyPoleByloStrzelone(x, y))
        {
            return WynikStrzalu.JuzStrzelono;
        }
        
        StrzelonePole.Add(new Pole(x, y));

        foreach (Statek statek in Statki)
        {
            if (statek.CzyZajmujePole(x, y))
            {
                statek.TrafionyStatek(x, y);

                if (statek.CzyZatopiony())
                {
                    return WynikStrzalu.Zatopiony;
                }

                return WynikStrzalu.Trafiony;
            }

        } 
        return WynikStrzalu.Pudlo;
    }


    public bool CzyWszystkieStatkiZatopione()
    {
        return Statki.All(statek => statek.CzyZatopiony());
    }
    
   


}