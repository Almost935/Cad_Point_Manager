using Cad_Point_Manager.Controls.D3DControl;
using netDxf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects.HelperClasses
{
    public class LineType
    {
        private readonly List<float> _pattern = [];

        public uint Id { get; set; }
        public netDxf.Tables.Linetype DxfLineType { get; init; }
        public string Name { get; init; } = "";
        public float PatternLength { get; private set; }
        
        public IReadOnlyList<float> Pattern => _pattern;

        public string Description => DxfLineType.Description;


        public LineType(netDxf.Tables.Linetype linetype)
        {
            Name = linetype.Name;
            DxfLineType = linetype;

            foreach (var segment in linetype.Segments)
            {
                _pattern.Add((float)segment.Length);
                PatternLength += MathF.Abs((float)segment.Length);
            }
        }
    }
}
