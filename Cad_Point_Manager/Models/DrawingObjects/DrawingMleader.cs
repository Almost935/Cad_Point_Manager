using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DxfImport;
using netDxf.Entities;
using netDxf.Tables;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Brush = SharpDX.Direct2D1.Brush;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMleader : DrawingObject
    {
        #region Fields
        #endregion

        #region Properties
        public ParsedMLeader ParsedMLeader { get; set; }
        public string Text { get; set; }
        public List<DrawingObject> DrawingObjects { get; set; } = [];
        public List<LineVertex> LineVertices { get; set; } = [];
        public List<TextVertex> TextVertices { get; set; } = [];
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }
        public TextStyle TextStyle { get; set; }
        #endregion

        #region Constructors
        public DrawingMleader(ParsedMLeader parsedMLeader, ObjectLayer layer, TextStyle textStyle, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingLine;

            ParsedMLeader = parsedMLeader;
            Text = parsedMLeader.Context.Text;
            Layer = layer;
            TextStyle = textStyle;
            ObjectColor = parsedMLeader.Color.color;
            ColorType = parsedMLeader.Color.colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            foreach (var leader in ParsedMLeader.Context.Leaders)
            {
                var dogLegStart = leader.LastLeaderLinePoint.ToNetDxfVector2();
                var dogLegEnd = (leader.LastLeaderLinePoint + (leader.DogLegLength * leader.DogLegVector)).ToNetDxfVector2();
                Line dogLeg = new(dogLegStart, dogLegEnd);
                DrawingLine drawingLineDogLeg = new(dogLeg, Layer, ObjectColor, ColorType, IsPartOfBlock, DrawingBlock);
                DrawingObjects.Add(drawingLineDogLeg);

                foreach (var leaderLine in leader.LeaderLines)
                {
                    Line line = new(dogLegStart, leaderLine.Vertex.ToNetDxfVector2());
                    DrawingLine drawingLine = new(line, Layer, ObjectColor, ColorType, IsPartOfBlock, DrawingBlock);
                    DrawingObjects.Add(drawingLine);
                }
            }
            MText mtext = new(ParsedMLeader.Context.Text, ParsedMLeader.Context.TextLocation.ToNetDxfVector3(), ParsedMLeader.Context.TextHeight, 0, TextStyle)
            {
                Layer = Layer.DxfLayer
            };

            DrawingMtext drawingMtext = new(mtext, Layer, ObjectColor, ColorType, false, IsPartOfBlock, DrawingBlock);
            DrawingObjects.Add(drawingMtext);
        }
        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var drawingObj in DrawingObjects)
            {
                Bounds = Rect.Union(Bounds, drawingObj.Bounds);
            }
        }
        public override double DistanceToPoint(System.Windows.Point p)
        {
            return 0;
        }
        public override void MouseEnter()
        {

        }
        public override void MouseLeave()
        {

        }
        public override void Select()
        {

        }
        public override void Deselect()
        {

        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {

        }
        public void UpdateGeometryVertices(uint layerId, uint objectId)
        {
            LineVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingGeometry geometry)
                {
                    geometry.UpdateVertices(layerId, objectId);
                    LineVertices.AddRange(geometry.Vertices);
                }
            }
        }
        public void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            TextVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingMtext mtext)
                {
                    mtext.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);

                    foreach (var segment in mtext.Segments)
                    {
                        TextVertices.AddRange(segment.TextVertices);
                    }
                }
            }
        }
        #endregion
    }
}
