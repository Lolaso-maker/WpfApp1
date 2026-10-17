using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfApp1.Modelos
{
    /// <summary>
    /// Clase base que avisa a la interfaz cuando cambia una propiedad
    /// (así los enlaces {Binding} se actualizan solos).
    /// </summary>
    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Notificar([CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        protected bool Asignar<T>(ref T campo, T valor, [CallerMemberName] string? propiedad = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor))
            {
                return false;
            }

            campo = valor;
            Notificar(propiedad);
            return true;
        }
    }
}
