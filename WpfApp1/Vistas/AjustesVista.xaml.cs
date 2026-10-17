using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class AjustesVista : UserControl
    {
        // Para restablecer hay que pulsar dos veces (la segunda, antes de 4 s).
        private readonly DispatcherTimer confirmacion = new() { Interval = TimeSpan.FromSeconds(4) };
        private bool esperandoConfirmacion;

        public AjustesVista()
        {
            InitializeComponent();

            RutaDatos.Text = "Se guardan en " + Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameStation");

            confirmacion.Tick += (_, _) => CancelarConfirmacion();
        }

        private Launcher? Datos => DataContext as Launcher;

        private void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            Datos?.Navegar("CerrarSesion");
        }

        private void Restablecer_Click(object sender, RoutedEventArgs e)
        {
            if (!esperandoConfirmacion)
            {
                esperandoConfirmacion = true;
                TextoRestablecer.Text = "Pulsa otra vez para confirmar";
                confirmacion.Start();
                return;
            }

            CancelarConfirmacion();
            Datos?.RestablecerEstadisticas();
        }

        private void CancelarConfirmacion()
        {
            confirmacion.Stop();
            esperandoConfirmacion = false;
            TextoRestablecer.Text = "Restablecer";
        }
    }
}
