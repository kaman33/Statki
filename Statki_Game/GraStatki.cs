namespace Statki_Game;

public class GraStatki
{
    private static readonly int[] StandardowaFlota = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

    private readonly Random _random;

    private Plansza _planszaGracz1 = new();
    private Plansza _planszaGracz2 = new();

    public Gracz AktualnyGracz { get; private set; }
    public StanGry Stan { get; private set; }
    public Gracz? Zwyciezca { get; private set; }

    public Plansza PlanszaGracz1 => _planszaGracz1;
    public Plansza PlanszaGracz2 => _planszaGracz2;

    public GraStatki()
    {
        _random = new Random();
        RozpocznijNowaGre();
    }

    public enum Gracz
    {
        Gracz1,
        Gracz2
    }

    public enum StanGry
    {
        WTrakcie,
        Zakonczona
    }

    public sealed class WynikRuchu
    {
        public WynikRuchu(Gracz strzelajacy, Gracz cel, Plansza.WynikStrzalu wynik, bool koniecGry, Gracz? zwyciezca)
        {
            Strzelajacy = strzelajacy;
            Cel = cel;
            Wynik = wynik;
            KoniecGry = koniecGry;
            Zwyciezca = zwyciezca;
        }

        public Gracz Strzelajacy { get; }
        public Gracz Cel { get; }
        public Plansza.WynikStrzalu Wynik { get; }
        public bool KoniecGry { get; }
        public Gracz? Zwyciezca { get; }
    }

    public void RozpocznijNowaGre()
    {
        _planszaGracz1 = new Plansza();
        _planszaGracz2 = new Plansza();

        RozstawFloteLosowo(_planszaGracz1);
        RozstawFloteLosowo(_planszaGracz2);

        AktualnyGracz = Gracz.Gracz1;
        Stan = StanGry.WTrakcie;
        Zwyciezca = null;
    }

    public WynikRuchu OddajStrzal(Gracz gracz, int x, int y)
    {
        Gracz cel = PobierzPrzeciwnika(gracz);

        if (Stan == StanGry.Zakonczona)
        {
            return new WynikRuchu(gracz, cel, Plansza.WynikStrzalu.GraZakonczona, true, Zwyciezca);
        }

        if (gracz != AktualnyGracz)
        {
            return new WynikRuchu(gracz, cel, Plansza.WynikStrzalu.NieTwojaTura, false, null);
        }

        Plansza planszaPrzeciwnika = PobierzPlansze(cel);
        Plansza.WynikStrzalu wynik = planszaPrzeciwnika.OddajStrzal(x, y);

        if (planszaPrzeciwnika.CzyWszystkieStatkiZatopione())
        {
            Stan = StanGry.Zakonczona;
            Zwyciezca = gracz;
            return new WynikRuchu(gracz, cel, wynik, true, Zwyciezca);
        }

        if (CzyStrzalZmieniaTure(wynik))
        {
            AktualnyGracz = cel;
        }

        return new WynikRuchu(gracz, cel, wynik, false, null);
    }

    public Plansza.WynikStrzalu Gracz1Strzela(int x, int y)
    {
        return OddajStrzalBezPilnowaniaTury(Gracz.Gracz1, x, y).Wynik;
    }

    public Plansza.WynikStrzalu Gracz2Strzela(int x, int y)
    {
        return OddajStrzalBezPilnowaniaTury(Gracz.Gracz2, x, y).Wynik;
    }

    public Statek? ZnajdzStatekGracz1NaPolu(int x, int y)
    {
        return _planszaGracz1.ZnajdzStatekNaPolu(x, y);
    }

    public Statek? ZnajdzStatekGracz2NaPolu(int x, int y)
    {
        return _planszaGracz2.ZnajdzStatekNaPolu(x, y);
    }

    public Plansza PobierzPlansze(Gracz gracz)
    {
        return gracz == Gracz.Gracz1 ? _planszaGracz1 : _planszaGracz2;
    }

    public static Gracz PobierzPrzeciwnika(Gracz gracz)
    {
        return gracz == Gracz.Gracz1 ? Gracz.Gracz2 : Gracz.Gracz1;
    }

    private static bool CzyStrzalZmieniaTure(Plansza.WynikStrzalu wynik)
    {
        return wynik == Plansza.WynikStrzalu.Pudlo;
    }

    private WynikRuchu OddajStrzalBezPilnowaniaTury(Gracz gracz, int x, int y)
    {
        Gracz cel = PobierzPrzeciwnika(gracz);

        if (Stan == StanGry.Zakonczona)
        {
            return new WynikRuchu(gracz, cel, Plansza.WynikStrzalu.GraZakonczona, true, Zwyciezca);
        }

        Plansza planszaPrzeciwnika = PobierzPlansze(cel);
        Plansza.WynikStrzalu wynik = planszaPrzeciwnika.OddajStrzal(x, y);

        if (planszaPrzeciwnika.CzyWszystkieStatkiZatopione())
        {
            Stan = StanGry.Zakonczona;
            Zwyciezca = gracz;
            return new WynikRuchu(gracz, cel, wynik, true, Zwyciezca);
        }

        return new WynikRuchu(gracz, cel, wynik, false, null);
    }

    private void RozstawFloteLosowo(Plansza plansza)
    {
        foreach (int wielkosc in StandardowaFlota)
        {
            bool dodano = false;

            for (int proba = 0; proba < 1000 && !dodano; proba++)
            {
                Statek.Orientacja orientacja = _random.Next(2) == 0
                    ? Statek.Orientacja.Pozioma
                    : Statek.Orientacja.Pionowa;

                int maxX = orientacja == Statek.Orientacja.Pozioma
                    ? Plansza.WymiarX - wielkosc
                    : Plansza.WymiarX - 1;

                int maxY = orientacja == Statek.Orientacja.Pionowa
                    ? Plansza.WymiarY - wielkosc
                    : Plansza.WymiarY - 1;

                int x = _random.Next(maxX + 1);
                int y = _random.Next(maxY + 1);

                dodano = plansza.DodajStatek(new Statek(x, y, wielkosc, orientacja));
            }

            if (!dodano)
            {
                throw new InvalidOperationException("Nie udalo sie rozstawic standardowej floty.");
            }
        }
    }
}
