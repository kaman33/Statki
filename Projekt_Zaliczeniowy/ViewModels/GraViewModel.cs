using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using Projekt_Zaliczeniowy.Data;
using Projekt_Zaliczeniowy.Network;
using Statki_Game;
using Statki_Network;

namespace Projekt_Zaliczeniowy.ViewModels;

public class GraViewModel : ViewModelBase
{
    private readonly GameResultRepository _gameResultRepository;
    private readonly GraStatki _gra;
    private readonly StatkiNetworkClient _networkClient;
    private readonly Random _random;
    private int _liczbaStrzalow;
    private int? _numerGraczaSieciowego;
    private TrybGry _trybGry;
    private bool _wynikZapisany;
    private string _statusSieci;
    private string _status;

    public GraViewModel()
    {
        _gameResultRepository = new GameResultRepository();
        _gra = new GraStatki();
        _networkClient = new StatkiNetworkClient();
        _random = new Random();
        _trybGry = TrybGry.Bot;
        _status = "Kliknij na pole przeciwnika";
        _statusSieci = "Nie polaczono";
        PolaGracza = new ObservableCollection<PolePlanszyViewModel>();
        PolaPrzeciwnika = new ObservableCollection<PolePlanszyViewModel>();
        HistoriaGier = new ObservableCollection<GameResult>();
        StrzelCommand = new RelayCommand(Strzel, CzyMoznaStrzelic);
        NowaGraCommand = new RelayCommand(_ => RozpocznijNowaGre());
        TrybBotCommand = new RelayCommand(_ => UstawTryb(TrybGry.Bot));
        TrybMultiplayerCommand = new RelayCommand(_ => UstawTryb(TrybGry.Multiplayer));
        PolaczCommand = new RelayCommand(_ => _ = PolaczZSerweremAsync(), _ => !_networkClient.IsConnected);

        _networkClient.MessageReceived += ObsluzWiadomoscSieciowa;
        _networkClient.Disconnected += ObsluzRozlaczenie;

        UtworzPolaPlansz();
        WczytajHistorieGier();
        OdswiezPlansze();
    }

    public ObservableCollection<PolePlanszyViewModel> PolaGracza { get; }
    public ObservableCollection<PolePlanszyViewModel> PolaPrzeciwnika { get; }
    public ObservableCollection<GameResult> HistoriaGier { get; }
    public ICommand StrzelCommand { get; }
    public ICommand NowaGraCommand { get; }
    public ICommand TrybBotCommand { get; }
    public ICommand TrybMultiplayerCommand { get; }
    public ICommand PolaczCommand { get; }

    public enum TrybGry
    {
        Bot,
        Multiplayer
    }

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

    public string StatusSieci
    {
        get => _statusSieci;
        private set
        {
            if (_statusSieci == value)
            {
                return;
            }

            _statusSieci = value;
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

    public string AktualnaTura => _gra.AktualnyGracz == GraStatki.Gracz.Gracz1 ? "Gracz" : "Przeciwnik";
    public string TrybGryOpis => _trybGry == TrybGry.Bot ? "Bot" : "Multiplayer";

    private void UtworzPolaPlansz()
    {
        for (int y = 0; y < Plansza.WymiarY; y++)
        {
            for (int x = 0; x < Plansza.WymiarX; x++)
            {
                PolaGracza.Add(new PolePlanszyViewModel(x, y));
                PolaPrzeciwnika.Add(new PolePlanszyViewModel(x, y));
            }
        }
    }

    private bool CzyMoznaStrzelic(object? parameter)
    {
        return parameter is PolePlanszyViewModel pole
            && pole.CzyMoznaKliknac
            && _gra.AktualnyGracz == GraStatki.Gracz.Gracz1
            && _gra.Stan == GraStatki.StanGry.WTrakcie
            && (_trybGry == TrybGry.Bot || _networkClient.IsConnected);
    }

    private void Strzel(object? parameter)
    {
        if (parameter is not PolePlanszyViewModel pole)
        {
            return;
        }

        if (_trybGry == TrybGry.Multiplayer)
        {
            _ = StrzelMultiplayerAsync(pole);
            return;
        }

        StrzelZBotem(pole);
    }

    private void StrzelZBotem(PolePlanszyViewModel pole)
    {
        GraStatki.WynikRuchu ruchGracza = _gra.OddajStrzal(GraStatki.Gracz.Gracz1, pole.X, pole.Y);
        Plansza.WynikStrzalu wynik = ruchGracza.Wynik;
        if (wynik is Plansza.WynikStrzalu.Pudlo
            or Plansza.WynikStrzalu.Trafiony
            or Plansza.WynikStrzalu.Zatopiony)
        {
            LiczbaStrzalow++;
        }

        Status = $"Gracz: {wynik}";
        OdswiezPlansze();
        ZapiszWynikJesliGraZakonczona();

        if (_gra.Stan == GraStatki.StanGry.WTrakcie && _gra.AktualnyGracz == GraStatki.Gracz.Gracz2)
        {
            WykonajRuchPrzeciwnika();
        }

        OdswiezKomendyITure();
    }

    private async Task StrzelMultiplayerAsync(PolePlanszyViewModel pole)
    {
        if (!_networkClient.IsConnected)
        {
            StatusSieci = "Najpierw polacz sie z serwerem.";
            OdswiezKomendyITure();
            return;
        }

        Status = $"Wyslano strzal: {pole.X}, {pole.Y}";
        await WyslijStrzalDoSerweraAsync(pole.X, pole.Y);
        OdswiezKomendyITure();
    }

    private async Task PolaczZSerweremAsync()
    {
        try
        {
            _trybGry = TrybGry.Multiplayer;
            OnPropertyChanged(nameof(TrybGryOpis));
            StatusSieci = "Laczenie z 127.0.0.1:5000...";
            await _networkClient.ConnectAsync("127.0.0.1", 5000);
            StatusSieci = "Polaczono z serwerem";
            OdswiezKomendyITure();
        }
        catch (SocketException exception)
        {
            StatusSieci = $"Blad polaczenia: {exception.Message}";
        }
        catch (IOException exception)
        {
            StatusSieci = $"Blad polaczenia: {exception.Message}";
        }
    }

    private async Task WyslijStrzalDoSerweraAsync(int x, int y)
    {
        if (!_networkClient.IsConnected)
        {
            return;
        }

        try
        {
            await _networkClient.SendShotAsync(x, y);
        }
        catch (IOException exception)
        {
            StatusSieci = $"Blad wysylania: {exception.Message}";
        }
    }

    private void ObsluzWiadomoscSieciowa(NetworkMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (message.Type == NetworkMessageType.Welcome)
            {
                _numerGraczaSieciowego = message.Player;
                StatusSieci = $"{message.Text} Tura: Gracz {message.CurrentPlayer}";
            }
            else if (message.Type == NetworkMessageType.ShotResult)
            {
                StatusSieci = $"Serwer: {message.Text}. Tura: Gracz {message.CurrentPlayer}";
                Status = $"Serwer: {message.Result}";
            }
            else if (message.Type == NetworkMessageType.Error)
            {
                StatusSieci = $"Serwer: {message.Text}";
            }

            OdswiezKomendyITure();
        });
    }

