namespace Projekt_Zaliczeniowy.Data;

public class GameResult
{
    public int Id { get; set; }
    public DateTime PlayedAt { get; set; }
    public string Winner { get; set; } = string.Empty;
    public int ShotCount { get; set; }
    public string Result { get; set; } = string.Empty;
}
