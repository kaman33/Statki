using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Statki_Game;
using Statki_Network;

ServerSettings settings = ServerSettings.Load();
IPAddress listenAddress = settings.GetListenAddress();

TcpListener listener = new(listenAddress, settings.Port);
GraStatki game = new();
SemaphoreSlim gameLock = new(1, 1);
List<ClientConnection> clients = new();

listener.Start();
Console.WriteLine($"Serwer Statki dziala na {listenAddress}:{settings.Port}");

while (true)
{
    TcpClient tcpClient = await listener.AcceptTcpClientAsync();

    if (clients.Count >= 2)
    {
        await RejectClientAsync(tcpClient, "Serwer obsluguje maksymalnie dwoch graczy.");
        continue;
    }

    int playerNumber = clients.Count + 1;
    ClientConnection client = new(playerNumber, tcpClient);
    clients.Add(client);

    await SendStateAsync(client, NetworkMessageType.Welcome, $"Polaczono jako Gracz {playerNumber}.", string.Empty, null);

    Console.WriteLine($"Polaczono Gracza {playerNumber}.");
    _ = Task.Run(() => HandleClientAsync(client));
}

async Task HandleClientAsync(ClientConnection client)
{
    try
    {
        while (true)
        {
            NetworkMessage? message = await NetworkProtocol.ReceiveAsync(client.Reader);
            if (message is null)
            {
                break;
            }

            if (message.Type == NetworkMessageType.Shot)
            {
                await HandleShotAsync(client, message);
            }
        }
    }
    catch (IOException)
    {
    }
    finally
    {
        clients.Remove(client);
        client.Dispose();
        Console.WriteLine($"Rozlaczono Gracza {client.PlayerNumber}.");
    }
}

async Task HandleShotAsync(ClientConnection client, NetworkMessage message)
{
    await gameLock.WaitAsync();

    try
    {
        GraStatki.Gracz player = FromNetworkPlayer(client.PlayerNumber);
        if (clients.Count < 2)
        {
            await SendStateAsync(client, NetworkMessageType.Error, "Poczekaj na drugiego gracza.", string.Empty, null);
            return;
        }

        GraStatki.WynikRuchu moveResult = game.OddajStrzal(player, message.X, message.Y);
        await BroadcastStateAsync($"Gracz {client.PlayerNumber}: {moveResult.Wynik}", moveResult.Wynik.ToString(), moveResult);
    }
    finally
    {
        gameLock.Release();
    }
}

async Task BroadcastStateAsync(string text, string result, GraStatki.WynikRuchu? moveResult)
{
    foreach (ClientConnection client in clients.ToList())
    {
        await SendStateAsync(client, NetworkMessageType.ShotResult, text, result, moveResult);
    }
}

async Task SendStateAsync(
    ClientConnection client,
    NetworkMessageType type,
    string text,
    string result,
    GraStatki.WynikRuchu? moveResult)
{
    GraStatki.Gracz player = FromNetworkPlayer(client.PlayerNumber);
    GraStatki.Gracz opponent = GraStatki.PobierzPrzeciwnika(player);

    NetworkMessage message = new()
    {
        Type = type,
        Player = client.PlayerNumber,
        Result = result,
        GameOver = moveResult?.KoniecGry ?? game.Stan == GraStatki.StanGry.Zakonczona,
        Winner = moveResult?.Zwyciezca is null ? null : ToNetworkPlayer(moveResult.Zwyciezca.Value),
        CurrentPlayer = ToNetworkPlayer(game.AktualnyGracz),
        Text = text,
        OwnBoard = CreateBoardSnapshot(game.PobierzPlansze(player), showShips: true),
        OpponentBoard = CreateBoardSnapshot(game.PobierzPlansze(opponent), showShips: false)
    };

    await NetworkProtocol.SendAsync(client.Writer, message);
}

static List<NetworkCell> CreateBoardSnapshot(Plansza board, bool showShips)
{
    List<NetworkCell> cells = new();

    for (int y = 0; y < Plansza.WymiarY; y++)
    {
        for (int x = 0; x < Plansza.WymiarX; x++)
        {
            cells.Add(new NetworkCell
            {
                X = x,
                Y = y,
                State = board.PobierzStanPola(x, y, showShips).ToString()
            });
        }
    }

    return cells;
}

async Task RejectClientAsync(TcpClient tcpClient, string reason)
{
    await using NetworkStream stream = tcpClient.GetStream();
    using StreamWriter writer = new(stream) { AutoFlush = true };

    await NetworkProtocol.SendAsync(writer, new NetworkMessage
    {
        Type = NetworkMessageType.Error,
        Text = reason
    });

    tcpClient.Close();
}

static GraStatki.Gracz FromNetworkPlayer(int player)
{
    return player == 1 ? GraStatki.Gracz.Gracz1 : GraStatki.Gracz.Gracz2;
}

static int ToNetworkPlayer(GraStatki.Gracz player)
{
    return player == GraStatki.Gracz.Gracz1 ? 1 : 2;
}

sealed class ClientConnection : IDisposable
{
    private readonly TcpClient _tcpClient;

    public ClientConnection(int playerNumber, TcpClient tcpClient)
    {
        PlayerNumber = playerNumber;
        _tcpClient = tcpClient;
        NetworkStream stream = tcpClient.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public int PlayerNumber { get; }
    public StreamReader Reader { get; }
    public StreamWriter Writer { get; }

    public void Dispose()
    {
        Reader.Dispose();
        Writer.Dispose();
        _tcpClient.Dispose();
    }
}

sealed class ServerSettings
{
    private const string FileName = "server-settings.json";
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5000;

    public static ServerSettings Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, FileName);
        if (!File.Exists(path))
        {
            return new ServerSettings();
        }
        
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ServerSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ServerSettings();
    }

    public IPAddress GetListenAddress()
    {
        if (string.Equals(Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return IPAddress.Loopback;
        }
            
        if (IPAddress.TryParse(Host, out IPAddress? address))
        {
            return address;
        }
        
        throw new InvalidOperationException($"Nieprawidlowy adres nasluchiwania serwera: {Host}");
    }
}
