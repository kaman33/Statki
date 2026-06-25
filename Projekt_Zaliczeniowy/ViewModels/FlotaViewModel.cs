using System.Windows.Media;

namespace Projekt_Zaliczeniowy.ViewModels;

public class FlotaViewModel : ViewModelBase
{
    private bool _czyZatopiony;

    public FlotaViewModel(int rozmiar)
    {
        Rozmiar = rozmiar;
        Rzad = 5 - rozmiar;
        Segmenty = new string('■', rozmiar);
    }

    public int Rozmiar { get; }
    public int Rzad { get; }
    public string Segmenty { get; }

    public bool CzyZatopiony
    {
        get => _czyZatopiony;
        set
        {
            if (_czyZatopiony == value)
            {
                return;
            }

            _czyZatopiony = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Kolor));
        }
    }

    public Brush Kolor => CzyZatopiony
        ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
        : new SolidColorBrush(Color.FromRgb(156, 163, 175));
}
