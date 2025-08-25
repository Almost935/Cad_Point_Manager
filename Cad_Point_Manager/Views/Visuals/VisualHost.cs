using Cad_Point_Manager.Models.PointRendering;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cad_Point_Manager.Common.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace Cad_Point_Manager.Views
{
    public class VisualHost : FrameworkElement
    {
        #region Fields
        private readonly VisualCollection _visuals;
        private readonly ContainerVisual _root = new();
        private readonly MatrixTransform _worldTx = new();
        private readonly Dictionary<PointGroup, ContainerVisual> _groupNodes = new();
        private readonly Dictionary<INotifyCollectionChanged, PointGroup> _pointsOwner = new();
        private readonly Dictionary<CogoPoint, PointGroup> _pointOwner = new();

        private Matrix _pendingMatrix = Matrix.Identity;
        private bool _hasPending;
        private bool _renderHooked;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty PointGroupsProperty =
            DependencyProperty.Register(
                nameof(PointGroups),
                typeof(BatchableObservableCollection<KeyValuePair<string, PointGroup>>),
                typeof(VisualHost),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointGroupsChanged));
        public BatchableObservableCollection<KeyValuePair<string, PointGroup>> PointGroups
        {
            get => (BatchableObservableCollection<KeyValuePair<string, PointGroup>>)GetValue(PointGroupsProperty);
            set => SetValue(PointGroupsProperty, value);
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
        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];

        public int RootVisualsCount => _root.Children.Count;

        private void RebuildAllFromGroups(IEnumerable<KeyValuePair<string, PointGroup>> groups)
        {
            ClearAll();
            foreach (var kv in groups) { AttachPointGroup(kv.Value); }
        }

        private void ClearAll()
        {
            foreach (var pg in _groupNodes.Keys.ToList()) { DetachPointGroup(pg); }
            _groupNodes.Clear();
            _pointOwner.Clear();
            _root.Children.Clear();
        }

        private static void OnPointGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (VisualHost)d;
            if (e.OldValue is ObservableCollection<KeyValuePair<string, PointGroup>> oldCol)
            {
                oldCol.CollectionChanged -= host.OnPointGroupsCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<KeyValuePair<string, PointGroup>> newCol)
            {
                newCol.CollectionChanged += host.OnPointGroupsCollectionChanged;
                host.RebuildAllFromGroups(newCol);
            }
            else
            {
                host.ClearAll();
            }
        }
        private void OnPointGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            //Debug.WriteLine($"OnPointGroupPropertyChanged: {e.PropertyName}");

            if (sender is not PointGroup pg) return;

            if (e.PropertyName == nameof(PointGroup.IsVisible))
            {
                var node = EnsureGroupNode(pg);
                if (pg.IsVisible)
                {
                    if (!_root.Children.Contains(node))
                        _root.Children.Add(node);
                }
                else { _root.Children.Remove(node); }
            }
            else if (e.PropertyName == nameof(PointGroup.PointScale))
            {
                //foreach (var p in pg.Points) { p.VisualGroup.ApplyGroupScale(pg.PointScale); }
            }
        }
        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            //Debug.WriteLine($"OnPointPropertyChanged: {e.PropertyName}");

            if (sender is not CogoPoint p) return;

            // If a point moves between groups, reparent its visuals
            if (e.PropertyName == nameof(CogoPoint.PointGroup))
            {
                var newPg = p.PointGroup;
                if (newPg == null) return;

                if (!_pointOwner.TryGetValue(p, out var oldPg) || ReferenceEquals(oldPg, newPg))
                    return;

                DetachPoint(p);
                AttachPoint(newPg, p);
            }
        }
        private ContainerVisual EnsureGroupNode(PointGroup pg)
        {
            if (!_groupNodes.TryGetValue(pg, out var node))
            {
                node = pg.VisualContainer;   // use the one stored on the group
                _groupNodes[pg] = node;

                // listen for visibility/scale changes once
                pg.PropertyChanged -= OnPointGroupPropertyChanged;
                pg.PropertyChanged += OnPointGroupPropertyChanged;

                if (pg.IsVisible && !_root.Children.Contains(node))
                    _root.Children.Add(node);
            }
            return node;
        }

        private void OnPointGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            //Debug.WriteLine($"OnPointGroupsCollectionChanged");

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (KeyValuePair<string, PointGroup> kv in e.NewItems!)
                        AttachPointGroup(kv.Value);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (KeyValuePair<string, PointGroup> kv in e.OldItems!)
                        DetachPointGroup(kv.Value);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (KeyValuePair<string, PointGroup> kv in e.OldItems!)
                        DetachPointGroup(kv.Value);
                    foreach (KeyValuePair<string, PointGroup> kv in e.NewItems!)
                        AttachPointGroup(kv.Value);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (PointGroups is { } pgList)
                        RebuildAllFromGroups(pgList);
                    else
                        ClearAll();
                    break;

                case NotifyCollectionChangedAction.Move:
                    // Z-order can be adjusted by re-adding nodes if you need it
                    break;
            }

            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    foreach (PointGroup pg in e.NewItems)
            //    {
            //        SubscribePointGroup(pg);
            //    }
            //}
            //else if (e.Action == NotifyCollectionChangedAction.Remove)
            //{
            //    foreach (PointGroup pg in e.NewItems)
            //    {
            //        UnsubscribePointGroup(pg);
            //    }
            //}
            //else if (e.Action == NotifyCollectionChangedAction.Reset)
            //{

            //}
        }
        private void AttachPointGroup(PointGroup pg)
        {
            if (pg == null || _groupNodes.ContainsKey(pg)) return;

            var node = new ContainerVisual();
            _groupNodes[pg] = node;

            pg.PropertyChanged += OnPointGroupPropertyChanged;

            if (pg.Points is INotifyCollectionChanged incc)
            {
                _pointsOwner[incc] = pg;
                incc.CollectionChanged += OnGroupPointsChanged;
            }

            foreach (var p in pg.Points) { AttachPoint(pg, p); }

            if (pg.IsVisible && !_root.Children.Contains(node)) { _root.Children.Add(node); }
        }
        private void DetachPointGroup(PointGroup pg)
        {
            if (pg == null) return;

            UnsubscribeGroup(pg);

            if (_groupNodes.TryGetValue(pg, out var node))
            {
                _root.Children.Remove(node);
                node.Children.Clear();
                _groupNodes.Remove(pg);
            }
        }
        private void UnsubscribeGroup(PointGroup pg)
        {
            if (pg == null) return;

            pg.PropertyChanged -= OnPointGroupPropertyChanged;

            if (pg.Points is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= OnGroupPointsChanged;
                _pointsOwner.Remove(incc);
            }

            foreach (var p in pg.Points)
                p.PropertyChanged -= OnPointPropertyChanged;
        }

        private void AttachPoint(PointGroup pg, CogoPoint p)
        {
            if (pg == null || p == null) return;
            if (!_groupNodes.TryGetValue(pg, out var node)) return;

            if (!node.Children.Contains(p.VisualGroup.Root))
            { node.Children.Add(p.VisualGroup.Root); }

            _pointOwner[p] = pg;
            p.PropertyChanged -= OnPointPropertyChanged;
            p.PropertyChanged += OnPointPropertyChanged;
        }
        private void DetachPoint(CogoPoint p)
        {
            if (_groupNodes.TryGetValue(p.PointGroup, out var node))
            {
                node.Children.Remove(p.VisualGroup.Root);
            }
        }
        
        private void OnGroupPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            //Debug.WriteLine($"OnGroupPointsChanged");

            if (sender is not INotifyCollectionChanged coll || !_pointsOwner.TryGetValue(coll, out var pg))
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (CogoPoint p in e.NewItems!)
                        AttachPoint(pg, p);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (CogoPoint p in e.OldItems!)
                        DetachPoint(p);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (CogoPoint p in e.OldItems!)
                        DetachPoint(p);
                    foreach (CogoPoint p in e.NewItems!)
                        AttachPoint(pg, p);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (_groupNodes.TryGetValue(pg, out var node))
                    {
                        node.Children.Clear();
                        foreach (var p in pg.Points)
                            AttachPoint(pg, p);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    // adjust Z within group if needed
                    break;
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

            if (_renderHooked) { return; }
            _renderHooked = true;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderHooked = false;

            if (!_hasPending) { return; }
            _hasPending = false;

            if (!SignificantChange(_worldTx.Matrix, _pendingMatrix)) { return; }

            _worldTx.Matrix = _pendingMatrix;
        }

        private static bool SignificantChange(Matrix a, Matrix b)
        {
            const double eps = 1e-6;
            return Math.Abs(a.M11 - b.M11) > eps || Math.Abs(a.M12 - b.M12) > eps ||
                   Math.Abs(a.M21 - b.M21) > eps || Math.Abs(a.M22 - b.M22) > eps ||
                   Math.Abs(a.OffsetX - b.OffsetX) > eps || Math.Abs(a.OffsetY - b.OffsetY) > eps;
        }
        #endregion
    }
}
