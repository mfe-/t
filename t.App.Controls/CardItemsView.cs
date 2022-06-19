using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Layouts;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace t.App.Controls;

public class CardItemsView : StackLayout
{
    public CardItemsView()
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

    private static void OnItemsSourceChanged(BindableObject? bindable, object oldValue, object newValue)
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
    /// <summary>
    /// It the <seealso cref="ItemsSource"/> gets updated we need to add or remove the container control for the item
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NotifyCollectionChanged_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
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
                        if (container is CardView cardView)
                        {
                            cardView.TappedEvent -= CardView_TappedEvent;
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
        catch (Exception ex)
        {
            //since the last visual studio update maui throws here an exception...
            System.Diagnostics.Debug.WriteLine(ex);
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
#if ANDROID
            //android workaround because of https://github.com/dotnet/maui/issues/8121
            //for android tapGestureRecognizer does not propagate tap events of childs we have to use our own event
            if (elementView is CardView cardView)
            {
                cardView.TappedEvent += CardView_TappedEvent;
            }

#else
            elementView.BindingContext = item;
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Parent = elementView;
            tapGestureRecognizer.Tapped += TapGestureRecognizer_Tapped;
            elementView.GestureRecognizers.Add(tapGestureRecognizer);
#endif

            Children.Add(elementView);
        }
    }

    private async Task CardView_TappedEvent(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled) return;
        if (e.DoubleTapped)
        {
            TapGestureRecognizer_Tapped(sender, e);
        }
        else
        {
            if (sender is CardView cardView)
            {
                await cardView.HighlightAnimationAsync();
            }
        }
    }

    public static readonly BindableProperty SelectionChangedCommandProperty = BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(CardItemsView), null);
    public ICommand? SelectionChangedCommand
    {
        get { return (ICommand?)GetValue(SelectionChangedCommandProperty); }
        set { SetValue(SelectionChangedCommandProperty, value); }
    }

    public static readonly BindableProperty SelectionChangedCommandParameterProperty = BindableProperty.Create(nameof(SelectionChangedCommandParameter), typeof(object), typeof(CardItemsView), null);
    public object? SelectionChangedCommandParameter
    {
        get { return (object?)GetValue(SelectionChangedCommandParameterProperty); }
        set { SetValue(SelectionChangedCommandParameterProperty, value); }
    }
    private void TapGestureRecognizer_Tapped(object? sender, EventArgs e)
    {
        if (!IsEnabled) return;
        if (sender is View view)
        {
            SelectedItem = GetItem(view);
            if (SelectionChangedCommand != null && SelectionChangedCommand.CanExecute(SelectedItem ?? SelectionChangedCommandParameter))
            {
                SelectionChangedCommand.Execute(SelectedItem ?? SelectionChangedCommandParameter);
            }
        }
    }

    private object? GetItem(View? sender)
    {
        if (sender is CardView cardView)
        {
            return cardView.Card;
        }
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
            if (child is CardView cardView && cardView.Card == item)
            {
                return cardView;
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
            //due to a maui bug https://github.com/dotnet/maui/issues/5287 we need to propagate IsEnalbed on the child
            viewCell.IsEnabled = IsEnabled;
            viewCell.View.IsEnabled = IsEnabled;
            return viewCell.View;
        }
        throw new NotSupportedException($"{templateContent} is not supported.");
    }

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    protected override ILayoutManager CreateLayoutManager() =>
        new CardListLayoutManager(this);

    public void UpdateChilds()
    {
        foreach (var child in Children)
        {
            if (child is View view)
            {
                view.IsEnabled = this.IsEnabled;
            }
        }
    }
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            //due to a maui bug https://github.com/dotnet/maui/issues/5287 we need to propagate IsEnalbed on the child
            UpdateChilds();
        }
    }


    public static readonly BindableProperty ItemsLayoutProperty = BindableProperty.Create(nameof(ItemsLayout), typeof(StackOrientation), typeof(CardItemsView), StackOrientation.Horizontal);

    public StackOrientation ItemsLayout
    {
        get { return (StackOrientation)GetValue(ItemsLayoutProperty); }
        set { SetValue(ItemsLayoutProperty, value); }
    }




}
