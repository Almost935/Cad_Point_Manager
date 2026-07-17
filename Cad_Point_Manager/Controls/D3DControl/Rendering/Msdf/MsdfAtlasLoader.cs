using SharpDX;
using SharpDX.Direct3D11;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public static class MsdfAtlasLoader
{
    public static MsdfAtlas Load(Device device, string pngPath, string jsonPath)
    {
        Texture2D texture = TextureLoader.LoadTexture(device, pngPath, out ShaderResourceView shaderResourceView);

        MsdfJsonRoot root = LoadJson(jsonPath);

        float invWidth = 1f / root.Atlas.Width;
        float invHeight = 1f / root.Atlas.Height;

        Dictionary<char, MsdfGlyph> glyphs = [];

        foreach (var glyph in root.Glyphs)
        {
            char character = (char)glyph.Unicode;

            Vector2 planeMin = Vector2.Zero;
            Vector2 planeMax = Vector2.Zero;

            if (glyph.PlaneBounds != null)
            {
                planeMin = new Vector2(
                    glyph.PlaneBounds.Left,
                    glyph.PlaneBounds.Bottom);

                planeMax = new Vector2(
                    glyph.PlaneBounds.Right,
                    glyph.PlaneBounds.Top);
            }

            Vector2 uvMin = Vector2.Zero;
            Vector2 uvMax = Vector2.Zero;

            if (glyph.AtlasBounds != null)
            {
                uvMin = new Vector2(
                    glyph.AtlasBounds.Left * invWidth,
                    glyph.AtlasBounds.Top * invHeight);

                uvMax = new Vector2(
                    glyph.AtlasBounds.Right * invWidth,
                    glyph.AtlasBounds.Bottom * invHeight);
            }

            glyphs.Add(character,
                new MsdfGlyph
                {
                    Character = character,
                    Advance = glyph.Advance,

                    PlaneMin = planeMin,
                    PlaneMax = planeMax,

                    UvMin = uvMin,
                    UvMax = uvMax
                });
        }

        Dictionary<uint, float> kernings = new();

        if (root.Kerning != null)
        {
            foreach (var pair in root.Kerning)
            {
                uint key =
                    ((uint)pair.Unicode1 << 16) |
                    (ushort)pair.Unicode2;

                kernings [key] = pair.Advance;
            }
        }

        return new MsdfAtlas(
            glyphs,
            kernings,
            root.Metrics.LineHeight,
            root.Metrics.Ascender,
            root.Metrics.Descender,
            root.Atlas.Width,
            root.Atlas.Height,
            root.Atlas.DistanceRange,
            texture,
            shaderResourceView);
    }

    private static MsdfJsonRoot LoadJson(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        MsdfJsonRoot? root =
            JsonSerializer.Deserialize<MsdfJsonRoot>(json, options);

        if (root == null)
            throw new InvalidOperationException(
                "Unable to deserialize MSDF atlas.");

        return root;
    }
}