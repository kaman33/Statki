using Statki_Game;

namespace Statki_Game.Tests;

public class PlanszaTests
{
    [Fact]
    public void DodajStatek_GdyStatekWychodziPozaPlansze_ZwracaFalse()
    {
        Plansza plansza = new();
        Statek statek = new(8, 0, 4, Statek.Orientacja.Pozioma);

        bool dodano = plansza.DodajStatek(statek);

        Assert.False(dodano);
        Assert.Empty(plansza.Statki);
    }

    [Fact]
    public void DodajStatek_GdyStatekDotykaInnego_ZwracaFalse()
    {
        Plansza plansza = new();
        plansza.DodajStatek(new Statek(0, 0, 3, Statek.Orientacja.Pozioma));

        bool dodano = plansza.DodajStatek(new Statek(0, 1, 2, Statek.Orientacja.Pozioma));

        Assert.False(dodano);
        Assert.Single(plansza.Statki);
    }

    [Fact]
    public void OddajStrzal_GdyPoleByloJuzStrzelone_ZwracaJuzStrzelono()
    {
        Plansza plansza = new();

        Plansza.WynikStrzalu pierwszyStrzal = plansza.OddajStrzal(5, 5);
        Plansza.WynikStrzalu drugiStrzal = plansza.OddajStrzal(5, 5);

        Assert.Equal(Plansza.WynikStrzalu.Pudlo, pierwszyStrzal);
        Assert.Equal(Plansza.WynikStrzalu.JuzStrzelono, drugiStrzal);
    }

    [Fact]
    public void OddajStrzal_GdyZatopionoStatek_OznaczaPolaDookolaJakoStrzelone()
    {
        Plansza plansza = new();
        plansza.DodajStatek(new Statek(4, 4, 1, Statek.Orientacja.Pozioma));

        Plansza.WynikStrzalu wynik = plansza.OddajStrzal(4, 4);

        Assert.Equal(Plansza.WynikStrzalu.Zatopiony, wynik);
        Assert.Equal(Plansza.StanPola.Zatopiony, plansza.PobierzStanPola(4, 4));
        Assert.Equal(Plansza.StanPola.Pudlo, plansza.PobierzStanPola(3, 3));
        Assert.Equal(Plansza.StanPola.Pudlo, plansza.PobierzStanPola(4, 3));
        Assert.Equal(Plansza.StanPola.Pudlo, plansza.PobierzStanPola(5, 5));
        Assert.Equal(9, plansza.StrzelonePola.Count);
    }

    [Fact]
    public void CzyWszystkieStatkiZatopione_GdyWszystkieSegmentyTrafione_ZwracaTrue()
    {
        Plansza plansza = new();
        plansza.DodajStatek(new Statek(0, 0, 2, Statek.Orientacja.Pozioma));

        plansza.OddajStrzal(0, 0);
        plansza.OddajStrzal(1, 0);

        Assert.True(plansza.CzyWszystkieStatkiZatopione());
    }
}
