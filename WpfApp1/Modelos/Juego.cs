using System.Globalization;
using System.Windows.Media;

namespace WpfApp1.Modelos
{
    /// <summary>
    /// Un juego de la biblioteca / tienda.
    /// </summary>
    public sealed class Juego : Observable
    {
        private static readonly CultureInfo Es = new("es-ES");

        private string estado;
        private int segundosJugados;
        private double progreso;
        private string ultimaSesion;
        private bool enListaDeseos;

        public Juego(
            string id,
            string titulo,
            string generos,
            string glifo,
            Brush portada,
            bool jugable,
            string estado,
            string descripcion,
            decimal precio,
            int descuento = 0)
        {
            Id = id;
            Titulo = titulo;
            Generos = generos;
            Glifo = glifo;
            Portada = portada;
            Jugable = jugable;
            this.estado = estado;
            Descripcion = descripcion;
            Precio = precio;
            Descuento = descuento;
            ultimaSesion = jugable ? "Aún no has jugado" : "—";
        }

        public string Id { get; }

        public string Titulo { get; }

        // Géneros separados por " · " (también se usan para filtrar).
        public string Generos { get; }

        // Icono (Segoe Fluent Icons / MDL2) de la portada.
        public string Glifo { get; }

        public Brush Portada { get; }

        // Solo los juegos jugables se pueden abrir desde el launcher.
        public bool Jugable { get; }

        public string Descripcion { get; }

        // ── Tienda ──

        public decimal Precio { get; }

        public int Descuento { get; }

        public bool TieneDescuento => Descuento > 0;

        public decimal PrecioFinal => Math.Round(Precio * (100 - Descuento) / 100m, 2);

        public string PrecioTexto => Precio == 0 ? "Gratis" : PrecioFinal.ToString("0.00", Es) + " €";

        public string PrecioOriginalTexto => Precio.ToString("0.00", Es) + " €";

        public string DescuentoTexto => $"−{Descuento} %";

        // ── Estado en la biblioteca ──

        // Etiqueta de la portada: INSTALADO, PRÓXIMAMENTE...
        public string Estado
        {
            get => estado;
            set => Asignar(ref estado, value);
        }

        public int SegundosJugados
        {
            get => segundosJugados;
            set
            {
                if (Asignar(ref segundosJugados, value))
                {
                    Notificar(nameof(HorasTexto));
                }
            }
        }

        public string HorasTexto
        {
            get
            {
                if (!Jugable)
                {
                    return "Disponible pronto";
                }

                if (SegundosJugados < 60)
                {
                    return SegundosJugados == 0 ? "Sin jugar todavía" : "1 min jugado";
                }

                if (SegundosJugados < 3600)
                {
                    return $"{SegundosJugados / 60} min jugados";
                }

                return (SegundosJugados / 3600.0).ToString("0.#", Es) + " h jugadas";
            }
        }

        // Porcentaje de logros conseguidos.
        public double Progreso
        {
            get => progreso;
            set
            {
                if (Asignar(ref progreso, value))
                {
                    Notificar(nameof(ProgresoTexto));
                }
            }
        }

        public string ProgresoTexto => Progreso >= 100 ? "Completado" : $"{Progreso:0} %";

        public string UltimaSesion
        {
            get => ultimaSesion;
            set => Asignar(ref ultimaSesion, value);
        }

        public bool EnListaDeseos
        {
            get => enListaDeseos;
            set => Asignar(ref enListaDeseos, value);
        }
    }
}
