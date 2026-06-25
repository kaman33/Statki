using Statki_Game;

namespace Statki_Game.Tests;

public class GraStatkiTests
{
    [Fact]
    public void OddajStrzal_GdyGraczTrafi_ZostawiaTureTemuSamemuGraczowi()
    {
        GraStatki gra = new();
        SegmentStatku segment = gra.PlanszaGracz2.Statki[0].Segmenty[0];

        GraStatki.WynikRuchu wynik = gra.OddajStrzal(GraStatki.Gracz.Gracz1, segment.X, segment.Y);

        Assert.NotEqual(Plansza.WynikStrzalu.Pudlo, wynik.Wynik);
        Assert.Equal(GraStatki.Gracz.Gracz1, gra.AktualnyGracz);
    }

    [Fact]
    public void OddajStrzal_GdyGraczPudluje_PrzekazujeTurePrzeciwnikowi()
    {
        GraStatki gra = new();
        Pole pustePole = ZnajdzPustePole(gra.PlanszaGracz2);

        GraStatki.WynikRuchu wynik = gra.OddajStrzal(GraStatki.Gracz.Gracz1, pustePole.X, pustePole.Y);

        Assert.Equal(Plansza.WynikStrzalu.Pudlo, wynik.Wynik);
        Assert.Equal(GraStatki.Gracz.Gracz2, gra.AktualnyGracz);
    }

    private static Pole ZnajdzPustePole(Plansza plansza)
    {
        for (int y = 0; y < Plansza.WymiarY; y++)
        {
            for (int x = 0; x < Plansza.WymiarX; x++)
            {
                if (plansza.ZnajdzStatekNaPolu(x, y) is null)
                {
                    return new Pole(x, y);
                }
            }
        }

        throw new InvalidOperationException("Nie znaleziono pustego pola.");
    }
}
