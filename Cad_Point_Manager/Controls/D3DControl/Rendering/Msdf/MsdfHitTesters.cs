using SharpDX;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public static class MsdfHitTester
{
    public static bool IsInside(MsdfAtlas atlas, Vector2 uv)
    {
        if (atlas?.CpuAtlas is null) { return false; }

        var cpu = atlas.CpuAtlas;

        int x = (int)(uv.X * cpu.Width);
        int y = (int)(uv.Y * cpu.Height);

        x = Math.Clamp(x, 0, cpu.Width - 1);
        y = Math.Clamp(y, 0, cpu.Height - 1);

        int index = y * cpu.Stride + x * 4;

        // TextureLoader converted the bitmap to BGRA32.
        float b = cpu.Pixels[index + 0] / 255f;
        float g = cpu.Pixels[index + 1] / 255f;
        float r = cpu.Pixels[index + 2] / 255f;

        float sd = Median(r, g, b);

        return sd >= 0.5f;
    }

    private static float Median(float r, float g, float b)
    {
        return Math.Max(Math.Min(r, g), Math.Min(Math.Max(r, g), b));
    }
}