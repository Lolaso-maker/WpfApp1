using System.Windows;
using System.Windows.Controls;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class ActividadVista : UserControl
    {
        public ActividadVista()
        {
            InitializeComponent();
        }

        private void Filtro_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is Launcher datos && e.OriginalSource is RadioButton chip)
            {
                datos.FiltroActividad = chip.Content?.ToString() ?? "Todo";
            }
        }
    }
}
