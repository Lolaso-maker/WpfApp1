using System.Collections.ObjectModel;
using WpfApp1.Buscaminas;

namespace WpfApp1.Modelos
{
    /// <summary>
    /// Resultado de registrar una partida (lo usa la ventana del juego).
    /// </summary>
    public sealed record ResultadoPartida(bool NuevoRecord, int? MejorTiempo, IReadOnlyList<Logro> NuevosLogros);

    // Estadísticas, logros y nivel del jugador.
    public sealed partial class Launcher
    {
        private const int XpPorNivel = 150;

        public ObservableCollection<Logro> Logros { get; } = new();

        public ObservableCollection<ResumenDificultad> ResumenDificultades { get; } = new();

        public int PartidasTotales => datos.Buscaminas.Values.Sum(e => e.Partidas);

        public int VictoriasTotales => datos.Buscaminas.Values.Sum(e => e.Victorias);

        public string PorcentajeVictoriasTexto =>
            PartidasTotales == 0 ? "—" : $"{VictoriasTotales * 100 / PartidasTotales} %";

        public int RachaActual => datos.RachaActual;

        public int MejorRacha => datos.MejorRacha;

        public string TiempoJugadoTexto
        {
            get
            {
                int s = datos.SegundosJugados;

                if (s < 60)
                {
                    return $"{s} s";
                }

                if (s < 3600)
                {
                    return $"{s / 60} min";
                }

                return $"{s / 3600} h {s % 3600 / 60} min";
            }
        }

        public int LogrosConseguidos => Logros.Count(l => l.Desbloqueado);

        public string LogrosTexto => $"{LogrosConseguidos} de {Logros.Count}";

        public double LogrosPorcentaje => Logros.Count == 0 ? 0 : LogrosConseguidos * 100.0 / Logros.Count;

        public int XpTotal => PartidasTotales * 10 + VictoriasTotales * 25 + LogrosConseguidos * 50;

        public int Nivel => 1 + XpTotal / XpPorNivel;

        public double ProgresoNivel => XpTotal % XpPorNivel * 100.0 / XpPorNivel;

        public string XpTexto => $"{XpTotal % XpPorNivel} / {XpPorNivel} XP";

        public string MejorTiempoPredeterminadoTexto =>
            MejorTiempo(DificultadPredeterminada) is int t ? $"{t} s" : "—";

        public int? MejorTiempo(Dificultad dificultad) => Estadistica(dificultad).MejorTiempo;

        public EstadisticaDificultad Estadistica(Dificultad dificultad)
        {
            string clave = dificultad.ToString();

            if (!datos.Buscaminas.TryGetValue(clave, out var estadistica))
            {
                estadistica = new EstadisticaDificultad();
                datos.Buscaminas[clave] = estadistica;
            }

            return estadistica;
        }

        /// <summary>
        /// Guarda el resultado de una partida terminada y comprueba los logros.
        /// </summary>
        public ResultadoPartida RegistrarPartida(Dificultad dificultad, bool ganada, int segundos, bool usoBanderas)
        {
            var estadistica = Estadistica(dificultad);
            bool nuevoRecord = false;

            estadistica.Partidas++;
            datos.SegundosJugados += segundos;
            datos.UltimaPartida = DateTime.Now;

            string nombre = Dificultades.Nombre(dificultad);

            if (ganada)
            {
                estadistica.Victorias++;
                datos.RachaActual++;
                datos.MejorRacha = Math.Max(datos.MejorRacha, datos.RachaActual);

                if (estadistica.MejorTiempo is null || segundos < estadistica.MejorTiempo)
                {
                    nuevoRecord = estadistica.MejorTiempo is not null;
                    estadistica.MejorTiempo = segundos;
                }

                RegistrarActividad(
                    TipoActividad.Victoria,
                    "Victoria en Buscaminas",
                    $"{nombre} · {segundos} s" + (nuevoRecord ? " · ¡nuevo récord!" : ""));
            }
            else
            {
                datos.RachaActual = 0;

                RegistrarActividad(
                    TipoActividad.Partida,
                    "Partida de Buscaminas",
                    $"{nombre} · mina pisada a los {segundos} s");
            }

            // Logros.
            var nuevos = new List<Logro>();

            void Comprobar(string id, bool condicion)
            {
                var logro = Logros.First(l => l.Id == id);

                if (condicion && !logro.Desbloqueado)
                {
                    logro.Fecha = DateTime.Now;
                    datos.Logros[id] = logro.Fecha.Value;
                    nuevos.Add(logro);
                }
            }

            Comprobar("primer-paso", PartidasTotales >= 1);
            Comprobar("primera-victoria", VictoriasTotales >= 1);
            Comprobar("pies-ligeros", ganada && dificultad == Dificultad.Principiante && segundos < 30);
            Comprobar("estratega", Estadistica(Dificultad.Intermedio).Victorias >= 1);
            Comprobar("experto", Estadistica(Dificultad.Experto).Victorias >= 1);
            Comprobar("constancia", PartidasTotales >= 10);
            Comprobar("racha", datos.RachaActual >= 3);
            Comprobar("sin-banderas", ganada && !usoBanderas);

            foreach (var logro in nuevos)
            {
                RegistrarActividad(TipoActividad.Logro, $"Logro conseguido: {logro.Titulo}", logro.Descripcion, notificar: true);
                MostrarAviso("", "¡Logro desbloqueado!", logro.Titulo);
            }

            ActualizarBuscaminas();
            Guardar();

            return new ResultadoPartida(nuevoRecord, estadistica.MejorTiempo, nuevos);
        }

