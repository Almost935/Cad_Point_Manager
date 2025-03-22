using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.DrawingObjects;
using Cad_Point_Manager.Helpers;
using netDxf;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

using Brush = SharpDX.Direct2D1.Brush;
using Factory1 = SharpDX.DirectWrite.Factory1;
using FontStyle = netDxf.Tables.FontStyle;
using Point = System.Windows.Point;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtext3D : DrawingObject3D
    {
        #region Fields
        private const double _mtextLineSpacingFactor = 1.5;
        private const int _fontRenderingMinimumSize = 50;
        #endregion

        #region Properties
        public MText DxfMtext { get; set; }
        public string Text { get; set; }
        public double MaxWidth { get; set; }
        public Vector3 Position { get; set; }
        public TextBox TextBox { get; set; } = TextBox.Empty;
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public float Rotation { get; set; } = 0;
        public int FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public List<DrawingMtextSegment3D> SegmentsList { get; set; } = [];
        public List<TextVertex> TextVertices { get; set; } = [];

        public bool TextSegmentsLoaded { get; set; } = false;
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
        public override void Select()
        {
            throw new NotImplementedException();
        }
        public override void Deselect()
        {
            throw new NotImplementedException();
        }
        public override double DistanceToPoint(Point p)
        {
            return 1000;
        }
        public override bool HitTest(Point point, float tolerance)
        {
            return false;
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
                AttachmentPoint = GetAttachmentPoint(mText.AttachmentPoint);
                IsBold = mText.Style.FontStyle == FontStyle.Bold;
                IsItalic = mText.Style.FontStyle == FontStyle.Italic;
                FontSize = TextRenderingHelpers.TextHeightToFontSize(mText.Height);
                FontFamilyName = mText.Style.FontFamilyName;
                //Transform = GetTransform(mText.Position);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText or Text");
            }
        }

        private protected System.Windows.Media.Matrix GetTransform(netDxf.Vector3 dxfPos)
        {
            System.Windows.Media.Matrix matrix = new();
            matrix.Translate(dxfPos.X, dxfPos.Y);

            return matrix;
        }

        /// <summary>
        /// Gets the upper left point of the MText.
        /// </summary>
        /// <param name="mText"></param>
        /// <param name="rect"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 GetMtextOrigin(Enums.TextAttachmentPoint attachmentPoint, RectangleF rect, Vector3 position)
        {
            Vector3 adjustedPos = Vector3.Zero;

            switch (attachmentPoint)
            {
                case Enums.TextAttachmentPoint.TopLeft:
                    adjustedPos = new Vector3(position.X,
                        position.Y - rect.Height, 0);
                    break;

                case Enums.TextAttachmentPoint.TopCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y - rect.Height, 0);
                    break;

                case Enums.TextAttachmentPoint.TopRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y - rect.Height, 0);
                    break;

                case Enums.TextAttachmentPoint.MiddleLeft:
                    adjustedPos = new Vector3(position.X,
                        position.Y - (rect.Height / 2), 0);
                    break;

                case Enums.TextAttachmentPoint.MiddleCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y - (rect.Height / 2), 0);
                    break;

                case Enums.TextAttachmentPoint.MiddleRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y - (rect.Height / 2), 0);
                    break;

                case Enums.TextAttachmentPoint.BottomLeft:
                    adjustedPos = position;
                    break;

                case Enums.TextAttachmentPoint.BottomCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y, 0);
                    break;

                case Enums.TextAttachmentPoint.BottomRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y, 0);
                    break;

                default:
                    adjustedPos = position;
                    break;
            }

            return adjustedPos;
        }

        public void InitializeTextBox(double initialTextHeight)
        {
            var spacing = (float)(initialTextHeight * _mtextLineSpacingFactor * DxfMtext.LineSpacingFactor);
            switch (AttachmentPoint)
            {
                case Enums.TextAttachmentPoint.TopLeft:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X, Position.Y), new Point(Position.X + MaxWidth, Position.Y - initialTextHeight));
                    break;

                case Enums.TextAttachmentPoint.TopCenter:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X - MaxWidth * 0.5f, Position.Y), new Point(Position.X + MaxWidth * 0.5f, Position.Y - initialTextHeight));
                    break;

                case Enums.TextAttachmentPoint.TopRight:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X - MaxWidth * 0.5f, Position.Y), new Point(Position.X - MaxWidth, Position.Y - initialTextHeight));
                    break;

                case Enums.TextAttachmentPoint.MiddleLeft:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X, Position.Y - initialTextHeight * 0.5f), new Point(Position.X + MaxWidth, Position.Y + initialTextHeight * 0.5f));
                    break;

                case Enums.TextAttachmentPoint.MiddleCenter:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X - MaxWidth * 0.5f, Position.Y - initialTextHeight * 0.5f), new Point(Position.X + (float)MaxWidth * 0.5f, Position.Y + initialTextHeight * 0.5f));
                    break;

                case Enums.TextAttachmentPoint.MiddleRight:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X - MaxWidth, Position.Y - initialTextHeight * 0.5f), new Point(Position.X - (float)MaxWidth * 0.5f, Position.Y + initialTextHeight * 0.5f));
                    break;

                case Enums.TextAttachmentPoint.BottomLeft:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X, Position.Y - initialTextHeight), new Point(Position.X + MaxWidth, Position.Y));
                    break;

                case Enums.TextAttachmentPoint.BottomCenter:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X - MaxWidth * 0.5f, Position.Y - initialTextHeight), new Point(Position.X + MaxWidth * 0.5f, Position.Y));
                    break;

                case Enums.TextAttachmentPoint.BottomRight:
                    TextBox = new TextBox(new Point(Position.X, Position.Y), new Point(Position.X - MaxWidth, Position.Y - initialTextHeight), new Point(Position.X - MaxWidth * 0.5f, Position.Y));
                    break;

                default:
                    break;
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            Bounds = AttachmentPoint switch
            {
                Enums.TextAttachmentPoint.TopLeft => new Rect(DxfMtext.Position.X, DxfMtext.Position.Y, MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.TopCenter => new Rect(DxfMtext.Position.X - (MaxWidth * 0.5), DxfMtext.Position.Y, MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.TopRight => new Rect(DxfMtext.Position.X - (MaxWidth * 0.5), DxfMtext.Position.Y, MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.MiddleLeft => new Rect(DxfMtext.Position.X, DxfMtext.Position.Y - (DxfMtext.Height * 0.5), MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.MiddleCenter => new Rect(DxfMtext.Position.X - (MaxWidth * 0.5), DxfMtext.Position.Y - (DxfMtext.Height * 0.5), MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.MiddleRight => new Rect(DxfMtext.Position.X - (MaxWidth * 0.5), DxfMtext.Position.Y - (DxfMtext.Height * 0.5), MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.BottomLeft => new Rect(DxfMtext.Position.X, DxfMtext.Position.Y - DxfMtext.Height, MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.BottomCenter => new Rect(DxfMtext.Position.X - (MaxWidth * 0.5), DxfMtext.Position.Y - DxfMtext.Height, MaxWidth, DxfMtext.Height),
                Enums.TextAttachmentPoint.BottomRight => new Rect(DxfMtext.Position.X - (MaxWidth * 0.5), DxfMtext.Position.Y - DxfMtext.Height, MaxWidth, DxfMtext.Height),
                _ => new Rect(DxfMtext.Position.X, DxfMtext.Position.Y, MaxWidth, DxfMtext.Height),
            };
        }

        public void UpdateTextVertices(Factory1 factory, Factory2 d2dFactory)
        {
            if (DxfMtext is null) { return; }

            SegmentsList.Clear();
            TextVertices.Clear();

            SegmentsList = GetMtextSegments(factory, d2dFactory);
            foreach (var segment in SegmentsList)
            {
                TextVertices.AddRange(segment.TextVertices);
            }

            TextSegmentsLoaded = true;
            UpdateBounds();
        }

        public List<DrawingMtextSegment3D> GetMtextSegments(Factory1 factory, Factory2 d2dFactory)
        {
            List<DrawingMtextSegment3D> mtextSegmentList = new();
            string rawText = DxfMtext.Value;

            if (!rawText.Contains('\\'))
            {
                InitializeTextBox(DxfMtext.Height);
                DrawingMtextSegment3D textSegment = new(this, DxfMtext.Value, Color, Position, Rotation, FontSize, FontFamilyName, (float)DxfMtext.Height,
                    false, false, false, false, _fontRenderingMinimumSize, (float)MaxWidth);
                textSegment.GetTextFormat(factory);
                textSegment.GetTextLayout(factory);
                textSegment.Tesselate(d2dFactory);
                mtextSegmentList.Add(textSegment);

                return mtextSegmentList;
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

            // Extract formatting changes and text segments
            List<TextSegmentInformation> textSegments = [];

            string pattern = $@"((\\[LOkoK])|{aciColorPattern}|{trueTypeColorPattern}|{fontPattern}|{italicPattern}|{boldPattern}|{heightPattern}|{lineBreakPattern}|
{underlineStartPattern}|{underlineEndPattern}|{overstrikeStartPattern}|{overstrikeEndPattern}|{strikethroughStartPattern}|{strikethroughEndPattern}|
{alignLeftPattern}|{alignCenterPattern}|{alignRightPattern}|{alignJustifyPattern}|{alignDistributedPattern}|[^{{}}\\]+)";

            TextSegmentInformation currentSegment = new("", Color, FontFamilyName, DxfMtext.Height, IsBold, IsItalic, false, false, false);

            foreach (var text in texts)
            {
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
                        currentSegment.TextHeight = double.Parse(Regex.Match(value, heightPattern).Groups[1].Value);
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

                    }
                    else if (Regex.IsMatch(value, alignCenterPattern))
                    {

                    }
                    else if (Regex.IsMatch(value, alignRightPattern))
                    {

                    }
                    else if (Regex.IsMatch(value, alignJustifyPattern))
                    {

                    }
                    else if (Regex.IsMatch(value, alignDistributedPattern))
                    {

                    }
                    else
                    {
                        currentSegment.Text += value;

                        if (currentSegment.HasValue) { textSegments.Add(currentSegment); }

                        currentSegment = new("", currentSegment.Color, currentSegment.Font, currentSegment.TextHeight, currentSegment.IsBold, currentSegment.IsItalic, currentSegment.IsUnderlined, currentSegment.IsOverstriked, currentSegment.IsStrikethrough, currentSegment.IsNewLine);

                        currentSegment.IsNewLine = false;
                    }
                }

                Vector3 basePosition = Position;
                float yOffset = 0;
                float xOffset = 0;

                for (int i = 0; i < textSegments.Count; i++)
                {
                    if (i == 0) { InitializeTextBox(textSegments[i].TextHeight); }

                    var textSegment = textSegments[i];

                    var splitTexts = textSegment.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    var spaceWidth = TextRenderingHelpers.GetSpaceWidth(factory, textSegment.Font, TextRenderingHelpers.TextHeightToFontSize(textSegment.TextHeight));
                    var segments = CreateMtextSegments(new Vector3(basePosition.X, basePosition.Y, 0), (float)MaxWidth, factory, d2dFactory, this, textSegment, splitTexts,
                        (float)spaceWidth, ref xOffset, ref yOffset);

                    mtextSegmentList.AddRange(segments);
                }
            }

            return mtextSegmentList;
        }
        #endregion

        #region Static Methods
        private List<DrawingMtextSegment3D> CreateMtextSegments(Vector3 basePosition, float maxWidth, Factory1 factory, Factory2 d2dFactory,
            DrawingMtext3D drawingMtext, TextSegmentInformation segmentInfo, List<string> texts, float spaceWidth, ref float xOffset, ref float yOffset)
        {
            List<DrawingMtextSegment3D> segments = new();

            float lineSpacing = (float)(segmentInfo.TextHeight * _mtextLineSpacingFactor * drawingMtext.DxfMtext.LineSpacingFactor);
            var fontSize = TextRenderingHelpers.TextHeightToFontSize(segmentInfo.TextHeight);

            foreach (string text in texts)
            {
                var segment = new DrawingMtextSegment3D(
                    drawingMtext, text, segmentInfo.Color,
                    new Vector3(basePosition.X + xOffset, basePosition.Y + yOffset, 0),
                    0, fontSize, segmentInfo.Font, (float)segmentInfo.TextHeight,
                    segmentInfo.IsItalic, segmentInfo.IsBold, segmentInfo.IsUnderlined,
                    segmentInfo.IsStrikethrough, _fontRenderingMinimumSize, 0);

                segment.GetTextFormat(factory);
                segment.GetTextLayout(factory);
                segment.Tesselate(d2dFactory);

                float segmentWidth = (float)segment.Bounds.Width;
                bool shouldWrapLine = segmentInfo.IsNewLine || (xOffset != 0 && (xOffset + segmentWidth + spaceWidth) > maxWidth);

                if (shouldWrapLine)
                {
                    yOffset -= lineSpacing;
                    xOffset = 0;

                    Debug.WriteLine($"\nsegment.Text: {segment.Text}" +
                        $"\nInitial: {TextBox}" +
                        $"\nlineSpacing: {lineSpacing}");
                    TextBox.Expand(0, lineSpacing, 0, 0); 
                    Debug.WriteLine($"Final: {TextBox}");

                    segment.Position = new Vector3(basePosition.X, basePosition.Y + yOffset, 0);
                    segment.UpdateTransform();

                    // Update segment layouts after position change
                    segment.GetTextFormat(factory);
                    segment.GetTextLayout(factory);
                    segment.Tesselate(d2dFactory);
                }

                segments.Add(segment);
                xOffset += segmentWidth + spaceWidth;
            }

            return segments;
        }

        private static protected Enums.TextAttachmentPoint GetAttachmentPoint(MTextAttachmentPoint mTextAttachment)
        {
            return mTextAttachment switch
            {
                MTextAttachmentPoint.TopLeft => Enums.TextAttachmentPoint.TopLeft,
                MTextAttachmentPoint.TopCenter => Enums.TextAttachmentPoint.TopCenter,
                MTextAttachmentPoint.TopRight => Enums.TextAttachmentPoint.TopRight,
                MTextAttachmentPoint.MiddleLeft => Enums.TextAttachmentPoint.MiddleLeft,
                MTextAttachmentPoint.MiddleCenter => Enums.TextAttachmentPoint.MiddleCenter,
                MTextAttachmentPoint.MiddleRight => Enums.TextAttachmentPoint.MiddleRight,
                MTextAttachmentPoint.BottomLeft => Enums.TextAttachmentPoint.BottomLeft,
                MTextAttachmentPoint.BottomCenter => Enums.TextAttachmentPoint.BottomCenter,
                MTextAttachmentPoint.BottomRight => Enums.TextAttachmentPoint.BottomRight,
                _ => Enums.TextAttachmentPoint.MiddleCenter,
            };
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

        public bool HasValue => !string.IsNullOrEmpty(Text);
        #endregion

        #region Constructors
        public TextSegmentInformation(string text = "", Vector4? color = null, string font = "Arial", double textHeight = 0, bool isBold = false,
            bool isItalic = false, bool isUnderlined = false, bool isOverstrike = false, bool isStrikethrough = false, bool isNewLine = false)
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
