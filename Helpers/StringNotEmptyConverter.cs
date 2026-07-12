using System.Globalization;

namespace GVC.Mobile.Helpers;

public sealed class StringNotEmptyConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is string texto &&
               !string.IsNullOrWhiteSpace(texto);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}