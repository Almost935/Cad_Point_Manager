using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.ViewModels;
using System.Diagnostics;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Page
    {
        public MainView(MainViewModel mainViewModel)
        {    
            this.DataContext = mainViewModel;
            
            InitializeComponent();
        }

        private void dxfGrid_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.ViewportSize = new System.Windows.Size((float)e.NewSize.Width, (float)e.NewSize.Height);
            }
        }
    }
}
