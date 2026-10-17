using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WpfApp1
{
    /// <summary>
    /// Pone la barra de título de Windows en oscuro (Windows 10 20H1+ y Windows 11)
    /// y, en Windows 11, del mismo color que la barra lateral.
    /// </summary>
    public static class VentanaOscura
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaBorderColor = 34;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int atributo, ref int valor, int tamano);

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint tipo);

        public static void Aplicar(Window ventana)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(ventana).Handle;

                int oscuro = 1;
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref oscuro, sizeof(int));

                // Colores en formato COLORREF (0x00BBGGRR).
                int titulo = 0x0016100E;  // #0E1016
                int borde = 0x00302620;   // #202630
                int texto = 0x00D6CDC9;   // #C9CDD6

                DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref titulo, sizeof(int));
                DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref borde, sizeof(int));
                DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref texto, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        // Sonidos del sistema (sin archivos de audio).
        public static void Sonido(bool victoria)
        {
            try
            {
                MessageBeep(victoria ? 0x40u : 0x10u);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
