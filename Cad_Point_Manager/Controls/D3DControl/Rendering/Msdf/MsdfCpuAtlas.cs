namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public sealed class MsdfCpuAtlas
{
    public byte[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }

    public MsdfCpuAtlas(byte[] pixels, int width, int height, int stride)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        Stride = stride;
    }
}