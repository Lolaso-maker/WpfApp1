using System.Windows;
using System.Windows.Controls;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class DescargasVista : UserControl
    {
        public DescargasVista()
        {
            InitializeComponent();
        }

        private Launcher? Datos => DataContext as Launcher;

        private void Pausa_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Descarga descarga })
            {
                Datos?.AlternarPausa(descarga);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Descarga descarga })
            {
                Datos?.CancelarDescarga(descarga);
            }
        }

        private void Todas_Click(object sender, RoutedEventArgs e)
        {
            Datos?.AlternarTodasLasDescargas();
        }

        private void Limpiar_Click(object sender, RoutedEventArgs e)
        {
            Datos?.LimpiarCompletadas();
        }
    }
}
