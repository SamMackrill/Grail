using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Grail;

namespace Grail.ViewModel.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public sealed class ObjectNotNullIsVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Extensions.InDesignMode) return Visibility.Visible;

            if (value == null) return Visibility.Collapsed;

            //check if object has a value or it is just empty
            var x = value.ToString();

            return x.HasValue() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"Error: NotImplementedException for {Extensions.CurrentMethod()} ");
            throw new NotImplementedException();
        }
    }
}
