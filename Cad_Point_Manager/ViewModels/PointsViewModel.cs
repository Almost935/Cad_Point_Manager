using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.Filtering;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.ViewModels.Editors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Cad_Point_Manager.ViewModels
{
    public sealed class PointsViewModel : INotifyPropertyChanged
    {
        #region Fields
        private CadManager _cadManager;
        private FilterType _selectedFilterType;
        private readonly DispatcherTimer _refreshTimer;
        #endregion

        #region Properties
        public CadManager CadManager
        {
            get => _cadManager;
            set
            {
                if (!ReferenceEquals(_cadManager, value))
                {
                    _cadManager = value;
                    OnPropertyChanged(nameof(CadManager));
                    OnPropertyChanged(nameof(PointsView));
                    OnPropertyChanged(nameof(PointGroupsView));
                }
            }
        }
        public FilterType SelectedFilterType
        {
            get => _selectedFilterType;
            set
            {
                if (_selectedFilterType == value) return;
                _selectedFilterType = value;
                OnPropertyChanged(nameof(SelectedFilterType));
                _addFilterCommand.NotifyCanExecuteChanged();
            }
        }

        public ObservableCollection<IPointFilter> ActiveFilters { get; } = [];

        // Filter Editors
        public PointNumberFilterEditor PointNumberFilterEditor { get; } = new();
        public NorthingFilterEditor NorthingFilterEditor { get; } = new();
        public EastingFilterEditor EastingFilterEditor { get; } = new();
        public ElevationFilterEditor ElevationFilterEditor { get; } = new();
        public DescriptionFilterEditor DescriptionFilterEditor { get; } = new();
        public PointGroupFilterEditor PointGroupFilterEditor { get; } = new();

        public ICollectionView PointsView => CadManager?.PointsView;
        public ICollectionView PointGroupsView => CadManager?.PointGroupsView;
        #endregion

        #region Commands
        public ICommand RemoveFilterCommand { get; }
        public ICommand ClearAllFiltersCommand { get; }


        private readonly RelayCommand _addFilterCommand;
        public ICommand AddFilterCommand => _addFilterCommand;
        #endregion

        #region Constructors
        public PointsViewModel(CadManager cadManager)
        {
            CadManager = cadManager;

            // IMPORTANT: set the view filter ONCE
            if (PointsView != null) { PointsView.Filter = CombinedFilterPredicate; }

            _addFilterCommand = new RelayCommand(OnAddFilterClicked, CanAddFilter);

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _refreshTimer.Tick += (_, __) =>
            {
                _refreshTimer.Stop();
                PointsView?.Refresh();
            };

            ActiveFilters.CollectionChanged += ActiveFilters_CollectionChanged;

            RemoveFilterCommand = new RelayCommand<IPointFilter>(f =>
            {
                if (f != null) { ActiveFilters.Remove(f); }
            });

            ClearAllFiltersCommand = new RelayCommand(() => ActiveFilters.Clear());
        }
        #endregion

        #region Methods
        private bool CanAddFilter()
        {
            return SelectedFilterType switch
            {
                FilterType.PointNumberFilter => PointNumberFilterEditor.IsValid,
                FilterType.NorthingFilter => NorthingFilterEditor.IsValid,
                FilterType.EastingFilter => EastingFilterEditor.IsValid,
                FilterType.ElevationFilter => ElevationFilterEditor.IsValid,
                FilterType.DescriptionFilter => DescriptionFilterEditor.IsValid,
                FilterType.PointGroupFilter => PointGroupFilterEditor.IsValid,
                _ => true
            };
        }
        private void OnAddFilterClicked()
        {
            switch (SelectedFilterType)
            {
                case FilterType.PointNumberFilter:
                    {
                        if (!PointNumberFilterEditor.TryGetRange(out var min, out var max)) { return; }
                        ActiveFilters.Add(new PointNumberRangeFilter(min, max));
                        break;
                    }
                case FilterType.NorthingFilter:
                    {
                        if (!NorthingFilterEditor.TryGetRange(out var min, out var max)) { return; }
                        ActiveFilters.Add(new NorthingRangeFilter(min, max));
                        break;
                    }
                case FilterType.EastingFilter:
                    {
                        if (!EastingFilterEditor.TryGetRange(out var min, out var max)) { return; }
                        ActiveFilters.Add(new EastingRangeFilter(min, max));
                        break;
                    }
                case FilterType.ElevationFilter:
                    {
                        if (!ElevationFilterEditor.TryGetRange(out var min, out var max)) { return; }
                        ActiveFilters.Add(new ElevationRangeFilter(min, max));
                        break;
                    }
                case FilterType.DescriptionFilter:
                    {
                        if (!DescriptionFilterEditor.TryGetText(out string text)) { return; }
                        ActiveFilters.Add(new DescriptionContainsFilter(text));
                        break;
                    }
                case FilterType.PointGroupFilter:
                    {
                        if (!PointGroupFilterEditor.TryGetPointGroup(out PointGroup pg)){ return; }
                        ActiveFilters.Add(new PointGroupFilter(pg));
                        break;
                    }
            }
        }

        private void ActiveFilters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // schedule one refresh (debounced)
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }

        private bool CombinedFilterPredicate(object obj)
        {
            if (obj is not CogoPoint p) { return false; }

            // AND all filters
            foreach (var filter in ActiveFilters)
            {
                if (!filter.IsMatch(p)) { return false; }
            }

            return true;
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}
