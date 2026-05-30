namespace Statki_Game;

public class GraStatki
{

    private Plansza _planszaGracz1;
    private Plansza _planszaGracz2;

    public GraStatki()
    {
        _planszaGracz1 = new Plansza();
        _planszaGracz2 = new Plansza();

        UstawStatkiTestowe();
        
    }

    private void UstawStatkiTestowe()
    {
        _planszaGracz1.DodajStatek(new Statek(0, 0, 4, Statek.Orientacja.Pozioma));
        _planszaGracz1.DodajStatek(new Statek(0, 2, 3, Statek.Orientacja.Pozioma));

        _planszaGracz2.DodajStatek(new Statek(0, 0, 4, Statek.Orientacja.Pozioma));
        _planszaGracz2.DodajStatek(new Statek(0, 2, 3, Statek.Orientacja.Pozioma));
        
    }
    
    public Plansza.WynikStrzalu Gracz1Strzela(int x, int y)
    {
        return _planszaGracz2.OddajStrzal(x, y);
    }
    
    public Plansza.WynikStrzalu Gracz2Strzela(int x, int y)
    {
        return _planszaGracz1.OddajStrzal(x, y);
    }
    
    
    
}