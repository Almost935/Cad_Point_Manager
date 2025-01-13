using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
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
                new PropertyMetadata(OnActiveItemChangedCallBack));

        public static readonly DependencyProperty ActiveObjectProperty =
            DependencyProperty.Register(
                "ActiveObject",
                typeof(object),
                typeof(DoubleClickSetListView),
                new PropertyMetadata(null));

        public DoubleClickSetListViewItem ActiveItem
        {
            get { return (DoubleClickSetListViewItem)GetValue(ActiveItemProperty); }
            set { SetValue(ActiveItemProperty, value); }
        }
        public object ActiveObject
        {
            get { return (object)GetValue(ActiveObjectProperty); }
            set { SetValue(ActiveObjectProperty, value); }
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
        private static void OnActiveItemChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DoubleClickSetListViewItem listViewItem = (DoubleClickSetListViewItem)e.NewValue;
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
            if (IsActive)
            {
                IsActive = false;
            }
            else
            {
                IsActive = true;
            }

            ListViewItemActivated();
        }
    }
}
