namespace t.App.Controls.Behaviour;

/// <summary>
/// Base class that extends on Xamarin Forms Behaviors.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>Original taken from https://github.com/PrismLibrary/Prism/blob/b1b8707455919caa3710d272556e0151aa82694e/src/Forms/Prism.Forms/Behaviors/BehaviorBase%7BT%7D.cs</remarks>
public class BehaviorBase<T> : Behavior<T> where T : BindableObject
{
    /// <summary>
    /// The Object associated with the Behavior
    /// </summary>
    public T AssociatedObject { get; private set; }

    /// <inheritDoc />
    protected override void OnAttachedTo(T bindable)
    {
        base.OnAttachedTo(bindable);
        AssociatedObject = bindable;

        if (bindable.BindingContext != null)
        {
            BindingContext = bindable.BindingContext;
        }

        bindable.BindingContextChanged += OnBindingContextChanged;
    }

    /// <inheritDoc />
    protected override void OnDetachingFrom(T bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.BindingContextChanged -= OnBindingContextChanged;
        AssociatedObject = null;
    }

    void OnBindingContextChanged(object sender, EventArgs e)
    {
        OnBindingContextChanged();
    }

    /// <inheritDoc />
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        BindingContext = AssociatedObject.BindingContext;
    }
}

