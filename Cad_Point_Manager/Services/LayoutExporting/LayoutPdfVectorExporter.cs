using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Services.LayoutExporting
{
    public class LayoutPdfVectorExporter : ILayoutPdfVectorExporter
    {
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
            string outputPath,
            CancellationToken ct = default)
        {
            return Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var viewSize = new Size(cadManager3D.ViewportSize.Width, cadManager3D.ViewportSize.Height);
                double pageWpts = layout.PageSize.Width * 72.0;
                double pageHpts = layout.PageSize.Height * 72.0;

                Rect pdfViewportPts = new Rect(
                    layout.Viewport.LocalRectIn.X * 72.0,
                    layout.Viewport.LocalRectIn.Y * 72.0,
                    layout.Viewport.LocalRectIn.Width * 72.0,
                    layout.Viewport.LocalRectIn.Height * 72.0);
                Matrix worldToPdf = LayoutPdfMatrixBuilder.BuildWorldToPdfContain_YDown(scene.Bounds.ToRect(), pdfViewportPts);

                LayoutPdfVectorExporter.Export(
                    layout,
                    cadManager3D,
                    stateController,
                    ids,
                    resCache,
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

            foreach (var kv in cadManager.Layers)
            {
                var layer = kv.Value;
                if (!layer.IsVisible) continue;

                foreach (var obj in layer.DrawingObjects)
                {
                    var pen = new XPen(PdfTransform.ToXColor(obj.Color.ToVector4()), 0.25);

                    switch (obj)
                    {
                        case DrawingLine line:
                            DrawLine(gfx, line, worldToPdf, pen);
                            break;

                        case DrawingPolyline pl:
                            DrawPolyline(gfx, pl.Vertices, worldToPdf, pen);
                            break;

                        case DrawingArc arc:
                            DrawPolyline(gfx, arc.Vertices, worldToPdf, pen);
                            break;

                        case DrawingCircle circle:
                            DrawPolyline(gfx, circle.Vertices, worldToPdf, pen);
                            break;

                        case DrawingMtext mtext:
                            DrawMtext(gfx, mtext, worldToPdf);
                            break;
                    }
                }
            }

            // Snapshot state (consistent during export)
            var labelStates = stateController.GetLabelStatesSnapshot();
            var pointStates = stateController.GetPointStatesSnapshot();
            var groupStates = stateController.GetGroupStatesSnapshot();

            // You need the same tessellator you already use for D3D text.
            IGlyphTessellator tess = resCache.GlyphTessellator; // <- whatever your app uses
            var glyphCache = new GlyphMeshCache(resCache.CogoPointFontFace, tess); // :contentReference[oaicite:4]{index=4}

            DrawCogoPointGlyphs(
                gfx, cadManager, ids, resCache, worldToPdf,
                labelStates, pointStates, groupStates,
                glyphCache);

            doc.Save(outputPdfPath);
        }

        private static void DrawLine(XGraphics gfx, DrawingLine line, Matrix worldToPdf, XPen pen)
        {
            var p0 = PdfTransform.WorldToPdf(line.Start.ToVector3(), worldToPdf);
            var p1 = PdfTransform.WorldToPdf(line.End.ToVector3(), worldToPdf);
            gfx.DrawLine(pen, p0, p1);
        }

        private static void DrawPolyline(XGraphics gfx, LineVertex[] verts, Matrix worldToPdf, XPen pen)
        {
            if (verts == null || verts.Length < 2) { return; }

            for (int i = 0; i + 1 < verts.Length; i += 2)
            {
                var a = PdfTransform.WorldToPdf(verts[i].Position.ToVector3(), worldToPdf);
                var b = PdfTransform.WorldToPdf(verts[i + 1].Position.ToVector3(), worldToPdf);
                gfx.DrawLine(pen, a, b);
            }
        }

        private static void DrawMtext(XGraphics gfx, DrawingMtext mtext, Matrix worldToPdf)
        {
            var verts = mtext.TextVertices;
            if (verts == null || verts.Count < 3) { return; }

            var brush = new XSolidBrush(PdfTransform.ToXColor(mtext.Color.ToVector4()));
            var path = new XGraphicsPath();

            for (int i = 0; i + 2 < verts.Count; i += 3)
            {
                var p0 = PdfTransform.WorldToPdf(verts[i].Position.ToVector3(), worldToPdf);
                var p1 = PdfTransform.WorldToPdf(verts[i + 1].Position.ToVector3(), worldToPdf);
                var p2 = PdfTransform.WorldToPdf(verts[i + 2].Position.ToVector3(), worldToPdf);
                path.AddPolygon(new[] { p0, p1, p2 });
            }

            gfx.DrawPath(brush, path);
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
                    DrawLabelLine(gfx, p.PointNumber.ToString(), p, line: 0);
                    DrawLabelLine(gfx, p.Elevation.ToString("F3"), p, line: 1);
                    if (p.HasDescription) DrawLabelLine(gfx, p.Description, p, line: 2);

                    void DrawLabelLine(XGraphics g, string s, CogoPoint cp, int line)
                    {
                        if (string.IsNullOrEmpty(s)) return;
                        if (!ids.TryGetLabelId(cp, line, out var labelId)) return;
                        if (labelId >= (uint)labelStates.Length) return;

                        var ls = labelStates[(int)labelId];

                        // shader visibility test :contentReference[oaicite:11]{index=11}
                        if (!IsVisible(ls, ps, gs)) return;

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
                            if (gid <= 0) continue;

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


        private static SharpDX.Vector2 ComputeGlyphWorld(
            SharpDX.Vector2 posDU,
            float penDU,
            SharpDX.Vector2 originWorldFromInstance,
            float duToWorldBase,
            float ySign,
            in LabelState ls,
            in PointState ps,
            in GroupState gs)
        {
            // Shader flags that affect direction
            bool flippedY = (ps.Flags & (1u << 6)) != 0;
            float isFlippedY = flippedY ? -1f : 1f;

            float textInfoOffset = gs.TextInfoBaseXoffset * isFlippedY;

            // originWorld (matches shader)
            float x = originWorldFromInstance.X
                      + ls.Offset.X
                      + textInfoOffset
                      + ps.PointInfoOffset.X
                      + ps.Offset.X;

            float y = originWorldFromInstance.Y
                      + (ls.Offset.Y * gs.Scale)
                      + ps.PointInfoOffset.Y
                      + ps.Offset.Y;

            float duToWorld = duToWorldBase * gs.Scale;

            return new SharpDX.Vector2(
                x + (penDU + posDU.X) * duToWorld,
                y + (posDU.Y * ySign) * duToWorld
            );
        }

        private static SharpDX.Vector2 TransformGlyphVertex(
            SharpDX.Vector2 posDU,
            in LabelState label,
            in PointState point,
            in GroupState group,
            float fontScaleDU)
        {
            SharpDX.Vector2 p = posDU * fontScaleDU;
            p += label.Offset;
            p += point.PointInfoOffset;
            p *= group.Scale;

            return p;
        }
        private static bool IsVisible(in LabelState ls, in PointState ps, in GroupState gs)
        {
            bool visLbl = (ls.Flags & LABEL_VISIBLE) != 0;
            bool visPt = (ps.Flags & POINT_VISIBLE) != 0;
            bool visGrp = (gs.Flags & GROUP_VISIBLE) != 0;
            return visLbl && visPt && visGrp;
        }
        private static SharpDX.Vector2 ComputeWorldFromDU(
            SharpDX.Vector2 posDU,
            SharpDX.Vector2 instOriginWorld,
            float duToWorldBase,
            float penDU,
            float ySign,
            in LabelState ls,
            in PointState ps,
            in GroupState gs)
        {
            float isFlippedY = ((ps.Flags & POINT_ISFLIPPEDY) != 0) ? -1f : 1f;
            float textInfoOffset = gs.TextInfoBaseXoffset * isFlippedY;

            float x = instOriginWorld.X + ls.Offset.X + textInfoOffset + ps.PointInfoOffset.X + ps.Offset.X;
            float y = instOriginWorld.Y + (ls.Offset.Y * gs.Scale) + ps.PointInfoOffset.Y + ps.Offset.Y;

            float duToWorld = duToWorldBase * gs.Scale;

            return new SharpDX.Vector2(
                x + (penDU + posDU.X) * duToWorld,
                y + (posDU.Y * ySign) * duToWorld
            );
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


        private static XPoint WorldToPdf(SharpDX.Vector2 w, Matrix worldToPdf)
        {
            var p = worldToPdf.Transform(new System.Windows.Point(w.X, w.Y));
            return new XPoint(p.X, p.Y);
        }
        private static XBrush PickBrush(in PointState ps, in GroupState gs)
        {
            // Match your shader: selected overrides group color
            var c = ((ps.Flags & POINT_SELECTED) != 0)
                ? GlobalHelperProperties.SelectedObjectColor // or pass this in
                : gs.Color;

            return new XSolidBrush(PdfTransform.ToXColor(c.ToVector4()));
        }
        private static XGraphicsPath GetDuGlyphPath(short gid, GlyphMeshCache cache)
        {
            if (_duGlyphPathCache.TryGetValue(gid, out var p))
                return p;

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
    }
}