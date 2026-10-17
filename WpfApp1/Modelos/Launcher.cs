using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp1.Buscaminas;

namespace WpfApp1.Modelos
{
    /// <summary>
    /// Estado de todo el launcher para el usuario que ha iniciado sesión.
    /// Las vistas se enlazan a esta clase (es el DataContext de la ventana principal).
    /// </summary>
    public sealed partial class Launcher : Observable, IDisposable
    {
        public static readonly string[] Categorias =
        {
            "Todos", "Acción", "Aventura", "RPG", "Estrategia", "Indie", "Puzle"
        };

        private static readonly CultureInfo Es = new("es-ES");

        private static readonly string[] NombresOrden = { "Recientes", "A–Z", "Más jugados" };

        private readonly DatosGuardados datos;
        private readonly DispatcherTimer reloj;

        private string textoBusqueda = "";
        private string categoria = "Todos";
        private int orden;
        private bool jugando;

        public Launcher(string usuario)
        {
            Usuario = string.IsNullOrWhiteSpace(usuario) ? "Jugador" : usuario.Trim();
            NombreVisible = char.ToUpper(Usuario[0], Es) + Usuario[1..];
            Iniciales = NombreVisible[..1].ToUpper(Es);

            datos = Almacen.Cargar(Usuario);

            CrearJuegos();

            Biblioteca = new ListCollectionView(Juegos) { Filter = FiltrarBiblioteca };

            CrearLogros();
            CrearAmigos();
            CrearActividad();
            CrearDescargas();
            ActualizarBuscaminas();

            // Refresca textos como "hace 5 min" cada medio minuto.
            reloj = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            reloj.Tick += (_, _) => ActualizarTextosRelativos();
            reloj.Start();
        }

        // ───────────────────────── Eventos para la ventana ─────────────────────────

        // Mostrar un aviso flotante.
        public event EventHandler<Aviso>? AvisoSolicitado;

        // Abrir el Buscaminas con una dificultad.
        public event EventHandler<Dificultad>? JuegoSolicitado;

        // Cambiar de sección ("Biblioteca", "Tienda"...).
        public event EventHandler<string>? NavegacionSolicitada;

        // ───────────────────────── Usuario ─────────────────────────

        public string Usuario { get; }

        public string NombreVisible { get; }

        public string Iniciales { get; }

        public string Saludo
        {
            get
            {
                int hora = DateTime.Now.Hour;

                string saludo = hora >= 6 && hora < 12
                    ? "Buenos días"
                    : hora >= 12 && hora < 21 ? "Buenas tardes" : "Buenas noches";

                return $"{saludo}, {NombreVisible}";
            }
        }

        public string FechaTexto
        {
            get
            {
                string texto = DateTime.Now.ToString("dddd, d 'de' MMMM", Es);
                return char.ToUpper(texto[0], Es) + texto[1..];
            }
        }

        // true mientras la ventana del Buscaminas está abierta.
        public bool Jugando
        {
            get => jugando;
            set
            {
                if (Asignar(ref jugando, value))
                {
                    Notificar(nameof(EstadoTexto));
                }
            }
        }

        public string EstadoTexto => Jugando ? "Jugando a Buscaminas" : "En línea";

        // ───────────────────────── Juegos y biblioteca ─────────────────────────

        public ObservableCollection<Juego> Juegos { get; } = new();

        public Juego Buscaminas { get; private set; } = null!;

        // Vista filtrada (buscador + categoría + orden) de la biblioteca.
        public ListCollectionView Biblioteca { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                if (Asignar(ref textoBusqueda, value ?? ""))
                {
                    RefrescarBiblioteca();
                }
            }
        }

        public string Categoria
        {
            get => categoria;
            set
            {
                if (Asignar(ref categoria, value ?? "Todos"))
                {
                    RefrescarBiblioteca();
                }
            }
        }

        public string OrdenTexto => NombresOrden[orden];

        public int JuegosVisibles => Biblioteca.Count;

        public bool SinResultados => Biblioteca.Count == 0;

        public int CantidadDeseados => Juegos.Count(j => j.EnListaDeseos);

