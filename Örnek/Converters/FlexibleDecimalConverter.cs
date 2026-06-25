using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Örnek.Converters;

public sealed class FlexibleDecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        if (value is decimal d)
        {
            var format = parameter as string;
            return string.IsNullOrWhiteSpace(format)
                ? d.ToString(culture)
                : d.ToString(format, culture);
        }

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return 0m;

        var s = value.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(s))
            return 0m;

        // 1) try parse with current culture
        if (decimal.TryParse(s, NumberStyles.Number, culture, out var parsed))
            return parsed;

        // 2) accept the "other" decimal separator too (e.g., allow '.' even if culture expects ',')
        var decSep = culture.NumberFormat.NumberDecimalSeparator;
        var otherSep = decSep == "," ? "." : ",";
        var normalized = s.Replace(otherSep, decSep);

        if (decimal.TryParse(normalized, NumberStyles.Number, culture, out parsed))
            return parsed;

        return DependencyProperty.UnsetValue;
    }
}
