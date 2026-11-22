using System.Data;
using MySql.Data.MySqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Acceso a la base de datos MySQL del catálogo.
    /// Centraliza la cadena de conexión y la apertura y cierre de la conexión.
    /// La base de datos y el usuario de conexión se crean con el script database.sql.
    /// </summary>
    public static class DatabaseHelper
    {
        // Código de error de MySql.Data cuando no se puede contactar con el servidor
        // (servicio parado, puerto incorrecto, equipo inaccesible...).
        public const int ErrorSinConexion = 1042;

        // Código de error de MySQL al insertar un valor repetido en una columna UNIQUE.
        public const int ErrorDuplicado = 1062;

        private const string CadenaConexion =
            "Server=localhost;" +
            "Port=3306;" +
            "Database=catalogo_videojuegos;" +
            "Uid=catalogo_app;" +
            "Pwd=Catalogo#2026;" +
            "SslMode=Disabled;" +
            "AllowPublicKeyRetrieval=True;" +
            "Connection Timeout=5;";

        /// <summary>
        /// Crea una conexión nueva y la abre.
        /// Si MySQL no está disponible lanza una MySqlException que se captura en la ventana.
        /// </summary>
        public static async Task<MySqlConnection> AbrirConexionAsync()
        {
            MySqlConnection conexion = new MySqlConnection(CadenaConexion);

            try
            {
                await conexion.OpenAsync();
                return conexion;
            }
            catch
            {
                // Si no se ha podido abrir, se libera antes de propagar el error.
                conexion.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Cierra la conexión (si está abierta) y libera sus recursos.
        /// </summary>
        public static void CerrarConexion(MySqlConnection? conexion)
        {
            if (conexion == null)
            {
                return;
            }

            if (conexion.State != ConnectionState.Closed)
            {
                conexion.Close();
            }

            conexion.Dispose();
        }
    }
}
