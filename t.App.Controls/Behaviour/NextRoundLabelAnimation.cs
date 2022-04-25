using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls.Behaviour;

public class NextRoundLabelAnimation : AnimationBehavior
{
    protected override async Task StartAnimationAsync(View control, object? param)
    {
        if (control is Label label)
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
