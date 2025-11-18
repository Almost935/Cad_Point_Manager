using System.ComponentModel;
using SharpDX;

namespace Cad_Point_Manager.Models.Printing
{
    public class Scene : INotifyPropertyChanged
    {
        #region Fields
        private string _name = string.Empty;
        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public int ZoomStep { get; set; } = 0;
        public Vector2 Translation { get; set; } = Vector2.Zero;
        #endregion

        #region Methods
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
