using System.ComponentModel;

namespace Cad_Point_Manager.Models.Printing
{
    public class TitleblockAttributes : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private Attribute _notes = new("Notes", 0.2);
        public Attribute Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private Attribute _drawingDesc1 = new("Drawing Description 1", 0.25);
        public Attribute DrawingDesc1
        {
            get => _drawingDesc1;
            set => SetProperty(ref _drawingDesc1, value);
        }

        private Attribute _drawingDesc2 = new("Drawing Description 2", 0.25);
        public Attribute DrawingDesc2
        {
            get => _drawingDesc2;
            set => SetProperty(ref _drawingDesc2, value);
        }

        private Attribute _drawingDesc3 = new("Drawing Description 3", 0.25);
        public Attribute DrawingDesc3
        {
            get => _drawingDesc3;
            set => SetProperty(ref _drawingDesc3, value);
        }

        private Attribute _drawingDesc4 = new("Drawing Description 4", 0.25);
        public Attribute DrawingDesc4
        {
            get => _drawingDesc4;
            set => SetProperty(ref _drawingDesc4, value);
        }

        private Attribute _drawingDesc5 = new("Drawing Description 5", 0.25);
        public Attribute DrawingDesc5
        {
            get => _drawingDesc5;
            set => SetProperty(ref _drawingDesc5, value);
        }

        private Attribute _drawingDesc6 = new("Drawing Description 6", 0.25);
        public Attribute DrawingDesc6
        {
            get => _drawingDesc6;
            set => SetProperty(ref _drawingDesc6, value);
        }

        private Attribute _drawingDate1 = new("Drawing Date 1", 0.25);
        public Attribute DrawingDate1
        {
            get => _drawingDate1;
            set => SetProperty(ref _drawingDate1, value);
        }

        private Attribute _drawingDate2 = new("Drawing Date 2", 0.25);
        public Attribute DrawingDate2
        {
            get => _drawingDate2;
            set => SetProperty(ref _drawingDate2, value);
        }

        private Attribute _drawingDate3 = new("Drawing Date 3", 0.25);
        public Attribute DrawingDate3
        {
            get => _drawingDate3;
            set => SetProperty(ref _drawingDate3, value);
        }

        private Attribute _drawingDate4 = new("Drawing Date 4", 0.25);
        public Attribute DrawingDate4
        {
            get => _drawingDate4;
            set => SetProperty(ref _drawingDate4, value);
        }

        private Attribute _drawingDate5 = new("Drawing Date 5", 0.25);
        public Attribute DrawingDate5
        {
            get => _drawingDate5;
            set => SetProperty(ref _drawingDate5, value);
        }

        private Attribute _drawingDate6 = new("Drawing Date 6", 0.25);
        public Attribute DrawingDate6
        {
            get => _drawingDate6;
            set => SetProperty(ref _drawingDate6, value);
        }

        private Attribute _drawnBy = new("Drawn By", 0.25);
        public Attribute DrawnBy
        {
            get => _drawnBy;
            set => SetProperty(ref _drawnBy, value);
        }

        private Attribute _dateDrawn = new("Date Drawn", 0.25);
        public Attribute DateDrawn
        {
            get => _dateDrawn;
            set => SetProperty(ref _dateDrawn, value);
        }

        private Attribute _projectName = new("Project Name", 0.25);
        public Attribute ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        private Attribute _pageTitle = new("Page Title", 0.25);
        public Attribute PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        private Attribute _pageNumber = new("Page Number", 0.5);
        public Attribute PageNumber
        {
            get => _pageNumber;
            set => SetProperty(ref _pageNumber, value);
        }

        private Attribute _scale = new("No Scale", 0.25);
        public Attribute Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        protected bool SetProperty<T>(
            ref T field,
            T value,
            string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            { return false; }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            { return true; }
        }
    }

}
