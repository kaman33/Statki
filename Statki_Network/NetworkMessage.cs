namespace Statki_Network;

public enum NetworkMessageType
{
    Connect,
    Welcome,
    Shot,
    ShotResult,
    Error
}

public class NetworkMessage
{
    public NetworkMessageType Type { get; set; }
    public int Player { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Result { get; set; } = string.Empty;
    public bool GameOver { get; set; }
    public int? Winner { get; set; }
    public int CurrentPlayer { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<NetworkCell> OwnBoard { get; set; } = new();
    public List<NetworkCell> OpponentBoard { get; set; } = new();
}

public class NetworkCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public string State { get; set; } = string.Empty;
}
