using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace WpfApp1
{
    public partial class F_Login : Window
    {
        // Práctica 3: las credenciales ya no están en memoria.
        // Se validan contra la tabla Usuarios de MySQL (ver DatabaseHelper y database.sql).

        // Bloqueo por intentos fallidos (se mantiene de la Práctica 2).
        // Son static para que no se pierdan si se vuelve a abrir el login.
        private const int MaxIntentos = 3;
        private const int SegundosBloqueo = 30;

        private static readonly Dictionary<string, int> intentosFallidos = new();
        private static readonly Dictionary<string, DateTime> bloqueadoHasta = new();

        // Evita lanzar dos operaciones contra la base de datos a la vez (doble clic).
        private bool operacionEnCurso;

        public F_Login()
        {
            InitializeComponent();

            TxtUsuario.Focus();
        }

        // ───────────────────────── Login ─────────────────────────

        private async void BtnLogin_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (operacionEnCurso)
            {
                return;
            }

            // TC 01: campos vacíos. Si falta algún dato no se consulta la base de datos.
            if (!ValidarCampos(out string usuario, out string password))
            {
                return;
            }

            // El nombre de usuario no distingue mayúsculas en MySQL; la clave del bloqueo tampoco.
            string clave = usuario.ToLowerInvariant();

            // Si el usuario está bloqueado no se le deja entrar (ni se consulta la BD).
            if (bloqueadoHasta.TryGetValue(clave, out DateTime hasta) &&
                hasta > DateTime.Now)
            {
                int segundos = (int)Math.Ceiling((hasta - DateTime.Now).TotalSeconds);

                MostrarMensaje(
                    $"Usuario bloqueado temporalmente. Inténtelo de nuevo en {segundos} segundos.",
                    false);

                TxtPassword.Clear();
                return;
            }

            string? usuarioValidado;
            MySqlConnection? conexion = null;

            IniciarOperacion();

            try
            {
                conexion = await DatabaseHelper.AbrirConexionAsync();

                // TC 03 / TC 04 / TC 09: consulta parametrizada.
                // Los datos escritos por el usuario viajan como parámetros y nunca se
                // concatenan al SQL, por lo que no es posible la inyección SQL.
                const string sql =
                    "SELECT nombre_usuario " +
                    "FROM Usuarios " +
                    "WHERE nombre_usuario = @usuario AND password = @password " +
                    "LIMIT 1;";

                using MySqlCommand comando = new MySqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("@usuario", usuario);
                comando.Parameters.AddWithValue("@password", password);

                // Devuelve el nombre del usuario si existe esa combinación, o null si no.
                usuarioValidado = await comando.ExecuteScalarAsync() as string;
            }
            catch (MySqlException ex)
            {
                // TC 08: MySQL no responde. Se avisa y la aplicación sigue abierta.
                MostrarErrorBaseDatos(ex);
                return;
            }
            catch (Exception ex)
            {
                MostrarErrorInesperado(ex);
                return;
            }
            finally
            {
                DatabaseHelper.CerrarConexion(conexion);
                FinalizarOperacion();
            }

            // TC 04: usuario inexistente o contraseña incorrecta.
            if (usuarioValidado == null)
            {
                RegistrarIntentoFallido(clave);
                return;
            }

            // TC 03: acceso correcto.
            intentosFallidos.Remove(clave);
            bloqueadoHasta.Remove(clave);

            MostrarMensaje(
                $"Inicio de sesión correcto. Bienvenido, {usuarioValidado}.",
                true);

            // Pequeña pausa para que se vea el mensaje antes de cambiar de ventana
            // (y para que no se pueda pulsar dos veces).
            IsHitTestVisible = false;
            await Task.Delay(800);

            MainWindow catalogo = new MainWindow(usuarioValidado);

            catalogo.Show();

            // Se cierra el login después de abrir el catálogo.
            Close();
        }

        // ───────────────────────── Nuevo usuario ─────────────────────────

        private async void BtnNuevoUsuario_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (operacionEnCurso)
            {
                return;
            }

            if (!ValidarCampos(out string usuario, out string password))
            {
                return;
            }

            MySqlConnection? conexion = null;

            IniciarOperacion();

            try
            {
                conexion = await DatabaseHelper.AbrirConexionAsync();

                // 1) TC 06: se comprueba si el nombre de usuario ya existe.
                const string sqlExiste =
                    "SELECT COUNT(*) FROM Usuarios WHERE nombre_usuario = @usuario;";

                using (MySqlCommand comprobar = new MySqlCommand(sqlExiste, conexion))
                {
                    comprobar.Parameters.AddWithValue("@usuario", usuario);

                    long existentes = Convert.ToInt64(await comprobar.ExecuteScalarAsync());

                    if (existentes > 0)
                    {
                        MostrarUsuarioDuplicado();
                        return;
                    }
                }

                // 2) TC 05: alta del usuario con un INSERT parametrizado.
                const string sqlAlta =
                    "INSERT INTO Usuarios (nombre_usuario, password) " +
                    "VALUES (@usuario, @password);";

                using (MySqlCommand alta = new MySqlCommand(sqlAlta, conexion))
                {
                    alta.Parameters.AddWithValue("@usuario", usuario);
                    alta.Parameters.AddWithValue("@password", password);

                    await alta.ExecuteNonQueryAsync();
                }

                MostrarMensaje(
                    $"Usuario «{usuario}» dado de alta correctamente. Ya puede iniciar sesión.",
                    true);

                TxtPassword.Clear();
                TxtPassword.Focus();
            }
            catch (MySqlException ex) when (ex.Number == DatabaseHelper.ErrorDuplicado)
            {
                // La restricción UNIQUE de la tabla también impide el duplicado
                // (por ejemplo, si otro equipo lo crea justo a la vez).
                MostrarUsuarioDuplicado();
            }
            catch (MySqlException ex)
            {
                // TC 08: MySQL no responde. Se avisa y la aplicación sigue abierta.
                MostrarErrorBaseDatos(ex);
            }
            catch (Exception ex)
            {
                MostrarErrorInesperado(ex);
            }
            finally
            {
                DatabaseHelper.CerrarConexion(conexion);
                FinalizarOperacion();
            }
        }

        // ───────────────────────── Validación ─────────────────────────

        // Comprueba que se han escrito usuario y contraseña (mensajes de la Práctica 2).
        private bool ValidarCampos(
            out string usuario,
            out string password)
        {
            usuario = TxtUsuario.Text.Trim();
            password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(usuario) &&
                string.IsNullOrEmpty(password))
            {
                MostrarMensaje(
                    "Introduzca los datos de acceso (usuario y contraseña).",
                    false);

                TxtUsuario.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MostrarMensaje(
                    "Introduzca el nombre de usuario.",
                    false);

                TxtUsuario.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                MostrarMensaje(
                    "Introduzca la contraseña.",
                    false);

                TxtPassword.Focus();
                return false;
            }

            return true;
        }

        // Cuenta los fallos de un usuario y lo bloquea un tiempo al llegar al máximo.
        private void RegistrarIntentoFallido(string clave)
        {
            intentosFallidos.TryGetValue(clave, out int intentos);
            intentos++;

            if (intentos >= MaxIntentos)
            {
                bloqueadoHasta[clave] = DateTime.Now.AddSeconds(SegundosBloqueo);
                intentosFallidos[clave] = 0;

                MostrarMensaje(
                    $"Usuario o contraseña incorrectos. Ha fallado {MaxIntentos} veces: " +
                    $"acceso bloqueado durante {SegundosBloqueo} segundos.",
                    false);
            }
            else
            {
                intentosFallidos[clave] = intentos;

                MostrarMensaje(
                    $"Usuario o contraseña incorrectos (intento {intentos} de {MaxIntentos}).",
                    false);
            }

            TxtPassword.Clear();
            TxtPassword.Focus();
        }

        // ───────────────────────── Mensajes ─────────────────────────

        private void MostrarMensaje(
            string mensaje,
            bool correcto)
        {
            TxtMensaje.Text = mensaje;

            TxtMensaje.Foreground = correcto
                ? Brushes.LightGreen
                : Brushes.Salmon;
        }

        private void MostrarUsuarioDuplicado()
        {
            MostrarMensaje(
                "El usuario ya existe. Elija otro nombre de usuario.",
                false);

            TxtUsuario.Focus();
            TxtUsuario.SelectAll();
        }

        private void MostrarErrorBaseDatos(MySqlException ex)
        {
            bool sinConexion = ex.Number == DatabaseHelper.ErrorSinConexion ||
                               ex.InnerException is System.Net.Sockets.SocketException;

            string titulo = sinConexion
                ? "No se ha podido conectar con la base de datos."
                : "Se ha producido un error al acceder a la base de datos.";

            string ayuda = sinConexion
                ? "Compruebe que el servicio de MySQL está iniciado y vuelva a intentarlo."
                : "Revise la configuración de la base de datos (script database.sql).";

            MostrarMensaje($"{titulo} {ayuda}", false);

            MessageBox.Show(
                this,
                $"{titulo}\n\n{ayuda}\n\nDetalle: {ex.Message}",
                "GameStation - Error de conexión",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void MostrarErrorInesperado(Exception ex)
        {
            MostrarMensaje("Se ha producido un error inesperado. Inténtelo de nuevo.", false);

            MessageBox.Show(
                this,
                $"Se ha producido un error inesperado.\n\nDetalle: {ex.Message}",
                "GameStation - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        // ───────────────────────── Utilidades ─────────────────────────

        private void IniciarOperacion()
        {
            operacionEnCurso = true;
            Cursor = Cursors.Wait;
        }

        private void FinalizarOperacion()
        {
            operacionEnCurso = false;
            Cursor = null;
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
