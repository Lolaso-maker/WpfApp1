using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace WpfApp1.Modelos
{
    // Gestor de descargas simulado: solo una descarga avanza a la vez.
    public sealed partial class Launcher
    {
        private readonly DispatcherTimer relojDescargas = new() { Interval = TimeSpan.FromMilliseconds(400) };
        private readonly Random azar = new();

        public ObservableCollection<Descarga> Descargas { get; } = new();

        public ObservableCollection<Descarga> DescargasCompletadas { get; } = new();

        public Descarga? DescargaActual => Descargas.FirstOrDefault(d => d.EstaDescargando);

        public int DescargasPendientes => Descargas.Count;

        public bool HayDescargas => Descargas.Count > 0;

        public bool HayCompletadas => DescargasCompletadas.Count > 0;

        public bool HayDescargaActiva => DescargaActual is not null;

        public string BotonDescargasTexto => HayDescargaActiva ? "Pausar todo" : "Reanudar todo";

        public string BotonDescargasGlifo => HayDescargaActiva ? "" : "";

        public string ResumenDescargasTexto
        {
            get
            {
                if (!HayDescargas)
                {
                    return "No hay descargas pendientes.";
                }

                double restante = Descargas.Sum(d => d.TotalMb - d.DescargadoMb);
                string elementos = Descargas.Count == 1 ? "1 elemento" : $"{Descargas.Count} elementos";
                return $"{elementos} · faltan {Descarga.Tamano(restante)}";
            }
        }

        public void AlternarPausa(Descarga descarga)
        {
            if (descarga.EstaCompletada)
            {
                return;
            }

            if (descarga.EstaDescargando)
            {
                descarga.Estado = EstadoDescarga.EnPausa;
                descarga.VelocidadMb = 0;
            }
            else
            {
                // La que se reanuda pasa delante; la que estuviera bajando espera.
                foreach (var otra in Descargas.Where(d => d.EstaDescargando))
                {
                    otra.Estado = EstadoDescarga.EnCola;
                    otra.VelocidadMb = 0;
                }

                descarga.Estado = EstadoDescarga.Descargando;
            }

            ActualizarDescargas();
        }

        public void CancelarDescarga(Descarga descarga)
        {
            if (Descargas.Remove(descarga))
            {
                MostrarAviso("", "Descarga cancelada", $"{descarga.Titulo} · {descarga.Contenido}");
                ActualizarDescargas();
            }
        }

        public void AlternarTodasLasDescargas()
        {
            if (HayDescargaActiva)
            {
                foreach (var d in Descargas)
                {
                    d.Estado = EstadoDescarga.EnPausa;
                    d.VelocidadMb = 0;
                }
            }
            else
            {
                for (int i = 0; i < Descargas.Count; i++)
                {
                    Descargas[i].Estado = i == 0 ? EstadoDescarga.Descargando : EstadoDescarga.EnCola;
                }
            }

            ActualizarDescargas();
        }

        public void LimpiarCompletadas()
        {
            DescargasCompletadas.Clear();
            Notificar(nameof(HayCompletadas));
        }

        private void CrearDescargas()
        {
            Juego J(string id) => Juegos.First(j => j.Id == id);

            Descargas.Add(new Descarga(J("neon"), "Actualización 1.4", 1843, 1253, EstadoDescarga.Descargando));
            Descargas.Add(new Descarga(J("oceanfall"), "Expansión «Abismo»", 3277, 0, EstadoDescarga.EnCola));
            Descargas.Add(new Descarga(J("horizon"), "Pack de mapas «Ártico»", 921, 0, EstadoDescarga.EnCola));

            relojDescargas.Tick += AvanzarDescargas;
            ActualizarDescargas();
        }

        private void AvanzarDescargas(object? sender, EventArgs e)
        {
            var actual = DescargaActual;

            if (actual is null)
            {
                ActualizarDescargas();
                return;
            }

            // Velocidad que varía un poco, como en la vida real.
            double objetivo = 38 + azar.NextDouble() * 18;
            actual.VelocidadMb = actual.VelocidadMb <= 0
                ? objetivo
                : actual.VelocidadMb * 0.7 + objetivo * 0.3;

            actual.DescargadoMb += actual.VelocidadMb * relojDescargas.Interval.TotalSeconds;

            if (actual.DescargadoMb >= actual.TotalMb)
            {
                actual.Estado = EstadoDescarga.Completada;
                actual.VelocidadMb = 0;
                actual.CompletadaEl = DateTime.Now;

                Descargas.Remove(actual);
                DescargasCompletadas.Insert(0, actual);

                RegistrarActividad(TipoActividad.Descarga, $"{actual.Titulo}: descarga completada", actual.Contenido, notificar: true);
                MostrarAviso("", "Descarga completada", $"{actual.Titulo} · {actual.Contenido}");

                // Empieza la siguiente de la cola.
                var siguiente = Descargas.FirstOrDefault(d => d.Estado == EstadoDescarga.EnCola);

                if (siguiente is not null)
                {
                    siguiente.Estado = EstadoDescarga.Descargando;
                }
            }

            ActualizarDescargas();
        }

        private void ActualizarDescargas()
        {
            if (HayDescargaActiva)
            {
                relojDescargas.Start();
            }
            else
            {
                relojDescargas.Stop();
            }

            Notificar(nameof(DescargaActual));
            Notificar(nameof(DescargasPendientes));
            Notificar(nameof(HayDescargas));
            Notificar(nameof(HayCompletadas));
            Notificar(nameof(HayDescargaActiva));
            Notificar(nameof(BotonDescargasTexto));
            Notificar(nameof(BotonDescargasGlifo));
            Notificar(nameof(ResumenDescargasTexto));
        }
    }
}
