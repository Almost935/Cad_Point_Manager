using Cad_Point_Manager.Models.PointRendering;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cad_Point_Manager.Common.Collections;

namespace Cad_Point_Manager.Views
{
    public class VisualHost : FrameworkElement
    {
        #region Fields
        private readonly VisualCollection _visuals;
        private readonly ContainerVisual _root = new();
        private readonly MatrixTransform _worldTx = new();
        private readonly Dictionary<CogoPoint, (DrawingVisual markerVisual, DrawingVisual textVisual)> _visualMap = [];

        private Matrix _pendingMatrix = Matrix.Identity;
        private bool _hasPending;
        private bool _renderHooked;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty CogoPointsProperty =
            DependencyProperty.Register(
                nameof(CogoPoints),
                typeof(BatchableObservableCollection<CogoPoint>),
                typeof(VisualHost),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnCogoPointsChanged));
        public BatchableObservableCollection<CogoPoint> CogoPoints
        {
            get => (BatchableObservableCollection<CogoPoint>)GetValue(CogoPointsProperty);
            set => SetValue(CogoPointsProperty, value);
        }

        public static readonly DependencyProperty TransformMatrixProperty =
            DependencyProperty.Register(
                nameof(TransformMatrix),
                typeof(Matrix),
                typeof(VisualHost),
                new FrameworkPropertyMetadata(Matrix.Identity, FrameworkPropertyMetadataOptions.AffectsRender, OnWorldMatrixChanged));
        public Matrix TransformMatrix
        {
            get => (Matrix)GetValue(TransformMatrixProperty);
            set => SetValue(TransformMatrixProperty, value);
        }
        #endregion

        #region Constructors
        public VisualHost()
        {
            _visuals = new VisualCollection(this);
            _root.Transform = _worldTx;
            _visuals.Add(_root);
        }
        #endregion

        #region Methods
        private void RebuildVisuals()
        {
            _visuals.Clear();
            _visualMap.Clear();

            //foreach (var (key, value) in _visualMap)
            //{
            //    _visualMap[key] = value;
            //    _visuals.Add(value.markerVisual);
            //    _visuals.Add(value.textVisual);
            //}
            foreach (var point in CogoPoints)
            {
                if (point.VisualGroup.MarkerVisual != null && point.VisualGroup.TextVisual != null)
                {
                    _visualMap[point] = (point.VisualGroup.MarkerVisual, point.VisualGroup.TextVisual);
                    _visuals.Add(point.VisualGroup.MarkerVisual);
                    _visuals.Add(point.VisualGroup.TextVisual);
                }
            }
        }

        private static void OnWorldMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (VisualHost)d;
            host.QueueMatrix((Matrix)e.NewValue);
        }

        private void QueueMatrix(Matrix m)
        {
            _pendingMatrix = m;
            _hasPending = true;

            if (_renderHooked) return;
            _renderHooked = true;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderHooked = false;

            if (!_hasPending) return;
            _hasPending = false;

            // Optional: ignore tiny changes (mouse jitter)
            if (!SignificantChange(_worldTx.Matrix, _pendingMatrix))
                return;

            _worldTx.Matrix = _pendingMatrix; // O(1) update for all children
        }

        private static bool SignificantChange(Matrix a, Matrix b)
        {
            const double eps = 1e-6;
            return Math.Abs(a.M11 - b.M11) > eps || Math.Abs(a.M12 - b.M12) > eps ||
                   Math.Abs(a.M21 - b.M21) > eps || Math.Abs(a.M22 - b.M22) > eps ||
                   Math.Abs(a.OffsetX - b.OffsetX) > eps || Math.Abs(a.OffsetY - b.OffsetY) > eps;
        }

        private static void OnCogoPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not VisualHost host) { return; }

            if (e.OldValue is ObservableCollection<CogoPoint> oldList)
            {
                oldList.CollectionChanged -= host.OnCogoPointsCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<CogoPoint> newList)
            {
                newList.CollectionChanged += host.OnCogoPointsCollectionChanged;
                host.RebuildVisuals();
            }
        }
        private void OnCogoPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (CogoPoint point in e.NewItems!)
                {
                    _visualMap[point] = (point.VisualGroup.MarkerVisual, point.VisualGroup.TextVisual);
                    _visuals.Add(point.VisualGroup.MarkerVisual);
                    _visuals.Add(point.VisualGroup.TextVisual);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (CogoPoint point in e.OldItems!)
                {
                    if (_visualMap.TryGetValue(point, out var visuals))
                    {
                        _visuals.Remove(visuals.markerVisual);
                        _visuals.Remove(visuals.textVisual);
                        _visualMap.Remove(point);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _visuals.Clear();
                _visualMap.Clear();
                RebuildVisuals();
            }
        }

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];
        #endregion
    }
}
