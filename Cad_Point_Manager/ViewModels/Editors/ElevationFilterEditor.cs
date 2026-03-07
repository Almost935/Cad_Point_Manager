using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.ViewModels.Editors
{
    public sealed class ElevationFilterEditor : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private string? _minText;
        private string? _maxText;

        public string? MinText
        {
            get => _minText;
            set
            {
                if (_minText == value) return;
                _minText = value;
                OnPropertyChanged(nameof(MinText));
                Validate();
            }
        }

        public string? MaxText
        {
            get => _maxText;
            set
            {
                if (_maxText == value) return;
                _maxText = value;
                OnPropertyChanged(nameof(MaxText));
                Validate();
            }
        }

        // Parsed values if valid
        public bool TryGetRange(out double min, out double max)
        {
            Validate();

            min = 0; max = 0;
            if (!double.TryParse(MinText, out min)) { return false; }
            if (!double.TryParse(MaxText, out max)) { return false; }
            return min <= max;
        }

        public bool IsValid => !HasErrors;

        // ---- Validation plumbing ----
        private readonly Dictionary<string, List<string>> _errors = new();
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) { return _errors.SelectMany(kvp => kvp.Value); }

            return _errors.TryGetValue(propertyName, out var list) ? list : Enumerable.Empty<string>();
        }

        private void Validate()
        {
            _errors.Clear();

            // Min
            if (string.IsNullOrWhiteSpace(MinText)) { AddError(nameof(MinText), "Min northing is required."); }
            else if (!double.TryParse(MinText, out _)) { AddError(nameof(MinText), "Min must be a number."); }

            // Max
            if (string.IsNullOrWhiteSpace(MaxText)) { AddError(nameof(MaxText), "Max northing is required."); }
            else if (!double.TryParse(MaxText, out _)) { AddError(nameof(MaxText), "Max must be a number."); }

            // Range rule (only if both parse)
            if (double.TryParse(MinText, out var min) && double.TryParse(MaxText, out var max))
            {
                if (min > max)
                {
                    AddError(nameof(MaxText), "Max must be greater than or equal to Min.");
                }
            }

            RaiseErrorsChanged(nameof(MinText));
            RaiseErrorsChanged(nameof(MaxText));
            RaiseErrorsChanged(null); // overall
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
