using System.Windows;
using System.Windows.Controls;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class SoporteVista : UserControl
    {
        public SoporteVista()
        {
            InitializeComponent();
        }

        private Launcher? Datos => DataContext as Launcher;

        private void Jugar_Click(object sender, RoutedEventArgs e)
        {
            Datos?.Jugar();
        }

        private void Perfil_Click(object sender, RoutedEventArgs e)
        {
            Datos?.Navegar("Perfil");
        }

        private void Ajustes_Click(object sender, RoutedEventArgs e)
        {
            Datos?.Navegar("Ajustes");
        }
    }
}
