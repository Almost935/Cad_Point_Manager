using SharpDX;
using System.Windows;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public static class MsdfHitTester
{
    public static bool IsInside(
        MsdfAtlas atlas,
        Vector2 uv)
    {
        return SampleSignedDistance(atlas, uv) >= 0.5f;
    }

    public static bool HitTest(MsdfAtlas atlas, MsdfGlyphHitRegion region, Point point)
    {
        Rect bounds = region.Bounds;

        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        if (!bounds.Contains(point))
        {
            return false;
        }

        Vector2 uv = WorldToUv(region, point);

        return IsInside(atlas, uv);
    }

    public static bool HitTest(MsdfAtlas atlas, MsdfGlyphHitRegion[] regions, Point point)
    {
        if (regions is null || regions.Length == 0)
        {
            return false;
        }

        foreach (var region in regions)
        {
            if (HitTest(atlas, region, point))
            {
                return true;
            }
        }

        return false;
    }

    public static double DistanceToGlyphs(MsdfAtlas atlas, MsdfGlyphHitRegion[] regions, Point point)
    {
        if (atlas?.CpuAtlas is null || regions is null || regions.Length == 0)
        {
            return double.MaxValue;
        }

        if (HitTest(atlas, regions, point))
        {
            return 0.0;
        }

        double minDistance = double.MaxValue;

        foreach (var region in regions)
        {
            double distance = DistanceToGlyph(atlas, region, point);

            minDistance = Math.Min(minDistance, distance);
        }

        return minDistance;
    }

    private static double DistanceToGlyph(MsdfAtlas atlas, MsdfGlyphHitRegion region, Point point)
    {
        Rect bounds = region.Bounds;

        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return double.MaxValue;
        }

        double clampedX = Math.Clamp(point.X, bounds.Left, bounds.Right);
        double clampedY = Math.Clamp(point.Y, bounds.Top, bounds.Bottom);

        Point samplePoint = new(clampedX, clampedY);

        double dx = point.X - clampedX;
        double dy = point.Y - clampedY;

        double outsideDistance = Math.Sqrt(dx * dx + dy * dy);

        Vector2 uv = WorldToUv(region, samplePoint);

        float sd = SampleSignedDistance(atlas, uv);

        double atlasDistance = Math.Max(0.0, 0.5 - sd) * atlas.DistanceRange;

        double atlasWidth = Math.Abs(region.UvMax.X - region.UvMin.X) * atlas.Width;
        double atlasHeight = Math.Abs(region.UvMax.Y - region.UvMin.Y) * atlas.Height;

        if (atlasWidth <= 0.0 || atlasHeight <= 0.0)
        {
            return double.MaxValue;
        }

        double worldPerAtlasPixelX = bounds.Width / atlasWidth;
        double worldPerAtlasPixelY = bounds.Height / atlasHeight;
        double worldPerAtlasPixel = (worldPerAtlasPixelX + worldPerAtlasPixelY) * 0.5;

        double msdfWorldDistance = atlasDistance * worldPerAtlasPixel;

        if (outsideDistance > 0.0)
        {
            return Math.Sqrt(outsideDistance * outsideDistance + msdfWorldDistance * msdfWorldDistance);
        }

        return msdfWorldDistance;
    }

    private static Vector2 WorldToUv(MsdfGlyphHitRegion region, Point point)
    {
        Rect bounds = region.Bounds;

        float localX = (float)((point.X - bounds.Left) / bounds.Width);
        float localY = 1.0f - (float)((point.Y - bounds.Top) / bounds.Height);

        float u = region.UvMin.X + localX * (region.UvMax.X - region.UvMin.X);
        float v = region.UvMin.Y + localY * (region.UvMax.Y - region.UvMin.Y);

        return new Vector2(u, v);
    }

    private static float SampleSignedDistance(MsdfAtlas atlas, Vector2 uv)
    {
        if (atlas?.CpuAtlas is null)
        {
            return float.NegativeInfinity;
        }

        var cpu = atlas.CpuAtlas;

        float fx = uv.X * cpu.Width - 0.5f;
        float fy = uv.Y * cpu.Height - 0.5f;

        int x0 = (int)MathF.Floor(fx);
        int y0 = (int)MathF.Floor(fy);

        float tx = fx - x0;
        float ty = fy - y0;

        int x1 = x0 + 1;
        int y1 = y0 + 1;

        x0 = Math.Clamp(x0, 0, cpu.Width - 1);
        x1 = Math.Clamp(x1, 0, cpu.Width - 1);
        y0 = Math.Clamp(y0, 0, cpu.Height - 1);
        y1 = Math.Clamp(y1, 0, cpu.Height - 1);

        Vector3 c00 = ReadRgb(cpu, x0, y0);
        Vector3 c10 = ReadRgb(cpu, x1, y0);
        Vector3 c01 = ReadRgb(cpu, x0, y1);
        Vector3 c11 = ReadRgb(cpu, x1, y1);

        Vector3 top = Vector3.Lerp(c00, c10, tx);
        Vector3 bottom = Vector3.Lerp(c01, c11, tx);

        Vector3 sample = Vector3.Lerp(top, bottom, ty);

        return Median(sample.X, sample.Y, sample.Z);
    }

    private static float Median(float r, float g, float b)
    {
        return Math.Max(Math.Min(r, g), Math.Min(Math.Max(r, g), b));
    }

    private static Vector3 ReadRgb(MsdfCpuAtlas cpu, int x, int y)
    {
        int index = y * cpu.Stride + x * 4;

        float b = cpu.Pixels[index + 0] / 255f;
        float g = cpu.Pixels[index + 1] / 255f;
        float r = cpu.Pixels[index + 2] / 255f;

        return new Vector3(r, g, b);
    }
}