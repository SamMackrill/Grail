using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Grail.ViewModel.Converters
{
    public class EnumerableToCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = -1;
            switch (value)
            {
                case StringCollection collection:
                    count = collection.Count;
                    break;

                case IEnumerable<object> list:
                    count = list.Count();
                    break;
            }

            return count < 0
                ? $"{parameter}: Failed."
                : (count == 0
                    ? $"{parameter}"
                    : $"{parameter}: {count}"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}