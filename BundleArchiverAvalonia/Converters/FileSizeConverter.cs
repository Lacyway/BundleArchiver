using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BundleArchiverAvalonia.Converters;

public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        return FileLengthToFileSize((long)value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public static string FileLengthToFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:N1} KB";
        }

        if (bytes < 1024L * 1024 * 1024) // less than 1 GB
        {
            return $"{bytes / 1024d / 1024d:N1} MB";
        }

        // 1 GB or more
        return $"{bytes / 1024d / 1024d / 1024d:N1} GB";
    }
}