        public void CambiarOrden()
        {
            orden = (orden + 1) % NombresOrden.Length;

            using (Biblioteca.DeferRefresh())
            {
                Biblioteca.SortDescriptions.Clear();

                if (orden == 1)
                {
                    Biblioteca.SortDescriptions.Add(
                        new SortDescription(nameof(Juego.Titulo), ListSortDirection.Ascending));
                }
                else if (orden == 2)
                {
                    Biblioteca.SortDescriptions.Add(
                        new SortDescription(nameof(Juego.SegundosJugados), ListSortDirection.Descending));
                }
            }

            Notificar(nameof(OrdenTexto));
        }

        public void AlternarDeseado(Juego juego)
        {
            if (juego.Jugable)
            {
                return;
            }

            juego.EnListaDeseos = !juego.EnListaDeseos;

            if (juego.EnListaDeseos)
            {
                datos.Deseados.Add(juego.Id);
                MostrarAviso("", "Añadido a tu lista de deseos", $"Te avisaremos cuando {juego.Titulo} esté disponible.");
            }
            else
            {
                datos.Deseados.Remove(juego.Id);
                MostrarAviso("", "Quitado de tu lista de deseos", juego.Titulo);
            }

            Notificar(nameof(CantidadDeseados));
            Guardar();
        }

        private bool FiltrarBiblioteca(object? elemento)
        {
            if (elemento is not Juego juego)
            {
                return false;
            }

            bool categoriaOk = Categoria == "Todos" || Contiene(juego.Generos, Categoria);

            string texto = TextoBusqueda.Trim();

            bool textoOk = texto.Length == 0 ||
                           Contiene(juego.Titulo, texto) ||
                           Contiene(juego.Generos, texto);

            return categoriaOk && textoOk;
        }

