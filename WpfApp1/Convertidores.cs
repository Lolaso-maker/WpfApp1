using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1
{
    /// <summary>
    /// Visible si el valor es "verdadero": true, un número distinto de 0,
    /// un texto no vacío o cualquier objeto no nulo.
    /// Con ConverterParameter="no" funciona al revés.
    /// </summary>
    public sealed class VisibleSi : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool verdadero = value switch
            {
                null => false,
                bool b => b,
                int i => i != 0,
                double d => d != 0,
                string s => s.Length > 0,
                _ => true
            };

            if (parameter is string p && p == "no")
            {
                verdadero = !verdadero;
            }

            return verdadero ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }

    /// <summary>
    /// Enlaza un enum con un grupo de RadioButton:
    /// IsChecked="{Binding Dificultad, Converter={StaticResource EnumABool}, ConverterParameter=Experto}".
    /// </summary>
    public sealed class EnumABool : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is not null && parameter is string p && value.ToString() == p;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true && parameter is string p)
            {
                return Enum.Parse(targetType, p);
            }

            return Binding.DoNothing;
        }
    }
}
