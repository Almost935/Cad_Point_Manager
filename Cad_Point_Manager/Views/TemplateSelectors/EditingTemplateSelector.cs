using Cad_Point_Manager.Models.PointRendering;
using System.Windows;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.TemplateSelectors
{
    public sealed class EditingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DisplayTemplate { get; set; }
        public DataTemplate? EditTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // item is CogoPoint now
            if (item is CogoPoint p && p.IsEditing) { return EditTemplate!; }
            return DisplayTemplate!;
        }
    }
}
