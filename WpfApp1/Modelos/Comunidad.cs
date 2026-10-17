using System.Globalization;
using System.Windows.Media;

namespace WpfApp1.Modelos
{
    public enum EstadoAmigo
    {
        Jugando,
        EnLinea,
        Ausente,
        Desconectado,
        Pendiente
    }

    /// <summary>
    /// Un amigo de la lista.
    /// </summary>
    public sealed class Amigo : Observable
    {
        private bool invitado;

        public Amigo(string nombre, Color color, EstadoAmigo estado, string detalle)
        {
            Nombre = nombre;
            Estado = estado;
            Detalle = detalle;

            var pincel = new SolidColorBrush(color);
            pincel.Freeze();
            Color = pincel;

            string[] partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Iniciales = partes.Length switch
            {
                0 => "?",
                1 => partes[0][..1].ToUpperInvariant(),
                _ => (partes[0][..1] + partes[1][..1]).ToUpperInvariant()
            };
        }

        public string Nombre { get; }

        public string Iniciales { get; }

        public Brush Color { get; }

        public EstadoAmigo Estado { get; }

        // Texto bajo el nombre: "Jugando a Buscaminas", "Desconectado · hace 2 h"...
        public string Detalle { get; }

        public bool EstaConectado => Estado is EstadoAmigo.EnLinea or EstadoAmigo.Jugando or EstadoAmigo.Ausente;

        public bool Invitado
        {
            get => invitado;
            set
            {
                if (Asignar(ref invitado, value))
                {
                    Notificar(nameof(PuedeInvitar));
                    Notificar(nameof(BotonTexto));
                }
            }
        }

        public bool PuedeInvitar => EstaConectado && !Invitado;

        public string BotonTexto => Invitado ? "Invitado" : "Invitar";
    }

    public enum TipoActividad
    {
        Partida,
        Victoria,
        Logro,
        Descarga,
        Amigo,
        Sistema
    }

    /// <summary>
    /// Una entrada del historial de actividad / notificaciones.
    /// </summary>
    public sealed class Actividad
    {
        private static readonly CultureInfo Es = new("es-ES");

        public Actividad(TipoActividad tipo, string titulo, string detalle, DateTime fecha, bool propia = true)
        {
            Tipo = tipo;
            Titulo = titulo;
            Detalle = detalle;
            Fecha = fecha;
            Propia = propia;
        }

        public TipoActividad Tipo { get; }

        public string Titulo { get; }

        public string Detalle { get; }

        public DateTime Fecha { get; }

        // false = actividad de un amigo (no se guarda en disco).
        public bool Propia { get; }

        public string Glifo => Tipo switch
        {
            TipoActividad.Victoria => "",
            TipoActividad.Logro => "",
            TipoActividad.Descarga => "",
            TipoActividad.Amigo => "",
            TipoActividad.Sistema => "",
            _ => ""
        };

        public string FechaTexto
        {
            get
            {
                DateTime hoy = DateTime.Today;

                if (Fecha.Date == hoy)
                {
                    return "Hoy, " + Fecha.ToString("HH:mm", Es);
                }

                if (Fecha.Date == hoy.AddDays(-1))
                {
                    return "Ayer, " + Fecha.ToString("HH:mm", Es);
                }

                return Fecha.ToString("d MMM, HH:mm", Es);
            }
        }
    }

    /// <summary>
    /// Un logro del Buscaminas.
    /// </summary>
    public sealed class Logro : Observable
    {
        private static readonly CultureInfo Es = new("es-ES");

        private DateTime? fecha;

        public Logro(string id, string titulo, string descripcion, string glifo)
        {
            Id = id;
            Titulo = titulo;
            Descripcion = descripcion;
            Glifo = glifo;
        }

        public string Id { get; }

        public string Titulo { get; }

        public string Descripcion { get; }

        public string Glifo { get; }

        public DateTime? Fecha
        {
            get => fecha;
            set
            {
                if (Asignar(ref fecha, value))
                {
                    Notificar(nameof(Desbloqueado));
                    Notificar(nameof(FechaTexto));
                }
            }
        }

        public bool Desbloqueado => Fecha.HasValue;

        public string FechaTexto => Fecha is DateTime f
            ? "Conseguido el " + f.ToString("d MMM yyyy", Es)
            : "Bloqueado";
    }

    /// <summary>
    /// Fila de la tabla de estadísticas por dificultad (perfil).
    /// </summary>
    public sealed class ResumenDificultad
    {
        public ResumenDificultad(string nombre, string detalle, int partidas, int victorias, int? mejorTiempo)
        {
            Nombre = nombre;
            Detalle = detalle;
            Partidas = partidas;
            Victorias = victorias;
            MejorTiempoTexto = mejorTiempo is int t ? $"{t} s" : "—";
            PorcentajeTexto = partidas == 0 ? "—" : $"{victorias * 100 / partidas} %";
        }

        public string Nombre { get; }

        public string Detalle { get; }

        public int Partidas { get; }

        public int Victorias { get; }

        public string MejorTiempoTexto { get; }

        public string PorcentajeTexto { get; }
    }

    /// <summary>
    /// Mensaje emergente del launcher.
    /// </summary>
    public sealed record Aviso(string Glifo, string Titulo, string Texto);
}
