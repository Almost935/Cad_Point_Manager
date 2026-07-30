using Cad_Point_Manager.Common;
using Cad_Point_Manager.Models.DrawingObjects;
using SharpDX;
using System.Diagnostics;

namespace Cad_Point_Manager.Models.DxfImport
{
    public enum ArrowheadType
    {
        ClosedFilled,
        ClosedBlank,
        Closed,
        Dot,
        DotSmall,
        DotBlank,
        DotSmallBlank,
        ArchitecturalTick,
        Oblique,
        Open,
        Open30,
        OriginIndicator,
        OriginIndicator2,
        RightAngle,
        Box,
        BoxFilled,
        DatumTriangle,
        DatumTriangleFilled,
        Integral,
        None
    }

    public enum TextAlignmentType
    {
        Left = 1,
        Center = 2,
        Right = 3
    }

    public enum TextAttachmentType
    {
        ByStyle = -1,
        TopOfTopLine = 0,
        MiddleOfTopLine = 1,
        MiddleOfText = 2,
        MiddleOfBottomLine = 3,
        BottomOfBottomLine = 4,
        UnderlineBottomLine = 5,
        UnderlineTopLine = 6,
        BottomOfTopLine = 7,
        UnderlineAllText = 8
    }

    public enum TextAttachmentSide
    {
        Left,
        Right,
        Center
    }


    public class ParsedMLeader : TagContainer
    {
        public ParsedMLeaderContext Context { get; } = new();
        public ParsedMLeaderStyle? Style { get; set; }

        public string EffectiveArrowheadId =>
            !string.IsNullOrWhiteSpace(ArrowheadId) ? ArrowheadId : Style?.ArrowheadHandle ?? "";

        public float EffectiveArrowheadSize =>
            ArrowheadSize ?? Style?.ArrowheadSize ?? Context.ArrowheadSize;

        public string LayerName => GetString(8) ?? string.Empty;
        public short? ColorIndex => GetShort(62);
        public int? Color24Bit => GetInt(420);
        public int? TextColor => GetInt(92);
        public string LeaderStyleId => GetString(340) ?? string.Empty;
        public string TextStyleId => GetString(343) ?? string.Empty;
        public string ArrowheadId => GetString(342) ?? string.Empty;
        public float? ArrowheadSize => GetFloat(42);
        public int? ArrowheadIndex => GetInt(94);

        /// <summary>
        /// Gets the text attachment type for the left side of the text.
        /// -1 = By Style, 0 = Top of Top Line, 1 = Middle of Top Line, 2 = Middle of Text, 3 = Middle of Bottom Line, 4 = Bottom of Bottom Line, 5 = Underline Bottom Line, 6 = Underline Top Line, 7 = Bottom of Top Line, 8 = Underline All Text
        /// </summary>
        public TextAttachmentType TextLeftAttachmentType => (TextAttachmentType)(GetInt(173) ?? 0);

        /// <summary>
        /// Gets the text attachment type for the left side of the text.
        /// -1 = By Style, 0 = Top of Top Line, 1 = Middle of Top Line, 2 = Middle of Text, 3 = Middle of Bottom Line, 4 = Bottom of Bottom Line, 5 = Underline Bottom Line, 6 = Underline Top Line, 7 = Bottom of Top Line, 8 = Underline All Text
        /// </summary>
        public TextAttachmentType TextRightAttachmentType => (TextAttachmentType)(GetInt(95) ?? 0);

        /// <summary>
        /// Gets the text alignment type.
        /// 1 = Left, 2 = Center, 3 = Right
        /// </summary>
        public TextAlignmentType TextAlignmentType => (TextAlignmentType)(GetInt(179) ?? 0);

