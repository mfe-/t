using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.App.Controls.Animations;

namespace t.App.Controls.Behaviour;

public class AnimationBehaviorStoryBoard : BehaviorBase<BindableObject>
{
    public static readonly BindableProperty StartAnimationProperty = BindableProperty.Create(nameof(StartAnimation), typeof(bool), typeof(AnimationBehaviorStoryBoard), null, propertyChanged: StartAnimationPropertyChanged);

    private static async void StartAnimationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimationBehaviorStoryBoard animationBehavior && animationBehavior.AssociatedObject is View view)
        {
            try
            {
                if (animationBehavior.StartAnimation && animationBehavior.StoryBoard != null)
                {
                    await animationBehavior.StoryBoard.Begin();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }
    }

    public bool StartAnimation
    {
        get { return (bool)GetValue(StartAnimationProperty); }
        set { SetValue(StartAnimationProperty, value); }
    }


    public static readonly BindableProperty StoryBoardProperty = BindableProperty.Create(nameof(StoryBoard), typeof(StoryBoard), typeof(AnimationBehaviorStoryBoard), null);

    public StoryBoard? StoryBoard
    {
        get { return (StoryBoard?)GetValue(StoryBoardProperty); }
        set { SetValue(StoryBoardProperty, value); }
    }




}
