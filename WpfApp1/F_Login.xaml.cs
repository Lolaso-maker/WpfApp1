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

        // Bloqueo por intentos fallidos (TC05).
        // Son static para que no se pierdan si se vuelve a abrir el login.
        private const int MaxIntentos = 3;
        private const int SegundosBloqueo = 30;

        private static readonly Dictionary<string, int> intentosFallidos = new();
        private static readonly Dictionary<string, DateTime> bloqueadoHasta = new();

        public F_Login()
        {
            InitializeComponent();

            TxtUsuario.Focus();
        }

        private async void BtnLogin_Click(
            object sender,
            RoutedEventArgs e)
        {
            string usuario = TxtUsuario.Text.Trim();
            string password = TxtPassword.Password;

            // TC04: validar campos vacíos.
            if (string.IsNullOrWhiteSpace(usuario) &&
                string.IsNullOrEmpty(password))
            {
                MostrarMensaje(
                    "Introduzca los datos de acceso (usuario y contraseña).",
                    false);

                TxtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MostrarMensaje(
                    "Introduzca el nombre de usuario.",
                    false);

                TxtUsuario.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MostrarMensaje(
                    "Introduzca la contraseña.",
                    false);

                TxtPassword.Focus();
                return;
            }

            int fila = BuscarUsuario(usuario);

            // TC03: el usuario no existe.
            // Se avisa, se vacían los campos y se vuelve a pedir el usuario.
            if (fila == -1)
            {
                MostrarMensaje(
                    "El usuario introducido no existe.",
                    false);

                TxtUsuario.Clear();
                TxtPassword.Clear();
                TxtUsuario.Focus();
                return;
            }

            // TC05: si el usuario está bloqueado no se le deja entrar.
            if (bloqueadoHasta.TryGetValue(usuario, out DateTime hasta) &&
                hasta > DateTime.Now)
            {
                int segundos = (int)Math.Ceiling((hasta - DateTime.Now).TotalSeconds);

                MostrarMensaje(
                    $"Usuario bloqueado temporalmente. Inténtelo de nuevo en {segundos} segundos.",
                    false);

                TxtPassword.Clear();
                return;
            }

            // TC02: el usuario existe pero la contraseña no es correcta.
            if (password != usuarios[fila, 1])
            {
                intentosFallidos.TryGetValue(usuario, out int intentos);
                intentos++;

                if (intentos >= MaxIntentos)
                {
                    // TC05: al tercer fallo se bloquea el usuario un tiempo.
                    bloqueadoHasta[usuario] = DateTime.Now.AddSeconds(SegundosBloqueo);
                    intentosFallidos[usuario] = 0;

                    MostrarMensaje(
                        $"Ha introducido {MaxIntentos} veces mal la contraseña. " +
                        $"Usuario bloqueado durante {SegundosBloqueo} segundos.",
                        false);
                }
                else
                {
                    intentosFallidos[usuario] = intentos;

                    MostrarMensaje(
                        $"La contraseña introducida no es correcta (intento {intentos} de {MaxIntentos}).",
                        false);
                }

                TxtPassword.Clear();
                TxtPassword.Focus();
                return;
            }

            // TC01: acceso correcto.
            intentosFallidos.Remove(usuario);
            bloqueadoHasta.Remove(usuario);

            MostrarMensaje(
                $"Inicio de sesión correcto. Bienvenido, {usuario}.",
                true);

            // Pequeña pausa para que se vea el mensaje antes de cambiar de ventana
            // (y para que no se pueda pulsar dos veces).
            IsHitTestVisible = false;
            await Task.Delay(800);

            MainWindow catalogo = new MainWindow(usuario);

            catalogo.Show();

            // Se cierra el login después de abrir el catálogo.
            Close();
        }

        // Devuelve la fila del usuario en la lista o -1 si no existe.
        private int BuscarUsuario(string usuario)
        {
            for (int i = 0; i < usuarios.GetLength(0); i++)
            {
                if (usuarios[i, 0] == usuario)
                {
                    return i;
                }
            }

            return -1;
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