        public (Vector4 color, ColorType colorType) Color
        {
            get
            {
                if (Color24Bit.HasValue)
                {
                    return (ConvertTrueColorToVector4(
                            Color24Bit.Value), ColorType.ByObject);
                }

                if (ColorIndex.HasValue)
                {
                    if (ColorIndex.Value == 256)
                    {
                        return (new Vector4(0, 0, 0, 1), ColorType.ByLayer);
                    }
                    if (ColorIndex.Value == 0)
                    {
                        return (new Vector4(0, 0, 0, 1), ColorType.ByBlock);
                    }
                    return (ConvertACINumberToRGBA(
                            ColorIndex.Value), ColorType.ByObject);
                }

                return (new Vector4(0, 0, 0, 1), ColorType.ByLayer);
            }
        }

        public TextAttachmentSide TextAttachmentSide
        {
            get
            {
                var leader = Context.Leaders.FirstOrDefault();
                if (leader == null)
                    return TextAttachmentSide.Right;

                return leader.LastLeaderLinePoint.X < Context.TextLocation.X
                    ? TextAttachmentSide.Left
                    : TextAttachmentSide.Right;
            }
        }

        public TextAttachmentPoint TextAttachment
        {
            get
            {
                var leader = Context.Leaders.FirstOrDefault();
                if (leader == null)
                {
                    return TextAttachmentPoint.MiddleLeft;
                }

                var textAttachmentSide = TextAttachmentSide;
                var textAlignmentType = TextAlignmentType;

                if (leader.LastLeaderLinePoint.X < Context.TextLocation.X)
                {
                    switch (TextLeftAttachmentType)
                    {
                        case TextAttachmentType.TopOfTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.MiddleOfTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.BottomOfTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.UnderlineTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.MiddleOfText:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.MiddleLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.MiddleCenter;
                            else
                                return TextAttachmentPoint.MiddleRight;

                        case TextAttachmentType.MiddleOfBottomLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.BottomLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.BottomCenter;
                            else
                                return TextAttachmentPoint.BottomRight;

                        case TextAttachmentType.BottomOfBottomLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.BottomLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.BottomCenter;
                            else
                                return TextAttachmentPoint.BottomRight;

                        case TextAttachmentType.UnderlineBottomLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.BottomLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.BottomCenter;
                            else
                                return TextAttachmentPoint.BottomRight;

                        case TextAttachmentType.UnderlineAllText:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.MiddleLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.MiddleCenter;
                            else
                                return TextAttachmentPoint.MiddleRight;

                        default:
                            return TextAttachmentPoint.MiddleLeft;
                    }
                }
                else
                {
                    switch (TextRightAttachmentType)
                    {
                        case TextAttachmentType.TopOfTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.MiddleOfTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.BottomOfTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.UnderlineTopLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.TopLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.TopCenter;
                            else
                                return TextAttachmentPoint.TopRight;

                        case TextAttachmentType.MiddleOfText:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.MiddleLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.MiddleCenter;
                            else
                                return TextAttachmentPoint.MiddleRight;

                        case TextAttachmentType.MiddleOfBottomLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.BottomLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.BottomCenter;
                            else
                                return TextAttachmentPoint.BottomRight;

                        case TextAttachmentType.BottomOfBottomLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.BottomLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.BottomCenter;
                            else
                                return TextAttachmentPoint.BottomRight;

                        case TextAttachmentType.UnderlineBottomLine:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.BottomLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.BottomCenter;
                            else
                                return TextAttachmentPoint.BottomRight;

                        case TextAttachmentType.UnderlineAllText:
                            if (textAlignmentType == TextAlignmentType.Left)
                                return TextAttachmentPoint.MiddleLeft;
                            else if (textAlignmentType == TextAlignmentType.Center)
                                return TextAttachmentPoint.MiddleCenter;
                            else
                                return TextAttachmentPoint.MiddleRight;

                        default:
                            return TextAttachmentPoint.MiddleLeft;
                    }
                }
            }
        }
    }
    public class ParsedLeaderLine : TagContainer
    {
        public Vector3 Vertex
        {
            get
            {
                float x = GetFloat(10) ?? 0;
                float y = GetFloat(20) ?? 0;
                float z = GetFloat(30) ?? 0;

                return new Vector3(x, y, z);
            }
        }
        public Vector3 BreakStartPoint
        {
            get
            {
                float x = GetFloat(11) ?? 0;
                float y = GetFloat(21) ?? 0;
                float z = GetFloat(31) ?? 0;

                return new Vector3(x, y, z);
            }
        }
        public Vector3 BreakEndPoint
        {
            get
            {
                float x = GetFloat(12) ?? 0;
                float y = GetFloat(22) ?? 0;
                float z = GetFloat(32) ?? 0;

                return new Vector3(x, y, z);
            }
        }
    }
    public class ParsedLeaderNode : TagContainer
    {
        public List<ParsedLeaderLine> LeaderLines { get; } = [];

