using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.ViewModels.Editors
{
    public sealed class DescriptionFilterEditor : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Fields
        private string? _text;
        private bool _caseSensitive;
        private readonly Dictionary<string, List<string>> _errors = [];
        #endregion

        #region Properties
        public string? Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                OnPropertyChanged(nameof(Text));
                Validate();
            }
        }
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set
            {
                if (_caseSensitive == value) return;
                _caseSensitive = value;
                OnPropertyChanged(nameof(CaseSensitive));
            }
        }

        public bool IsValid => !HasErrors;
        public bool HasErrors => _errors.Count > 0;
        #endregion

        #region Events
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        #endregion

        #region Methods
        public bool TryGetText(out string text)
        {
            Validate();

            text = (Text ?? string.Empty).Trim();
            return !string.IsNullOrWhiteSpace(text);
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _errors.SelectMany(kvp => kvp.Value);

            return _errors.TryGetValue(propertyName, out var list)
                ? list
                : Enumerable.Empty<string>();
        }

        private void Validate()
        {
            _errors.Clear();

            if (string.IsNullOrWhiteSpace(Text))
            {
                AddError(nameof(Text), "Description text is required.");
            }

            RaiseErrorsChanged(nameof(Text));
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
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}
