using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WpfUtils.Converters
{
    /// <summary>
    /// Convertir boolean en si o no
    /// </summary>
    /// <remarks>https://stackoverflow.com/questions/48839476/bind-boolean-values-as-yes-no-in-datagrid-text-column-in-wpf</remarks>
    public class BooleanToSiNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value) return "Sí";
            return "No";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
