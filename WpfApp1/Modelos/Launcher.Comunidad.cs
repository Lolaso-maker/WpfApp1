using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1.Modelos
{
    // Amigos, actividad y notificaciones.
    public sealed partial class Launcher
    {
        private const int MaximoActividadGuardada = 60;
        private const int MaximoNotificaciones = 8;

        private bool soloConectados;
        private int notificacionesSinLeer;
        private string filtroActividad = "Todo";

        public ObservableCollection<Amigo> Amigos { get; } = new();

        public ListCollectionView AmigosFiltrados { get; private set; } = null!;

        public ObservableCollection<Actividad> Actividad { get; } = new();

        public ListCollectionView ActividadFiltrada { get; private set; } = null!;

        public ObservableCollection<Actividad> Notificaciones { get; } = new();

        public int AmigosConectados => Amigos.Count(a => a.EstaConectado);

        public bool SoloConectados
        {
            get => soloConectados;
            set
            {
                if (Asignar(ref soloConectados, value))
                {
                    AmigosFiltrados.Refresh();
                }
            }
        }

        // "Todo", "Partidas", "Logros", "Amigos", "Sistema".
        public string FiltroActividad
        {
            get => filtroActividad;
            set
            {
                if (Asignar(ref filtroActividad, value ?? "Todo"))
                {
                    ActividadFiltrada.Refresh();
                    Notificar(nameof(ActividadVacia));
                }
            }
        }

        public bool ActividadVacia => ActividadFiltrada.Count == 0;

        public int NotificacionesSinLeer
        {
            get => notificacionesSinLeer;
            private set
            {
                if (Asignar(ref notificacionesSinLeer, value))
                {
                    Notificar(nameof(HayNotificacionesSinLeer));
                }
            }
        }

        public bool HayNotificacionesSinLeer => NotificacionesSinLeer > 0;

        public void MarcarNotificacionesLeidas()
        {
            NotificacionesSinLeer = 0;
        }

        public void Invitar(Amigo amigo)
        {
            if (!amigo.PuedeInvitar)
            {
                return;
            }

            amigo.Invitado = true;

            RegistrarActividad(TipoActividad.Amigo, $"Has retado a {amigo.Nombre}", "Partida de Buscaminas");
            MostrarAviso("", "Reto enviado", $"{amigo.Nombre} recibirá tu invitación al Buscaminas.");

            // Respuesta simulada del amigo unos segundos después.
            Despues(TimeSpan.FromSeconds(5), () =>
            {
                RegistrarActividad(
                    TipoActividad.Amigo,
                    $"{amigo.Nombre} ha aceptado tu reto",
                    "¡A ver quién despeja antes el tablero!",
                    notificar: true,
                    propia: false);

                MostrarAviso("", $"{amigo.Nombre} ha aceptado", "Abre el Buscaminas cuando quieras.");
            });
        }

        /// <summary>
        /// Envía una solicitud de amistad. Devuelve un mensaje de error o null si ha ido bien.
        /// </summary>
        public string? AgregarAmigo(string nombre)
        {
            nombre = (nombre ?? "").Trim();

            if (nombre.Length < 3)
            {
                return "Escribe un nombre de al menos 3 letras.";
            }

            if (nombre.Length > 30)
            {
                return "El nombre es demasiado largo.";
            }

            if (Amigos.Any(a => string.Equals(a.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
            {
                return "Esa persona ya está en tu lista.";
            }

            Amigos.Add(new Amigo(nombre, ColorAmigo(nombre), EstadoAmigo.Pendiente, "Solicitud enviada"));
            datos.AmigosAnadidos.Add(nombre);

            RegistrarActividad(TipoActividad.Amigo, $"Solicitud de amistad a {nombre}", "Pendiente de respuesta");
            MostrarAviso("", "Solicitud enviada", $"Has invitado a {nombre} a ser tu amigo.");
            Notificar(nameof(AmigosConectados));
            Guardar();
            return null;
        }

        public void RegistrarActividad(
            TipoActividad tipo,
            string titulo,
            string detalle,
            bool notificar = false,
            bool propia = true)
        {
            var entrada = new Actividad(tipo, titulo, detalle, DateTime.Now, propia);

            Actividad.Insert(0, entrada);

            if (propia)
            {
                datos.Actividad.Insert(0, new ActividadGuardada
                {
                    Tipo = tipo.ToString(),
                    Titulo = titulo,
                    Detalle = detalle,
                    Fecha = entrada.Fecha
                });

                if (datos.Actividad.Count > MaximoActividadGuardada)
                {
                    datos.Actividad.RemoveRange(MaximoActividadGuardada, datos.Actividad.Count - MaximoActividadGuardada);
                }
            }

            if (notificar)
            {
                Notificaciones.Insert(0, entrada);

                while (Notificaciones.Count > MaximoNotificaciones)
                {
                    Notificaciones.RemoveAt(Notificaciones.Count - 1);
                }

                NotificacionesSinLeer++;
            }

            Notificar(nameof(ActividadVacia));
        }

        private void CrearAmigos()
        {
            Amigos.Add(new Amigo("Lucía Martín", Color.FromRgb(0xF4, 0x72, 0xB6), EstadoAmigo.Jugando, "Jugando a Buscaminas"));
            Amigos.Add(new Amigo("Irene López", Color.FromRgb(0x34, 0xD3, 0x99), EstadoAmigo.Jugando, "Jugando a Buscaminas · Experto"));
            Amigos.Add(new Amigo("Marcos Ruiz", Color.FromRgb(0x60, 0xA5, 0xFA), EstadoAmigo.EnLinea, "En línea"));
            Amigos.Add(new Amigo("Dani Pérez", Color.FromRgb(0xA7, 0x8B, 0xFA), EstadoAmigo.EnLinea, "Mirando la tienda"));
            Amigos.Add(new Amigo("Hugo Navarro", Color.FromRgb(0xF8, 0x71, 0x71), EstadoAmigo.EnLinea, "En línea"));
            Amigos.Add(new Amigo("Sara Gil", Color.FromRgb(0xFB, 0xBF, 0x24), EstadoAmigo.Ausente, "Ausente · hace 15 min"));
            Amigos.Add(new Amigo("Pablo Ortega", Color.FromRgb(0x22, 0xD3, 0xEE), EstadoAmigo.Desconectado, "Desconectado · hace 2 h"));
            Amigos.Add(new Amigo("Nerea Sanz", Color.FromRgb(0xFB, 0x92, 0x3C), EstadoAmigo.Desconectado, "Desconectado · ayer"));

            foreach (string nombre in datos.AmigosAnadidos)
            {
                Amigos.Add(new Amigo(nombre, ColorAmigo(nombre), EstadoAmigo.Pendiente, "Solicitud enviada"));
            }

            AmigosFiltrados = new ListCollectionView(Amigos)
            {
                Filter = o => !SoloConectados || (o is Amigo a && a.EstaConectado)
            };

            AmigosFiltrados.SortDescriptions.Add(new SortDescription(nameof(Amigo.Estado), ListSortDirection.Ascending));
        }

        private static Color ColorAmigo(string nombre)
        {
            Color[] colores =
            {
                Color.FromRgb(0x60, 0xA5, 0xFA),
                Color.FromRgb(0xF4, 0x72, 0xB6),
                Color.FromRgb(0x34, 0xD3, 0x99),
                Color.FromRgb(0xFB, 0xBF, 0x24),
                Color.FromRgb(0xA7, 0x8B, 0xFA),
                Color.FromRgb(0x22, 0xD3, 0xEE)
            };

            int suma = nombre.Sum(c => c);
            return colores[suma % colores.Length];
        }

        private void CrearActividad()
        {
            DateTime ahora = DateTime.Now;

            var lista = new List<Actividad>
            {
                // Actividad de amigos (de ejemplo, no se guarda).
                new(TipoActividad.Amigo, "Lucía ha batido su récord", "Buscaminas · Experto en 142 s", ahora.AddMinutes(-25), propia: false),
                new(TipoActividad.Logro, "Irene ha conseguido «Experto en explosivos»", "Buscaminas", ahora.AddHours(-3), propia: false),
                new(TipoActividad.Amigo, "Marcos ha añadido Neon City a su lista de deseos", "Tienda", ahora.AddDays(-1).AddHours(-2), propia: false),
                new(TipoActividad.Sistema, "Neon City: actualización 1.4 disponible", "Se está descargando automáticamente", ahora.AddMinutes(-40), propia: false)
            };

            if (datos.Actividad.Count == 0)
            {
                datos.Actividad.Add(new ActividadGuardada
                {
                    Tipo = nameof(TipoActividad.Sistema),
                    Titulo = "¡Bienvenido a GameStation!",
                    Detalle = "Tu primer juego, el Buscaminas, ya está en tu biblioteca.",
                    Fecha = ahora
                });
            }

            foreach (var g in datos.Actividad)
            {
                var tipo = Enum.TryParse(g.Tipo, out TipoActividad t) ? t : TipoActividad.Sistema;
                lista.Add(new Actividad(tipo, g.Titulo, g.Detalle, g.Fecha));
            }

            foreach (var entrada in lista.OrderByDescending(a => a.Fecha))
            {
                Actividad.Add(entrada);
            }

            ActividadFiltrada = new ListCollectionView(Actividad) { Filter = FiltrarActividad };

            // Notificaciones iniciales.
            Notificaciones.Add(new Actividad(TipoActividad.Amigo, "Lucía te ha retado a una partida", "Buscaminas · Principiante", ahora.AddMinutes(-10), propia: false));
            Notificaciones.Add(lista[3]);
            NotificacionesSinLeer = 2;
        }

        private bool FiltrarActividad(object? elemento)
        {
            if (elemento is not Actividad a)
            {
                return false;
            }

            return FiltroActividad switch
            {
                "Partidas" => a.Tipo is TipoActividad.Partida or TipoActividad.Victoria,
                "Logros" => a.Tipo == TipoActividad.Logro,
                "Amigos" => a.Tipo == TipoActividad.Amigo || !a.Propia,
                "Sistema" => a.Tipo is TipoActividad.Sistema or TipoActividad.Descarga,
                _ => true
            };
        }
    }
}
