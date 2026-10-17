using System.Windows;
using System.Windows.Controls;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class TiendaVista : UserControl
    {
        public static readonly DependencyProperty ColumnasProperty =
            DependencyProperty.Register(nameof(Columnas), typeof(int), typeof(TiendaVista), new PropertyMetadata(3));

        public TiendaVista()
        {
            InitializeComponent();
        }

        public int Columnas
        {
            get => (int)GetValue(ColumnasProperty);
            set => SetValue(ColumnasProperty, value);
        }

        private Launcher? Datos => DataContext as Launcher;

        private void Jugar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Juego juego })
            {
                Datos?.Jugar(juego);
            }
        }

        private void Deseado_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Juego juego })
            {
                Datos?.AlternarDeseado(juego);
            }
        }

        private void Rejilla_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int columnas = Math.Clamp((int)(e.NewSize.Width / 250), 2, 5);

            if (columnas != Columnas)
            {
                Columnas = columnas;
            }
        }
    }
}
