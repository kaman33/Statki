using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
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
    private int _aktualnyGraczSieciowy = 1;
    private int? _numerGraczaSieciowego;
    private TrybGry _trybGry;
    private bool _botWykonujeRuch;
    private bool _pokazanoKoniecGry;
    private bool _wynikZapisany;
    private string _serverHost;
    private string _serverPort;
    private string _statusSieci;
    private string _status;

    public GraViewModel()
    {
        ClientNetworkSettings networkSettings = ClientNetworkSettings.Load();
        _gameResultRepository = new GameResultRepository();
        _gra = new GraStatki();
        _networkClient = new StatkiNetworkClient();
        _random = new Random();
        _trybGry = TrybGry.Bot;
        _serverHost = networkSettings.Host;
        _serverPort = networkSettings.Port.ToString();
        _status = "Kliknij na pole przeciwnika";
        _statusSieci = "Nie polaczono";
        PolaGracza = new ObservableCollection<PolePlanszyViewModel>();
        PolaPrzeciwnika = new ObservableCollection<PolePlanszyViewModel>();
        FlotaGracza = new ObservableCollection<FlotaViewModel>();
        FlotaPrzeciwnika = new ObservableCollection<FlotaViewModel>();
        HistoriaGier = new ObservableCollection<GameResult>();
        StrzelCommand = new RelayCommand(Strzel, CzyMoznaStrzelic);
        NowaGraCommand = new RelayCommand(_ => RozpocznijNowaGre());
        TrybBotCommand = new RelayCommand(_ => UstawTryb(TrybGry.Bot));
        TrybMultiplayerCommand = new RelayCommand(_ => UstawTryb(TrybGry.Multiplayer));
        PolaczCommand = new RelayCommand(_ => _ = PolaczZSerweremAsync(), _ => !_networkClient.IsConnected);

        _networkClient.MessageReceived += ObsluzWiadomoscSieciowa;
        _networkClient.Disconnected += ObsluzRozlaczenie;

        UtworzPolaPlansz();
        UtworzPodgladFloty();
        WczytajHistorieGier();
        OdswiezPlansze();
    }

    public ObservableCollection<PolePlanszyViewModel> PolaGracza { get; }
    public ObservableCollection<PolePlanszyViewModel> PolaPrzeciwnika { get; }
    public ObservableCollection<FlotaViewModel> FlotaGracza { get; }
    public ObservableCollection<FlotaViewModel> FlotaPrzeciwnika { get; }
    public IEnumerable<IGrouping<int, FlotaViewModel>> FlotaGraczaWedlugRzedow => FlotaGracza.GroupBy(statek => statek.Rzad);
    public IEnumerable<IGrouping<int, FlotaViewModel>> FlotaPrzeciwnikaWedlugRzedow => FlotaPrzeciwnika.GroupBy(statek => statek.Rzad);
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
    public string AktualnaTuraSieciowa => _aktualnyGraczSieciowy == 1 ? "Gracz 1" : "Gracz 2";
    public string AktualnaTuraOpis => _trybGry == TrybGry.Bot ? AktualnaTura : AktualnaTuraSieciowa;
    public string TrybGryOpis => _trybGry == TrybGry.Bot ? "Bot" : "Multiplayer";

    public string ServerHost
    {
        get => _serverHost;
        set
        {
            if (_serverHost == value)
            {
                return;
            }

            _serverHost = value;
            OnPropertyChanged();
        }
    }

    public string ServerPort
    {
        get => _serverPort;
        set
        {
            if (_serverPort == value)
            {
                return;
            }

            _serverPort = value;
            OnPropertyChanged();
            OdswiezKomendyITure();
        }
    }

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

    private void UtworzPodgladFloty()
    {
        foreach (int rozmiar in new[] { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 })
        {
            FlotaGracza.Add(new FlotaViewModel(rozmiar));
            FlotaPrzeciwnika.Add(new FlotaViewModel(rozmiar));
        }
    }

    private bool CzyMoznaStrzelic(object? parameter)
    {
        return parameter is PolePlanszyViewModel pole
            && pole.CzyMoznaKliknac
            && CzyTrybPozwalaStrzelic();
    }

    private bool CzyTrybPozwalaStrzelic()
    {
        if (_trybGry == TrybGry.Bot)
        {
            return _gra.AktualnyGracz == GraStatki.Gracz.Gracz1
                && _gra.Stan == GraStatki.StanGry.WTrakcie
                && !_botWykonujeRuch;
        }

        return _networkClient.IsConnected
            && _numerGraczaSieciowego is not null
            && _numerGraczaSieciowego == _aktualnyGraczSieciowy;
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

        _ = StrzelZBotemAsync(pole);
    }

    private async Task StrzelZBotemAsync(PolePlanszyViewModel pole)
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
            await WykonajRuchPrzeciwnikaAsync();
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
            if (!int.TryParse(ServerPort, out int port) || port is < 1 or > 65535)
            {
                StatusSieci = "Nieprawidlowy port serwera.";
                OdswiezKomendyITure();
                return;
            }

            string host = string.IsNullOrWhiteSpace(ServerHost) ? "127.0.0.1" : ServerHost.Trim();
            _trybGry = TrybGry.Multiplayer;
            OnPropertyChanged(nameof(TrybGryOpis));
            StatusSieci = $"Laczenie z {host}:{port}...";
            await _networkClient.ConnectAsync(host, port);
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
                _aktualnyGraczSieciowy = message.CurrentPlayer;
                ZastosujStanZSerwera(message);
                StatusSieci = $"{message.Text} Tura: Gracz {message.CurrentPlayer}";
            }
            else if (message.Type == NetworkMessageType.ShotResult)
            {
                _aktualnyGraczSieciowy = message.CurrentPlayer;
                ZastosujStanZSerwera(message);
                StatusSieci = $"Serwer: {message.Text}. Tura: Gracz {message.CurrentPlayer}";
                Status = $"Serwer: {message.Result}";

                if (message.Player == _numerGraczaSieciowego
                    && message.Result is nameof(Plansza.WynikStrzalu.Pudlo)
                        or nameof(Plansza.WynikStrzalu.Trafiony)
                        or nameof(Plansza.WynikStrzalu.Zatopiony))
                {
                    LiczbaStrzalow++;
                }

                if (message.GameOver && message.Winner is not null)
                {
                    Status = $"Koniec gry. Zwyciezca: Gracz {message.Winner}";
                    PokazPopupKoncaGry($"Wygrał Gracz {message.Winner}.");
                }
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

    private void ZastosujStanZSerwera(NetworkMessage message)
    {
        ZastosujStanPol(PolaGracza, message.OwnBoard);
        ZastosujStanPol(PolaPrzeciwnika, message.OpponentBoard);
        OdswiezPodgladFlotyZeSnapshotu(FlotaGracza, message.OwnBoard, includeVisibleShips: true);
        OdswiezPodgladFlotyZeSnapshotu(FlotaPrzeciwnika, message.OpponentBoard, includeVisibleShips: false);
        OdswiezKomendyITure();
    }

    private static void ZastosujStanPol(
        IEnumerable<PolePlanszyViewModel> pola,
        IReadOnlyCollection<NetworkCell> networkCells)
    {
        Dictionary<(int X, int Y), NetworkCell> cells = networkCells.ToDictionary(cell => (cell.X, cell.Y));

        foreach (PolePlanszyViewModel pole in pola)
        {
            if (cells.TryGetValue((pole.X, pole.Y), out NetworkCell? cell)
                && Enum.TryParse(cell.State, out Plansza.StanPola stanPola))
            {
                pole.StanPola = stanPola;
            }
        }
    }

    private void RozpocznijNowaGre()
    {
        _gra.RozpocznijNowaGre();
        _aktualnyGraczSieciowy = 1;
        LiczbaStrzalow = 0;
        _pokazanoKoniecGry = false;
        _wynikZapisany = false;
        Status = "Kliknij na pole przeciwnika";
        OdswiezPlansze();
        OdswiezKomendyITure();
    }

    private async Task WykonajRuchPrzeciwnikaAsync()
    {
        List<string> wynikiBota = new();
        _botWykonujeRuch = true;
        OdswiezKomendyITure();

        try
        {
            while (_gra.Stan == GraStatki.StanGry.WTrakcie && _gra.AktualnyGracz == GraStatki.Gracz.Gracz2)
            {
                await Task.Delay(450);

                PolePlanszyViewModel? pole = WybierzPoleDlaBota();
                if (pole is null)
                {
                    break;
                }

                GraStatki.WynikRuchu ruchPrzeciwnika = _gra.OddajStrzal(GraStatki.Gracz.Gracz2, pole.X, pole.Y);
                wynikiBota.Add(ruchPrzeciwnika.Wynik.ToString());
                OdswiezPlansze();
                ZapiszWynikJesliGraZakonczona();
            }
        }
        finally
        {
            _botWykonujeRuch = false;

            if (wynikiBota.Count > 0)
            {
                Status = $"Gracz: {Status.Replace("Gracz: ", string.Empty)} | Przeciwnik: {string.Join(", ", wynikiBota)}";
            }

            OdswiezKomendyITure();
        }
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

    private PolePlanszyViewModel? WybierzPoleDlaBota()
    {
        List<PolePlanszyViewModel> celeObokTrafien = PolaGracza
            .Where(pole => pole.StanPola == Plansza.StanPola.Trafiony)
            .SelectMany(PobierzSasiedniePola)
            .Where(pole => pole.StanPola is Plansza.StanPola.Puste or Plansza.StanPola.Statek)
            .DistinctBy(pole => (pole.X, pole.Y))
            .ToList();

        if (celeObokTrafien.Count > 0)
        {
            return celeObokTrafien[_random.Next(celeObokTrafien.Count)];
        }

        return WylosujPoleDoStrzalu(PolaGracza);
    }

    private IEnumerable<PolePlanszyViewModel> PobierzSasiedniePola(PolePlanszyViewModel pole)
    {
        (int X, int Y)[] kierunki =
        {
            (pole.X + 1, pole.Y),
            (pole.X - 1, pole.Y),
            (pole.X, pole.Y + 1),
            (pole.X, pole.Y - 1)
        };

        foreach ((int x, int y) in kierunki)
        {
            PolePlanszyViewModel? sasiedniePole = PolaGracza.FirstOrDefault(p => p.X == x && p.Y == y);
            if (sasiedniePole is not null)
            {
                yield return sasiedniePole;
            }
        }
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

        OdswiezPodgladFloty(FlotaGracza, planszaGracza);
        OdswiezPodgladFloty(FlotaPrzeciwnika, planszaPrzeciwnika);
    }

    private static void OdswiezPodgladFloty(IEnumerable<FlotaViewModel> flota, Plansza plansza)
    {
        Dictionary<int, Queue<bool>> zatopieniaWedlugRozmiaru = plansza.Statki
            .GroupBy(statek => statek.Wielkosc)
            .ToDictionary(
                group => group.Key,
                group => new Queue<bool>(group.Select(statek => statek.CzyZatopiony())));

        foreach (FlotaViewModel statekFloty in flota)
        {
            statekFloty.CzyZatopiony = zatopieniaWedlugRozmiaru.TryGetValue(statekFloty.Rozmiar, out Queue<bool>? zatopienia)
                && zatopienia.Count > 0
                && zatopienia.Dequeue();
        }
    }

    private static void OdswiezPodgladFlotyZeSnapshotu(
        IEnumerable<FlotaViewModel> flota,
        IReadOnlyCollection<NetworkCell> networkCells,
        bool includeVisibleShips)
    {
        HashSet<(int X, int Y)> zatopionePola = networkCells
            .Where(cell => Enum.TryParse(cell.State, out Plansza.StanPola stan) && stan == Plansza.StanPola.Zatopiony)
            .Select(cell => (cell.X, cell.Y))
            .ToHashSet();

        Dictionary<int, int> zatopioneWedlugRozmiaru = new();

        while (zatopionePola.Count > 0)
        {
            (int X, int Y) start = zatopionePola.First();
            int rozmiar = PoliczSpojnyStatek(start, zatopionePola);
            zatopioneWedlugRozmiaru[rozmiar] = zatopioneWedlugRozmiaru.GetValueOrDefault(rozmiar) + 1;
        }

        Dictionary<int, int> wykorzystaneZatopienia = new();

        foreach (FlotaViewModel statekFloty in flota)
        {
            int wykorzystane = wykorzystaneZatopienia.GetValueOrDefault(statekFloty.Rozmiar);
            int zatopione = zatopioneWedlugRozmiaru.GetValueOrDefault(statekFloty.Rozmiar);
            statekFloty.CzyZatopiony = wykorzystane < zatopione;
            wykorzystaneZatopienia[statekFloty.Rozmiar] = wykorzystane + 1;
        }
    }

    private static int PoliczSpojnyStatek((int X, int Y) start, HashSet<(int X, int Y)> polaStatkow)
    {
        Queue<(int X, int Y)> kolejka = new();
        kolejka.Enqueue(start);
        polaStatkow.Remove(start);
        int rozmiar = 0;

        while (kolejka.Count > 0)
        {
            (int x, int y) = kolejka.Dequeue();
            rozmiar++;

            foreach ((int nx, int ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (polaStatkow.Remove((nx, ny)))
                {
                    kolejka.Enqueue((nx, ny));
                }
            }
        }

        return rozmiar;
    }

    private void OdswiezKomendyITure()
    {
        OnPropertyChanged(nameof(AktualnaTura));
        OnPropertyChanged(nameof(AktualnaTuraSieciowa));
        OnPropertyChanged(nameof(AktualnaTuraOpis));
        OnPropertyChanged(nameof(TrybGryOpis));
        OnPropertyChanged(nameof(FlotaGraczaWedlugRzedow));
        OnPropertyChanged(nameof(FlotaPrzeciwnikaWedlugRzedow));

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
        PokazPopupKoncaGry($"Wygrał {gameResult.Winner}.");
        WczytajHistorieGier();
    }

    private void PokazPopupKoncaGry(string message)
    {
        if (_pokazanoKoniecGry)
        {
            return;
        }

        _pokazanoKoniecGry = true;
        MessageBox.Show(message, "Koniec gry", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void WczytajHistorieGier()
    {
        HistoriaGier.Clear();

        foreach (GameResult result in _gameResultRepository.GetLatestResults())
        {
            HistoriaGier.Add(result);
        }
    }

    private sealed class ClientNetworkSettings
    {
        private const string FileName = "client-network-settings.json";

        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;

        public static ClientNetworkSettings Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(path))
            {
                return new ClientNetworkSettings();
            }

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ClientNetworkSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ClientNetworkSettings();
        }
    }
}
