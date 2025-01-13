using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Cad_Point_Manager.Controls
{
    /// <summary>
    /// Interaction logic for AutoHideControl.xaml
    /// </summary>
    public partial class AutoHideControl : UserControl
    {
        private DispatcherTimer _hideTimer;

        public AutoHideControl()
        {
            InitializeComponent();

            // Initialize the auto-hide timer
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // Adjust delay as needed
            };
            _hideTimer.Tick += HideTimer_Tick;
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            HideControl();
        }

        private void MainPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            _hideTimer.Stop(); // Cancel auto-hide if mouse is inside
        }

        private void MainPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            _hideTimer.Start(); // Start the timer when mouse leaves
        }

        private void Tab_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowControl(); // Show the control when tab is hovered
        }

        private void ShowControl()
        {
            Tab.Visibility = Visibility.Collapsed; // Hide the tab

            DoubleAnimation slideIn = new DoubleAnimation
            {
                From = this.ActualWidth,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300) // Adjust speed
            };

            TranslateTransform transform = new();
            MainPanel.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, slideIn);

            MainPanel.Visibility = Visibility.Visible; // Show main panel
        }

        private void HideControl()
        {
            DoubleAnimation slideOut = new DoubleAnimation
            {
                From = 0,
                To = this.ActualWidth,
                Duration = TimeSpan.FromMilliseconds(300) // Adjust speed
            };

            TranslateTransform transform = new TranslateTransform();
            MainPanel.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, slideOut);

            slideOut.Completed += (s, e) =>
            {
                MainPanel.Visibility = Visibility.Collapsed; // Hide main panel
                Tab.Visibility = Visibility.Visible;         // Show tab
            };
        }
    }
}
