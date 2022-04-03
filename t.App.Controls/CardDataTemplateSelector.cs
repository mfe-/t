using t.lib.Game;

namespace t.App.Controls
{
    public class CardCollectionDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FirstItem { get; set; }
        public DataTemplate OtherItem { get; set; }
        public DataTemplate LastItem { get; set; }
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (container is not ItemsView) throw new InvalidOperationException("We expected a collectionview or an ItemsView");
            if (container is ItemsView itemsView)
            {
                var cards = itemsView.ItemsSource.Cast<Card>().ToArray();
                if (cards.Length > 0)
                {
                    var first = cards[0];
                    var last = cards[cards.Length - 1];
                    if (first.Equals(item))
                    {
                        System.Diagnostics.Debug.WriteLine("Take FirstItem");
                        return FirstItem;
                    }
                    else if(last.Equals(item))
                    {
                        System.Diagnostics.Debug.WriteLine("Take LastItem");
                        return LastItem;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Take OtherItem");
                        return OtherItem;
                    }
                }
                return FirstItem;
            }
            return FirstItem;
        }
    }
}
