using Microsoft.Maui.Layouts;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls;

public class CardItemsView : StackLayout
{
    public CardItemsView()
    {
        Loaded += CardItemsView_Loaded;
    }

    private void CardItemsView_Loaded(object? sender, EventArgs e)
    {

    }

    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(CardItemsView), null);

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(CardItemsView), null, propertyChanged: OnItemsSourceChanged);

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CardItemsView cardItemsView && newValue is IEnumerable enumerable)
        {
            cardItemsView.AddItems(enumerable);
            if (newValue is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged += cardItemsView.NotifyCollectionChanged_CollectionChanged;
            }
        }
    }

    private void NotifyCollectionChanged_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender == ItemsSource)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
            {
                foreach (var item in e.OldItems)
                {
                    var container = GetContainer(item);
                    if (container is View view && view.GestureRecognizers.Count != 0 && view.GestureRecognizers[0] is TapGestureRecognizer tap)
                    {
                        tap.Tapped -= TapGestureRecognizer_Tapped;
                        view.GestureRecognizers.Remove(tap);
                    }
                    this.Children.Remove(container);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (var item in e.NewItems)
                {
                    AddItem(item);
                }
            }
        }
    }
    private void AddItems(IEnumerable enumerable)
    {
        if (ItemsSource == null)
        {
            ItemsSource = enumerable;
        }
        foreach (var item in ItemsSource)
        {
            AddItem(item);
        }
    }

    private void AddItem(object item)
    {
        var elementView = GenerateContainer(item);
        if (elementView != null)
        {
            elementView.BindingContext = item;
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Parent = elementView;
            tapGestureRecognizer.Tapped += TapGestureRecognizer_Tapped;
            elementView.GestureRecognizers.Add(tapGestureRecognizer);
            Children.Add(elementView);
        }
    }

    private void TapGestureRecognizer_Tapped(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            SelectedItem = GetItem(view);
        }
    }

    private object? GetItem(View? sender)
    {
        return sender?.BindingContext;
    }

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(CardView), null, BindingMode.TwoWay);

    public object? SelectedItem
    {
        get { return (object?)GetValue(SelectedItemProperty); }
        set { SetValue(SelectedItemProperty, value); }
    }


    public IView? GetContainer(object item)
    {
        foreach (var child in Children)
        {
            if (child is View view && view.BindingContext == item)
            {
                return view;
            }
        }
        return null;
    }
    private View? GenerateContainer(object item)
    {
        var templateContent = ItemTemplate?.CreateContent();
        if (templateContent == null) return null;
        if (templateContent is ViewCell viewCell)
        {
            viewCell.Parent = this;
            viewCell.BindingContext = item;
            return viewCell.View;
        }
        throw new NotSupportedException($"{templateContent} is not supported.");
    }

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    protected override ILayoutManager CreateLayoutManager()
    {
        return new CardListLayoutManager(this);
    }



}
