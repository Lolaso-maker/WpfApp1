using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // Constructor predeterminado.
        public MainWindow() : this("Jugador")
        {
        }

        // Constructor que recibe al usuario autenticado.
        public MainWindow(string usuario)
        {
            InitializeComponent();

            UserNameText.Text = usuario;
        }

        private void CerrarSesion_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Crear y mostrar nuevamente el login.
            F_Login login = new F_Login();

            login.Show();

            // Cerrar la ventana del catálogo.
            Close();
        }
    }
}