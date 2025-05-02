using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class PointGroup
    {
        #region Properties
        public string Name { get; set; } = string.Empty;
        public Vector4 Color { get; set; } = new Vector4(0, 0, 0, 1);
        public float PointScale { get; set; } = 1.0f;
        public float TextHeight { get; set; }
        public float BaseTextHeight { get; set; } = 1.0f;
        public List<DxfPoint> Points { get; set; } = [];
        #endregion

        #region Methods
        public PointGroup(string name, Vector4 color, float pointScale, float baseTextHeight)
        {
            Name = name;
            Color = color;
            PointScale = pointScale;
            BaseTextHeight = baseTextHeight;
            TextHeight = baseTextHeight * pointScale;
        }

        public void UpdatePointsScale(float newTextHeight)
        {
           
        }
        #endregion
    }
}
