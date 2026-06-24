using System.Collections.ObjectModel;
using System.Windows.Input;
using Statki_Game;

namespace Projekt_Zaliczeniowy.ViewModels;

public class GraViewModel : ViewModelBase
{
    private readonly GraStatki _gra;
    private string _status;

    public GraViewModel()
    {
        _gra = new GraStatki();
        _status = "Kliknij na pole przeciwnika";
        PolaPrzeciwnika = new ObservableCollection<PolePlanszyViewModel>();
        StrzelCommand = new RelayCommand(Strzel, CzyMoznaStrzelic);

        UtworzPolaPrzeciwnika();
        OdswiezPlanszePrzeciwnika();
    }

    public ObservableCollection<PolePlanszyViewModel> PolaPrzeciwnika { get; }
    public ICommand StrzelCommand { get; }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    private void UtworzPolaPrzeciwnika()
    {
        for (int y = 0; y < Plansza.WymiarY; y++)
        {
            for (int x = 0; x < Plansza.WymiarX; x++)
            {
                PolaPrzeciwnika.Add(new PolePlanszyViewModel(x, y));
            }
        }
    }

    private bool CzyMoznaStrzelic(object? parameter)
    {
        return parameter is PolePlanszyViewModel pole
            && pole.CzyMoznaKliknac
            && _gra.Stan == GraStatki.StanGry.WTrakcie;
    }

    private void Strzel(object? parameter)
    {
        if (parameter is not PolePlanszyViewModel pole)
        {
            return;
        }

        Plansza.WynikStrzalu wynik = _gra.Gracz1Strzela(pole.X, pole.Y);
        Status = wynik.ToString();
        OdswiezPlanszePrzeciwnika();

        if (StrzelCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }

    private void OdswiezPlanszePrzeciwnika()
    {
        Plansza planszaPrzeciwnika = _gra.PlanszaGracz2;

        foreach (PolePlanszyViewModel pole in PolaPrzeciwnika)
        {
            pole.StanPola = planszaPrzeciwnika.PobierzStanPola(pole.X, pole.Y);
        }
    }
}
