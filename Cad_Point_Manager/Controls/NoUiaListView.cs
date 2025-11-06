using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Cad_Point_Manager.Controls
{
    public class NoUiaListViewItem : ListViewItem
    {
        protected override AutomationPeer OnCreateAutomationPeer() => null; // no peer
    }

    public class NoUiaListView : ListView
    {
        protected override DependencyObject GetContainerForItemOverride()
            => new NoUiaListViewItem();
    }
}
