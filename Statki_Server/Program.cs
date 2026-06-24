using System.Net;
using System.Net.Sockets;
using Statki_Game;
using Statki_Network;

const int port = 5000;

TcpListener listener = new(IPAddress.Loopback, port);
GraStatki game = new();
SemaphoreSlim gameLock = new(1, 1);
List<ClientConnection> clients = new();

listener.Start();
Console.WriteLine($"Serwer Statki dziala na 127.0.0.1:{port}");

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

    await NetworkProtocol.SendAsync(client.Writer, new NetworkMessage
    {
        Type = NetworkMessageType.Welcome,
        Player = playerNumber,
        CurrentPlayer = ToNetworkPlayer(game.AktualnyGracz),
        Text = $"Polaczono jako Gracz {playerNumber}."
    });

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
        GraStatki.WynikRuchu moveResult = game.OddajStrzal(player, message.X, message.Y);

        NetworkMessage response = new()
        {
            Type = NetworkMessageType.ShotResult,
            Player = client.PlayerNumber,
            X = message.X,
            Y = message.Y,
            Result = moveResult.Wynik.ToString(),
            GameOver = moveResult.KoniecGry,
            Winner = moveResult.Zwyciezca is null ? null : ToNetworkPlayer(moveResult.Zwyciezca.Value),
            CurrentPlayer = ToNetworkPlayer(game.AktualnyGracz),
            Text = $"Gracz {client.PlayerNumber}: {moveResult.Wynik}"
        };

        await BroadcastAsync(response);
    }
    finally
    {
        gameLock.Release();
    }
}

async Task BroadcastAsync(NetworkMessage message)
{
    foreach (ClientConnection client in clients.ToList())
    {
        await NetworkProtocol.SendAsync(client.Writer, message);
    }
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
