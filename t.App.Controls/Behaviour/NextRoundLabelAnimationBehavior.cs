using t.App.Controls.Animations;

namespace t.App.Controls.Behaviour;

public class NextRoundLabelAnimationBehavior : AnimationBehavior
{
    protected override async Task StartAnimationAsync(View control, object? param)
    {
        if (control is Label label)
        {
            var nextRoundLabel = new NextRoundLabelAnimation();
            nextRoundLabel.Target = label;
            await nextRoundLabel.Begin();
        }
    }
}
