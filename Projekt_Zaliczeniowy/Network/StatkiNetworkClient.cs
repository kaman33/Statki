using System.IO;
using System.Net.Sockets;
using Statki_Network;

namespace Projekt_Zaliczeniowy.Network;

public class StatkiNetworkClient : IDisposable
{
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cancellationTokenSource;

    public event Action<NetworkMessage>? MessageReceived;
    public event Action<string>? Disconnected;

    public bool IsConnected => _tcpClient?.Connected == true;

    public async Task ConnectAsync(string host, int port)
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port);

        NetworkStream stream = _tcpClient.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
        _cancellationTokenSource = new CancellationTokenSource();

        _ = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token));
    }

    public async Task SendShotAsync(int x, int y)
    {
        if (_writer is null)
        {
            return;
        }

        await NetworkProtocol.SendAsync(_writer, new NetworkMessage
        {
            Type = NetworkMessageType.Shot,
            X = x,
            Y = y
        });
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _reader is not null)
            {
                NetworkMessage? message = await NetworkProtocol.ReceiveAsync(_reader, cancellationToken);
                if (message is null)
                {
                    break;
                }

                MessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException exception)
        {
            Disconnected?.Invoke(exception.Message);
            return;
        }
        catch (SocketException exception)
        {
            Disconnected?.Invoke(exception.Message);
            return;
        }

        Disconnected?.Invoke("Polaczenie z serwerem zostalo zamkniete.");
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _reader?.Dispose();
        _writer?.Dispose();
        _tcpClient?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
