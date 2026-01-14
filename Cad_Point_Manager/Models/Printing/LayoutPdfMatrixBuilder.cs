using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.Printing
{
    public static class LayoutPdfMatrixBuilder
    {
        // layout.Viewport.LocalRectIn is in inches, top-left origin on the page in your layout system
        public static Matrix BuildWorldToPdf(
            Layout layout,
            Matrix worldToView,
            Size viewSizeInViewUnits, // the size that worldToView maps into (see note below)
            out double pageHeightPts)
        {
            double pageWpts = layout.PageSize.Width * 72.0;
            pageHeightPts = layout.PageSize.Height * 72.0;

            // Viewport rectangle on page in POINTS
            var vpIn = layout.Viewport.LocalRectIn;     // inches
            double vpX = vpIn.X * 72.0;
            double vpY = vpIn.Y * 72.0;
            double vpW = vpIn.Width * 72.0;
            double vpH = vpIn.Height * 72.0;

            // Scale view-units into viewport points
            // If worldToView maps into pixel coords, then viewSizeInViewUnits = (pixelW, pixelH).
            // If it maps into DIPs, then viewSizeInViewUnits = (dipW, dipH).
            double sx = vpW / viewSizeInViewUnits.Width;
            double sy = vpH / viewSizeInViewUnits.Height;

            // We want uniform scale typically (CAD), but if your view matrix is uniform
            // you can use sx (or min(sx, sy)). Using non-uniform keeps full fill.
            double s = Math.Min(sx, sy);

            // Build view->pdf:
            // 1) scale to points
            // 2) flip Y (because view Y is down, PDF Y is up)
            // 3) translate into viewport rect and also account for flip around bottom
            var viewToPdf = Matrix.Identity;

            // scale
            viewToPdf.Scale(s, s);

            // flip Y around 0 (we'll handle offsets with translate)
            viewToPdf.Scale(1, -1);

            // translate:
            // - X: viewport left
            // - Y: pdf origin bottom => pageHeightPts, then move down to viewport top, then add viewport height
            // After flip, Y grows up, so to place top-left correctly:
            // y = pageH - vpY - vpH
            double pdfVpTopLeftY = pageHeightPts - vpY - vpH;
            viewToPdf.Translate(vpX, pdfVpTopLeftY + vpH); // +vpH because of the Y flip

            // Combine
            // world -> view -> pdf
            var worldToPdf = worldToView;
            worldToPdf.Append(viewToPdf);

            return worldToPdf;
        }

        /// <summary>
        /// Builds a world->PDF matrix that CONTAINS worldBounds inside pdfViewportPts.
        /// Aspect ratio preserved, centered, Y flipped (world Y-up -> PDF Y-down).
        /// </summary>
        public static Matrix BuildWorldToPdf(
            Rect worldBounds,
            Rect pdfViewportPts)
        {
            if (worldBounds.Width <= 0 || worldBounds.Height <= 0)
                throw new ArgumentException("worldBounds must have positive size.", nameof(worldBounds));

            if (pdfViewportPts.Width <= 0 || pdfViewportPts.Height <= 0)
                throw new ArgumentException("pdfViewportPts must have positive size.", nameof(pdfViewportPts));

            // Uniform scale for best-fit (contain)
            double sx = pdfViewportPts.Width / worldBounds.Width;
            double sy = pdfViewportPts.Height / worldBounds.Height;
            double s = Math.Min(sx, sy);

            // Size after fitting
            double fittedW = worldBounds.Width * s;
            double fittedH = worldBounds.Height * s;

            // Centering padding inside viewport
            double padX = (pdfViewportPts.Width - fittedW) * 0.5;
            double padY = (pdfViewportPts.Height - fittedH) * 0.5;

            // Build matrix step-by-step
            var m = Matrix.Identity;

            // 1) Move world bounds origin to (0,0)
            //    Use Bottom because we'll flip Y next
            m.Translate(-worldBounds.Left, -worldBounds.Bottom);

            // 2) Scale and flip Y
            m.Scale(s, -s);

            // 3) Move into PDF viewport and center
            m.Translate(
                pdfViewportPts.Left + padX,
                pdfViewportPts.Top + padY + fittedH);

            return m;
        }

        public static Matrix BuildWorldToPdfContain_YDown(
            Rect worldBounds,
            Rect pdfViewportPts)
        {
            if (worldBounds.Width <= 0 || worldBounds.Height <= 0)
                throw new ArgumentException("worldBounds must have positive size.", nameof(worldBounds));

            if (pdfViewportPts.Width <= 0 || pdfViewportPts.Height <= 0)
                throw new ArgumentException("pdfViewportPts must have positive size.", nameof(pdfViewportPts));

            // contain
            double sx = pdfViewportPts.Width / worldBounds.Width;
            double sy = pdfViewportPts.Height / worldBounds.Height;
            double s = Math.Min(sx, sy);

            double fittedW = worldBounds.Width * s;
            double fittedH = worldBounds.Height * s;

            double padX = (pdfViewportPts.Width - fittedW) * 0.5;
            double padY = (pdfViewportPts.Height - fittedH) * 0.5;

            var m = Matrix.Identity;

            // 1) move world top-left to (0,0)
            m.Translate(-worldBounds.Left, -worldBounds.Bottom);

            // 2) scale (no flip)
            m.Scale(s, -s);

            // 3) move into pdf viewport
            m.Translate(pdfViewportPts.Left + padX, pdfViewportPts.Top + padY);

            return m;
        }
    }

}
