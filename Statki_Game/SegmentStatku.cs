namespace Statki_Game;

public class SegmentStatku
{
    public int X { get; }
    public int Y { get; }
    public bool CzyTrafiony { get; private set; }

    public SegmentStatku(int x, int y)
    {
        X = x;
        Y = y;
        this.CzyTrafiony = false;
    }

    public void Traf()
    {
        CzyTrafiony = true;
    }
    
}