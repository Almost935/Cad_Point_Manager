using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    /// Builds all label triangles for a CogoPoint using TextMeshBuilder.
    public sealed class CogoPointTextAssembler
    {
        private readonly TextMeshBuilder _builder;
        private readonly float _duPerEm; // cached from FontFace.Metrics.DesignUnitsPerEm

        public CogoPointTextAssembler(TextMeshBuilder builder, float designUnitsPerEm)
        {
            _builder = builder;
            _duPerEm = designUnitsPerEm;
        }

        /// <summary>
        /// Build the 3 stacked text items for a point (PointNumber, Elevation, Description).
        /// Uses point.PointGroup.PointScale and the point’s precomputed label positions.
        /// </summary>
        public List<TextVertex> Build(CogoPoint point)
        {
            var all = new List<TextVertex>();
            if (point is null || point.PointGroup is null) return all;

            // Desired world "em" height for this point:
            float desiredEmWorld = 4f * point.PointGroup.PointScale.ToFloat();   // your _textBaseHeight * scale
            float duToWorld = desiredEmWorld / _duPerEm;

            // Shared color + flags:
            var color = point.PointGroup.Color; // Vector4
            var flags = (
                isVisible: point.PointGroup.IsVisible ? 1f : 0f,
                isMouseOver: point.IsMouseOver ? 1f : 0f,
                isSelected: point.IsSelected ? 1f : 0f
            );

            // Y-up: +1 (flip to -1 if your world is Y-down)
            const float yUp = +1f;

            // 1) Point Number
            var pn = point.PointNumber.ToString();
            all.AddRange(_builder.Build(pn, point.PointNumberPosition, duToWorld, color, flags, yUp));

            // 2) Elevation
            var el = point.Elevation.ToString("F3");
            all.AddRange(_builder.Build(el, point.ElevationPosition, duToWorld, color, flags, yUp));

            // 3) Description (can be empty)
            var desc = point.Description ?? string.Empty;
            if (desc.Length > 0)
                all.AddRange(_builder.Build(desc, point.DescriptionPosition, duToWorld, color, flags, yUp));

            return all;
        }
    }
}