        // Sin distinguir mayúsculas ni tildes ("accion" = "Acción").
        private static bool Contiene(string fuente, string valor) =>
            Es.CompareInfo.IndexOf(fuente, valor, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

        private void RefrescarBiblioteca()
        {
            Biblioteca.Refresh();
            Notificar(nameof(JuegosVisibles));
            Notificar(nameof(SinResultados));
        }

        private void CrearJuegos()
        {
            Brush P(string clave) => (Brush)Application.Current.FindResource(clave);

            Buscaminas = new Juego(
                "buscaminas", "Buscaminas", "Puzle · Clásico", "", P("PortadaBuscaminas"),
                jugable: true, estado: "INSTALADO",
                descripcion: "El clásico de siempre: despeja el campo sin pisar ninguna mina. Tres dificultades, récords y logros.",
                precio: 0);

            Juegos.Add(Buscaminas);
            Juegos.Add(Proximamente("eclipse", "Eclipse", "Acción · RPG", "", P("PortadaEclipse"), 39.99m, 0,
                "Un RPG de acción en un mundo donde el sol no ha vuelto a salir."));
            Juegos.Add(Proximamente("neon", "Neon City", "Aventura · Indie", "", P("PortadaNeon"), 19.99m, 25,
                "Recorre una ciudad de neón resolviendo misterios bajo la lluvia."));
            Juegos.Add(Proximamente("horizon", "Dark Horizon", "Acción · Shooter", "", P("PortadaHorizon"), 49.99m, 40,
                "Shooter táctico por equipos en mapas enormes."));
            Juegos.Add(Proximamente("wildlands", "Wildlands", "Aventura · Supervivencia", "", P("PortadaWildlands"), 29.99m, 0,
                "Sobrevive, construye y explora un continente salvaje."));
            Juegos.Add(Proximamente("kingdoms", "Kingdoms", "Estrategia", "", P("PortadaKingdoms"), 24.99m, 50,
                "Levanta tu reino y defiéndelo en batallas por turnos."));
            Juegos.Add(Proximamente("void", "Void", "RPG · Fantasía", "", P("PortadaVoid"), 34.99m, 0,
                "Una odisea de fantasía entre las estrellas."));
            Juegos.Add(Proximamente("oceanfall", "Oceanfall", "Aventura", "", P("PortadaOceanfall"), 14.99m, 30,
                "Explora las ruinas de una ciudad hundida."));
            Juegos.Add(Proximamente("afterlight", "Afterlight", "Indie · Historia", "", P("PortadaAfterlight"), 12.99m, 0,
                "Una historia íntima sobre la luz que queda al final del día."));

            foreach (var juego in Juegos)
            {
                juego.EnListaDeseos = datos.Deseados.Contains(juego.Id);
            }
        }

        private static Juego Proximamente(
            string id, string titulo, string generos, string glifo, Brush portada,
            decimal precio, int descuento, string descripcion) =>
            new(id, titulo, generos, glifo, portada, jugable: false, estado: "PRÓXIMAMENTE",
                descripcion: descripcion, precio: precio, descuento: descuento);

        // ───────────────────────── Ajustes ─────────────────────────

        public bool MostrarAvisos
        {
            get => datos.Ajustes.MostrarAvisos;
            set
            {
                datos.Ajustes.MostrarAvisos = value;
                Notificar();
                Guardar();
            }
        }

        public bool Sonidos
        {
            get => datos.Ajustes.Sonidos;
            set
            {
                datos.Ajustes.Sonidos = value;
                Notificar();
                Guardar();
            }
        }

        public bool PrimerClicSeguro
        {
            get => datos.Ajustes.PrimerClicSeguro;
            set
            {
                datos.Ajustes.PrimerClicSeguro = value;
                Notificar();
                Guardar();
            }
        }

        public Dificultad DificultadPredeterminada
        {
            get => Enum.TryParse(datos.Ajustes.Dificultad, out Dificultad d) ? d : Dificultad.Principiante;
            set
            {
                datos.Ajustes.Dificultad = value.ToString();
                Notificar();
                Notificar(nameof(DificultadPredeterminadaTexto));
                Notificar(nameof(MejorTiempoPredeterminadoTexto));
                Guardar();
            }
        }

        public string DificultadPredeterminadaTexto => Dificultades.Nombre(DificultadPredeterminada);

        // ───────────────────────── Acciones generales ─────────────────────────

        // Abre un juego. Los que no son jugables solo muestran un aviso.
        public void Jugar(Juego? juego = null, Dificultad? dificultad = null)
        {
            juego ??= Buscaminas;

            if (!juego.Jugable)
            {
                MostrarAviso("", "Próximamente", $"{juego.Titulo} todavía no está disponible. ¡Añádelo a tu lista de deseos!");
                return;
            }

            JuegoSolicitado?.Invoke(this, dificultad ?? DificultadPredeterminada);
        }

        public void Navegar(string seccion)
        {
            NavegacionSolicitada?.Invoke(this, seccion);
        }

        public void MostrarAviso(string glifo, string titulo, string texto)
        {
            if (MostrarAvisos)
            {
                AvisoSolicitado?.Invoke(this, new Aviso(glifo, titulo, texto));
            }
        }

        public void Guardar()
        {
            Almacen.Guardar(Usuario, datos);
        }

        public void Dispose()
        {
            reloj.Stop();
            relojDescargas.Stop();
            Guardar();
        }

        // Ejecuta una acción pasado un tiempo (para simular respuestas de amigos).
        private static void Despues(TimeSpan espera, Action accion)
        {
            var temporizador = new DispatcherTimer { Interval = espera };

            temporizador.Tick += (_, _) =>
            {
                temporizador.Stop();
                accion();
            };

            temporizador.Start();
        }

        private void ActualizarTextosRelativos()
        {
            ActualizarBuscaminas();
            Notificar(nameof(Saludo));
        }

        private static string HaceCuanto(DateTime fecha)
        {
            TimeSpan t = DateTime.Now - fecha;

            if (t.TotalMinutes < 1)
            {
                return "Hace un momento";
            }

            if (t.TotalMinutes < 60)
            {
                return $"Hace {(int)t.TotalMinutes} min";
            }

            if (t.TotalHours < 24)
            {
                return $"Hace {(int)t.TotalHours} h";
            }

            if (t.TotalDays < 2)
            {
                return "Ayer";
            }

            return $"Hace {(int)t.TotalDays} días";
        }
    }
}
