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
    }
}
