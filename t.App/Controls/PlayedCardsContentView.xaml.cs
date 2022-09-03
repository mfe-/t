using System.Collections.ObjectModel;
using t.App.Models;

namespace t.App.Controls;

public partial class PlayedCardsContentView : ContentView
{
    public PlayedCardsContentView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty PlayerContainersProperty =
    BindableProperty.Create(nameof(PlayerContainers), typeof(ObservableCollection<PlayerCardContainer>), typeof(PlayedCardsContentView), null, propertyChanged: PlayerContainersPropertyChanged);

    public ObservableCollection<PlayerCardContainer> PlayerContainers
    {
        get => (ObservableCollection<PlayerCardContainer>)GetValue(PlayerContainersProperty);
        set => SetValue(PlayerContainersProperty, value);
    }

    private static void PlayerContainersPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PlayedCardsContentView playedCardsContentView)
        {
            if (playedCardsContentView.PlayerContainers is ObservableCollection<PlayerCardContainer> collection && newValue is ObservableCollection<PlayerCardContainer>)
            {
                collection.CollectionChanged += playedCardsContentView.Collection_CollectionChanged;
            }
            if (playedCardsContentView.PlayerContainers is ObservableCollection<PlayerCardContainer> col && newValue is null)
            {
                col.CollectionChanged -= playedCardsContentView.Collection_CollectionChanged;
            }
        }
    }

    private void Collection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                if (item is PlayerCardContainer playerCardContainer)
                {
                    CreateSelectedCardPlayerContainer(playerCardContainer);
                }
            }
        }
    }
    private int gridCurrentColumn = 1;
    private SelectedCardPlayerContainer CreateSelectedCardPlayerContainer(PlayerCardContainer playerCardContainer)
    {
        var selectedCardPlayerContainer = new SelectedCardPlayerContainer();
        selectedCardPlayerContainer.BindingContext = playerCardContainer;

        //rootStackLayout.Add(selectedCardPlayerContainer);

        rootGrid.Add(selectedCardPlayerContainer);
        rootGrid.SetColumn(selectedCardPlayerContainer, gridCurrentColumn);
        gridCurrentColumn++;
        //BindingContext = "{Binding Path=Player1Container}" >
        return selectedCardPlayerContainer;
    }
}