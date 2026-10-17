using System.Windows;
using System.Windows.Controls;
using WpfApp1.Buscaminas;
using WpfApp1.Modelos;

namespace WpfApp1.Vistas
{
    public partial class InicioVista : UserControl
    {
        // Columnas de la cuadrícula de juegos (se adapta al ancho).
        public static readonly DependencyProperty ColumnasProperty =
            DependencyProperty.Register(nameof(Columnas), typeof(int), typeof(InicioVista), new PropertyMetadata(4));

        public InicioVista()
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
            Datos?.Jugar();
        }

        private void JugarDificultad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: Dificultad dificultad })
            {
                Datos?.Jugar(null, dificultad);
            }
        }

        private void Tienda_Click(object sender, RoutedEventArgs e)
        {
            Datos?.Navegar("Destacados");
        }

        private void Ordenar_Click(object sender, RoutedEventArgs e)
        {
            Datos?.CambiarOrden();
        }

        private void Categoria_Checked(object sender, RoutedEventArgs e)
        {
            if (Datos is not null && e.OriginalSource is RadioButton chip)
            {
                Datos.Categoria = chip.Content?.ToString() ?? "Todos";
            }
        }

        private void Juego_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement { DataContext: Juego juego })
            {
                Datos?.Jugar(juego);
            }
        }

        private void Rejilla_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int columnas = Math.Clamp((int)(e.NewSize.Width / 220), 2, 6);

            if (columnas != Columnas)
            {
                Columnas = columnas;
            }
        }
    }
}
