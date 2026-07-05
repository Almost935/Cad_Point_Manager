using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Cad_Point_Manager.Behaviors
{
    public static class ListViewSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListViewSelectedItemsBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject element, IList value)
        {
            element.SetValue(SelectedItemsProperty, value);
        }

        public static IList GetSelectedItems(DependencyObject element)
        {
            return (IList)element.GetValue(SelectedItemsProperty);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                listView.SelectionChanged -= OnSelectionChanged;
                listView.SelectionChanged += OnSelectionChanged;
            }
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView listView)
                return;

            // 🔥 THIS IS REQUIRED
            if (e.OriginalSource is not ListView)
                return;

            var boundCollection = GetSelectedItems(listView);
            if (boundCollection == null)
                return;

            foreach (var item in e.AddedItems)
                boundCollection.Add(item);

            foreach (var item in e.RemovedItems)
                boundCollection.Remove(item);
        }
    }

}
