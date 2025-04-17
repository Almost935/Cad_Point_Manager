using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.DrawingObjects;
using Cad_Point_Manager.Helpers;
using netDxf;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

using Brush = SharpDX.Direct2D1.Brush;
using Factory1 = SharpDX.DirectWrite.Factory1;
using FontStyle = netDxf.Tables.FontStyle;
using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtext3D : DrawingObject3D
    {
        #region Fields
        private const float _mtextLineSpacingFactor = 0.4f;
        private const int _fontRenderingMinimumSize = 50;
        #endregion

        #region Properties
        public MText DxfMtext { get; set; }
        public string Text { get; set; }
        public double MaxWidth { get; set; }
        public Vector3 Position { get; set; }
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public float Rotation { get; set; } = 0;
        public float FontHeight { get; set; }
        public string FontFamilyName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public DrawingMtext3DBlock MtextBlock { get; set; }
        public Vector3 TextAttachmentOffset { get; set; } = Vector3.Zero;
        #endregion

        #region Constructor
        public DrawingMtext3D(MText mtext, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            DxfMtext = mtext;
            EntityObject = mtext;
            Layer = layer;
            DrawingBlock3D = block;

            UpdateColor();
            UpdateData(mtext);
        }
        #endregion

        #region Methods
        public override void MouseEnter()
        {
            this.IsMouseOver = true;

            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    for (int i = 0; i < segment.TextVertices.Length; i++)
                    {
                        var vertex = segment.TextVertices[i];
                        vertex.IsMouseOver = 1.0f;
                        segment.TextVertices[i] = vertex;
                    }
                }
            }
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;

            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    for (int i = 0; i < segment.TextVertices.Length; i++)
                    {
                        var vertex = segment.TextVertices[i];
                        vertex.IsMouseOver = 0.0f;
                        segment.TextVertices[i] = vertex;
                    }
                }
            }
        }
        public override double DistanceToPoint(Point p)
        {
            double finalDist = double.MaxValue;
            var vertices = MtextBlock.Rows.SelectMany(row => row.Segments.SelectMany(segment => segment.TextVertices)).ToList();

            if (PointInTextGeometry(new Vector2((float)p.X, (float)p.Y), vertices))
            {
                return 0;
            }
            else
            {
                for (int i = 0; i < vertices.Count; i += 3)
                {
                    var v1 = vertices[i];
                    var v2 = vertices[i + 1];
                    var v3 = vertices[i + 2];

                    var dist = MathHelpers.DistanceToTriangle(new Vector2((float)p.X, (float)p.Y), new Vector2(v1.Position.X, v1.Position.Y),
                        new Vector2(v2.Position.X, v2.Position.Y), new Vector2(v3.Position.X, v3.Position.Y));

                    if (dist < finalDist) { finalDist = dist; }
                }
            }

            return finalDist;
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            //deviceContext.DrawTextLayout(new RawVector2((float)Position.X, -(float)Position.Y), TextLayout, brush);
        }
        public override void UpdateData(EntityObject entity)
        {
            if (entity is MText mText)
            {
                DxfMtext = mText;
                Text = mText.Value;
                MaxWidth = mText.RectangleWidth;
                Position = new((float)mText.Position.X, (float)mText.Position.Y, 0);
                Rotation = (float)mText.Rotation;
                UpdateBounds();
                IsBold = mText.Style.FontStyle == FontStyle.Bold;
                IsItalic = mText.Style.FontStyle == FontStyle.Italic;
                FontHeight = (float)mText.Height;
                FontFamilyName = mText.Style.FontFamilyName;
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText or Text");
            }
        }

        public override void UpdateBounds()
        {
            if (MtextBlock is null) { return; } // No text to update bounds for.

            Bounds = Rect.Empty;
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    for (int i = 0; i < segment.TextVertices.Length; i++)
                    {
                        Bounds = Rect.Union(Bounds, (Point)segment.TextVertices[i]);
                    }
                }
            }
        }

        public void UpdateTextVertices(Factory1 factory, Factory2 d2dFactory)
        {
            if (DxfMtext is null) { return; }

            UpdateMtextBlock(factory, d2dFactory);
            MtextBlock.SetTextPositions();
            MtextBlock.GetTextBox(MtextBlock.Height);
            SetRotation();
            UpdateBounds();
            GetGlowVertices();
        }
        public void GetGlowVertices()
        {
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    segment.GetGlowVertices();
                }
            }
        }

        public void UpdateMtextBlock(Factory1 factory, Factory2 d2dFactory)
        {
            MtextBlock?.Dispose();
            MtextBlock = new((float)MaxWidth, Position, DxfMtext.AttachmentPoint, Rotation);

            string rawText = DxfMtext.Value;

            if (!rawText.Contains('\\'))
            {
                var segmentTexts = rawText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var text in segmentTexts)
                {
                    TextSegmentInformation segmentInfo = new(text, Color, FontFamilyName, DxfMtext.Height, IsBold, IsItalic, false, false, false, false, Enums.TextAlignment.Left);
                    var textSegment = CreateMtextSegment(segmentInfo, factory, d2dFactory);
                    textSegment.GetTextLayout(factory);
                    textSegment.Tesselate(d2dFactory);
                    MtextBlock.AddSegment(textSegment);
                }

                return;
            }

            var texts = rawText.Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);

            // Regex patterns for DXF formatting
            string aciColorPattern = @"\\[C](\d+);";
            string trueTypeColorPattern = @"\\[c](\d+);";
            string fontPattern = @"\\f([^;]+);";
            string italicPattern = @"\|i(\d+)";
            string boldPattern = @"\|b(\d+)";
            string heightPattern = @"\\H([\d.]+)x?;";
            string lineBreakPattern = @"\\P";
            string underlineStartPattern = @"\\L";
            string underlineEndPattern = @"\\l";
            string overstrikeStartPattern = @"\\O";
            string overstrikeEndPattern = @"\\o";
            string strikethroughStartPattern = @"\\K";
            string strikethroughEndPattern = @"\\k";
            string alignLeftPattern = @"\\pxql;";
            string alignCenterPattern = @"\\pxqc;";
            string alignRightPattern = @"\\pxqr;";
            string alignJustifyPattern = @"\\pxqj;";
            string alignDistributedPattern = @"\\pxqd;";

            string pattern = $@"((\\[LOkoK])|{aciColorPattern}|{trueTypeColorPattern}|{fontPattern}|{italicPattern}|{boldPattern}|{heightPattern}|{lineBreakPattern}|{underlineStartPattern}|{underlineEndPattern}|{overstrikeStartPattern}|{overstrikeEndPattern}|{strikethroughStartPattern}|{strikethroughEndPattern}|{alignLeftPattern}|{alignCenterPattern}|{alignRightPattern}|{alignJustifyPattern}|{alignDistributedPattern}|[^{{}}\\]+)";

            Enums.TextAlignment baseAlignment;
            if (AttachmentPoint == Enums.TextAttachmentPoint.TopRight || AttachmentPoint == Enums.TextAttachmentPoint.MiddleRight || AttachmentPoint == Enums.TextAttachmentPoint.BottomRight) { baseAlignment = Enums.TextAlignment.Right; }
            else if (AttachmentPoint == Enums.TextAttachmentPoint.TopCenter || AttachmentPoint == Enums.TextAttachmentPoint.MiddleCenter || AttachmentPoint == Enums.TextAttachmentPoint.BottomCenter) { baseAlignment = Enums.TextAlignment.Center; }
            else { baseAlignment = Enums.TextAlignment.Left; }

            TextSegmentInformation currentSegment = new("", Color, FontFamilyName, DxfMtext.Height, IsBold, IsItalic, false, false, false, false, baseAlignment);

            foreach (var text in texts)
            {
                List<TextSegmentInformation> textSegments = [];
                MatchCollection matches = Regex.Matches(text, pattern);
                currentSegment.Color = Color;

                foreach (Match match in matches)
                {
                    string value = match.Value;

                    if (string.IsNullOrWhiteSpace(value)) { continue; }

                    if (Regex.IsMatch(value, aciColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, aciColorPattern).Groups[1].Value);

                        if (colorI == 0)
                        {
                            if (IsPartOfBlock)
                            {
                                currentSegment.Color = DrawingBlock3D.Color;
                            }
                            else
                            {
                                var aciColor = AciColor.Default;
                                currentSegment.Color = new(aciColor.R / 255.0f, aciColor.G / 255.0f, aciColor.B / 255.0f, 1.0f);
                            }
                        }
                        else if (colorI == 256)
                        {
                            currentSegment.Color = Layer.Color;
                        }
                        else
                        {
                            var vector = AutoCadColorConverter.ConvertACINumberToRGBA((short)colorI);
                            currentSegment.Color = new((float)vector.X, (float)vector.Y, (float)vector.Z, (float)vector.W);
                        }
                    }
                    else if (Regex.IsMatch(value, trueTypeColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, trueTypeColorPattern).Groups[1].Value);
                        var trueTypeColor = AutoCadColorConverter.ConvertTrueColorToVector4(colorI);

                        currentSegment.Color = new((float)trueTypeColor.X, (float)trueTypeColor.Y, (float)trueTypeColor.Z, (float)trueTypeColor.W);
                    }
                    else if (Regex.IsMatch(value, fontPattern))
                    {
                        currentSegment.Font = Regex.Match(value, fontPattern).Groups[1].Value.Split('|')[0];
                    }
                    else if (Regex.IsMatch(value, italicPattern))
                    {
                        currentSegment.IsItalic = Regex.Match(value, italicPattern).Groups[1].Value == "1";
                    }
                    else if (Regex.IsMatch(value, boldPattern))
                    {
                        currentSegment.IsBold = Regex.Match(value, boldPattern).Groups[1].Value == "1";
                    }
                    else if (Regex.IsMatch(value, heightPattern))
                    {
                        currentSegment.TextHeight *= double.Parse(Regex.Match(value, heightPattern).Groups[1].Value);
                    }
                    else if (Regex.IsMatch(value, lineBreakPattern))
                    {
                        currentSegment.IsNewLine = true;
                    }
                    else if (Regex.IsMatch(value, underlineStartPattern))
                    {
                        currentSegment.IsUnderlined = true;
                    }
                    else if (Regex.IsMatch(value, underlineEndPattern))
                    {
                        currentSegment.IsUnderlined = false;
                    }
                    else if (Regex.IsMatch(value, overstrikeStartPattern))
                    {
                        currentSegment.IsOverstriked = true;
                    }
                    else if (Regex.IsMatch(value, overstrikeEndPattern))
                    {
                        currentSegment.IsOverstriked = false;
                    }
                    else if (Regex.IsMatch(value, strikethroughStartPattern))
                    {
                        currentSegment.IsStrikethrough = true;
                    }
                    else if (Regex.IsMatch(value, strikethroughEndPattern))
                    {
                        currentSegment.IsStrikethrough = false;
                    }
                    else if (Regex.IsMatch(value, alignLeftPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Left;
                    }
                    else if (Regex.IsMatch(value, alignCenterPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Center;
                    }
                    else if (Regex.IsMatch(value, alignRightPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Right;
                    }
                    else if (Regex.IsMatch(value, alignJustifyPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Justified;
                    }
                    else if (Regex.IsMatch(value, alignDistributedPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Distributed;
                    }
                    else
                    {
                        currentSegment.Text += value;
                        currentSegment.Text = currentSegment.Text.TrimEnd();
                        if (currentSegment.HasValue) { textSegments.Add(currentSegment); }

                        currentSegment = new("", currentSegment.Color, currentSegment.Font, currentSegment.TextHeight, currentSegment.IsBold,
                            currentSegment.IsItalic, currentSegment.IsUnderlined, currentSegment.IsOverstriked, currentSegment.IsStrikethrough,
                            currentSegment.IsNewLine, currentSegment.TextAlignment);

                        currentSegment.IsNewLine = false;
                    }
                }

                Vector3 basePosition = Position;

                foreach (var segmentInfo in textSegments)
                {
                    var segmentTexts = segmentInfo.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (segmentTexts.Length > 1)
                    {
                        for (int i = 0; i < segmentTexts.Length; i++)
                        {
                            var segmentText = segmentTexts[i];

                            bool isNewLine = segmentInfo.IsNewLine;
                            if (i != 0) { isNewLine = false; } // Only the very first segment can be a new line.    

                            var newSegmentInfo = new TextSegmentInformation(segmentText, segmentInfo.Color, segmentInfo.Font, segmentInfo.TextHeight,
                                segmentInfo.IsBold, segmentInfo.IsItalic, segmentInfo.IsUnderlined, segmentInfo.IsOverstriked, segmentInfo.IsStrikethrough,
                                isNewLine, segmentInfo.TextAlignment);
                            var newSegment = CreateMtextSegment(newSegmentInfo, factory, d2dFactory);
                            MtextBlock.AddSegment(newSegment);
                        }
                    }
                    else
                    {
                        var segment = CreateMtextSegment(segmentInfo, factory, d2dFactory);
                        MtextBlock.AddSegment(segment); // Add the segment to the block for vertex generation and other purposes.
                    }
                }
            }
        }
        private void SetRotation()
        {
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    for (int i = 0; i < segment.TextVertices.Length; i++)
                    {
                        segment.TextVertices[i] = TextVertex.RotateAroundPoint(segment.TextVertices[i], new Vector2(Position.X, Position.Y), (float)(MathHelper.DegToRad * Rotation));
                    }
                }
            }

            //for (int i = 0; i < TextVertices.Count(); i++)
            //{
            //    TextVertices[i] = TextVertex.RotateAroundPoint(TextVertices[i], new Vector2(Position.X, Position.Y), (float)(MathHelper.DegToRad * Rotation));
            //}
        }
        private DrawingMtextSegment3D CreateMtextSegment(TextSegmentInformation segmentInfo, Factory1 factory, Factory2 d2dFactory)
        {
            var segment = new DrawingMtextSegment3D(this, segmentInfo.Text, segmentInfo.Color, Vector3.Zero, 0, (float)segmentInfo.TextHeight, segmentInfo.Font,
                segmentInfo.IsItalic, segmentInfo.IsBold, segmentInfo.IsUnderlined, segmentInfo.IsStrikethrough, segmentInfo.IsNewLine, _fontRenderingMinimumSize, 0);
            segment.GetTextLayout(factory);
            segment.Tesselate(d2dFactory);

            return segment;
        }
        #endregion

        #region Static Methods
        public static bool PointInTextGeometry(Vector2 point, List<TextVertex> textVertices)
        {
            int triangleCount = textVertices.Count / 3;
            bool hit = false;
            object lockObj = new();

            Parallel.For(0, triangleCount, (i, state) =>
            {
                // Exit early if already hit
                if (hit)
                {
                    state.Stop();
                    return;
                }

                var v0 = (Vector2)textVertices[i * 3].Position;
                var v1 = (Vector2)textVertices[i * 3 + 1].Position;
                var v2 = (Vector2)textVertices[i * 3 + 2].Position;

                if (MathHelpers.IsPointInTriangle(point, v0, v1, v2))
                {
                    lock (lockObj) // ensure no race condition on write
                    {
                        hit = true;
                        state.Stop(); // stop the rest
                    }
                }
            });

            return hit;


            //for (int i = 0; i < textVertices.Count; i += 3)
            //{
            //    var v0 = new Vector2(textVertices[i].Position.X, textVertices[i].Position.Y);
            //    var v1 = new Vector2(textVertices[i + 1].Position.X, textVertices[i + 1].Position.Y);
            //    var v2 = new Vector2(textVertices[i + 2].Position.X, textVertices[i + 2].Position.Y);

            //    if (MathHelpers.IsPointInTriangle(point, v0, v1, v2)) { return true; }
            //}
            //return false;
        }
        #endregion
    }

    public class TextSegmentInformation
    {
        #region Properties
        public string Text { get; set; }
        public Vector4 Color { get; set; }
        public string Font { get; set; }
        public double TextHeight { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsOverstriked { get; set; }
        public bool IsStrikethrough { get; set; }
        public bool IsUnderlined { get; set; }
        public bool IsNewLine { get; set; }
        public Enums.TextAlignment TextAlignment { get; set; }

        public bool HasValue => !string.IsNullOrEmpty(Text);
        #endregion

        #region Constructors
        public TextSegmentInformation(string text = "", Vector4? color = null, string font = "Arial", double textHeight = 0, bool isBold = false,
            bool isItalic = false, bool isUnderlined = false, bool isOverstrike = false, bool isStrikethrough = false, bool isNewLine = false,
            Enums.TextAlignment textAlignment = Enums.TextAlignment.Left)
        {
            Text = text;
            Color = color ?? new Vector4(0, 0, 0, 1);
            Font = font;
            TextHeight = textHeight;
            IsBold = isBold;
            IsItalic = isItalic;
            IsUnderlined = isUnderlined;
            IsOverstriked = isOverstrike;
            IsStrikethrough = isStrikethrough;
            IsNewLine = isNewLine;
            TextAlignment = textAlignment;
        }
        #endregion

        #region Methods
        public static TextSegmentInformation GetLineBreak()
        {
            return new TextSegmentInformation("\n");
        }
        #endregion
    }
}
