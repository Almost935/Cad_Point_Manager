using Cad_Point_Manager.Models.Printing;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for BaseTitleblock.xaml
    /// </summary>
    public partial class BaseTitleblock : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties
        public static readonly DependencyProperty ViewportWidthProperty =
            DependencyProperty.Register(
                nameof(ViewportWidth),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(28.9375));
        public double ViewportWidth
        {
            get => (double)GetValue(ViewportWidthProperty);
            set => SetValue(ViewportWidthProperty, value);
        }

        public static readonly DependencyProperty ViewportHeightProperty =
            DependencyProperty.Register(
                nameof(ViewportHeight),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(23.0));
        public double ViewportHeight
        {
            get => (double)GetValue(ViewportHeightProperty);
            set => SetValue(ViewportHeightProperty, value);
        }

        public static readonly DependencyProperty ViewportLeftProperty =
            DependencyProperty.Register(
                nameof(ViewportLeft),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(0.5));
        public double ViewportLeft
        {
            get => (double)GetValue(ViewportLeftProperty);
            set => SetValue(ViewportLeftProperty, value);
        }

        public static readonly DependencyProperty ViewportTopProperty =
            DependencyProperty.Register(
                nameof(ViewportTop),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(0.5));
        public double ViewportTop
        {
            get => (double)GetValue(ViewportTopProperty);
            set => SetValue(ViewportTopProperty, value);
        }

        public static readonly DependencyProperty NotesTextProperty =
           DependencyProperty.Register(
               nameof(NotesText),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Notes"));
        public string NotesText
        {
            get => (string)GetValue(NotesTextProperty);
            set => SetValue(NotesTextProperty, value);
        }


        public static readonly DependencyProperty DrawingDesc1Property =
           DependencyProperty.Register(
               nameof(DrawingDesc1),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Description 1"));
        public string DrawingDesc1
        {
            get => (string)GetValue(DrawingDesc1Property);
            set => SetValue(DrawingDesc1Property, value);
        }

        public static readonly DependencyProperty DrawingDesc2Property =
           DependencyProperty.Register(
               nameof(DrawingDesc2),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Description 2"));
        public string DrawingDesc2
        {
            get => (string)GetValue(DrawingDesc2Property);
            set => SetValue(DrawingDesc2Property, value);
        }

        public static readonly DependencyProperty DrawingDesc3Property =
           DependencyProperty.Register(
               nameof(DrawingDesc3),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Description 3"));
        public string DrawingDesc3
        {
            get => (string)GetValue(DrawingDesc4Property);
            set => SetValue(DrawingDesc4Property, value);
        }

        public static readonly DependencyProperty DrawingDesc4Property =
           DependencyProperty.Register(
               nameof(DrawingDesc4),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Description 4"));
        public string DrawingDesc4
        {
            get => (string)GetValue(DrawingDesc4Property);
            set => SetValue(DrawingDesc4Property, value);
        }

        public static readonly DependencyProperty DrawingDesc5Property =
           DependencyProperty.Register(
               nameof(DrawingDesc5),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Description 5"));
        public string DrawingDesc5
        {
            get => (string)GetValue(DrawingDesc5Property);
            set => SetValue(DrawingDesc5Property, value);
        }

        public static readonly DependencyProperty DrawingDesc6Property =
           DependencyProperty.Register(
               nameof(DrawingDesc6),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Description 6"));
        public string DrawingDesc6
        {
            get => (string)GetValue(DrawingDesc6Property);
            set => SetValue(DrawingDesc6Property, value);
        }


        public static readonly DependencyProperty DrawingDate1Property =
           DependencyProperty.Register(
               nameof(DrawingDate1),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Date 1"));
        public string DrawingDate1
        {
            get => (string)GetValue(DrawingDate1Property);
            set => SetValue(DrawingDate1Property, value);
        }

        public static readonly DependencyProperty DrawingDate2Property =
           DependencyProperty.Register(
               nameof(DrawingDate2),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Date 2"));
        public string DrawingDate2
        {
            get => (string)GetValue(DrawingDate2Property);
            set => SetValue(DrawingDate2Property, value);
        }

        public static readonly DependencyProperty DrawingDate3Property =
           DependencyProperty.Register(
               nameof(DrawingDate3),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Date 3"));
        public string DrawingDate3
        {
            get => (string)GetValue(DrawingDate4Property);
            set => SetValue(DrawingDate4Property, value);
        }

        public static readonly DependencyProperty DrawingDate4Property =
           DependencyProperty.Register(
               nameof(DrawingDate4),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Date 4"));
        public string DrawingDate4
        {
            get => (string)GetValue(DrawingDate4Property);
            set => SetValue(DrawingDate4Property, value);
        }

        public static readonly DependencyProperty DrawingDate5Property =
           DependencyProperty.Register(
               nameof(DrawingDate5),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Date 5"));
        public string DrawingDate5
        {
            get => (string)GetValue(DrawingDate5Property);
            set => SetValue(DrawingDate5Property, value);
        }

        public static readonly DependencyProperty DrawingDate6Property =
           DependencyProperty.Register(
               nameof(DrawingDate6),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawing Date 6"));
        public string DrawingDate6
        {
            get => (string)GetValue(DrawingDate6Property);
            set => SetValue(DrawingDate6Property, value);
        }


        public static readonly DependencyProperty DrawnByProperty =
           DependencyProperty.Register(
               nameof(DrawnBy),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Drawn By"));
        public string DrawnBy
        {
            get => (string)GetValue(DrawnByProperty);
            set => SetValue(DrawnByProperty, value);
        }

        public static readonly DependencyProperty DateDrawnProperty =
           DependencyProperty.Register(
               nameof(DateDrawn),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Date Drawn"));
        public string DateDrawn
        {
            get => (string)GetValue(DateDrawnProperty);
            set => SetValue(DateDrawnProperty, value);
        }

        public static readonly DependencyProperty ProjectNameProperty =
           DependencyProperty.Register(
               nameof(ProjectName),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Project Name"));
        public string ProjectName
        {
            get => (string)GetValue(ProjectNameProperty);
            set => SetValue(ProjectNameProperty, value);
        }

        public static readonly DependencyProperty PageTitleProperty =
           DependencyProperty.Register(
               nameof(PageTitle),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Page Title"));
        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        public static readonly DependencyProperty PageNumberProperty =
           DependencyProperty.Register(
               nameof(PageNumber),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("Page Number"));
        public string PageNumber
        {
            get => (string)GetValue(PageNumberProperty);
            set => SetValue(PageNumberProperty, value);
        }

        public static readonly DependencyProperty ScaleProperty =
           DependencyProperty.Register(
               nameof(Scale),
               typeof(string),
               typeof(BaseTitleblock),
               new PropertyMetadata("No Scale"));
        public string Scale
        {
            get => (string)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }


        public static readonly DependencyProperty AttributesProperty =
            DependencyProperty.Register(
                nameof(Attributes),
                typeof(TitleblockAttributes),
                typeof(BaseTitleblock),
                new PropertyMetadata(null));

        public TitleblockAttributes Attributes
        {
            get => (TitleblockAttributes)GetValue(AttributesProperty);
            set => SetValue(AttributesProperty, value);
        }
        #endregion

        #region Constructors
        public BaseTitleblock()
        {
            InitializeComponent();
            Attributes ??= new TitleblockAttributes();
        }
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
