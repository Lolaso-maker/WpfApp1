using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfApp1.Modelos;

namespace WpfApp1.Buscaminas
{
    /// <summary>
    /// Ventana del juego. La lógica está en Tablero; aquí solo se conecta
    /// con la interfaz, el cronómetro y las estadísticas del launcher.
    /// </summary>
    public partial class BuscaminasWindow : Window
    {
        // Columnas del tablero actual (la usa el UniformGrid de la casilla).
        public static readonly DependencyProperty ColumnasProperty =
            DependencyProperty.Register(nameof(Columnas), typeof(int), typeof(BuscaminasWindow), new PropertyMetadata(9));

        private const double TamanoMaximoCasilla = 58;

        private readonly Launcher launcher;
        private readonly DispatcherTimer reloj = new() { Interval = TimeSpan.FromMilliseconds(200) };
        private readonly Stopwatch cronometro = new();

        private Tablero? tablero;
        private Dificultad dificultad;
        private bool cambiandoChip;

        public BuscaminasWindow(Launcher launcher, Dificultad dificultad)
        {
            InitializeComponent();

            this.launcher = launcher;
            DataContext = launcher;

            reloj.Tick += (_, _) => ActualizarTiempo();

            Empezar(dificultad);
        }

        public int Columnas
        {
            get => (int)GetValue(ColumnasProperty);
            set => SetValue(ColumnasProperty, value);
        }

        // Partida en marcha (ya se ha hecho el primer clic y no ha terminado).
        public bool PartidaEnCurso => tablero?.Estado == EstadoPartida.EnCurso;

        /// <summary>
        /// El launcher pide otra dificultad. Si hay una partida empezada no se pierde.
        /// </summary>
        public void Solicitar(Dificultad nueva)
        {
            if (nueva != dificultad && !PartidaEnCurso)
            {
                Empezar(nueva);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            VentanaOscura.Aplicar(this);
        }

        // ───────────────────────── Partidas ─────────────────────────

        private void Empezar(Dificultad nueva)
        {
            RegistrarAbandono();

            if (tablero is not null)
            {
                tablero.Cambiado -= Tablero_Cambiado;
                tablero.PartidaTerminada -= Tablero_PartidaTerminada;
            }

            dificultad = nueva;
            tablero = new Tablero(nueva, launcher.PrimerClicSeguro);
            tablero.Cambiado += Tablero_Cambiado;
            tablero.PartidaTerminada += Tablero_PartidaTerminada;

            Columnas = tablero.Columnas;
            TableroCasillas.ItemsSource = tablero.Celdas;

            // Las casillas pueden crecer hasta un tamaño máximo.
            Escala.MaxWidth = tablero.Columnas * TamanoMaximoCasilla;
            Escala.MaxHeight = tablero.Filas * TamanoMaximoCasilla;

            cronometro.Reset();
            reloj.Stop();

            MarcarChip(nueva);
            DificultadTexto.Text = $"{Dificultades.Nombre(nueva)} · {Dificultades.Descripcion(nueva)}";

            OcultarResultado(animar: false);
            ActualizarMarcador();
            ActualizarTiempo();
            ActualizarPanel();

            // Pequeña animación de entrada del tablero.
            Marco.BeginAnimation(OpacityProperty, new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(250)));
        }

        // Si se deja una partida a medias, cuenta el tiempo jugado.
        private void RegistrarAbandono()
        {
            if (PartidaEnCurso)
            {
                launcher.RegistrarTiempo((int)cronometro.Elapsed.TotalSeconds);
            }
        }

        private void Tablero_Cambiado(object? sender, EventArgs e)
        {
            if (tablero is { Estado: EstadoPartida.EnCurso } && !cronometro.IsRunning)
            {
                cronometro.Start();
                reloj.Start();
            }

            ActualizarMarcador();
        }

