using System.Windows.Media;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public static class VisualTreeHelpers
    {
        public static T? FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null)
            {
                if (d is T t) return t;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
