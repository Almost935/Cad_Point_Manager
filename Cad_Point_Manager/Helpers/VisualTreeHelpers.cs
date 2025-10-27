using System.Windows.Media;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public static class VisualTreeHelpers
    {
        // find first descendant of type T
        public static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var sub = FindVisualChild<T>(child);
                if (sub != null) return sub;
            }
            return null;
        }

        // find a child element by x:Name inside a container
        public static FrameworkElement? FindByName(DependencyObject root, string name)
        {
            if (root is FrameworkElement fe && fe.Name == name) return fe;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var hit = FindByName(child, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
