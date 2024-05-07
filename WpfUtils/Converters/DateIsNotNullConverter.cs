using System;
using System.Globalization;
using System.Windows.Data;
using Utils;

namespace WpfUtils.Converters
{
    public class DateIsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !value.IsNullOrEmptyOrDbNull() && (DateTime)value != DateTime.MinValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
