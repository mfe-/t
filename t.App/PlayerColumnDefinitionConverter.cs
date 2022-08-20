using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App
{
    public class PlayerColumnDefinitionConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var columnDefinitions = new ColumnDefinitionCollection();
            if (value is int amountPlayers)
            {
                for (int i = 0; i < amountPlayers + 1; i++)
                {
                    ColumnDefinition columnDefinition = new ColumnDefinition(GridLength.Star);
                    columnDefinitions.Add(columnDefinition);
                }
            }
            return columnDefinitions;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var columnDefinitions = new ColumnDefinitionCollection();
            ColumnDefinition columnDefinition = new ColumnDefinition(GridLength.Star);
            columnDefinitions.Add(columnDefinition);
            return columnDefinitions;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { value };
        }
    }
}
