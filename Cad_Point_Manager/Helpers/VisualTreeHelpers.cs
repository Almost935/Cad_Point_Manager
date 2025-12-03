using System.Windows;
using System.Windows.Media;

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

        public static T FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            var d = start;
            while (d != null)
            {
                if (d is T t) { return t; }
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        public static T FindDescendantByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T fe && fe.Name == name) return fe;
                var match = FindDescendantByName<T>(child, name);
                if (match != null) return match;
            }
            return null;
        }
    }
}