        // Partidas abandonadas: solo suman tiempo jugado.
        public void RegistrarTiempo(int segundos)
        {
            if (segundos <= 0)
            {
                return;
            }

            datos.SegundosJugados += segundos;
            datos.UltimaPartida = DateTime.Now;
            ActualizarBuscaminas();
            Guardar();
        }

        public void RestablecerEstadisticas()
        {
            datos.Buscaminas.Clear();
            datos.Logros.Clear();
            datos.RachaActual = 0;
            datos.MejorRacha = 0;
            datos.SegundosJugados = 0;
            datos.UltimaPartida = null;

            foreach (var logro in Logros)
            {
                logro.Fecha = null;
            }

            RegistrarActividad(TipoActividad.Sistema, "Estadísticas restablecidas", "Buscaminas");
            ActualizarBuscaminas();
            Guardar();
            MostrarAviso("", "Estadísticas restablecidas", "Tus partidas, récords y logros vuelven a cero.");
        }

        private void CrearLogros()
        {
            Logros.Add(new Logro("primer-paso", "Primer paso", "Juega tu primera partida.", ""));
            Logros.Add(new Logro("primera-victoria", "Primera victoria", "Gana una partida.", ""));
            Logros.Add(new Logro("pies-ligeros", "Pies ligeros", "Gana en Principiante en menos de 30 s.", ""));
            Logros.Add(new Logro("estratega", "Estratega", "Gana una partida en Intermedio.", ""));
            Logros.Add(new Logro("experto", "Experto en explosivos", "Gana una partida en Experto.", ""));
            Logros.Add(new Logro("constancia", "Constancia", "Juega 10 partidas.", ""));
            Logros.Add(new Logro("racha", "En racha", "Gana 3 partidas seguidas.", ""));
            Logros.Add(new Logro("sin-banderas", "A ojo", "Gana sin poner ninguna bandera.", ""));

            foreach (var logro in Logros)
            {
                if (datos.Logros.TryGetValue(logro.Id, out DateTime fecha))
                {
                    logro.Fecha = fecha;
                }
            }
        }

        // Actualiza la tarjeta del Buscaminas y todos los datos del perfil.
        private void ActualizarBuscaminas()
        {
            Buscaminas.SegundosJugados = datos.SegundosJugados;
            Buscaminas.Progreso = LogrosPorcentaje;
            Buscaminas.UltimaSesion = datos.UltimaPartida is DateTime f
                ? "Jugado " + HaceCuanto(f).ToLowerInvariant()
                : "Aún no has jugado";

            ResumenDificultades.Clear();

            foreach (var d in Dificultades.Todas)
            {
                var e = Estadistica(d);
                ResumenDificultades.Add(new ResumenDificultad(
                    Dificultades.Nombre(d), Dificultades.Descripcion(d), e.Partidas, e.Victorias, e.MejorTiempo));
            }

            Notificar(nameof(PartidasTotales));
            Notificar(nameof(VictoriasTotales));
            Notificar(nameof(PorcentajeVictoriasTexto));
            Notificar(nameof(RachaActual));
            Notificar(nameof(MejorRacha));
            Notificar(nameof(TiempoJugadoTexto));
            Notificar(nameof(LogrosConseguidos));
            Notificar(nameof(LogrosTexto));
            Notificar(nameof(LogrosPorcentaje));
            Notificar(nameof(XpTotal));
            Notificar(nameof(Nivel));
            Notificar(nameof(ProgresoNivel));
            Notificar(nameof(XpTexto));
            Notificar(nameof(MejorTiempoPredeterminadoTexto));
        }
    }
}
