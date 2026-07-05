using System.Windows;
using System.Windows.Controls;

namespace Cad_Point_Manager.Controls
{
    public class SortableListView : ListView
    {
        static SortableListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SortableListView),
                new FrameworkPropertyMetadata(typeof(SortableListView)));
        }

        public bool IsColumnHeaderHitTestVisible
        {
            get => (bool)GetValue(IsColumnHeaderHitTestVisibleProperty);
            set => SetValue(IsColumnHeaderHitTestVisibleProperty, value);
        }

        public static readonly DependencyProperty IsColumnHeaderHitTestVisibleProperty =
            DependencyProperty.Register(
                nameof(IsColumnHeaderHitTestVisible),
                typeof(bool),
                typeof(SortableListView),
                new FrameworkPropertyMetadata(true));
    }
}
