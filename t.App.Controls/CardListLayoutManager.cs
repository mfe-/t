namespace t.App.Controls;

public class CardListLayoutManager : Microsoft.Maui.Layouts.StackLayoutManager
{
    private readonly StackLayout stackLayout;

    public CardListLayoutManager(StackLayout stackLayout) : base(stackLayout)
    {
        this.stackLayout = stackLayout;
    }
    public double SpacingWidth { get; private set; }
    public double SpacingHeight { get; private set; }
    public override Size Measure(double widthConstraint, double heightConstraint)
    {
        var padding = stackLayout.Padding;

        widthConstraint -= padding.HorizontalThickness;
        heightConstraint -= padding.VerticalThickness;

        double totalWidth = 0;
        double maxControlWidth = 0;
        double maxControlHeight = 0;
        double totalHeight = 0;

        var totalAmountItems = stackLayout.Count;
        CardView? cardView = null;
        //calculate the total width and height which will be required for the control
        foreach (var child in stackLayout)
        {
            if (cardView == null && child is CardView card)
            {
                cardView = card;
            }
            if (child is View view)
            {
                //only reserve space for the control if its visible
                if (view.IsVisible)
                {
                    //call measure on the control itself to get the size of it
                    var current = child.Measure(widthConstraint, heightConstraint);
                    maxControlWidth = Math.Max(current.Width, maxControlWidth);
                    maxControlHeight = Math.Max(current.Height, maxControlHeight);
                    totalWidth += current.Width;
                    if (!AreCardsVertical)
                    {
                        totalHeight = Math.Max(current.Height, totalHeight);
                    }
                    else
                    {
                        totalHeight += current.Height;
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(IView)} not implemented.");
            }

        }
        //calculate spacing between cards
        //if we don't have enough space left we need to overlap the cards
        //the overlapping is stored in Spacing
        if (widthConstraint < totalWidth)
        {
            var totalWidthofControls = totalAmountItems * maxControlWidth;
            var remaining = totalWidthofControls - widthConstraint;
            SpacingWidth = (remaining / totalAmountItems);
            //even if we require more space we have to use the contraint width
            totalWidth = widthConstraint;
        }
        else
        {
            SpacingWidth = 0;
        }
        //the height spacing is for us only relevant when the control is roated
        if (AreCardsVertical && heightConstraint < totalHeight)
        {
            var totalHeightofControls = totalAmountItems * maxControlWidth;
            var remaining = totalHeightofControls - heightConstraint;
            SpacingHeight = (remaining / totalAmountItems);
            //even if we require more space we have to use the contraint width
            totalHeight = heightConstraint;
        }
        else
        {
            SpacingHeight = 0;
        }

        if (!AreCardsVertical)
        {
            return new Size(totalWidth + padding.HorizontalThickness,
                totalHeight + padding.VerticalThickness + cardView?.TranslationYOffset ?? 0);
        }
        else
        {
            return new Size(maxControlHeight + padding.HorizontalThickness,
                totalHeight + padding.VerticalThickness + cardView?.TranslationYOffset ?? 0);
        }
    }
    public override Size ArrangeChildren(Rect bounds)
    {
        var padding = stackLayout.Padding;

        double x = padding.Left;
        double y = padding.Top;

        double totalWidth = 0;
        double totalHeight = 0;
        CardView? cardView = null;
        foreach (var child in stackLayout)
        {
            if (cardView == null && child is CardView card)
            {
                cardView = card;
            }
            if (child is View view)
            {
                if (view.IsVisible)
                {
                    if (!AreCardsVertical)
                    {
                        var width = child.DesiredSize.Width;
                        var height = child.DesiredSize.Height;
                        child.Arrange(new Rect(x, y, width, height));
                        totalWidth = Math.Max(totalWidth, x + width);
                        x += (width - SpacingWidth);
                    }
                    else
                    {
                        var width = child.DesiredSize.Height;
                        var height = child.DesiredSize.Width;
                        child.Arrange(new Rect(x, y, width, height));
                        y += (height - SpacingHeight);
                        totalHeight = Math.Max(totalHeight, y + height);
                        totalWidth = width;
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(IView)} not implemented.");
            }
        }
        return new Size(totalWidth + padding.HorizontalThickness,
            totalHeight + padding.VerticalThickness + cardView?.TranslationYOffset ?? 0);
    }
    public bool AreCardsVertical
    {
        get
        {
            if (stackLayout is CardItemsView cardItemsView)
            {
                return cardItemsView.ItemsLayout == StackOrientation.Vertical;
            }
            return false;
        }
    }
}
