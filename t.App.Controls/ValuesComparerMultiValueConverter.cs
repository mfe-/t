using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls;

public class ValuesComparerMultiValueConverter : IMultiValueConverter
{
    public static readonly string  TreatNullAsFalse = "TreatNullAsFalse";
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if(values.Length >= 2)
        {
            if((string)parameter == TreatNullAsFalse)
            {
                return values[0] != null && values[1] != null && values[0] == values[1];
            }
            return values[0] == values[1];
        }
        throw new InvalidOperationException($"{nameof(values)} array expects at least two elements for comparing!" );
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
