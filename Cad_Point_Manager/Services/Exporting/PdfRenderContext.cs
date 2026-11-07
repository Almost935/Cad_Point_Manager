using Cad_Point_Manager.Common;
using PdfSharpCore.Drawing;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.Exporting
{
    public sealed class PdfRenderContext
    {
        #region Properties
        public XGraphics Graphics { get; init; }

        public Matrix WorldToPdf { get; init; }

        public double GlobalLineTypeScale { get; init; } = 1.0;

        public double AnnotationScale { get; init; } = 1.0;

        public Vector2d RenderOrigin { get; init; } = Vector2d.Zero;
        #endregion

        #region Constructors
        public PdfRenderContext(
            XGraphics graphics,
            Matrix worldToPdf,
            double globalLineTypeScale,
            double annotationScale,
            Vector2d renderOrigin)
        {
            Graphics = graphics;
            WorldToPdf = worldToPdf;
            GlobalLineTypeScale = globalLineTypeScale;
            AnnotationScale = annotationScale;
            RenderOrigin = renderOrigin;
        }
        #endregion
    }
}
