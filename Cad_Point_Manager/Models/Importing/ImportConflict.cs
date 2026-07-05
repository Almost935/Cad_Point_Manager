using Cad_Point_Manager.ViewModels;
using System.Collections;
using System.ComponentModel;

namespace Cad_Point_Manager.Models.Importing
{
    public class ImportConflict : BaseViewModel, INotifyDataErrorInfo
    {
        #region Fields
        private readonly Dictionary<string, List<string>> _errors = [];

        private int? _newPointNumberParsed;
        private string _newPointNumberText;
        #endregion

        #region Properties
        public ParsedPointImportRow Row { get; set; }
        public bool HasErrors => _errors.Any();
        public int ExistingPointNumber { get; set; }
        public string Reason { get; set; }
        public Func<IEnumerable<ImportConflict>> GetAllConflicts { get; set; }
        public Func<IEnumerable<int>> GetExistingPointNumbers { get; set; }

        public int? NewPointNumberParsed
        {
            get => _newPointNumberParsed;
            set
            {
                _newPointNumberParsed = value;
                OnPropertyChanged(nameof(NewPointNumberParsed));
            }
        }
        public string NewPointNumberText
        {
            get => _newPointNumberText;
            set
            {
                if (_newPointNumberText != value)
                {
                    _newPointNumberText = value;
                    OnPropertyChanged(nameof(NewPointNumberText));
                    ValidateNewPointNumber();
                }
            }
        }
        #endregion

        #region Constructors
        public ImportConflict(ParsedPointImportRow row, int existingPointNumber, string reason)
        {
            Row = row;
            ExistingPointNumber = existingPointNumber;
            Reason = reason;
            ValidateNewPointNumber();
        }
        #endregion

        #region Events
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion

        #region Methods
        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName != null && _errors.TryGetValue(propertyName, out var list))
                return list;
            return Enumerable.Empty<string>();
        }

        private void AddError(string prop, string message)
        {
            if (!_errors.ContainsKey(prop))
                _errors[prop] = new List<string>();

            if (!_errors[prop].Contains(message))
            {
                _errors[prop].Add(message);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void ClearErrors(string prop)
        {
            if (_errors.Remove(prop))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void ValidateNewPointNumber()
        {
            const string prop = nameof(NewPointNumberText);
            ClearErrors(prop);

            if (string.IsNullOrWhiteSpace(NewPointNumberText))
            {
                AddError(prop, "Value is required");
                NewPointNumberParsed = null;
                return;
            }

            if (!int.TryParse(NewPointNumberText, out var val))
            {
                AddError(prop, "Must be a valid integer");
                NewPointNumberParsed = null;
                return;
            }

            // ❌ Rule 1: cannot match existing point
            if (val == ExistingPointNumber)
            {
                AddError(prop, "Cannot match existing point number");
            }

            // ❌ Rule 2: duplicates in list
            if (GetAllConflicts != null)
            {
                var duplicates = GetAllConflicts()
                    .Where(x => x != this && x.NewPointNumberParsed == val)
                    .Any();

                if (duplicates)
                {
                    AddError(prop, "Duplicate number in list");
                }
            }

            // ❌ Rule 3: already exists in dataset
            if (GetExistingPointNumbers != null &&
                GetExistingPointNumbers().Contains(val))
            {
                AddError(prop, "Number already exists in project");
            }

            NewPointNumberParsed = val;
        }

        public void ValidateAll()
        {
            ValidateNewPointNumber();
        }
        #endregion
    }
}
