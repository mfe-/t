using System.Collections.ObjectModel;
using System.Globalization;
using t.lib.Game;

namespace t.App.Controls.Converter;

/// <summary>
/// Convert a single card or a list of cards to an int (neccessary for <see cref="CardView"/>) or the other way round
/// </summary>
public class CardConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //convert list of cards to a list of numbers
        if (value is IEnumerable<Card> cards)
        {
            return cards.Select(a => a.Value);
        }
        //int is a single card
        if (value is int i)
        {
            return new Card(i);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<int> cards && targetType == typeof(ObservableCollection<Card>))
        {
            return new ObservableCollection<Card>(cards.Select(a => new Card(a)));
        }
        if (value is int i && targetType == typeof(Card))
        {
            return new Card(i);
        }
        return value;
    }
}
