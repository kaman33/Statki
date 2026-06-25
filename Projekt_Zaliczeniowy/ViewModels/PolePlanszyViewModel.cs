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
            OnPropertyChanged(nameof(Symbol));
            OnPropertyChanged(nameof(KolorSymbolu));
            OnPropertyChanged(nameof(CzyMoznaKliknac));
        }
    }

    public Brush Kolor => StanPola switch
    {
        Plansza.StanPola.Statek => new SolidColorBrush(Color.FromRgb(255, 229, 217)),
        Plansza.StanPola.Pudlo => new SolidColorBrush(Color.FromRgb(235, 241, 255)),
        Plansza.StanPola.Trafiony => new SolidColorBrush(Color.FromRgb(255, 235, 222)),
        Plansza.StanPola.Zatopiony => new SolidColorBrush(Color.FromRgb(255, 219, 205)),
        _ => Brushes.White
    };

    public string Symbol => StanPola switch
    {
        Plansza.StanPola.Pudlo => "•",
        Plansza.StanPola.Trafiony => "×",
        Plansza.StanPola.Zatopiony => "×",
        _ => string.Empty
    };

    public Brush KolorSymbolu => StanPola switch
    {
        Plansza.StanPola.Pudlo => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
        Plansza.StanPola.Trafiony => new SolidColorBrush(Color.FromRgb(255, 54, 21)),
        Plansza.StanPola.Zatopiony => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
        _ => Brushes.Transparent
    };

    public bool CzyMoznaKliknac => StanPola == Plansza.StanPola.Puste;
}
