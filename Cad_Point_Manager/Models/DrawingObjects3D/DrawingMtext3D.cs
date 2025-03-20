using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using FreeTypeSharp;

using Brush = SharpDX.Direct2D1.Brush;
using Factory1 = SharpDX.DirectWrite.Factory1;
using Point = System.Windows.Point;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TextAlignment = netDxf.Entities.TextAlignment;
using netDxf;
using Vector3 = SharpDX.Vector3;
using Cad_Point_Manager.Helpers;
using netDxf.Tables;
using System.Runtime.CompilerServices;
using Vector4 = SharpDX.Vector4;
using FontStyle = netDxf.Tables.FontStyle;
using netDxf.Collections;
using System.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D9;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtext3D : DrawingObject3D
    {
        #region Fields
        private const double _textHeightToFontSizeFactor = 1.25;
        private const double _mtextLineSpacingFactor = 1.5;
        private const int _fontRenderingMinimumSize = 50;
        #endregion

        #region Properties
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public float Rotation { get; set; } = 0;
        public int FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public MText DxfMtext { get; set; }
        public List<DrawingMtextSegment3D> SegmentsList { get; set; } = [];

        public bool TextSegmentsLoaded { get; set; } = false;
        #endregion

        #region Functions
        Func<double, double, int> textHeightToPoints = (textHeight, textHeightToFontSizeFactor) => (int)Math.Ceiling(textHeight * textHeightToFontSizeFactor);
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
        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var segment in SegmentsList)
            {
                Bounds = Rect.Union(Bounds, segment.Bounds);
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            //deviceContext.DrawTextLayout(new RawVector2((float)Position.X, -(float)Position.Y), TextLayout, brush);
        }
        public override void UpdateData(EntityObject entity)
        {
            if (entity is MText mText)
            {
                Text = mText.Value;
                Bounds = new(mText.Position.X, mText.Position.Y, mText.RectangleWidth, mText.Height * 1.2);
                Rotation = (float)mText.Rotation;
                AttachmentPoint = GetAttachmentPoint(mText.AttachmentPoint);
                IsBold = mText.Style.FontStyle == FontStyle.Bold;
                IsItalic = mText.Style.FontStyle == FontStyle.Italic;
                Position = GetTextOrigin(AttachmentPoint, new RectangleF((float)Bounds.Left, (float)Bounds.Top, (float)Bounds.Width, (float)Bounds.Height), new Vector3((float)mText.Position.X, (float)mText.Position.Y, 0));
                FontSize = (int)Math.Ceiling(mText.Height * _textHeightToFontSizeFactor);
                FontFamilyName = mText.Style.FontFamilyName;
                Transform = GetTransform(mText.Position);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText or Text");
            }
        }

        private protected System.Windows.Media.Matrix GetTransform(netDxf.Vector3 dxfPos)
        {
            System.Windows.Media.Matrix matrix = new();
            //matrix.ScaleAt(1, 1, dxfPos.X, dxfPos.Y);
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
        public Vector3 GetTextOrigin(Enums.TextAttachmentPoint attachmentPoint, RectangleF rect, Vector3 position)
        {
            Vector3 adjustedPos = Vector3.Zero;

            switch (attachmentPoint)
            {
                case Enums.TextAttachmentPoint.TopLeft:
                    adjustedPos = position;
                    break;

                case Enums.TextAttachmentPoint.TopCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y, 0);
                    break;

                case Enums.TextAttachmentPoint.TopRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y, 0);
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
                    adjustedPos = new Vector3(position.X,
                        position.Y - (rect.Height), 0);
                    break;

                case Enums.TextAttachmentPoint.BottomCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y - (rect.Height), 0);
                    break;

                case Enums.TextAttachmentPoint.BottomRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y - (rect.Height), 0);
                    break;

                default:
                    adjustedPos = position;
                    break;
            }

            return adjustedPos;
        }

        public void GetSegments(Factory1 factory, SharpDX.Direct2D1.Factory2 d2dFactory)
        {
            if (DxfMtext is null) { return; }

            SegmentsList.Clear();
            SegmentsList = GetMtextSegments(factory, d2dFactory);
            TextSegmentsLoaded = true;
            UpdateBounds();
        }

        public List<DrawingMtextSegment3D> GetMtextSegments(Factory1 factory, SharpDX.Direct2D1.Factory2 d2dFactory)
        {
            List<DrawingMtextSegment3D> mtextSegmentList = new();
            string rawText = DxfMtext.Value;

            if (!rawText.Contains('\\'))
            {
                DrawingMtextSegment3D textSegment = new(this, DxfMtext.Value, Color, Position, Rotation, FontSize, FontFamilyName, (float)DxfMtext.Height,
                    false, false, false, false, _fontRenderingMinimumSize, (float)DxfMtext.RectangleWidth);
                textSegment.GetTextFormat(factory);
                textSegment.GetTextLayout(factory);
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
                    else if (Regex.IsMatch(value, alignLeftPattern)) { }
                    else if (Regex.IsMatch(value, alignCenterPattern)) { }
                    else if (Regex.IsMatch(value, alignRightPattern)) { }
                    else if (Regex.IsMatch(value, alignJustifyPattern)) { }
                    else if (Regex.IsMatch(value, alignDistributedPattern)) { }
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
                    var textSegment = textSegments[i];

                    //List<string> textList = new List<string> { textSegment.Text };

                    var splitTexts = textSegment.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    var spaceWidth = TextRenderingHelpers.GetSpaceWidth(factory, textSegment.Font, textHeightToPoints(textSegment.TextHeight, _textHeightToFontSizeFactor)) * _textHeightToFontSizeFactor;

                    var segments = CreateMtextSegments(new Vector3(basePosition.X, basePosition.Y, 0), (float)DxfMtext.RectangleWidth, factory, d2dFactory, this, textSegment, splitTexts,
                        (float)spaceWidth, ref xOffset, ref yOffset);

                    mtextSegmentList.AddRange(segments);
                }
            }

            return mtextSegmentList;
        }
        #endregion

        #region Static Methods
        private static List<DrawingMtextSegment3D> CreateMtextSegments(Vector3 basePosition, float maxWidth, Factory1 factory, SharpDX.Direct2D1.Factory2 d2dFactory,
            DrawingMtext3D drawingMtext, TextSegmentInformation segmentInfo, List<string> texts, float spaceWidth, ref float xOffset, ref float yOffset)
        {
            List<DrawingMtextSegment3D> segments = new();

            float lineSpacing = (float)(segmentInfo.TextHeight * _mtextLineSpacingFactor * drawingMtext.DxfMtext.LineSpacingFactor);
            int fontSize = (int)Math.Ceiling(segmentInfo.TextHeight * _textHeightToFontSizeFactor);

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
        private static protected Enums.TextAttachmentPoint GetAttachmentPoint(TextAlignment mTextAttachment)
        {
            return mTextAttachment switch
            {
                netDxf.Entities.TextAlignment.TopLeft => Enums.TextAttachmentPoint.TopLeft,
                netDxf.Entities.TextAlignment.TopCenter => Enums.TextAttachmentPoint.TopCenter,
                netDxf.Entities.TextAlignment.TopRight => Enums.TextAttachmentPoint.TopRight,
                netDxf.Entities.TextAlignment.MiddleLeft => Enums.TextAttachmentPoint.MiddleLeft,
                netDxf.Entities.TextAlignment.MiddleCenter => Enums.TextAttachmentPoint.MiddleCenter,
                netDxf.Entities.TextAlignment.MiddleRight => Enums.TextAttachmentPoint.MiddleRight,
                netDxf.Entities.TextAlignment.BottomLeft => Enums.TextAttachmentPoint.BottomLeft,
                netDxf.Entities.TextAlignment.BottomCenter => Enums.TextAttachmentPoint.BottomCenter,
                netDxf.Entities.TextAlignment.BottomRight => Enums.TextAttachmentPoint.BottomRight,
                _ => Enums.TextAttachmentPoint.MiddleCenter,
            };
        }
        private static TextAlignment ConvertMTextAlignment(MTextAttachmentPoint attach)
        {
            return attach switch
            {
                MTextAttachmentPoint.BottomLeft => TextAlignment.BottomLeft,
                MTextAttachmentPoint.BottomCenter => TextAlignment.BottomCenter,
                MTextAttachmentPoint.BottomRight => TextAlignment.BottomRight,
                MTextAttachmentPoint.MiddleLeft => TextAlignment.MiddleLeft,
                MTextAttachmentPoint.MiddleCenter => TextAlignment.MiddleCenter,
                MTextAttachmentPoint.MiddleRight => TextAlignment.MiddleRight,
                MTextAttachmentPoint.TopLeft => TextAlignment.TopLeft,
                MTextAttachmentPoint.TopCenter => TextAlignment.TopCenter,
                MTextAttachmentPoint.TopRight => TextAlignment.TopRight,
                _ => TextAlignment.BaselineLeft,
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
