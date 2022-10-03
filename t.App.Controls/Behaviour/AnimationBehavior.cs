using System.Threading.Tasks;
using System.Windows.Input;
using t.App.Controls.Behaviour;


namespace t.App.Controls.Behaviour;

public class AnimationBehavior : BehaviorBase<BindableObject>
{
    /// <summary>
    /// Set the property to start the animation
    /// </summary>
    public static readonly BindableProperty StartAnimationProperty = BindableProperty.Create(nameof(StartAnimation), typeof(object), typeof(AnimationBehavior), null,
        propertyChanged: StartAnimationPropertyChanged);

    private static async void StartAnimationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimationBehavior animationBehavior && animationBehavior.AssociatedObject is View view)
        {
            try
            {
                await animationBehavior.StartAnimationAsync(view, animationBehavior.StartAnimation);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }
    }

    public object? StartAnimation
    {
        get { return GetValue(StartAnimationProperty); }
        set { SetValue(StartAnimationProperty, value); }
    }

    protected virtual Task StartAnimationAsync(View control, object? animationParam = null)
    {
        return Task.CompletedTask;
    }
}