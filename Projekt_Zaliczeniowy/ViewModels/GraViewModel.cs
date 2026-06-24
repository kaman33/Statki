using System.Collections.ObjectModel;
using System.Windows.Input;
using Projekt_Zaliczeniowy.Data;
using Statki_Game;

namespace Projekt_Zaliczeniowy.ViewModels;

public class GraViewModel : ViewModelBase
{
    private readonly GameResultRepository _gameResultRepository;
    private readonly GraStatki _gra;
    private int _liczbaStrzalow;
    private bool _wynikZapisany;
    private string _status;

    public GraViewModel()
    {
        _gameResultRepository = new GameResultRepository();
        _gra = new GraStatki();
        _status = "Kliknij na pole przeciwnika";
        PolaPrzeciwnika = new ObservableCollection<PolePlanszyViewModel>();
        HistoriaGier = new ObservableCollection<GameResult>();
        StrzelCommand = new RelayCommand(Strzel, CzyMoznaStrzelic);
        NowaGraCommand = new RelayCommand(_ => RozpocznijNowaGre());

        UtworzPolaPrzeciwnika();
        WczytajHistorieGier();
        OdswiezPlanszePrzeciwnika();
    }

    public ObservableCollection<PolePlanszyViewModel> PolaPrzeciwnika { get; }
    public ObservableCollection<GameResult> HistoriaGier { get; }
    public ICommand StrzelCommand { get; }
    public ICommand NowaGraCommand { get; }

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

    public int LiczbaStrzalow
    {
        get => _liczbaStrzalow;
        private set
        {
            if (_liczbaStrzalow == value)
            {
                return;
            }

            _liczbaStrzalow = value;
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
        if (wynik is Plansza.WynikStrzalu.Pudlo
            or Plansza.WynikStrzalu.Trafiony
            or Plansza.WynikStrzalu.Zatopiony)
        {
            LiczbaStrzalow++;
        }

        Status = wynik.ToString();
        OdswiezPlanszePrzeciwnika();
        ZapiszWynikJesliGraZakonczona();

        if (StrzelCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }

    private void RozpocznijNowaGre()
    {
        _gra.RozpocznijNowaGre();
        LiczbaStrzalow = 0;
        _wynikZapisany = false;
        Status = "Kliknij na pole przeciwnika";
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

    private void ZapiszWynikJesliGraZakonczona()
    {
        if (_wynikZapisany || _gra.Stan != GraStatki.StanGry.Zakonczona || _gra.Zwyciezca is null)
        {
            return;
        }

        GameResult gameResult = new()
        {
            PlayedAt = DateTime.Now,
            Winner = _gra.Zwyciezca.Value.ToString(),
            ShotCount = LiczbaStrzalow,
            Result = "Wygrana"
        };

        _gameResultRepository.Add(gameResult);
        _wynikZapisany = true;
        Status = $"Koniec gry. Zwyciezca: {gameResult.Winner}";
        WczytajHistorieGier();
    }

    private void WczytajHistorieGier()
    {
        HistoriaGier.Clear();

        foreach (GameResult result in _gameResultRepository.GetLatestResults())
        {
            HistoriaGier.Add(result);
        }
    }
}
