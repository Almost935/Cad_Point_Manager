using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using Cad_Point_Manager.Models.DxfImport;
using netDxf.Entities;
using netDxf.Tables;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Windows;
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
        public List<SolidVertex> SolidVertices { get; set; } = [];
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }
        public int StartSolidVertexIndex { get; set; }
        public int EndSolidVertexIndex { get; set; }
        public TextStyle TextStyle { get; set; }
        public List<ArrowheadInstance> Arrowheads { get; } = [];
        public DrawingObject? Arrowhead { get; set; } = null;
        #endregion

        #region Constructors
        public DrawingMleader(ParsedMLeader parsedMLeader, ObjectLayer layer, TextStyle textStyle,
            bool isPartOfBlock = false, DrawingBlock block = null, DrawingBlock? arrowHeadBlock = null)
        {
            Type = DrawingObjectType.DrawingMleader;

            ParsedMLeader = parsedMLeader;
            Text = parsedMLeader.Context.Text;
            Layer = layer;
            TextStyle = textStyle;
            ObjectColor = parsedMLeader.Color.color;
            ColorType = parsedMLeader.Color.colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            Arrowhead = arrowHeadBlock;

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

            GetArrowheadBlocks();
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
        public void UpdateGeometryVertices(ResCache resCache, uint layerId, uint objectId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            LineVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingGeometry geometry)
                {
                    geometry.UpdateVertices(resCache, layerId, objectId);
                    LineVertices.AddRange(geometry.Vertices);
                }
                if (obj is DrawingMtext mtext)
                {
                    mtext.UpdateVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    foreach (var segment in mtext.Segments)
                    {
                        LineVertices.AddRange(segment.LineVertices);
                    }
                }
            }

            if (Arrowhead is not null)
            {
                if (Arrowhead is DrawingBlock block)
                {
                    block.UpdateGeometryVertices(resCache, layerId, objectId);

                    foreach (var arrowhead in Arrowheads)
                    {
                        var transform = arrowhead.Transform;

                        foreach (var vertex in block.LineVertices)
                        {
                            var transformed = vertex.Transform(transform);
                            LineVertices.Add(transformed);
                        }
                    }
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
                    mtext.UpdateVertices(resCache, layerId, sceneIdMap, stateBuffers);

                    foreach (var segment in mtext.Segments)
                    {
                        TextVertices.AddRange(segment.TextVertices);
                    }
                }
            }

            if (Arrowhead is not null)
            {
                if (Arrowhead is DrawingBlock block)
                {
                    block.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);

                    foreach (var arrowhead in Arrowheads)
                    {
                        var transform = arrowhead.Transform;

                        foreach (var vertex in block.TextVertices)
                        {
                            var transformed = vertex.Transform(transform);
                            TextVertices.Add(transformed);
                        }
                    }
                }
            }
        }
        public void UpdateSolidVertices(ResCache resCache, uint layerId, uint objectId)
        {
            SolidVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingSolid solid)
                {
                    solid.UpdateVertices(layerId, objectId);
                    SolidVertices.AddRange(solid.Vertices);
                }
            }
            if (Arrowhead is not null)
            {
                if (Arrowhead is DrawingBlock block)
                {
                    block.UpdateSolidVertices(resCache, layerId, objectId);

                    foreach (var arrowhead in Arrowheads)
                    {
                        var transform = arrowhead.Transform;

                        foreach (var vertex in block.SolidVertices)
                        {
                            var transformed = vertex.Transform(transform);
                            SolidVertices.Add(transformed);
                        }
                    }
                }
                else if (Arrowhead is DrawingSolid solid)
                {
                    solid.UpdateVertices(layerId, objectId);

                    foreach (var arrowhead in Arrowheads)
                    {
                        var transform = arrowhead.Transform;

                        foreach (var vertex in solid.Vertices)
                        {
                            var transformed = vertex.Transform(transform);
                            SolidVertices.Add(transformed);
                        }
                    }
                }
            }
        }
        public void GetArrowheadBlocks()
        {
            if (ParsedMLeader.Style.ArrowheadType == ArrowheadType.ClosedFilled)
            {
                foreach (var leader in ParsedMLeader.Context.Leaders)
                {
                    Vector2 dogLegStart = leader.LastLeaderLinePoint.ToSharpDXVector2();

                    foreach (var leaderLine in leader.LeaderLines)
                    {
                        var vertex = leaderLine.Vertex.ToSharpDXVector2();
                        Vector2 dir = Vector2.Normalize(dogLegStart - vertex);

                        float length = ParsedMLeader.Style.ArrowheadSize;
                        float halfWidth = length / 6f;

                        Vector2 baseCenter = vertex + dir * length;
                        Vector2 perp = new(-dir.Y, dir.X);

                        Vector2 leftBase = baseCenter + perp * halfWidth;
                        Vector2 rightBase = baseCenter - perp * halfWidth;

                        Solid dxfSolid = new(
                            vertex.ToNetDxfVector2(), leftBase.ToNetDxfVector2(), rightBase.ToNetDxfVector2(), rightBase.ToNetDxfVector2());
                        DrawingSolid solid = new(dxfSolid, Layer, ObjectColor, ColorType, IsPartOfBlock, DrawingBlock);
                        ArrowheadInstance arrowheadInstance = new()
                        {
                            DrawingObject = solid,
                            Translation = Vector3.Zero,
                            RotationRadians = 0f,
                            Scale = 1f
                        };
                        Arrowhead = solid;
                        Arrowheads.Add(arrowheadInstance);
                    }
                }
            }
            else
            {
                foreach (var leader in ParsedMLeader.Context.Leaders)
                {
                    Vector2 dogLegStart = leader.LastLeaderLinePoint.ToSharpDXVector2();
                    foreach (var leaderLine in leader.LeaderLines)
                    {
                        var tip = leaderLine.Vertex.ToSharpDXVector2();
                        Vector2 dir = Vector2.Normalize(tip - dogLegStart);

                        float rotation = MathF.Atan2(dir.Y, dir.X);

                        ArrowheadInstance arrowheadInstance = new()
                        {
                            DrawingObject = Arrowhead,
                            Translation = new Vector3(tip.X, tip.Y, 0f),
                            RotationRadians = rotation,
                            Scale = ParsedMLeader.Style.ArrowheadSize
                        };
                        Arrowheads.Add(arrowheadInstance);
                    }
                }
            }
        }
        #endregion
    }
}
