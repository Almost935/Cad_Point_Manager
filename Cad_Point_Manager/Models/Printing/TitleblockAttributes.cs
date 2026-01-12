using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.Printing
{
    public class TitleblockAttributes : INotifyPropertyChanged
    {
        public Attribute Notes { get; } = new() { Text = "Notes", FontSize = 0.2 };

        public Attribute DrawingDesc1 { get; } = new() { Text = "Drawing Description 1", FontSize = 0.25 };
        public Attribute DrawingDesc2 { get; } = new() { Text = "Drawing Description 2", FontSize = 0.25 };
        public Attribute DrawingDesc3 { get; } = new() { Text = "Drawing Description 3", FontSize = 0.25 };
        public Attribute DrawingDesc4 { get; } = new() { Text = "Drawing Description 4", FontSize = 0.25 };
        public Attribute DrawingDesc5 { get; } = new() { Text = "Drawing Description 5", FontSize = 0.25 };
        public Attribute DrawingDesc6 { get; } = new() { Text = "Drawing Description 6", FontSize = 0.25 };

        public Attribute DrawingDate1 { get; } = new() { Text = "Drawing Date 1", FontSize = 0.25 };
        public Attribute DrawingDate2 { get; } = new() { Text = "Drawing Date 2", FontSize = 0.25 };
        public Attribute DrawingDate3 { get; } = new() { Text = "Drawing Date 3", FontSize = 0.25 };
        public Attribute DrawingDate4 { get; } = new() { Text = "Drawing Date 4", FontSize = 0.25 };
        public Attribute DrawingDate5 { get; } = new() { Text = "Drawing Date 5", FontSize = 0.25 };
        public Attribute DrawingDate6 { get; } = new() { Text = "Drawing Date 6", FontSize = 0.25 };

        public Attribute DrawnBy { get; } = new() { Text = "Drawn By", FontSize = 0.25 };
        public Attribute DateDrawn { get; } = new() { Text = "Date Drawn", FontSize = 0.25 };
        public Attribute ProjectName { get; } = new() { Text = "Project Name", FontSize = 0.25 };
        public Attribute PageTitle { get; } = new() { Text = "Page Title", FontSize = 0.25 };
        public Attribute PageNumber { get; } = new() { Text = "Page Number", FontSize = 0.5 };
        public Attribute Scale { get; } = new() { Text = "No Scale", FontSize = 0.25 };

        public event PropertyChangedEventHandler? PropertyChanged;
    }

}
