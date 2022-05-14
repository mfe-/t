using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls;

public class CardListLayoutManager : Microsoft.Maui.Layouts.StackLayoutManager
{
    private readonly StackLayout stackLayout;

    public CardListLayoutManager(StackLayout stackLayout) : base(stackLayout)
    {
        this.stackLayout = stackLayout;
    }
    public double Spacing { get; private set; }
    public override Size Measure(double widthConstraint, double heightConstraint)
    {
        var padding = stackLayout.Padding;

        widthConstraint -= padding.HorizontalThickness;
        heightConstraint -= padding.VerticalThickness;

        double totalWidth = 0;
        double maxWidth = 0;
        double totalHeight = 0;

        var totalAmountItems = stackLayout.Count;
        CardView? cardView = null;
        //if(stackLayout is CardItemsView views)
        //{
        //    if (views.Count == 2) Debugger.Break();
        //}
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
                    maxWidth = Math.Max(current.Width, maxWidth);
                    totalWidth += current.Width;
                    totalHeight = Math.Max(current.Height, totalHeight);
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(IView)} not implemented.");
            }

        }
        //if we don't have enough space left we need to overlap the cards
        //the overlapping is stored in Spacing
        if (widthConstraint < totalWidth)
        {
            var a = totalAmountItems * maxWidth;
            var remaining = a - widthConstraint;
            Spacing = (remaining / totalAmountItems);
        }
        else
        {
            Spacing = 0;
        }

        return new Size(totalWidth + padding.HorizontalThickness,
            totalHeight + padding.VerticalThickness + cardView?.TranslationYOffset ?? 0);
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
                if(view.IsVisible)
                {
                    var width = child.DesiredSize.Width;
                    var height = child.DesiredSize.Height;
                    child.Arrange(new Rect(x, y, width, height));
                    totalWidth = Math.Max(totalHeight, y + height);
                    x += (width - Spacing);
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
}
