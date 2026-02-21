using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.LayoutExporting
{
    public class LayoutPdfVectorExporter : ILayoutPdfVectorExporter
    {
        static double Pt(double inches) => inches * 72.0;
        static readonly Dictionary<short, XGraphicsPath> _duGlyphPathCache = new();

        const uint LABEL_VISIBLE = 1u << 0;

        const uint POINT_VISIBLE = 1u << 0;
        const uint POINT_SELECTED = 1u << 1;
        const uint POINT_MOUSEOVR = 1u << 2;
        const uint POINT_ISFLIPPEDY = 1u << 6;

        const uint GROUP_VISIBLE = 1u << 0;

        public Task ExportAsync(
            Layout layout,
            CadManager cadManager3D,
            Scene scene,
            D3dStateController stateController,
            SceneIdMap ids,
            ResCache resCache,
            List<TbPrimitive> templatePrims,
            string outputPath,
            CancellationToken ct = default)
        {
            return Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var viewSize = new Size(cadManager3D.ViewportSize.Width, cadManager3D.ViewportSize.Height);
                double pageWpts = layout.PageSize.Width * 72.0;
                double pageHpts = layout.PageSize.Height * 72.0;

                Rect pdfViewportPts = new(
                    layout.Viewport.LocalRectIn.X * 72.0,
                    layout.Viewport.LocalRectIn.Y * 72.0,
                    layout.Viewport.LocalRectIn.Width * 72.0,
                    layout.Viewport.LocalRectIn.Height * 72.0);
                Matrix worldToPdf = BuildWorldToPdfFromCameraNew(layout, cadManager3D, scene);

                LayoutPdfVectorExporter.Export(
                    layout,
                    cadManager3D,
                    stateController,
                    ids,
                    resCache,
                    templatePrims,
                    worldToPdf,
                    outputPath);

            }).Task;
        }

        public static void Export(
            Layout layout,
            CadManager cadManager,
            D3dStateController stateController,
            SceneIdMap ids,
            ResCache resCache,
            List<TbPrimitive> templatePrims,
            Matrix worldToPdf,
            string outputPdfPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);

            double pageWpts = layout.PageSize.Width * 72.0;
            double pageHpts = layout.PageSize.Height * 72.0;

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = pageWpts;
            page.Height = pageHpts;

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XBrushes.White, 0, 0, pageWpts, pageHpts);

            // --- CLIP TO VIEWPORT FOR CAD ONLY ---
            var viewportClip = new XRect(
                layout.Viewport.LocalRectIn.X * 72.0,
                layout.Viewport.LocalRectIn.Y * 72.0,
                layout.Viewport.LocalRectIn.Width * 72.0,
                layout.Viewport.LocalRectIn.Height * 72.0);

            // Capture graphics state *object* so we can restore the exact state.
            XGraphicsState cadClipState = gfx.Save();
            gfx.IntersectClip(viewportClip);

            foreach (var kv in cadManager.Layers)
            {
                var layer = kv.Value;
                if (!layer.IsVisible) { continue; }

                foreach (var obj in layer.DrawingObjects)
                {
                    var pen = new XPen(PdfTransform.ToXColor(obj.Color.ToVector4()), 0.25);
                    obj.DrawToPdf(gfx, worldToPdf, pen);
                }
            }

            var labelStates = stateController.GetLabelStatesSnapshot();
            var pointStates = stateController.GetPointStatesSnapshot();
            var groupStates = stateController.GetGroupStatesSnapshot();

            DrawCogoPointStrings(
                gfx, cadManager, ids, resCache, worldToPdf,
                labelStates, pointStates, groupStates);

            gfx.Restore(cadClipState);

            DrawTitleblockPdf(gfx, templatePrims);

            doc.Save(outputPdfPath);

            _duGlyphPathCache.Clear();
        }

        private static void DrawCogoPointStrings(
            XGraphics gfx,
            CadManager cadManager,
            SceneIdMap ids,
            ResCache resCache,
            Matrix worldToPdf,
            LabelState[] labelStates,
            PointState[] pointStates,
            GroupState[] groupStates)
        {
            // Pick a font family. If you have a setting on PointGroup, use that instead.
            // Otherwise pick something installed (Arial is usually safe).
            const string fallbackFontFamily = "Arial";

            foreach (var pg in cadManager.CogoPointManager.PointGroups)
            {
                if (pg is null || !pg.IsVisible) { continue; }

                foreach (var p in pg.Points)
                {
                    if (p is null) { continue; }

                    if (!ids.TryGetGroupId(pg, out var gId)) { continue; }
                    if (!ids.TryGetPointId(p, out var pId)) { continue; }

                    if (pId >= (uint)pointStates.Length) { continue; }
                    var ps = pointStates[(int)pId];

                    if (ps.GroupId >= (uint)groupStates.Length) { continue; }
                    var gs = groupStates[(int)ps.GroupId];

                    var color = PickColor(ps, gs);
                    var brush = new XSolidBrush(color);
                    var pen = new XPen(color, 0.25);


                    // Draw PN / Elev / Desc (same order you do now)
                    DrawLine(p.PointNumber.ToString(), p, line: 0);
                    DrawLine(p.Elevation.ToString("F3"), p, line: 1);
                    if (p.HasDescription) DrawLine(p.Description, p, line: 2);

                    //// Draw point marker
                    //float radiusWorld = GlobalHelperProperties.CogoPointCirclePixelRadius * gs.Scale;
                    ////SharpDX.Vector2 worldTL = ps.Offset - radiusWorld;
                    //SharpDX.Vector2 worldTL = new(ps.Offset.X - radiusWorld, ps.Offset.Y + radiusWorld);
                    //(double xTransformed, double yTransformed) = TransformPoint(worldToPdf, worldTL);
                    //double radiusX = Math.Abs(radiusWorld * worldToPdf.M11);
                    //double radiusY = Math.Abs(radiusWorld * worldToPdf.M22);
                    //gfx.DrawEllipse(brush, new XRect(xTransformed, yTransformed, radiusX, radiusY));

                    // Match shader: center = position + ps.Offset
                    var centerW = ps.Offset;
                    var centerP = TransformPoint(worldToPdf, centerW);

                    // Match shader: radiusWorld = input.radius * gs.Scale
                    float radiusWorld = GlobalHelperProperties.CogoPointCirclePixelRadius * gs.Scale;

                    // Convert world radius -> PDF points using true axis scales
                    double rx = radiusWorld * PointsPerWorldUnitX(worldToPdf);
                    double ry = radiusWorld * PointsPerWorldUnitY(worldToPdf);

                    // Filled disc (like your pixel shader)
                    gfx.DrawEllipse(
                        brush,
                        centerP.X - rx,
                        centerP.Y - ry,
                        rx * 2.0,
                        ry * 2.0
                    );

                    if (p.HasLeaderLine)
                    {
                        var start = TransformPoint(worldToPdf, p.Position.ToSharpDXVector2());
                        var end = TransformPoint(worldToPdf, p.Position.ToSharpDXVector2() + ps.PointInfoOffset);
                        gfx.DrawLine(pen, new XPoint(start.X, start.Y), new XPoint(end.X, end.Y));    
                    }

                    void DrawLine(string s, CogoPoint cp, int line)
                    {
                        if (string.IsNullOrEmpty(s)) { return; }
                        if (!ids.TryGetLabelId(cp, line, out var labelId)) { return; }
                        if (labelId >= (uint)labelStates.Length) { return; }

                        var ls = labelStates[(int)labelId];

                        if (!IsVisible(ls, ps, gs)) { return; }

                        // 1) Compute label world origin (matches BuildDuToPdf origin math)
                        var world = ComputeCogoLabelWorldOrigin(ls, ps, gs);

                        // 2) Convert world -> PDF points
                        var pdf = TransformPoint(worldToPdf, world);

                        // 3) Compute font size in points from your world font height
                        // pg.FontBaseSize is your "world" font height baseline (you already use it in tessellation).
                        double worldEm = pg.FontBaseSize * gs.Scale;
                        double ptsPerWorld = PointsPerWorldUnit(worldToPdf);
                        double fontSizePts = Math.Max(0.1, worldEm * ptsPerWorld);

                        // 4) Create font (embed Unicode so PDFs behave better)
                        // If you have a custom font resolver, keep using it; otherwise this uses installed fonts.
                        var font = new XFont(
                            fallbackFontFamily,
                            fontSizePts,
                            XFontStyle.Regular,
                            new XPdfFontOptions(PdfFontEncoding.Unicode));

                        // 5) Draw.
                        // PdfSharp draws text relative to the top-left of the layout rect in most cases.
                        // If your offsets were tuned to a baseline, you may want to subtract font.GetHeight() here.
                        gfx.DrawString(s, font, brush, new XPoint(pdf.X, pdf.Y), XStringFormats.BaseLineLeft);
                    }
                }
            }
        }


        private static void DrawCogoPointGlyphs(
            XGraphics gfx,
            CadManager cadManager,
            SceneIdMap ids,
            ResCache resCache,
            Matrix worldToPdf,
            LabelState[] labelStates,
            PointState[] pointStates,
            GroupState[] groupStates,
            GlyphMeshCache glyphCache)
        {
            var face = resCache.CogoPointFontFace;
            var duPerEm = face.Metrics.DesignUnitsPerEm;

            foreach (var pg in cadManager.CogoPointManager.PointGroups)
            {
                if (pg is null || !pg.IsVisible) { continue; }

                // This matches your D3D logic for DU->world scales :contentReference[oaicite:9]{index=9}
                float duToWorldBase = (float)pg.FontBaseSize / duPerEm;
                float ySign = -1f;

                foreach (var p in pg.Points)
                {
                    if (p is null) { continue; }

                    // IMPORTANT: Don't create IDs during export
                    if (!ids.TryGetGroupId(pg, out var gId)) { continue; }
                    if (!ids.TryGetPointId(p, out var pId)) { continue; }

                    if (pId >= (uint)pointStates.Length) { continue; }
                    var ps = pointStates[(int)pId];

                    // group comes from PointState.GroupId in shader :contentReference[oaicite:10]{index=10}
                    if (ps.GroupId >= (uint)groupStates.Length) { continue; }
                    var gs = groupStates[(int)ps.GroupId];

                    // Draw PN / Elev / Desc lines
                    DrawTesselatedLabelLine(gfx, p.PointNumber.ToString(), p, line: 0);
                    DrawTesselatedLabelLine(gfx, p.Elevation.ToString("F3"), p, line: 1);
                    if (p.HasDescription) DrawTesselatedLabelLine(gfx, p.Description, p, line: 2);

                    void DrawLabelLine(XGraphics g, string s, CogoPoint cp, int line)
                    {
                        if (string.IsNullOrEmpty(s)) { return; }
                        if (!ids.TryGetLabelId(cp, line, out var labelId)) { return; }
                        if (labelId >= (uint)labelStates.Length) { return; }

                        var ls = labelStates[(int)labelId];

                        // shader visibility test :contentReference[oaicite:11]{index=11}
                        if (!IsVisible(ls, ps, gs)) { return; }

                        // Convert chars -> glyph IDs (same as AddCogoTextLabelLine) :contentReference[oaicite:12]{index=12}
                        Span<int> cps = stackalloc int[s.Length];
                        for (int i = 0; i < s.Length; i++) cps[i] = s[i];
                        var gids = face.GetGlyphIndices(cps.ToArray());

                        float penDU = 0f;
                        var brush = PickBrush(ps, gs);

                        var instOrigin = SharpDX.Vector2.Zero;

                        for (int i = 0; i < gids.Length; i++)
                        {
                            short gid = (short)gids[i];
                            if (gid <= 0) { continue; }

                            // 1) Get cached DU path for this glyph (tessellates once per gid)
                            var duPath = GetDuGlyphPath(gid, glyphCache);

                            // 2) Build DU->PDF transform for THIS glyph instance (includes penDU)
                            var duToPdf = BuildDuToPdf(
                                instOrigin,
                                duToWorldBase,
                                penDU,
                                ySign,
                                ls, ps, gs,
                                worldToPdf);

                            // 3) Draw cached path with transform (NO per-triangle allocations)
                            gfx.Save();
                            gfx.MultiplyTransform(duToPdf);
                            gfx.DrawPath(brush, duPath);
                            gfx.Restore();

                            // 4) advance pen
                            penDU += resCache.AdvanceWidthCache[gid];
                        }
                    }

                    void DrawTesselatedLabelLine(XGraphics g, string s, CogoPoint cp, int line)
                    {
                        if (string.IsNullOrEmpty(s)) { return; }
                        if (!ids.TryGetLabelId(cp, line, out var labelId)) { return; }
                        if (labelId >= (uint)labelStates.Length) { return; }

                        var ls = labelStates[(int)labelId];

                        // shader visibility test :contentReference[oaicite:11]{index=11}
                        if (!IsVisible(ls, ps, gs)) { return; }

                        // Convert chars -> glyph IDs (same as AddCogoTextLabelLine) :contentReference[oaicite:12]{index=12}
                        Span<int> cps = stackalloc int[s.Length];
                        for (int i = 0; i < s.Length; i++) cps[i] = s[i];
                        var gids = face.GetGlyphIndices(cps.ToArray());

                        float penDU = 0f;
                        var brush = PickBrush(ps, gs);

                        var instOrigin = SharpDX.Vector2.Zero;

                        for (int i = 0; i < gids.Length; i++)
                        {
                            short gid = (short)gids[i];
                            if (gid <= 0) { continue; }

                            // 1) Get cached DU path for this glyph (tessellates once per gid)
                            var duPath = GetDuGlyphPath(gid, glyphCache);

                            // 2) Build DU->PDF transform for THIS glyph instance (includes penDU)
                            var duToPdf = BuildDuToPdf(
                                instOrigin,
                                duToWorldBase,
                                penDU,
                                ySign,
                                ls, ps, gs,
                                worldToPdf);

                            // 3) Draw cached path with transform (NO per-triangle allocations)
                            gfx.Save();
                            gfx.MultiplyTransform(duToPdf);
                            gfx.DrawPath(brush, duPath);
                            gfx.Restore();

                            // 4) advance pen
                            penDU += resCache.AdvanceWidthCache[gid];
                        }
                    }
                }
            }
        }
        private static void DrawTitleblockPdf(XGraphics gfx, IEnumerable<TbPrimitive> prims)
        {
            foreach (var prim in prims)
            {
                switch (prim)
                {
                    case TbRect r:
                        {
                            if (r.FillBrush is not null)
                            {
                                gfx.DrawRectangle(r.StrokePen, r.FillBrush,
                                    Pt(r.X), Pt(r.Y), Pt(r.W), Pt(r.H));
                            }
                            else
                            {
                                gfx.DrawRectangle(r.StrokePen, Pt(r.X), Pt(r.Y), Pt(r.W), Pt(r.H));
                            }
                            break;
                        }
                    case TbLine l:
                        {
                            gfx.DrawLine(l.StrokePen, Pt(l.X1), Pt(l.Y1), Pt(l.X2), Pt(l.Y2));
                            break;
                        }
                    case TbText t:
                        {
                            var style = t.Bold ? XFontStyle.Bold : XFontStyle.Regular;
                            var font = new XFont(t.FontFamily, Pt(t.FontSizeIn), style);

                            // IMPORTANT: rect is derived from anchor semantics of Align
                            var rect = AnchorRect(t.X, t.Y, t.W, t.H, t.Align);

                            DrawWrappedText(gfx, t.Text ?? "", font, t.FontBrush, rect, t.Align, clipToRect: true);
                            break;
                        }
                    case TbImage img:
                        {
                            using var ms = new MemoryStream(img.ImageBytes);
                            using var xi = XImage.FromStream(() => ms);
                            gfx.DrawImage(xi, Pt(img.X), Pt(img.Y), Pt(img.W), Pt(img.H));
                            break;
                        }
                }
            }
        }
        private static void DrawWrappedText(
            XGraphics gfx,
             string text,
             XFont font,
             XBrush brush,
             XRect rect,
             TbAlign align,
             bool clipToRect = true)
        {
            if (clipToRect)
            {
                gfx.Save();
                gfx.IntersectClip(rect);
            }

            var lines = WrapLinesNoJustify(gfx, font, text ?? "", rect.Width);

            double lineH = gfx.MeasureString("Ag", font).Height;
            double ascent = lineH * 0.83; // baseline approximation
            double textH = lines.Count * lineH;

            // vertical alignment of the BLOCK within rect
            double yStart = align switch
            {
                TbAlign.TopLeft or TbAlign.TopCenter or TbAlign.TopRight => rect.Y,
                TbAlign.MiddleLeft or TbAlign.MiddleCenter or TbAlign.MiddleRight => rect.Y + Math.Max(0, (rect.Height - textH) / 2),
                TbAlign.BottomLeft or TbAlign.BottomCenter or TbAlign.BottomRight => rect.Y + Math.Max(0, rect.Height - textH),
                _ => rect.Y
            };

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                double yTop = yStart + i * lineH;

                if (yTop + lineH > rect.Bottom) { break; }

                double lineW = gfx.MeasureString(line, font).Width;

                double x = align switch
                {
                    TbAlign.TopCenter or TbAlign.MiddleCenter or TbAlign.BottomCenter => rect.X + (rect.Width - lineW) / 2,
                    TbAlign.TopRight or TbAlign.MiddleRight or TbAlign.BottomRight => rect.X + (rect.Width - lineW),
                    _ => rect.X
                };

                gfx.DrawString(line, font, brush, new XPoint(x, yTop + ascent));
            }

            if (clipToRect) gfx.Restore();
        }

        private static bool IsVisible(in LabelState ls, in PointState ps, in GroupState gs)
        {
            bool visLbl = (ls.Flags & LABEL_VISIBLE) != 0;
            bool visPt = (ps.Flags & POINT_VISIBLE) != 0;
            bool visGrp = (gs.Flags & GROUP_VISIBLE) != 0;
            return visLbl && visPt && visGrp;
        }

        private static XMatrix BuildDuToPdf(
            SharpDX.Vector2 instOriginWorld,
            float duToWorldBase,
            float penDU,
            float ySign,
            in LabelState ls,
            in PointState ps,
            in GroupState gs,
            Matrix worldToPdf)
        {
            float isFlippedY = ((ps.Flags & POINT_ISFLIPPEDY) != 0) ? -1f : 1f;
            float textInfoOffset = gs.TextInfoBaseXoffset * isFlippedY;

            // Same origin math as your shader
            float x = instOriginWorld.X
                      + ls.Offset.X
                      + textInfoOffset
                      + ps.PointInfoOffset.X
                      + ps.Offset.X;

            float y = instOriginWorld.Y
                      + (ls.Offset.Y * gs.Scale)
                      + ps.PointInfoOffset.Y
                      + ps.Offset.Y;

            float duToWorld = duToWorldBase * gs.Scale;

            // --- DU -> WORLD (WPF matrix math) ---
            var duToWorldM = new Matrix(
                duToWorld, 0,
                0, duToWorld * ySign,
                x + penDU * duToWorld,
                y);

            // Compose with WORLD -> PDF (still WPF)
            duToWorldM.Append(worldToPdf);

            // --- CONVERT WPF Matrix -> XMatrix ---
            return new XMatrix(
                duToWorldM.M11,
                duToWorldM.M12,
                duToWorldM.M21,
                duToWorldM.M22,
                duToWorldM.OffsetX,
                duToWorldM.OffsetY);
        }

        private static XBrush PickBrush(in PointState ps, in GroupState gs)
        {
            // Match your shader: selected overrides group color
            var c = ((ps.Flags & POINT_SELECTED) != 0)
                ? GlobalHelperProperties.SelectedObjectColor // or pass this in
                : gs.Color;

            return new XSolidBrush(PdfTransform.ToXColor(c.ToVector4()));
        }
        private static XColor PickColor(in PointState ps, in GroupState gs)
        {
            // Match your shader: selected overrides group color
            var c = ((ps.Flags & POINT_SELECTED) != 0)
                ? GlobalHelperProperties.SelectedObjectColor // or pass this in
                : gs.Color;

            return PdfTransform.ToXColor(c.ToVector4());
        }
        private static XGraphicsPath GetDuGlyphPath(short gid, GlyphMeshCache cache)
        {
            if (_duGlyphPathCache.TryGetValue(gid, out var p)) { return p; }

            var mesh = cache.Get(gid);
            var path = new XGraphicsPath();

            var v = mesh.PositionsDU;
            for (int i = 0; i + 2 < v.Length; i += 3)
            {
                path.AddLine(v[i].X, v[i].Y, v[i + 1].X, v[i + 1].Y);
                path.AddLine(v[i + 1].X, v[i + 1].Y, v[i + 2].X, v[i + 2].Y);
                path.CloseFigure();
            }

            _duGlyphPathCache[gid] = path;
            return path;
        }
        private static List<string> WrapLinesNoJustify(XGraphics gfx, XFont font, string text, double maxWidthPts)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return lines;

            // normalize newlines + tabs
            text = text.Replace("\r\n", "\n").Replace("\t", "    ");

            foreach (var paragraph in text.Split('\n'))
            {
                // preserve blank lines
                if (paragraph.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var words = paragraph.Split(' ', StringSplitOptions.None); // keep empty entries for multiple spaces
                var current = new StringBuilder();

                for (int i = 0; i < words.Length; i++)
                {
                    // Rebuild the original spacing: each token after the first gets a single space.
                    // If you truly want to preserve multiple spaces, we can—most titleblocks don’t need it.
                    var token = words[i];
                    var candidate = current.Length == 0 ? token : current + " " + token;

                    double w = gfx.MeasureString(candidate, font).Width;

                    if (w <= maxWidthPts || current.Length == 0)
                    {
                        current.Clear();
                        current.Append(candidate);
                    }
                    else
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                        current.Append(token);
                    }
                }

                if (current.Length > 0) { lines.Add(current.ToString()); }
            }

            return lines;
        }
        private static XRect AnchorRect(double xIn, double yIn, double wIn, double hIn, TbAlign align)
        {
            double x = Pt(xIn);
            double y = Pt(yIn);
            double w = Pt(wIn);
            double h = Pt(hIn);

            // Convert anchor (x,y) to top-left rect (rx, ry)
            double rx = x;
            double ry = y;

            switch (align)
            {
                case TbAlign.TopLeft:
                    // already top-left
                    break;

                case TbAlign.TopCenter:
                    rx = x - w / 2;
                    break;

                case TbAlign.TopRight:
                    rx = x - w;
                    break;

                case TbAlign.MiddleLeft:
                    ry = y - h / 2;
                    break;

                case TbAlign.MiddleCenter:
                    rx = x - w / 2;
                    ry = y - h / 2;
                    break;

                case TbAlign.MiddleRight:
                    rx = x - w;
                    ry = y - h / 2;
                    break;

                case TbAlign.BottomLeft:
                    ry = y - h;
                    break;

                case TbAlign.BottomCenter:
                    rx = x - w / 2;
                    ry = y - h;
                    break;

                case TbAlign.BottomRight:
                    rx = x - w;
                    ry = y - h;
                    break;
            }

            return new XRect(rx, ry, w, h);
        }

        private static Matrix BuildWorldToPdfFromCamera(Layout layout, CadManager cadManager, Scene scene)
        {
            if (layout is null) throw new ArgumentNullException(nameof(layout));
            if (cadManager is null) throw new ArgumentNullException(nameof(cadManager));
            if (scene is null) throw new ArgumentNullException(nameof(scene));

            var cam = cadManager.Camera;
            if (cam is null) throw new InvalidOperationException("cadManager.Camera is null.");

            // PDF viewport rect in POINTS (page coords, Y-down)
            var vpIn = layout.Viewport.LocalRectIn;
            double vpX = vpIn.X * 72.0;
            double vpY = vpIn.Y * 72.0;
            double vpW = vpIn.Width * 72.0;
            double vpH = vpIn.Height * 72.0;

            // Save camera state
            var oldViewport = cam.Viewport;
            var oldInitial = cam.InitialViewMatrix;
            var oldTranslate = cam.Translate;
            var oldZoomStep = cam.CurrentZoomStep;

            try
            {
                // Virtual viewport matching the PDF viewport size.
                // Only aspect/size matters for the camera math; using points makes it 1:1.
                var virtualViewport = new SharpDX.ViewportF(0, 0, (float)vpW, (float)vpH);

                // Rebuild the extents-fit matrix for THIS viewport size
                cam.InitialViewMatrix = GetExtentsFittingMatrix(virtualViewport, cadManager.Extents);

                // Recompute projection/view/windows matrices for this viewport
                cam.UpdateViewportSize(virtualViewport);

                // Apply the saved scene pan/zoom
                cam.SetPanAndZoom(scene.Translation, scene.ZoomStep);

                // Camera.WindowsMatrix maps world -> view (top-left origin, Y-down)
                var worldToView = cam.WindowsMatrix;

                // Shift view coords into the PDF viewport's top-left corner on the page
                worldToView.Translate(vpX, vpY);

                return worldToView;
            }
            finally
            {
                // Restore camera state
                cam.UpdateViewportSize(oldViewport);
                cam.InitialViewMatrix = oldInitial;
                cam.SetPanAndZoom(oldTranslate, oldZoomStep);
            }
        }

        private static Matrix BuildWorldToPdfFromCameraNew(Layout layout, CadManager cadManager, Scene scene)
        {
            if (layout is null) throw new ArgumentNullException(nameof(layout));
            if (cadManager is null) throw new ArgumentNullException(nameof(cadManager));
            if (scene is null) throw new ArgumentNullException(nameof(scene));

            var cam = cadManager.Camera ?? throw new InvalidOperationException("cadManager.Camera is null.");

            // PDF viewport rect in POINTS (page coords, Y-down)
            var vpIn = layout.Viewport.LocalRectIn;
            double vpX = vpIn.X * 72.0;
            double vpY = vpIn.Y * 72.0;
            double vpW = vpIn.Width * 72.0;
            double vpH = vpIn.Height * 72.0;

            // Save camera state (IMPORTANT: include Extents)
            var oldViewport = cam.Viewport;
            var oldInitial = cam.InitialViewMatrix;
            var oldExtents = cam.Extents;
            var oldTranslate = cam.Translate;
            var oldZoomStep = cam.CurrentZoomStep;

            try
            {
                // Virtual viewport matching the PDF viewport size (in points)
                var virtualViewport = new SharpDX.ViewportF(0, 0, (float)vpW, (float)vpH);

                // Use the SAVED visible scene bounds (world rect)
                var b = scene.Bounds; // RectangleF
                if (b.Width <= 0 || b.Height <= 0)
                    throw new InvalidOperationException("Scene.Bounds is invalid (width/height <= 0).");

                // Add small padding to avoid hairline clipping
                const double padFrac = 0.02; // 2%
                double padX = b.Width * padFrac;
                double padY = b.Height * padFrac;

                var padded = new Rect(
                    b.X - padX,
                    b.Y - padY,
                    b.Width + 2 * padX,
                    b.Height + 2 * padY);

                // Center projection on the scene bounds and fit BOTH axes (min(scaleX, scaleY))
                cam.Extents = padded;
                cam.InitialViewMatrix = GetExtentsFittingMatrix(virtualViewport, padded);

                // IMPORTANT: neutralize aspect-dependent view state for export
                cam.Translate = SharpDX.Vector2.Zero;
                cam.CurrentZoomStep = 0;

                // Recompute camera transforms for the virtual viewport
                cam.Viewport = virtualViewport;
                cam.UpdateProjection();
                cam.UpdateView();
                // UpdateViewProjection() is private in your Camera; so call the public path:
                cam.Update2DTransformationMatrix(); // ensures WindowsMatrix is correct after View/Projection change
                                                    // BUT Update2DTransformationMatrix depends on ViewProjectionMatrix, so we need a public way.
                                                    // Easiest: call cam.ResetToDefaults() after setting InitialViewMatrix/Extents/Viewport:
                cam.ResetToDefaults(); // uses CurrentZoomStep/Translate we just set to 0/0

                // Camera.WindowsMatrix maps world -> view (top-left origin, Y-down)
                var worldToView = cam.WindowsMatrix;

                // Shift into the PDF viewport’s location on the page
                worldToView.Translate(vpX, vpY);

                return worldToView;
            }
            finally
            {
                // Restore camera state
                cam.Extents = oldExtents;
                cam.InitialViewMatrix = oldInitial;
                cam.Viewport = oldViewport;
                cam.Translate = oldTranslate;
                cam.CurrentZoomStep = oldZoomStep;

                cam.UpdateProjection();
                cam.UpdateView();
                cam.ResetToDefaults(); // puts matrices back in sync with restored state
            }
        }

        private static SharpDX.Matrix GetExtentsFittingMatrix(SharpDX.ViewportF viewport, Rect extents)
        {
            // same as D3dDxfControl.GetExtentsFittingMatrix :contentReference[oaicite:8]{index=8}
            double scale = Math.Min(viewport.Width / extents.Width, viewport.Height / extents.Height);
            return SharpDX.Matrix.Scaling((float)scale, (float)scale, 1f)
                 * SharpDX.Matrix.Translation((float)-extents.Left, (float)-extents.Top, 0f);
        }

        private static SharpDX.Vector2 ComputeCogoLabelWorldOrigin(in LabelState ls, in PointState ps, in GroupState gs)
        {
            // These constants already exist in your exporter:
            // const uint POINT_ISFLIPPEDY = ...
            float isFlippedY = ((ps.Flags & POINT_ISFLIPPEDY) != 0) ? -1f : 1f;
            float textInfoOffset = gs.TextInfoBaseXoffset * isFlippedY;

            float x =
                ls.Offset.X +
                textInfoOffset +
                ps.PointInfoOffset.X +
                ps.Offset.X;

            float y =
                (ls.Offset.Y * gs.Scale) +
                ps.PointInfoOffset.Y +
                ps.Offset.Y;

            return new SharpDX.Vector2(x, y);
        }

        private static (double X, double Y) TransformPoint(Matrix m, SharpDX.Vector2 p)
        {
            // WPF Matrix semantics:
            // x' = x*M11 + y*M21 + OffsetX
            // y' = x*M12 + y*M22 + OffsetY
            double x = p.X;
            double y = p.Y;
            return (x * m.M11 + y * m.M21 + m.OffsetX,
                    x * m.M12 + y * m.M22 + m.OffsetY);
        }

        private static double PointsPerWorldUnit(Matrix worldToPdf)
        {
            // Length of transformed (0,1) gives “points per world unit” in Y direction.
            double dx = worldToPdf.M21;
            double dy = worldToPdf.M22;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len <= 0 ? 1.0 : len;
        }

        static double PointsPerWorldUnitX(Matrix worldToPdf)
        {
            double dx = worldToPdf.M11;
            double dy = worldToPdf.M12;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len <= 0 ? 1.0 : len;
        }

        static double PointsPerWorldUnitY(Matrix worldToPdf)
        {
            double dx = worldToPdf.M21;
            double dy = worldToPdf.M22;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len <= 0 ? 1.0 : len;
        }

    }
}