using System.Globalization;
using System.Windows.Media;

namespace WpfApp1.Modelos
{
    public enum EstadoDescarga
    {
        Descargando,
        EnPausa,
        EnCola,
        Completada
    }

    /// <summary>
    /// Una descarga simulada (el launcher la hace avanzar con un temporizador).
    /// </summary>
    public sealed class Descarga : Observable
    {
        private static readonly CultureInfo Es = new("es-ES");

        private double descargadoMb;
        private double velocidadMb;
        private EstadoDescarga estado;
        private DateTime? completadaEl;

        public Descarga(Juego juego, string contenido, double totalMb, double descargadoMb, EstadoDescarga estado)
        {
            Juego = juego;
            Contenido = contenido;
            TotalMb = totalMb;
            this.descargadoMb = descargadoMb;
            this.estado = estado;
        }

        public Juego Juego { get; }

        public string Titulo => Juego.Titulo;

        public Brush Portada => Juego.Portada;

        public string Glifo => Juego.Glifo;

        // Qué se descarga: "Actualización 1.4", "Expansión"...
        public string Contenido { get; }

        public double TotalMb { get; }

        public double DescargadoMb
        {
            get => descargadoMb;
            set
            {
                if (Asignar(ref descargadoMb, Math.Min(value, TotalMb)))
                {
                    Notificar(nameof(Progreso));
                    Notificar(nameof(ProgresoTexto));
                    Notificar(nameof(TamanoTexto));
                    Notificar(nameof(DetalleTexto));
                }
            }
        }

        public double VelocidadMb
        {
            get => velocidadMb;
            set
            {
                if (Asignar(ref velocidadMb, value))
                {
                    Notificar(nameof(VelocidadTexto));
                    Notificar(nameof(DetalleTexto));
                }
            }
        }

        public EstadoDescarga Estado
        {
            get => estado;
            set
            {
                if (Asignar(ref estado, value))
                {
                    Notificar(nameof(EstadoTexto));
                    Notificar(nameof(VelocidadTexto));
                    Notificar(nameof(DetalleTexto));
                    Notificar(nameof(BotonTexto));
                    Notificar(nameof(BotonGlifo));
                    Notificar(nameof(EstaDescargando));
                    Notificar(nameof(EstaCompletada));
                }
            }
        }

        public DateTime? CompletadaEl
        {
            get => completadaEl;
            set
            {
                if (Asignar(ref completadaEl, value))
                {
                    Notificar(nameof(DetalleTexto));
                }
            }
        }

        public bool EstaDescargando => Estado == EstadoDescarga.Descargando;

        public bool EstaCompletada => Estado == EstadoDescarga.Completada;

        public double Progreso => TotalMb <= 0 ? 0 : DescargadoMb * 100 / TotalMb;

        public string ProgresoTexto => $"{Math.Floor(Progreso):0} %";

        public string TamanoTexto => $"{Tamano(DescargadoMb)} de {Tamano(TotalMb)}";

        public string VelocidadTexto => Estado switch
        {
            EstadoDescarga.Descargando => VelocidadMb.ToString("0.0", Es) + " MB/s",
            EstadoDescarga.EnPausa => "En pausa",
            EstadoDescarga.EnCola => "En cola",
            _ => "Completada"
        };

        public string EstadoTexto => Estado switch
        {
            EstadoDescarga.Descargando => "Descargando",
            EstadoDescarga.EnPausa => "En pausa",
            EstadoDescarga.EnCola => "En cola",
            _ => "Completada"
        };

        // Línea secundaria de la fila: tamaño, velocidad y tiempo restante.
        public string DetalleTexto
        {
            get
            {
                if (Estado == EstadoDescarga.Completada)
                {
                    return CompletadaEl is DateTime fecha
                        ? $"{Tamano(TotalMb)} · terminada a las {fecha:HH:mm}"
                        : Tamano(TotalMb);
                }

                if (Estado == EstadoDescarga.Descargando && VelocidadMb > 0)
                {
                    double segundos = (TotalMb - DescargadoMb) / VelocidadMb;
                    return $"{TamanoTexto} · {Restante(segundos)}";
                }

                return TamanoTexto;
            }
        }

        public string BotonTexto => Estado == EstadoDescarga.Descargando ? "Pausar" : "Reanudar";

        public string BotonGlifo => Estado == EstadoDescarga.Descargando ? "" : "";

        public static string Tamano(double mb) =>
            mb >= 1024
                ? (mb / 1024).ToString("0.0", Es) + " GB"
                : mb.ToString("0", Es) + " MB";

        private static string Restante(double segundos)
        {
            if (segundos < 60)
            {
                return "menos de 1 min";
            }

            if (segundos < 3600)
            {
                return $"quedan {Math.Ceiling(segundos / 60):0} min";
            }

            return $"quedan {segundos / 3600:0.#} h";
        }
    }
}
