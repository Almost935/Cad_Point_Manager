using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Controls
{
    public class NoRevertComboBox : ComboBox
    {
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyDown(e);
        }
    }

}
