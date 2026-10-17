using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfApp1.Buscaminas;
using WpfApp1.Modelos;
using WpfApp1.Vistas;

namespace WpfApp1
{
    /// <summary>
    /// Ventana principal del launcher: menú, barra superior y la sección actual.
    /// Todo el estado vive en Modelos.Launcher (su DataContext).
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Launcher launcher;

        // Vistas ya creadas (se reutilizan para no perder el scroll ni los filtros).
        private readonly Dictionary<string, UserControl> vistas = new();

        // Sección -> botón del menú.
        private readonly Dictionary<string, RadioButton> menu;

        private string seccionActual = "";
        private bool sincronizandoMenu;
        private DateTime notificacionesCerradas = DateTime.MinValue;
        private BuscaminasWindow? juego;

        // Constructor predeterminado.
        public MainWindow() : this("Jugador")
        {
        }

        // Constructor que recibe al usuario autenticado.
        public MainWindow(string usuario)
        {
            InitializeComponent();

            launcher = new Launcher(usuario);
            DataContext = launcher;

            launcher.AvisoSolicitado += Launcher_AvisoSolicitado;
            launcher.JuegoSolicitado += Launcher_JuegoSolicitado;
            launcher.NavegacionSolicitada += Launcher_NavegacionSolicitada;

            menu = new Dictionary<string, RadioButton>
            {
                ["Biblioteca"] = NavBiblioteca,
                ["MisJuegos"] = NavMisJuegos,
                ["Destacados"] = NavDestacados,
                ["Descargas"] = NavDescargas,
                ["Amigos"] = NavAmigos,
                ["Actividad"] = NavActividad,
                ["Perfil"] = NavPerfil,
                ["Ajustes"] = NavAjustes
            };

            MostrarSeccion("Biblioteca");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            VentanaOscura.Aplicar(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            launcher.AvisoSolicitado -= Launcher_AvisoSolicitado;
            launcher.JuegoSolicitado -= Launcher_JuegoSolicitado;
            launcher.NavegacionSolicitada -= Launcher_NavegacionSolicitada;
            launcher.Dispose();

            base.OnClosed(e);
        }

        // ───────────────────────── Navegación ─────────────────────────

        private void MostrarSeccion(string seccion)
        {
            if (seccion == seccionActual)
            {
                return;
            }

            if (!vistas.TryGetValue(seccion, out var vista))
            {
                vista = seccion switch
                {
                    "MisJuegos" => new MisJuegosVista(),
                    "Destacados" => new TiendaVista(),
                    "Descargas" => new DescargasVista(),
                    "Amigos" => new AmigosVista(),
                    "Actividad" => new ActividadVista(),
                    "Perfil" => new PerfilVista(),
                    "Ajustes" => new AjustesVista(),
                    "Soporte" => new SoporteVista(),
                    _ => new InicioVista()
                };

                vistas[seccion] = vista;
            }

            seccionActual = seccion;
            Contenido.Content = vista;

            // Marca el botón del menú (Soporte no tiene botón: se desmarca todo).
            sincronizandoMenu = true;

            if (menu.TryGetValue(seccion, out var boton))
            {
                boton.IsChecked = true;
            }
            else
            {
                foreach (var b in menu.Values)
                {
                    b.IsChecked = false;
                }
            }

            sincronizandoMenu = false;

            // Transición suave entre secciones.
            var suave = new CubicEase { EasingMode = EasingMode.EaseOut };
            Contenido.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
            ContenidoDesplazamiento.BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = suave });
        }

        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
            if (sincronizandoMenu || e.OriginalSource is not RadioButton boton)
            {
                return;
            }

            foreach (var (seccion, b) in menu)
            {
                if (ReferenceEquals(b, boton))
                {
                    MostrarSeccion(seccion);
                    return;
                }
            }
        }

        private void Seccion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string seccion })
            {
                MostrarSeccion(seccion);
            }
        }

        private void Perfil_Click(object sender, RoutedEventArgs e)
        {
            MostrarSeccion("Perfil");
        }

        private void IrADescargas_Click(object sender, RoutedEventArgs e)
        {
            MostrarSeccion("Descargas");
        }

        private void Launcher_NavegacionSolicitada(object? sender, string seccion)
        {
            if (seccion == "CerrarSesion")
            {
                CerrarSesion();
            }
            else
            {
                MostrarSeccion(seccion);
            }
        }

        // ───────────────────────── Buscador ─────────────────────────

        private void Busqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Si se busca desde otra sección, se salta a la biblioteca.
            if (SearchBox.IsKeyboardFocusWithin && SearchBox.Text.Length > 0 && seccionActual != "Biblioteca")
            {
                MostrarSeccion("Biblioteca");
            }
        }

        // Ctrl + K enfoca el buscador; Esc lo vacía.
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && SearchBox.IsKeyboardFocusWithin)
            {
                SearchBox.Clear();
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        // ───────────────────────── Notificaciones ─────────────────────────

        private void Campana_Click(object sender, RoutedEventArgs e)
        {
            // Si el panel se acaba de cerrar por este mismo clic, no se vuelve a abrir.
            if ((DateTime.Now - notificacionesCerradas).TotalMilliseconds < 250)
            {
                return;
            }

            PanelNotificaciones.IsOpen = !PanelNotificaciones.IsOpen;

            if (PanelNotificaciones.IsOpen)
            {
                launcher.MarcarNotificacionesLeidas();
            }
        }

        private void PanelNotificaciones_Closed(object? sender, EventArgs e)
        {
            notificacionesCerradas = DateTime.Now;
        }

        private void VerActividad_Click(object sender, RoutedEventArgs e)
        {
            PanelNotificaciones.IsOpen = false;
            MostrarSeccion("Actividad");
        }

        private void Launcher_AvisoSolicitado(object? sender, Aviso aviso)
        {
            ToastIcon.Text = aviso.Glifo;
            ToastTitle.Text = aviso.Titulo;
            ToastText.Text = aviso.Texto;

            var suave = new CubicEase { EasingMode = EasingMode.EaseOut };

            var opacidad = new DoubleAnimationUsingKeyFrames();
            opacidad.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)), suave));
            opacidad.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3000))));
            opacidad.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3400)), suave));

            Toast.BeginAnimation(OpacityProperty, opacidad);
            ToastShift.BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = suave });
        }

        // ───────────────────────── Juego ─────────────────────────

        private void Launcher_JuegoSolicitado(object? sender, Dificultad dificultad)
        {
            // Si ya está abierto, se trae al frente.
            if (juego is not null)
            {
                juego.Solicitar(dificultad);

                if (juego.WindowState == WindowState.Minimized)
                {
                    juego.WindowState = WindowState.Normal;
                }

                juego.Activate();
                return;
            }

            juego = new BuscaminasWindow(launcher, dificultad) { Owner = this };

            juego.Closed += (_, _) =>
            {
                juego = null;
                launcher.Jugando = false;

                if (IsVisible)
                {
                    Activate();
                }
            };

            launcher.Jugando = true;
            juego.Show();
        }

        // ───────────────────────── Sesión ─────────────────────────

        private void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            CerrarSesion();
        }

        private void CerrarSesion()
        {
            // Crear y mostrar nuevamente el login.
            F_Login login = new F_Login();

            login.Show();

            // Cerrar la ventana del catálogo (y el juego, si está abierto).
            Close();
        }
    }
}