    private void UstawTryb(TrybGry trybGry)
    {
        if (_trybGry == trybGry)
        {
            return;
        }

        _trybGry = trybGry;
        RozpocznijNowaGre();
        Status = _trybGry == TrybGry.Bot
            ? "Tryb gry z botem"
            : "Tryb multiplayer. Uruchom serwer i kliknij Polacz.";
        OnPropertyChanged(nameof(TrybGryOpis));
        OdswiezKomendyITure();
    }

    private void ObsluzRozlaczenie(string reason)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _numerGraczaSieciowego = null;
            StatusSieci = reason;
            OdswiezKomendyITure();
        });
    }

    private void RozpocznijNowaGre()
    {
        _gra.RozpocznijNowaGre();
        LiczbaStrzalow = 0;
        _wynikZapisany = false;
        Status = "Kliknij na pole przeciwnika";
        OdswiezPlansze();
        OdswiezKomendyITure();
    }

    private void WykonajRuchPrzeciwnika()
    {
        PolePlanszyViewModel? pole = WylosujPoleDoStrzalu(PolaGracza);
        if (pole is null)
        {
            return;
        }

        GraStatki.WynikRuchu ruchPrzeciwnika = _gra.OddajStrzal(GraStatki.Gracz.Gracz2, pole.X, pole.Y);
        Status = $"Gracz: {Status.Replace("Gracz: ", string.Empty)} | Przeciwnik: {ruchPrzeciwnika.Wynik}";
        OdswiezPlansze();
        ZapiszWynikJesliGraZakonczona();
    }

    private PolePlanszyViewModel? WylosujPoleDoStrzalu(IEnumerable<PolePlanszyViewModel> pola)
    {
        List<PolePlanszyViewModel> dostepnePola = pola
            .Where(pole => pole.StanPola is Plansza.StanPola.Puste or Plansza.StanPola.Statek)
            .ToList();

        if (dostepnePola.Count == 0)
        {
            return null;
        }

        return dostepnePola[_random.Next(dostepnePola.Count)];
    }

    private void OdswiezPlansze()
    {
        Plansza planszaGracza = _gra.PlanszaGracz1;
        Plansza planszaPrzeciwnika = _gra.PlanszaGracz2;

        foreach (PolePlanszyViewModel pole in PolaGracza)
        {
            pole.StanPola = planszaGracza.PobierzStanPola(pole.X, pole.Y, pokazNieodkryteStatki: true);
        }

        foreach (PolePlanszyViewModel pole in PolaPrzeciwnika)
        {
            pole.StanPola = planszaPrzeciwnika.PobierzStanPola(pole.X, pole.Y);
        }
    }

    private void OdswiezKomendyITure()
    {
        OnPropertyChanged(nameof(AktualnaTura));
        OnPropertyChanged(nameof(TrybGryOpis));

        if (StrzelCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }

        if (PolaczCommand is RelayCommand polaczCommand)
        {
            polaczCommand.RaiseCanExecuteChanged();
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