        private void Tablero_PartidaTerminada(object? sender, EventArgs e)
        {
            if (tablero is null)
            {
                return;
            }

            cronometro.Stop();
            reloj.Stop();
            ActualizarTiempo();

            bool ganada = tablero.Estado == EstadoPartida.Ganada;
            int segundos = Math.Max(1, (int)Math.Round(cronometro.Elapsed.TotalSeconds));

            var resultado = launcher.RegistrarPartida(dificultad, ganada, segundos, tablero.SeHanUsadoBanderas);

            if (launcher.Sonidos)
            {
                VentanaOscura.Sonido(ganada);
            }

            if (!ganada)
            {
                Sacudir();
            }

            ActualizarMarcador();
            ActualizarPanel();
            PrepararResultado(ganada, segundos, resultado);

            // Se deja un momento para ver el tablero antes de mostrar el resultado.
            var espera = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ganada ? 450 : 900) };
            var tableroTerminado = tablero;

            espera.Tick += (_, _) =>
            {
                espera.Stop();

                // Si ya se ha empezado otra partida, no se muestra.
                if (ReferenceEquals(tablero, tableroTerminado))
                {
                    MostrarResultado();
                }
            };

            espera.Start();
        }

        // ───────────────────────── Ratón y teclado ─────────────────────────

        private void Casilla_Click(object sender, RoutedEventArgs e)
        {
            if (tablero is not null && e.OriginalSource is FrameworkElement { DataContext: Celda celda })
            {
                tablero.Revelar(celda);
            }
        }

        private void Casilla_ClicDerecho(object sender, MouseButtonEventArgs e)
        {
            if (tablero is not null && e.OriginalSource is FrameworkElement { DataContext: Celda celda })
            {
                tablero.AlternarBandera(celda);
                e.Handled = true;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    Empezar(dificultad);
                    e.Handled = true;
                    break;

                case Key.D1:
                case Key.NumPad1:
                    Empezar(Dificultad.Principiante);
                    e.Handled = true;
                    break;

                case Key.D2:
                case Key.NumPad2:
                    Empezar(Dificultad.Intermedio);
                    e.Handled = true;
                    break;

                case Key.D3:
                case Key.NumPad3:
                    Empezar(Dificultad.Experto);
                    e.Handled = true;
                    break;

                case Key.Escape when Resultado.Visibility == Visibility.Visible:
                    OcultarResultado(animar: true);
                    e.Handled = true;
                    break;
            }
        }

        private void Dificultad_Checked(object sender, RoutedEventArgs e)
        {
            if (cambiandoChip || e.OriginalSource is not RadioButton { Tag: Dificultad elegida })
            {
                return;
            }

            if (elegida != dificultad)
            {
                Empezar(elegida);
            }
        }

        private void NuevaPartida_Click(object sender, RoutedEventArgs e)
        {
            Empezar(dificultad);
        }

        private void VerTablero_Click(object sender, RoutedEventArgs e)
        {
            OcultarResultado(animar: true);
        }

        private void Salir_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            reloj.Stop();
            RegistrarAbandono();
        }

        // ───────────────────────── Interfaz ─────────────────────────

        private void MarcarChip(Dificultad d)
        {
            cambiandoChip = true;

            ChipPrincipiante.IsChecked = d == Dificultad.Principiante;
            ChipIntermedio.IsChecked = d == Dificultad.Intermedio;
            ChipExperto.IsChecked = d == Dificultad.Experto;

            cambiandoChip = false;
        }

        private void ActualizarMarcador()
        {
            if (tablero is null)
            {
                return;
            }

            MinasTexto.Text = tablero.MinasRestantes.ToString("000");

            (string texto, string fondo, string color) = tablero.Estado switch
            {
                EstadoPartida.Lista => ("Haz clic en una casilla para empezar", "#1E1A38", "#A996FF"),
                EstadoPartida.EnCurso => ($"En juego · {tablero.MinasRestantes} minas por marcar", "#1E1A38", "#A996FF"),
                EstadoPartida.Ganada => ("¡Tablero despejado!", "#14301F", "#4ADE80"),
                _ => ("¡Boom! Has pisado una mina", "#3A1822", "#FF5C7A")
            };

            EstadoTexto.Text = texto;
            EstadoPastilla.Background = Pincel(fondo);
            EstadoTexto.Foreground = Pincel(color);
        }

        private void ActualizarTiempo()
        {
            int segundos = Math.Min(999, (int)cronometro.Elapsed.TotalSeconds);
            TiempoTexto.Text = segundos.ToString("000");
        }

        // Estadísticas de la dificultad actual en el panel derecho.
        private void ActualizarPanel()
        {
            var e = launcher.Estadistica(dificultad);

            RecordTexto.Text = e.MejorTiempo is int t ? $"{t} s" : "Sin récord";
            RecordDificultad.Text = $"Mejor tiempo en {Dificultades.Nombre(dificultad)}";
            PartidasTexto.Text = e.Partidas.ToString();
            VictoriasTexto.Text = e.Victorias.ToString();
            PorcentajeTexto.Text = e.Partidas == 0 ? "—" : $"{e.Victorias * 100 / e.Partidas} %";
        }

        private void PrepararResultado(bool ganada, int segundos, ResultadoPartida resultado)
        {
            if (ganada)
            {
                ResultadoTitulo.Text = "¡Has ganado!";
                ResultadoIcono.Text = "";
                ResultadoIcono.Foreground = Pincel("#4ADE80");
                ResultadoCirculo.Fill = Pincel("#14301F");

                string record = resultado.MejorTiempo is int mejor ? $" · Récord: {mejor} s" : "";
                ResultadoDetalle.Text = $"{Dificultades.Nombre(dificultad)} en {segundos} s{record}";
            }
            else
            {
                ResultadoTitulo.Text = "¡Boom!";
                ResultadoIcono.Text = "";
                ResultadoIcono.Foreground = Pincel("#FF5C7A");
                ResultadoCirculo.Fill = Pincel("#3A1822");

                int total = tablero is null ? 0 : tablero.Celdas.Count - tablero.Minas;
                int hechas = tablero?.CeldasReveladas ?? 0;
                int porcentaje = total == 0 ? 0 : hechas * 100 / total;
                ResultadoDetalle.Text = $"Has pisado una mina. Habías despejado el {porcentaje} % del tablero.";
            }

            RecordPastilla.Visibility = ganada && resultado.NuevoRecord ? Visibility.Visible : Visibility.Collapsed;
            LogrosNuevos.ItemsSource = resultado.NuevosLogros;
        }

        private void MostrarResultado()
        {
            Resultado.Visibility = Visibility.Visible;
            Resultado.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }

        private void OcultarResultado(bool animar)
        {
            if (!animar || Resultado.Visibility != Visibility.Visible)
            {
                Resultado.BeginAnimation(OpacityProperty, null);
                Resultado.Opacity = 0;
                Resultado.Visibility = Visibility.Collapsed;
                return;
            }

            var salida = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160));
            salida.Completed += (_, _) => Resultado.Visibility = Visibility.Collapsed;
            Resultado.BeginAnimation(OpacityProperty, salida);
        }

        // Pequeño temblor del tablero al perder.
        private void Sacudir()
        {
            var temblor = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(420) };
            double[] valores = { -10, 9, -7, 6, -4, 2, 0 };

            for (int i = 0; i < valores.Length; i++)
            {
                temblor.KeyFrames.Add(new LinearDoubleKeyFrame(
                    valores[i],
                    KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60 * (i + 1)))));
            }

            Sacudida.BeginAnimation(TranslateTransform.XProperty, temblor);
        }

        private static SolidColorBrush Pincel(string hex)
        {
            var pincel = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            pincel.Freeze();
            return pincel;
        }
    }
}
