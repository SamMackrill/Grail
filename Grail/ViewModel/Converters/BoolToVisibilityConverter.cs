using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Grail;

namespace Grail
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Extensions.InDesignMode) return Visibility.Visible;

            if (value == null) throw new ArgumentNullException();
            
            return (bool) value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"Error: NotImplementedException for {Extensions.CurrentMethod()} ");
            throw new NotImplementedException();
        }
    }
}
