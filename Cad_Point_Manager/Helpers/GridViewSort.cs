using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cad_Point_Manager.Helpers
{
    public static class GridViewSort
    {
        #region Attached Properties

        public static readonly DependencyProperty AutoSortProperty =
            DependencyProperty.RegisterAttached(
                "AutoSort",
                typeof(bool),
                typeof(GridViewSort),
                new UIPropertyMetadata(false, OnAutoSortChanged));

        public static bool GetAutoSort(DependencyObject obj)
            => (bool)obj.GetValue(AutoSortProperty);

        public static void SetAutoSort(DependencyObject obj, bool value)
            => obj.SetValue(AutoSortProperty, value);

        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.RegisterAttached(
                "PropertyName",
                typeof(string),
                typeof(GridViewSort));

        public static string GetPropertyName(DependencyObject obj)
            => (string)obj.GetValue(PropertyNameProperty);

        public static void SetPropertyName(DependencyObject obj, string value)
            => obj.SetValue(PropertyNameProperty, value);

        public static readonly DependencyProperty ShowSortGlyphProperty =
            DependencyProperty.RegisterAttached(
                "ShowSortGlyph",
                typeof(bool),
                typeof(GridViewSort),
                new PropertyMetadata(true));

        public static bool GetShowSortGlyph(DependencyObject obj)
            => (bool)obj.GetValue(ShowSortGlyphProperty);

        public static void SetShowSortGlyph(DependencyObject obj, bool value)
            => obj.SetValue(ShowSortGlyphProperty, value);

        #endregion

        private static GridViewColumnHeader _lastHeader;
        private static ListSortDirection _lastDirection;

        private static void OnAutoSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                if ((bool)e.NewValue)
                {
                    listView.AddHandler(
                        GridViewColumnHeader.ClickEvent,
                        new RoutedEventHandler(ColumnHeader_Click),
                        handledEventsToo: true);
                }
                else
                {
                    listView.RemoveHandler(
                        GridViewColumnHeader.ClickEvent,
                        new RoutedEventHandler(ColumnHeader_Click));
                }
            }
        }

        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader headerClicked) { return; }

            string propertyName = GetPropertyName(headerClicked.Column);
            if (string.IsNullOrEmpty(propertyName)) { return; }

            ListView listView = (ListView)sender;
            ICollectionView view = CollectionViewSource.GetDefaultView(listView.ItemsSource);

            ListSortDirection direction =
                headerClicked != _lastHeader
                ? ListSortDirection.Ascending
                : (_lastDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending);

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(propertyName, direction));
            view.Refresh();

            _lastHeader = headerClicked;
            _lastDirection = direction;
        }
    }
}