        public bool HasSetLastLeaderPoint => GetBool(290);
        public bool HasSetDogLegVector => GetBool(291);
        public float DogLegLength => GetFloat(40) ?? 0f;
        public Vector3 DogLegVector
        {
            get
            {
                float x = GetFloat(11) ?? 0;
                float y = GetFloat(21) ?? 0;
                float z = GetFloat(31) ?? 0;

                return new Vector3(x, y, z);
            }
        }
        public Vector3 LastLeaderLinePoint
        {
            get
            {
                float x = GetFloat(10) ?? 0;
                float y = GetFloat(20) ?? 0;
                float z = GetFloat(30) ?? 0;

                return new Vector3(x, y, z);
            }
        }
        public Vector3 BreakStartPoint
        {
            get
            {
                float x = GetFloat(12) ?? 0;
                float y = GetFloat(22) ?? 0;
                float z = GetFloat(32) ?? 0;

                return new Vector3(x, y, z);
            }
        }
        public Vector3 BreakEndPoint
        {
            get
            {
                float x = GetFloat(13) ?? 0;
                float y = GetFloat(23) ?? 0;
                float z = GetFloat(33) ?? 0;

                return new Vector3(x, y, z);
            }
        }
    }
    public class ParsedMLeaderContext : TagContainer
    {
        public List<ParsedLeaderNode> Leaders { get; } = [];
        public float TextHeight => GetFloat(41) ?? 0f;
        public float ArrowheadSize => GetFloat(140) ?? 0f;
        public float LandingGap => GetFloat(145) ?? 0f;
        public string Text => GetString(304) ?? string.Empty;
        public int? BlockColor => GetInt(93);
        public int? TextColor => GetInt(90);
        public int? TextBackgroundColor => GetInt(91);
        public bool HasMtext => GetBool(290);
        public float TextWidth => GetFloat(43) ?? 0f;
        public int TextAttachment => GetInt(171) ?? 1;
        public Vector3 TextLocation
        {
            get
            {
                float x = GetFloat(12) ?? 0;
                float y = GetFloat(22) ?? 0;
                float z = GetFloat(32) ?? 0;

                return new Vector3(x, y, z);
            }
        }
    }
    public class MLeaderTag
    {
        public int Code { get; init; }
        public string Value { get; init; } = "";

        public override string ToString()
        {
            return $"{Code}: {Value}";
        }
    }

    public class ParsedMLeaderStyle : TagContainer
    {
        public string DictionaryName { get; set; } = "";
        public string Name => DictionaryName;
        public string ArrowheadHandle => GetString(341) ?? "";
        public float ArrowheadSize => GetFloat(44) ?? 0f;
        public string TextStyleHandle => GetString(342) ?? "";
        public TextAttachmentType TextLeftAttachmentType => (TextAttachmentType)(GetInt(174) ?? 0);
        public TextAttachmentType TextRightAttachmentType => (TextAttachmentType)(GetInt(178) ?? 0);

        public ArrowheadType ArrowheadType { get; set; }
    }

    public class ParsedBlockRecord : TagContainer
    {
        public string Name => GetString(2) ?? "";
    }

    public class ParsedDictionary : TagContainer
    {
        public Dictionary<string, string> Entries { get; } = [];
    }
}
