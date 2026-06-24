using System.Windows.Media;
using Statki_Game;

namespace Projekt_Zaliczeniowy.ViewModels;

public class PolePlanszyViewModel : ViewModelBase
{
    private Plansza.StanPola _stanPola;

    public PolePlanszyViewModel(int x, int y)
    {
        X = x;
        Y = y;
        _stanPola = Plansza.StanPola.Puste;
    }

    public int X { get; }
    public int Y { get; }

    public Plansza.StanPola StanPola
    {
        get => _stanPola;
        set
        {
            if (_stanPola == value)
            {
                return;
            }

            _stanPola = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Kolor));
            OnPropertyChanged(nameof(CzyMoznaKliknac));
        }
    }

    public Brush Kolor => StanPola switch
    {
        Plansza.StanPola.Statek => Brushes.Gray,
        Plansza.StanPola.Pudlo => Brushes.LightBlue,
        Plansza.StanPola.Trafiony => Brushes.Red,
        Plansza.StanPola.Zatopiony => Brushes.DarkRed,
        _ => Brushes.White
    };

    public bool CzyMoznaKliknac => StanPola == Plansza.StanPola.Puste;
}
