﻿namespace t.App.Controls.Animations
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Source: https://github.com/jsuarezruiz/MauiAnimation
    /// </remarks>
    [ContentProperty("Animations")]
    public class StoryBoard : AnimationBase
    {
        public StoryBoard()
        {
            Animations = new List<AnimationBase>();
        }

        public StoryBoard(List<AnimationBase> animations)
        {
            Animations = animations;
        }

        public List<AnimationBase> Animations
        {
            get;
        }

        protected override async Task BeginAnimation()
        {
            foreach (var animation in Animations)
            {
                if (animation.Target == null)
                    animation.Target = Target;

                await animation.Begin();
            }
        }
    }
}
