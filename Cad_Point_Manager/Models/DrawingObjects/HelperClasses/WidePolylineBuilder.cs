using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects.HelperClasses
{
    internal sealed class WidePolylineBuilder
    {
        public List<DrawingSolid> Build(
            Polyline2D polyline,
            ObjectLayer layer,
            Vector4 objectColor,
            ColorType colorType,
            bool isPartOfBlock = false,
            DrawingBlock drawingBlock = null)
        {
            // 1. Convert the DXF polyline into construction geometry
            List<OffsetSegment> segments = CreateSegments(polyline);

            // Nothing to draw
            if (segments.Count == 0)
                return [];

            // 2. Offset every segment to produce preliminary left/right boundaries
            List<OffsetSegment> offsetSegments = OffsetSegments(segments);

            // 3. Resolve every joint between consecutive segments
            ResolveSegmentJoins(offsetSegments, polyline.IsClosed);

            // 4. Convert the finished boundaries into DrawingSolids
            return GenerateDrawingSolids(
                offsetSegments,
                layer,
                objectColor,
                colorType,
                isPartOfBlock,
                drawingBlock);
        }
    }
}
