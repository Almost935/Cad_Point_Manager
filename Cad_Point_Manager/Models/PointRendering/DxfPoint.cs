using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class DxfPoint
    {
        #region Fields
        #endregion

        #region Properties
        public int PointNumber { get; set; }
        public Vector3 Position { get; set; } = Vector3.Zero;
        public float TextHeight { get; set; } = 5.0f;
        public PointGroup PointGroup { get; set; }
        public TextVertex[] TextVertices { get; set; }
        public LineVertex[] LineVertices { get; set; }
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int LineStartIndex { get; set; }
        public int LineEndIndex { get; set; }
        #endregion

        #region Constructors
        public DxfPoint(PointGroup pointGroup, int pointNum, Vector3 position)
        {
            PointGroup = pointGroup;
            UpdatePointScale();
            PointNumber = pointNum;
            Position = position;
        }
        #endregion

        #region Methods
        public void UpdateTextVertices(DxfPointTextVerticesDict textDict)
        {
            TextVertices ??= Array.Empty<TextVertex>();
            Array.Clear(TextVertices);

            TextVertices = textDict.GetIntTextVertices(PointNumber, TextHeight, new Vector2(Position.X, Position.Y));
        }

        public void UpdatePointScale()
        {
            TextHeight = PointGroup.BaseTextHeight * PointGroup.PointScale;
        }
        #endregion
    }
}
