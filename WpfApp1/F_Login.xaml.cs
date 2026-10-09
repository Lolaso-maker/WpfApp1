using System.Windows;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class F_Login : Window
    {
        // Almacén de credenciales en memoria.
        // Columna 0: usuario.
        // Columna 1: contraseña.
        private readonly string[,] usuarios =
        {
            { "admin", "1234" },
            { "jugador", "game2026" },
            { "alumno", "12345" }
        };

        public F_Login()
        {
            InitializeComponent();

            TxtUsuario.Focus();
        }

        private void BtnLogin_Click(
            object sender,
            RoutedEventArgs e)
        {
            string usuario = TxtUsuario.Text.Trim();
            string password = TxtPassword.Password;

            // TC04 y TC05: validar campos vacíos.
            if (string.IsNullOrWhiteSpace(usuario))
            {
                MostrarMensaje(
                    "Introduce tu nombre de usuario.",
                    false);

                TxtUsuario.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MostrarMensaje(
                    "Introduce tu contraseña.",
                    false);

                TxtPassword.Focus();
                return;
            }

            // Validar las credenciales almacenadas.
            bool autenticado = false;

            for (int i = 0; i < usuarios.GetLength(0); i++)
            {
                if (usuario == usuarios[i, 0] &&
                    password == usuarios[i, 1])
                {
                    autenticado = true;
                    break;
                }
            }

            if (autenticado)
            {
                // TC01: acceso correcto.
                MainWindow catalogo = new MainWindow(usuario);

                catalogo.Show();

                // Se cierra el login después de abrir el catálogo.
                Close();
            }
            else
            {
                // TC02 y TC03: credenciales incorrectas.
                MostrarMensaje(
                    "Usuario o contraseña incorrectos.",
                    false);

                TxtPassword.Clear();
                TxtPassword.Focus();
            }
        }

        private void MostrarMensaje(
            string mensaje,
            bool correcto)
        {
            TxtMensaje.Text = mensaje;

            TxtMensaje.Foreground = correcto
                ? Brushes.LightGreen
                : Brushes.Salmon;
        }

        private void BtnSalir_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Cierra la aplicación completa.
            Application.Current.Shutdown();
        }
    }
}