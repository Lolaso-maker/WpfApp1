namespace WpfApp1.Buscaminas
{
    public enum Dificultad
    {
        Principiante,
        Intermedio,
        Experto
    }

    /// <summary>
    /// Tamaño del tablero y número de minas de cada dificultad
    /// (los mismos valores que el Buscaminas clásico de Windows).
    /// </summary>
    public static class Dificultades
    {
        public static readonly Dificultad[] Todas =
        {
            Dificultad.Principiante,
            Dificultad.Intermedio,
            Dificultad.Experto
        };

        public static int Filas(Dificultad d) => d switch
        {
            Dificultad.Intermedio => 16,
            Dificultad.Experto => 16,
            _ => 9
        };

        public static int Columnas(Dificultad d) => d switch
        {
            Dificultad.Intermedio => 16,
            Dificultad.Experto => 30,
            _ => 9
        };

        public static int Minas(Dificultad d) => d switch
        {
            Dificultad.Intermedio => 40,
            Dificultad.Experto => 99,
            _ => 10
        };

        public static string Nombre(Dificultad d) => d switch
        {
            Dificultad.Intermedio => "Intermedio",
            Dificultad.Experto => "Experto",
            _ => "Principiante"
        };

        public static string Descripcion(Dificultad d) =>
            $"{Columnas(d)} × {Filas(d)} · {Minas(d)} minas";
    }
}
