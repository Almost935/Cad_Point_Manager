using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cad_Point_Manager.Behavior
{
    public static class PreserveSelectionOnDoubleClickBehavior
    {
        [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
        private const int SM_CXDOUBLECLK = 36; // width in *pixels*
        private const int SM_CYDOUBLECLK = 37; // height in *pixels*

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(PreserveSelectionOnDoubleClickBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

        private static ListViewItem? _lastItem;
        private static Point _lastPoint;
        private static DateTime _lastTime;

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView list)
            {
                if ((bool)e.NewValue)
                    list.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                        new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown), true);
                else
                    list.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                        new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown));
            }
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject src) return;
            var lvi = FindAncestor<ListViewItem>(src);
            if (lvi == null) return;

            if (!lvi.IsSelected) { Cache(lvi, e); return; }

            var now = DateTime.UtcNow;
            var pos = e.GetPosition(lvi);

            // Convert Win32 pixel metrics -> WPF DIPs for this visual (handles per-monitor DPI)
            var dpi = VisualTreeHelper.GetDpi(lvi);
            double dxDip = GetSystemMetrics(SM_CXDOUBLECLK) / dpi.DpiScaleX;
            double dyDip = GetSystemMetrics(SM_CYDOUBLECLK) / dpi.DpiScaleY;

            bool sameItem = ReferenceEquals(lvi, _lastItem);
            bool closeInTime = (now - _lastTime).TotalMilliseconds <= GetDoubleClickTime();
            bool closeInSpace =
                Math.Abs(pos.X - _lastPoint.X) <= dxDip &&
                Math.Abs(pos.Y - _lastPoint.Y) <= dyDip;

            if (sameItem && closeInTime && closeInSpace)
            {
                // Swallow the first click of the double-click so the selection doesn’t collapse
                e.Handled = true;
            }

            Cache(lvi, e);
        }

        private static void Cache(ListViewItem item, MouseButtonEventArgs e)
        {
            _lastItem = item;
            _lastPoint = e.GetPosition(item);
            _lastTime = DateTime.UtcNow;
        }

        private static T? FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null && d is not T) d = VisualTreeHelper.GetParent(d);
            return d as T;
        }
    }
}
