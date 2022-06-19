#if ANDROID
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using View = Android.Views.View;

namespace t.App.Controls;

public static partial class VisualTreeHelper
{
    public static IEnumerable<View> GetAllChildren(View v)
    {
        var visited = new List<View>();
        var unvisited = new List<View>();
        unvisited.Add(v);
        while (unvisited.Any())
        {
            View? child = unvisited.FirstOrDefault();
            unvisited.RemoveAt(0);
            if (child != null)
            {
                visited.Add(child);
                if (child is ViewGroup group)
                {
                    int childCount = group.ChildCount;
                    for (int i = 0; i < childCount; i++)
                    {
                        var c = group.GetChildAt(i);
                        if (c != null)
                        {
                            unvisited.Add(c);
                        }
                    }
                }

            }
        }

        return visited;
    }
}

#endif