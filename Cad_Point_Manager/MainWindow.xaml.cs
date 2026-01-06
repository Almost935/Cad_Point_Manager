using Cad_Point_Manager.ViewModels;
using Cad_Point_Manager.Views;
using System.Windows;

namespace Cad_Point_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel MainViewModel = new();

        public MainWindow()
        {
            this.DataContext = MainViewModel;

            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            mainFrame.NavigationService.Navigate(new MainView(MainViewModel));
        }
    }
}