using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.ViewModels.Editors
{
    public sealed class PointGroupFilterEditor : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private PointGroup? _selectedPointGroup;

        public PointGroup? SelectedPointGroup
        {
            get => _selectedPointGroup;
            set
            {
                if (ReferenceEquals(_selectedPointGroup, value)) { return; }
                _selectedPointGroup = value;
                OnPropertyChanged(nameof(SelectedPointGroup));
                Validate();
            }
        }
        public bool TryGetPointGroup(out PointGroup? pointGroup)
        {
            Validate();
            pointGroup = SelectedPointGroup;
            return pointGroup is not null;
        }

        public bool IsValid => !HasErrors;

        private readonly Dictionary<string, List<string>> _errors = new();

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) { return _errors.SelectMany(kvp => kvp.Value); }

            return _errors.TryGetValue(propertyName, out var list)
                ? list
                : Enumerable.Empty<string>();
        }

        private void Validate()
        {
            _errors.Clear();

            if (SelectedPointGroup is null)
            {
                AddError(nameof(SelectedPointGroup), "Point group is required.");
            }

            RaiseErrorsChanged(nameof(SelectedPointGroup));
            RaiseErrorsChanged(null);
            OnPropertyChanged(nameof(IsValid));
        }

        private void AddError(string property, string message)
        {
            if (!_errors.TryGetValue(property, out var list))
                _errors[property] = list = new List<string>();

            list.Add(message);
        }

        private void RaiseErrorsChanged(string? propertyName) =>
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
