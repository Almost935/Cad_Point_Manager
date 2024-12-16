using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        public DxfDocument DxfDocument { get; set; }

        public List<DrawingLine3D> Lines = [];

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadDxf(DxfDocument dxfDocument)
        {
            DxfDocument = dxfDocument;
            foreach (var line in DxfDocument.Entities.Lines)
            {
                if (line is not null)
                {
                    Lines.Add(new DrawingLine3D(line));
                }
            }
        }

        public void ClearDxf()
        {
            DxfDocument = null;
            Lines.Clear();
        }
    }
}
