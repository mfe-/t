namespace t.App.Controls.Animations;

public class NextRoundLabelAnimation : AnimationBase
{
    protected override async Task BeginAnimation()
    {
        if (Target is Label label)
        {
            const int timems = 400;
            label.IsVisible = true;
            label.Opacity = 1;
            var scale = label.Scale;
            var taskscale = label.ScaleTo(scale * 6, timems, Easing.Linear);
            var taskfade = label.FadeTo(0.5, timems * 2, Easing.Linear);

            Task[] animationtasks = new Task[] { taskscale, taskfade };
            await Task.WhenAll(animationtasks);
            label.IsVisible = false;
            label.Scale = scale;
        }
    }
}
