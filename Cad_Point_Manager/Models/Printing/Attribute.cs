using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.Printing
{
    public class Attribute : INotifyPropertyChanged
    {
        #region Fields
        private readonly string _baseText;

        private string _text;
        private double _fontSize;
        private Color _fontColor = Colors.Black;
        #endregion

        #region Properties
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                    if (_text != _baseText)
                    {
                        IsBaseValue = false;
                    }
                    else
                    {
                        IsBaseValue = true;
                    }
                }
            }
        }
        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                }
            }
        }
        public Color FontColor
        {
            get { return _fontColor; }
            set
            {
                if (_fontColor != value)
                {
                    _fontColor = value;
                    OnPropertyChanged(nameof(FontColor));
                }
            }
        }

        public bool IsBaseValue { get; private set; } = true;
        #endregion

        #region Constructors
        public Attribute(string baseValue, double fontSize)
        {
            _baseText = baseValue;
            Text = baseValue;
            FontSize = fontSize;
        }
        #endregion

        #region Methods
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
