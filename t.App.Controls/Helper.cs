using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls;


public static partial class VisualTreeHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="element"></param>
    /// <returns></returns>
    /// <remarks>Taken from http://www.bryancook.net/2017/03/visualtreehelper-for-xamarinforms.html</remarks>
    public static T GetParent<T>(this Element element) where T : Element
    {
        if (element is T t)
        {
            return t;
        }
        else
        {
            if (element.Parent != null)
            {
                return element.Parent.GetParent<T>();
            }

            return default(T);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="element"></param>
    public static IEnumerable<T> GetChildren<T>(this Element element) where T : Element
    {
        if (element is ILayoutController contentProperty)
        {
            if (element is T t)
            {
                yield return t;
            }
            foreach (var child in contentProperty.Children)
            {
                GetChildren<T>(child);
            }
        }
    }
}

