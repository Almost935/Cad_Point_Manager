using Cad_Point_Manager.Extensions;

namespace Cad_Point_Manager.Models.DxfImport
{
    public static class ArrowheadToNetDxfBlockNameResolver
    {
        private static readonly Dictionary<ArrowheadType, string> _arrowheadMap = new()
        {
            [ArrowheadType.ClosedFilled] = "_ClosedFilled",
            [ArrowheadType.ClosedBlank] = "_ClosedBlank",
            [ArrowheadType.Closed] = "_Closed",
            [ArrowheadType.Dot] = "_Dot",
            [ArrowheadType.DotSmall] = "_DotSmall",
            [ArrowheadType.DotBlank] = "_DotBlank",
            [ArrowheadType.DotSmallBlank] = "_Small",
            [ArrowheadType.ArchitecturalTick] = "_ArchTick",
            [ArrowheadType.Oblique] = "_Oblique",
            [ArrowheadType.Open] = "_Open",
            [ArrowheadType.Open30] = "_Open30",
            [ArrowheadType.OriginIndicator] = "_Origin",
            [ArrowheadType.OriginIndicator2] = "_Origin2",
            [ArrowheadType.RightAngle] = "_Open90",
            [ArrowheadType.Box] = "_BoxBlank",
            [ArrowheadType.BoxFilled] = "_BoxFilled",
            [ArrowheadType.DatumTriangle] = "_DatumBlank",
            [ArrowheadType.DatumTriangleFilled] = "_DatumFilled",
            [ArrowheadType.Integral] = "_Integral",
            [ArrowheadType.None] = "_None",
        };

        private static readonly Dictionary<ArrowheadType, float> _arrowheadOffsetMap = new()
        {
            [ArrowheadType.ClosedFilled] = 1,
            [ArrowheadType.ClosedBlank] = 1,
            [ArrowheadType.Closed] = 1,
            [ArrowheadType.Dot] = 0.3f,
            [ArrowheadType.DotSmall] = 0.25f,
            [ArrowheadType.DotBlank] = 0.5f,
            [ArrowheadType.DotSmallBlank] = 0.3f,
            [ArrowheadType.ArchitecturalTick] = 0,
            [ArrowheadType.Oblique] = 0,
            [ArrowheadType.Open] = 0,
            [ArrowheadType.Open30] = 0,
            [ArrowheadType.OriginIndicator] = 0.5f,
            [ArrowheadType.OriginIndicator2] = 0.5f,
            [ArrowheadType.RightAngle] = 0,
            [ArrowheadType.Box] = 0.5f,
            [ArrowheadType.BoxFilled] = 0.5f,
            [ArrowheadType.DatumTriangle] = 1.0f,
            [ArrowheadType.DatumTriangleFilled] = 1.0f,
            [ArrowheadType.Integral] = 0,
            [ArrowheadType.None] = 0,
        };

        public static bool ResolveArrowhead(ArrowheadType arrowheadType, out string blockName)
        {
            if (_arrowheadMap.TryGetValue(arrowheadType, out var mapped))
            {
                blockName = mapped;
                return true;
            }

            blockName = null;
            return false;
        }

        public static bool ResolveBlockName(string blockName, out ArrowheadType arrowhead)
        {
            if (_arrowheadMap.TryGetKey(blockName, out ArrowheadType mapped))
            {
                arrowhead = mapped;
                return true;
            }

            arrowhead = default;
            return false;
        }

        public static float ResolveArrowheadOffset(ArrowheadType arrowheadType)
        {
            if (_arrowheadOffsetMap.TryGetValue(arrowheadType, out var mapped))
            {
                return mapped;
            }

            return 0;
        }
    }
}
