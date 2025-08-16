using Cad_Point_Manager.Models.PointRendering;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cad_Point_Manager.Models.HitTesting;

namespace Cad_Point_Manager.Views
{
    public class VisualHost : FrameworkElement
    {
        #region Fields
        private readonly VisualCollection _visuals;
        private readonly Dictionary<CogoPoint, (DrawingVisual markerVisual, DrawingVisual textVisual)> _visualMap = [];
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty CogoPointsProperty =
            DependencyProperty.Register(
                nameof(CogoPoints),
                typeof(ObservableCollection<CogoPoint>),
                typeof(VisualHost),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnCogoPointsChanged));
        public ObservableCollection<CogoPoint> CogoPoints
        {
            get => (ObservableCollection<CogoPoint>)GetValue(CogoPointsProperty);
            set => SetValue(CogoPointsProperty, value);
        }
        #endregion

        #region Constructors
        public VisualHost()
        {
            _visuals = new VisualCollection(this);
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

        //private static void OnHitTestablePointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is not VisualHost host) { return; }

        //    if (e.OldValue is ObservableCollection<HitTestablePoint> oldList)
        //    {
        //        oldList.CollectionChanged -= host.OnCogoPointsCollectionChanged;
        //    }

        //    if (e.NewValue is ObservableCollection<HitTestablePoint> newList)
        //    {
        //        newList.CollectionChanged += host.OnCogoPointsCollectionChanged;
        //        host.RebuildVisuals();
        //    }
        //}
        //private void OnHitTestablePointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        //{
        //    if (e.Action == NotifyCollectionChangedAction.Add)
        //    {
        //        foreach (HitTestablePoint point in e.NewItems!)
        //        {
        //            _visuals.Add(point.);
        //            _visuals.Add(point.VisualGroup.TextVisual);
        //        }
        //    }
        //    else if (e.Action == NotifyCollectionChangedAction.Remove)
        //    {
        //        foreach (HitTestablePoint point in e.OldItems!)
        //        {
        //            _visuals.Remove(visuals.markerVisual);
        //            _visuals.Remove(visuals.textVisual);
        //            _visualMap.Remove(point);
        //        }
        //    }
        //    else if (e.Action == NotifyCollectionChangedAction.Reset)
        //    {
        //        _visuals.Clear();
        //        _visualMap.Clear();
        //        RebuildVisuals();
        //    }
        //}

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];
        #endregion
    }
}
