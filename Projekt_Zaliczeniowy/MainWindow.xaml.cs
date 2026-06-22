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
        for (int y = 0; y < Plansza.WymiarY; y++)
        {
            for (int x = 0; x < Plansza.WymiarX; x++)
            {
                Button przycisk = new Button();
                przycisk.Style = (Style)FindResource("PolePlanszyButtonStyle");
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
        
        OdswiezPlanszePrzeciwnika();
        
    }

    private void OdswiezPlanszePrzeciwnika()
    {
        Plansza planszaPrzeciwnika = _gra.PlanszaGracz2;

        for (int y = 0; y < Plansza.WymiarY; y++)
        {
            for (int x = 0; x < Plansza.WymiarX; x++)
            {
                Plansza.StanPola stanPola = planszaPrzeciwnika.PobierzStanPola(x, y);
                _przyciskPrzeciwnika[(x, y)].Background = PobierzKolorPola(stanPola);
            }
        }
    }

    private static Brush PobierzKolorPola(Plansza.StanPola stanPola)
    {
        return stanPola switch
        {
            Plansza.StanPola.Pudlo => Brushes.LightBlue,
            Plansza.StanPola.Trafiony => Brushes.Red,
            Plansza.StanPola.Zatopiony => Brushes.DarkRed,
            _ => Brushes.White
        };
    }
    
}
