using Cad_Point_Manager.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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

            Loaded += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    //Application.Current.MainWindow.KeyUp += vm.Window_KeyUp;
                    vm.ResetSelectionRequested += Vm_ResetSelectionRequested;
                    vm.ResetLayoutsViewRequested += OnResetLayoutsView;
                    vm.RebuildLayoutsViewRequested += OnRebuildLayoutsView;
                }
            };
            Unloaded += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    //Application.Current.MainWindow.KeyUp -= vm.Window_KeyUp;
                    vm.ResetSelectionRequested -= Vm_ResetSelectionRequested;
                    vm.ResetLayoutsViewRequested -= OnResetLayoutsView;
                    vm.RebuildLayoutsViewRequested += OnRebuildLayoutsView;
                }
            };
        }

        private void OnResetLayoutsView(object? sender, EventArgs e)
        {
            LayoutsViewControl.ResetView();
        }

        private void OnRebuildLayoutsView(object? sender, EventArgs e)
        {
            LayoutsViewControl.ReloadPreview();
        }

        private void Vm_ResetSelectionRequested(object? sender, EventArgs e)
        {
            // Call directly into the control (this is “View stuff” so it’s fine here)
            d3dDxfControl?.ResetSelectedObjects();
        }

        private void DxfGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.ViewportSize = new Size((float)e.NewSize.Width, (float)e.NewSize.Height);
            }
        }

        private void SelectedPointButtonsItemsControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Forward the mouse wheel event to the D3dDxfControl manually
            if (d3dDxfControl != null)
            {
                var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = e.OriginalSource
                };
                d3dDxfControl.RaiseEvent(args);
                e.Handled = true;
            }
        }

        private void SelectedPointButtonsItemsControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (d3dDxfControl != null)
            {
                // Create a new MouseEventArgs for forwarding
                var args = new MouseEventArgs(e.MouseDevice, e.Timestamp)
                {
                    RoutedEvent = UIElement.MouseMoveEvent,
                    Source = e.OriginalSource
                };
                d3dDxfControl.RaiseEvent(args);
            }
        }

        private void TextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.SelectAll();
                }), DispatcherPriority.Input);
            }
        }

        private void LayoutsModeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            LayoutsViewControl.ReloadPreview();
        }
    }
}
