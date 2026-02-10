using Attribute = Cad_Point_Manager.Models.Printing.Attribute;

namespace Cad_Point_Manager.Extensions
{
    public static class AttributeExtensions
    {
        public static string ToPrintableText(this Attribute? a)
        {
            if (a == null) { return string.Empty; }
            return a.IsBaseValue ? string.Empty : a.Text;
        }
    }
}
