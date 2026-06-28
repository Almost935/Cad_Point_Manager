using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            [ArrowheadType.RightAngle] = "_RightAngle",
            [ArrowheadType.Box] = "_BoxBlank",
            [ArrowheadType.BoxFilled] = "_BoxFilled",
            [ArrowheadType.DatumTriangle] = "_DatumBlank",
            [ArrowheadType.DatumTriangleFilled] = "_DatumFilled",
            [ArrowheadType.Integral] = "_Integral",
            [ArrowheadType.None] = "_None",
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
    }
}
