using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.App.Controls.Animations;

namespace t.App.Controls.Behaviour;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// Source: https://github.com/jsuarezruiz/MauiAnimation
/// </remarks>
[ContentProperty("Animation")]
public class BeginAnimation : TriggerAction<VisualElement>
{
    public AnimationBase? Animation { get; set; }

    protected override async void Invoke(VisualElement sender)
    {
        if (Animation != null)
            await Animation.Begin();
    }

}
