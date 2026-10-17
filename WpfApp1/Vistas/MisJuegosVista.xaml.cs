using System.Windows;
using System.Windows.Controls;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class MisJuegosVista : UserControl
    {
        public MisJuegosVista()
        {
            InitializeComponent();
        }

        private Launcher? Datos => DataContext as Launcher;

        private void Jugar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Juego juego })
            {
                Datos?.Jugar(juego);
            }
        }

        private void Deseado_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Juego juego })
            {
                Datos?.AlternarDeseado(juego);
            }
        }
    }
}
