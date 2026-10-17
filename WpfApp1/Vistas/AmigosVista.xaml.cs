using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class AmigosVista : UserControl
    {
        public AmigosVista()
        {
            InitializeComponent();
        }

        private Launcher? Datos => DataContext as Launcher;

        private void Invitar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Amigo amigo })
            {
                Datos?.Invitar(amigo);
            }
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            Agregar();
        }

        private void NuevoAmigo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Agregar();
                e.Handled = true;
            }
        }

        private void Agregar()
        {
            if (Datos is null)
            {
                return;
            }

            string? error = Datos.AgregarAmigo(NuevoAmigo.Text);

            if (error is null)
            {
                NuevoAmigo.Clear();
                Error.Visibility = Visibility.Collapsed;
            }
            else
            {
                Error.Text = error;
                Error.Visibility = Visibility.Visible;
                NuevoAmigo.Focus();
            }
        }

        private void Todos_Checked(object sender, RoutedEventArgs e)
        {
            if (Datos is not null)
            {
                Datos.SoloConectados = false;
            }
        }

        private void Conectados_Checked(object sender, RoutedEventArgs e)
        {
            if (Datos is not null)
            {
                Datos.SoloConectados = true;
            }
        }
    }
}
