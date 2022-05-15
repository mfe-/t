using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls.Converter;

public class BoolMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool v = false;
        //use inital value of first element
        if (values != null && values.Length > 0 && values[0] != null)
        {
            v = (bool)values[0];
        }
        foreach (var item in values ?? Enumerable.Empty<object>())
        {
            if (item == null) continue;
            v = v && (bool)item;
        }
        if (parameter == null) return v;
        return !v;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
