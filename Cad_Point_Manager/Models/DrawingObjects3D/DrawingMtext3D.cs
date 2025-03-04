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

        public bool TextFormatsCreated { get; set; } = false;
        public bool TextLayoutsCreated { get; set; } = false;
        public bool TextVerticesCreated { get; set; } = false;
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

            var splitText = rawText.Split(new[] { '{', '}' }, StringSplitOptions.None);
            var texts = splitText.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

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
            List<(string text, Vector4 color, string font, double textHeight, bool bold, bool italic, bool underline, bool overstrike, 
                bool strikethrough)> formattedSegments = new();

            string pattern = $@"({aciColorPattern}|{trueTypeColorPattern}|{fontPattern}|{italicPattern}|{boldPattern}|{heightPattern}|{lineBreakPattern}|
{underlineStartPattern}|{underlineEndPattern}|{overstrikeStartPattern}|{overstrikeEndPattern}|{strikethroughStartPattern}|{strikethroughEndPattern}|
{alignLeftPattern}|{alignCenterPattern}|{alignRightPattern}|{alignJustifyPattern}|{alignDistributedPattern}|[^{{}}\\]+)";

            foreach (var text in texts)
            {
                MatchCollection matches = Regex.Matches(text, pattern);

                // Default text properties
                string currentText = "";
                Vector4 currentColor = Color;
                double currentTextHeight = DxfMtext.Height;
                string currentFont = FontFamilyName;
                bool currentIsBold = IsBold;
                bool currentIsItalic = IsItalic;
                bool currentIsUnderlined = false;
                bool currentIsOverstriked = false;
                bool currentIsStrikethrough = false;
                
                Debug.WriteLine($"\n");
                foreach (Match match in matches)
                {
                    string value = match.Value;
                    if (string.IsNullOrWhiteSpace(value)) { continue; }

                    TextSegmentInformation segment = new("", Color, FontFamilyName, DxfMtext.Height, IsBold, IsItalic, false, false, false);

                    if (Regex.IsMatch(value, aciColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, aciColorPattern).Groups[1].Value);
                        //Vector4 newColor = new(0, 0, 0, 1);

                        if (colorI == 0)
                        {
                            if (IsPartOfBlock)
                            {
                                currentColor = DrawingBlock3D.Color;
                            }
                            else
                            {
                                var aciColor = AciColor.Default;
                                currentColor = new(aciColor.R / 255.0f, aciColor.G / 255.0f, aciColor.B / 255.0f, 1.0f);
                            }
                        }
                        else if (colorI == 256)
                        {
                            currentColor = Layer.Color;
                        }
                        else
                        {
                            var vector = AutoCadColorConverter.ConvertACINumberToRGBA((short)colorI);
                            currentColor = new((float)vector.X, (float)vector.Y, (float)vector.Z, (float)vector.W);
                        }

                        //if (newColor != currentColor)
                        //{
                        //    if (!string.IsNullOrEmpty(currentText))
                        //    {
                        //        formattedSegments.Add((currentText, currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, currentIsUnderlined, currentIsOverstriked, currentIsStrikethrough));
                        //        currentText = "";
                        //    }
                        //    currentColor = newColor;
                        //}
                    }
                    else if (Regex.IsMatch(value, trueTypeColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, trueTypeColorPattern).Groups[1].Value);
                        var trueTypeColor = AutoCadColorConverter.ConvertTrueColorToVector4(colorI);
                        Vector4 newColor = new((float)trueTypeColor.X, (float)trueTypeColor.Y, (float)trueTypeColor.Z, (float)trueTypeColor.W);

                        //Vector4 newColor = AutoCadColorConverter.ConvertACINumberToRGBA(int.Parse(Regex.Match(value, colorPattern).Groups[1].Value));
                        if (newColor != currentColor)
                        {
                            if (!string.IsNullOrEmpty(currentText))
                            {
                                formattedSegments.Add((currentText, currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, currentIsUnderlined, currentIsOverstriked, currentIsStrikethrough));
                                currentText = "";
                            }
                            currentColor = newColor;
                        }
                    }
                    else if (Regex.IsMatch(value, fontPattern))
                    {
                        string newFont = Regex.Match(value, fontPattern).Groups[1].Value.Split('|')[0];
                        if (newFont != currentFont)
                        {
                            if (!string.IsNullOrEmpty(currentText))
                            {
                                formattedSegments.Add((currentText, currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, currentIsUnderlined, currentIsOverstriked, currentIsStrikethrough));
                                currentText = "";
                            }
                            currentFont = newFont;
                        }
                    }
                    else if (Regex.IsMatch(value, italicPattern))
                    {
                        currentIsItalic = Regex.Match(value, italicPattern).Groups[1].Value == "1";
                    }
                    else if (Regex.IsMatch(value, boldPattern))
                    {
                        currentIsBold = Regex.Match(value, boldPattern).Groups[1].Value == "1";
                    }
                    else if (Regex.IsMatch(value, heightPattern))
                    {
                        double newTextHeight = double.Parse(Regex.Match(value, heightPattern).Groups[1].Value);
                        if (newTextHeight != currentTextHeight)
                        {
                            if (!string.IsNullOrEmpty(currentText))
                            {
                                formattedSegments.Add((currentText, currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, currentIsUnderlined, currentIsOverstriked, currentIsStrikethrough));
                                currentText = "";
                            }
                            currentTextHeight = newTextHeight;
                        }
                    }
                    else if (Regex.IsMatch(value, lineBreakPattern))
                    {
                        if (!string.IsNullOrEmpty(currentText))
                        {
                            formattedSegments.Add((currentText, currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, currentIsUnderlined, currentIsOverstriked, currentIsStrikethrough));
                            currentText = "";
                        }
                        formattedSegments.Add(("\n", currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, currentIsUnderlined, currentIsOverstriked, currentIsStrikethrough));
                    }
                    else if (Regex.IsMatch(value, underlineStartPattern))
                    {
                        currentIsUnderlined = true;
                    }
                    else if (Regex.IsMatch(value, underlineEndPattern))
                    {
                        currentIsUnderlined = false;
                    }
                    else if (Regex.IsMatch(value, overstrikeStartPattern))
                    {
                        currentIsOverstriked = true;
                    }
                    else if (Regex.IsMatch(value, overstrikeEndPattern))
                    {
                        currentIsOverstriked = false;
                    }
                    else if (Regex.IsMatch(value, strikethroughStartPattern))
                    {
                        currentIsStrikethrough = true;
                    }
                    else if (Regex.IsMatch(value, strikethroughEndPattern))
                    {
                        //isStrikethrough = false;
                    }
                    else if (Regex.IsMatch(value, alignLeftPattern)) { }
                    else if (Regex.IsMatch(value, alignCenterPattern)) { }
                    else if (Regex.IsMatch(value, alignRightPattern)) { }
                    else if (Regex.IsMatch(value, alignJustifyPattern)) { }
                    else if (Regex.IsMatch(value, alignDistributedPattern)) { }
                    else
                    {
                        currentText += value;
                    }
                }

                //if (!string.IsNullOrEmpty(currentText))
                //{
                //    var segment = (currentText, currentColor, currentFont, currentTextHeight, currentIsBold, currentIsItalic, isUnderlined, isOverstriked, isStrikethrough);
                //    formattedSegments.Add(segment);
                //}

                // Convert each segment to a TEXT entity
                Vector3 basePosition = Position;
                float yOffset = 0;
                float xOffset = 0;
                double currentLineWidth = 0;

                //Debug.WriteLine($"\n");
                foreach (var segment in formattedSegments)
                {
                    if (segment.text == "\n")
                    {
                        yOffset -= (float)(segment.textHeight * _mtextLineSpacingFactor * DxfMtext.LineSpacingFactor);
                        xOffset = 0;
                        currentLineWidth = 0;
                    }
                    List<string> textList = segment.text.Split(' ').ToList();
                    //if (textList.Count > 0)
                    //{
                    //    var mtextSegmentList = CreateMtextSegments()
                    //}
                    
                    //Debug.WriteLine($"text: \"{segment.text}\" font: {segment.font} color: {segment.color} text: {segment.textHeight}");


                    //if (segment.text == "\n")
                    //{
                    //    yOffset -= (float)(segment.textHeight * _mtextLineSpacingFactor * DxfMtext.LineSpacingFactor);
                    //    xOffset = 0;
                    //    currentLineWidth = 0;
                    //}
                    //if (!string.IsNullOrWhiteSpace(segment.text))
                    //{
                    //    netDxf.Vector3 pos = new(basePosition.X + xOffset, basePosition.Y + yOffset, 0);
                    //    TextStyle newTextStyle = new($"{segment.font}", $"{segment.font}.ttf");

                    //    DrawingMtextSegment3D mtextSegment = new(this, segment.text, segment.color, new Vector3(basePosition.X + xOffset, basePosition.Y + yOffset, 0), 
                    //        Rotation, (int)Math.Ceiling(segment.textHeight * _textHeightToFontSizeFactor), segment.font, (float)segment.textHeight, segment.italic, segment.bold, 
                    //        segment.underline, segment.strikethrough, _fontRenderingMinimumSize, (float)DxfMtext.RectangleWidth);
                    //    mtextSegment.GetTextFormat(factory);
                    //    mtextSegment.GetTextLayout(factory);
                    //    mtextSegment.Tesselate(d2dFactory);

                    //    mtextSegmentList.Add(mtextSegment);
                    //    xOffset += (float)mtextSegment.Bounds.Width;

                    //    if (currentLineWidth + mtextSegment.Bounds.Width > DxfMtext.RectangleWidth)
                    //    {
                    //        yOffset -= (float)(segment.textHeight * _mtextLineSpacingFactor * DxfMtext.LineSpacingFactor);
                    //        xOffset = 0;
                    //        currentLineWidth = 0;
                    //    }
                    //    currentLineWidth += mtextSegment.Bounds.Width;
                    //}
                }
            }
            return mtextSegmentList;
        }
        #endregion

        #region Static Methods
        private static List<DrawingMtextSegment3D> CreateMtextSegments(netDxf.Vector3 basePosition, float maxWidth, DrawingMtext3D drawingMtext, 
            List<string> texts, Vector4 color, string font, double textHeight, bool isBold, bool isItalic, bool isOverstrike, bool isStrikethrough,
            Factory1 factory, SharpDX.Direct2D1.Factory2 d2dFactory)
        {
            List<DrawingMtextSegment3D> segments = [];

            float yOffset = 0;
            float xOffset = 0;

            foreach (var text in texts)
            {
                DrawingMtextSegment3D segment = new(drawingMtext, text, color, new Vector3((float)basePosition.X, (float)basePosition.Y, 0), 0, 
                    (int)Math.Ceiling(textHeight * 1.25), font, (float)textHeight, isItalic, isBold, false, false, _fontRenderingMinimumSize, 0);
                segment.GetTextFormat(factory);
                segment.GetTextLayout(factory);
                segment.Tesselate(d2dFactory);
                segments.Add(segment);

                xOffset += (float)segment.Bounds.Width;

                if (xOffset + segment.Bounds.Width > maxWidth)
                {
                    yOffset -= (float)(textHeight * _mtextLineSpacingFactor * drawingMtext.DxfMtext.LineSpacingFactor);
                    xOffset = 0;
                }
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
        private static protected Enums.TextAttachmentPoint GetAttachmentPoint(netDxf.Entities.TextAlignment mTextAttachment)
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
        public bool IsOverstrike { get; set; }
        public bool IsStrikethrough { get; set; }
        public bool IsUnderlined { get; set; }

        public bool HasValue => !string.IsNullOrEmpty(Text);
        #endregion

        #region Constructors
        public TextSegmentInformation(string text = "", Vector4? color = null, string font = "Arial", double textHeight = 5, bool isBold = false, 
            bool isItalic = false, bool isUnderlined = false, bool isOverstrike = false, bool isStrikethrough = false)
        {
            Text = text;
            Color = color ?? new Vector4(0, 0, 0, 1);
            Font = font;
            TextHeight = textHeight;
            IsBold = isBold;
            IsItalic = isItalic;
            IsUnderlined = isUnderlined;
            IsOverstrike = isOverstrike;
            IsStrikethrough = isStrikethrough;
        }
        #endregion
    }
}
