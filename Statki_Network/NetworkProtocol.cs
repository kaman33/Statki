using System.Text.Json;

namespace Statki_Network;

public static class NetworkProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SendAsync(StreamWriter writer, NetworkMessage message, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(message, JsonOptions);
        await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
        await writer.FlushAsync();
    }

    public static async Task<NetworkMessage?> ReceiveAsync(StreamReader reader, CancellationToken cancellationToken = default)
    {
        string? line = await reader.ReadLineAsync(cancellationToken);
        return line is null ? null : JsonSerializer.Deserialize<NetworkMessage>(line, JsonOptions);
    }
}
