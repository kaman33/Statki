using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Statki_Game;

namespace Projekt_Zaliczeniowy;
public partial class MainWindow : Window
{

    private GraStatki _gra = new GraStatki();
    public MainWindow()
    {
        InitializeComponent();
        UtworzPlansze();
    }

    private Dictionary<(int X, int Y), Button> _przyciskPrzeciwnika = new();
    private void UtworzPlansze()
    {
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                Button przycisk = new Button();
                przycisk.Margin = new Thickness(2);
                przycisk.Tag = new Pole(x, y);
                przycisk.Click += Pole_Click;
                _przyciskPrzeciwnika[(x, y)] = przycisk;
                PlanszaPrzeciwnika.Children.Add(przycisk);
            }
        }
    }

    private void Pole_Click(object sender, RoutedEventArgs e)
    {
        Button przycisk = (Button)sender;
        Pole pole = (Pole)przycisk.Tag;
        
        Plansza.WynikStrzalu wynik = _gra.Gracz1Strzela(pole.X, pole.Y);
        StatusText.Text = wynik.ToString();
        
        if (wynik == Plansza.WynikStrzalu.Pudlo)
        {
            przycisk.Background = Brushes.LightBlue;
        }
        else if (wynik == Plansza.WynikStrzalu.Trafiony)
        {
            przycisk.Background = Brushes.Red;
        }
        else if (wynik == Plansza.WynikStrzalu.Zatopiony)
        {
            Statek? zatopionyStaek = _gra.ZnajdzStatekGracz2NaPolu(pole.X, pole.Y);
            if (zatopionyStaek != null)
            {
                foreach(SegmentStatku segment in zatopionyStaek.Segmenty)
                {
                    _przyciskPrzeciwnika[(segment.X, segment.Y)].Background = Brushes.DarkRed;
                }
            }
            przycisk.Background = Brushes.DarkRed;
            
        }
        
    }
    
}