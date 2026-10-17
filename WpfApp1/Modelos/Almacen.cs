using System.IO;
using System.Text.Json;

namespace WpfApp1.Modelos
{
    /// <summary>
    /// Lo que se guarda en disco para cada usuario
    /// (%AppData%\GameStation\&lt;usuario&gt;.json).
    /// </summary>
    public sealed class DatosGuardados
    {
        public AjustesGuardados Ajustes { get; set; } = new();

        // Clave = nombre de la dificultad.
        public Dictionary<string, EstadisticaDificultad> Buscaminas { get; set; } = new();

        public int RachaActual { get; set; }

        public int MejorRacha { get; set; }

        public int SegundosJugados { get; set; }

        public DateTime? UltimaPartida { get; set; }

        // Clave = id del logro, valor = fecha en que se consiguió.
        public Dictionary<string, DateTime> Logros { get; set; } = new();

        public List<string> Deseados { get; set; } = new();

        public List<string> AmigosAnadidos { get; set; } = new();

        public List<ActividadGuardada> Actividad { get; set; } = new();
    }

    public sealed class AjustesGuardados
    {
        public bool MostrarAvisos { get; set; } = true;

        public bool Sonidos { get; set; } = true;

        public bool PrimerClicSeguro { get; set; } = true;

        public string Dificultad { get; set; } = "Principiante";
    }

    public sealed class EstadisticaDificultad
    {
        public int Partidas { get; set; }

        public int Victorias { get; set; }

        public int? MejorTiempo { get; set; }
    }

    public sealed class ActividadGuardada
    {
        public string Tipo { get; set; } = "";

        public string Titulo { get; set; } = "";

        public string Detalle { get; set; } = "";

        public DateTime Fecha { get; set; }
    }

    /// <summary>
    /// Lee y escribe los datos del usuario en JSON.
    /// </summary>
    public static class Almacen
    {
        private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

        private static string Carpeta =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GameStation");

        private static string Ruta(string usuario)
        {
            // Solo letras y números en el nombre del archivo.
            string limpio = new string(usuario.Where(char.IsLetterOrDigit).ToArray());

            if (limpio.Length == 0)
            {
                limpio = "jugador";
            }

            return Path.Combine(Carpeta, limpio.ToLowerInvariant() + ".json");
        }

        public static DatosGuardados Cargar(string usuario)
        {
            try
            {
                string ruta = Ruta(usuario);

                if (File.Exists(ruta))
                {
                    return JsonSerializer.Deserialize<DatosGuardados>(File.ReadAllText(ruta), Opciones)
                           ?? new DatosGuardados();
                }
            }
            catch (Exception)
            {
                // Archivo dañado o sin permisos: se empieza de cero.
            }

            return new DatosGuardados();
        }

        public static void Guardar(string usuario, DatosGuardados datos)
        {
            try
            {
                Directory.CreateDirectory(Carpeta);
                File.WriteAllText(Ruta(usuario), JsonSerializer.Serialize(datos, Opciones));
            }
            catch (Exception)
            {
                // Si no se puede guardar, el launcher sigue funcionando igual.
            }
        }
    }
}
