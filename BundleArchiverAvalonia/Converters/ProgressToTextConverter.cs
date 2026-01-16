using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BundleArchiverAvalonia.Converters;

public class ProgressToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return $"{d:0}%";
        }

        return "0%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
