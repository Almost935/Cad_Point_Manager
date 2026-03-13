using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Controls
{
    public class DoubleClickSetListView : ListView
    {
        public static readonly DependencyProperty ActiveItemProperty =
            DependencyProperty.Register(
                "ActiveItem",
                typeof(DoubleClickSetListViewItem),
                typeof(DoubleClickSetListView),
                new PropertyMetadata(OnActiveItemChanged));
        public DoubleClickSetListViewItem ActiveItem
        {
            get { return (DoubleClickSetListViewItem)GetValue(ActiveItemProperty); }
            set { SetValue(ActiveItemProperty, value); }
        }

        public static readonly DependencyProperty ActiveObjectProperty =
            DependencyProperty.Register(
                "ActiveObject",
                typeof(object),
                typeof(DoubleClickSetListView),
                new PropertyMetadata(null, OnActiveObjectChanged));
        public object ActiveObject
        {
            get { return (object)GetValue(ActiveObjectProperty); }
            set { SetValue(ActiveObjectProperty, value); }
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
                typeof(DoubleClickSetListView),
                new FrameworkPropertyMetadata(true));

        public DoubleClickSetListView()
        {
            ItemContainerGenerator.StatusChanged += (_, __) =>
            {
                if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    ApplyActiveObjectToContainer();
            };
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DoubleClickSetListViewItem;
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            DoubleClickSetListViewItem listViewItem = new();
            listViewItem.IsActiveChangedHandler += ListViewItem_IsActiveChangedHandler;

            return listViewItem;
        }

        private void ListViewItem_IsActiveChangedHandler(object sender, RoutedEventArgs e)
        {
            DoubleClickSetListViewItem listViewItem = sender as DoubleClickSetListViewItem;
            if (listViewItem == ActiveItem)
            {
                if (!listViewItem.IsActive)
                {
                    ActiveItem = null;
                    ActiveObject = null;
                }
                return;
            }

            if (ActiveItem is not null)
            {
                ActiveItem.IsActive = false;
            }
            ActiveItem = listViewItem;
            ActiveItem.IsActive = true;
            ActiveObject = ActiveItem.Content;
        }
        private static void OnActiveItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DoubleClickSetListViewItem listViewItem = (DoubleClickSetListViewItem)e.NewValue;
        }
        private static void OnActiveObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var lv = (DoubleClickSetListView)d;

            // If the change came from inside the control, don't fight it.
            if (ReferenceEquals(lv.ActiveItem?.Content, e.NewValue)) { return; }

            lv.ApplyActiveObjectToContainer();
        }

        private void ApplyActiveObjectToContainer()
        {
            if (ActiveObject == null)
            {
                if (ActiveItem != null) ActiveItem.IsActive = false;
                ActiveItem = null;
                return;
            }

            // Try to get the container for the data item
            var container = ItemContainerGenerator.ContainerFromItem(ActiveObject) as DoubleClickSetListViewItem;

            // If virtualization hasn't generated it yet, you can optionally ScrollIntoView to force it:
            if (container == null)
            {
                ScrollIntoView(ActiveObject);
                container = ItemContainerGenerator.ContainerFromItem(ActiveObject) as DoubleClickSetListViewItem;
                if (container == null) return; // still not ready
            }

            // Update active visuals
            if (ActiveItem != null && ActiveItem != container)
                ActiveItem.IsActive = false;

            ActiveItem = container;
            ActiveItem.IsActive = true;
        }
    }
    public class DoubleClickSetListViewItem : ListViewItem
    {
        // Register a custom routed event using the Bubble routing strategy.
        public static readonly RoutedEvent IsActivatedChangedEvent = EventManager.RegisterRoutedEvent(
            name: "ListViewItemIsActiveEvent",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(DoubleClickSetListViewItem));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
            "IsActive",
            typeof(bool),
            typeof(DoubleClickSetListViewItem),
            new PropertyMetadata(false));
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        // Provide CLR accessors for assigning an event handler.
        public event RoutedEventHandler IsActiveChangedHandler
        {
            add { AddHandler(IsActivatedChangedEvent, value); }
            remove { RemoveHandler(IsActivatedChangedEvent, value); }
        }

        void ListViewItemActivated()
        {
            // Create a RoutedEventArgs instance.
            RoutedEventArgs routedEventArgs = new(routedEvent: IsActivatedChangedEvent);

            // Raise the event, which will bubble up through the element tree.
            RaiseEvent(routedEventArgs);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            IsActive = true;
            ListViewItemActivated();
        }
    }
}
