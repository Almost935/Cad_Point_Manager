using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D11;
using System.Diagnostics;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public sealed class MsdfAtlas : IDisposable
{
    public IReadOnlyDictionary<char, MsdfGlyph> Glyphs { get; }
    public IReadOnlyDictionary<uint, float> Kernings { get; }
    public float LineHeight { get; }
    public float Ascender { get; }
    public float Descender { get; }
    public int Width { get; }
    public int Height { get; }
    public float DistanceRange { get; }
    public Texture2D Texture { get; }
    public ShaderResourceView ShaderResourceView { get; }

    public MsdfAtlas(
        IReadOnlyDictionary<char, MsdfGlyph> glyphs,
        IReadOnlyDictionary<uint, float> kernings,
        float lineHeight,
        float ascender,
        float descender,
        int width,
        int height,
        float distanceRange,
        Texture2D texture,
        ShaderResourceView shaderResourceView)
    {
        Glyphs = glyphs;
        Kernings = kernings;
        LineHeight = lineHeight;
        Ascender = ascender;
        Descender = descender;
        Width = width;
        Height = height;
        DistanceRange = distanceRange;

        Debug.WriteLine($"\nAscender: {Ascender}");
        Debug.WriteLine($"Descender: {Descender}");
        Debug.WriteLine($"LineHeight: {LineHeight}");

        Texture = texture;
        ShaderResourceView = shaderResourceView;
    }

    public void Dispose()
    {
        ShaderResourceView?.Dispose();
        Texture?.Dispose();
    }
}