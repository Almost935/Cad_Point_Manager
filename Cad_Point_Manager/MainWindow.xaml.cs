using Cad_Point_Manager.ViewModels;
using Cad_Point_Manager.Views;
using System.Windows;
using System.Windows.Threading;

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


            LocationChanged += (_, __) =>
            {
                if (WindowState == WindowState.Maximized)
                {
                    // Force WPF to recompute maximized bounds for the new monitor
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var ws = WindowState;
                        WindowState = WindowState.Normal;
                        WindowState = ws; // back to Maximized
                    }), DispatcherPriority.Background);
                }
            };
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            mainFrame.NavigationService.Navigate(new MainView(MainViewModel));
        }
    }
}