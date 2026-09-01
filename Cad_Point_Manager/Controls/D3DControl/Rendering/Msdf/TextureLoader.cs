using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using Buffer = SharpDX.DataStream;
using Device = SharpDX.Direct3D11.Device;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public static class TextureLoader
{
    public static Texture2D LoadTexture(
        Device device, string filename, out ShaderResourceView srv, out MsdfCpuAtlas cpuAtlas)
    {
        using var stream = File.OpenRead(filename);

        var decoder = new PngBitmapDecoder(
            stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        BitmapSource bmp = decoder.Frames[0];

        if (bmp.Format != PixelFormats.Bgra32)
        {
            bmp = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
        }

        int width = bmp.PixelWidth;
        int height = bmp.PixelHeight;

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        bmp.CopyPixels(pixels, stride, 0);

        cpuAtlas = new MsdfCpuAtlas(pixels, width, height, stride);

        var texDesc = new Texture2DDescription
        {
            Width = width,
            Height = height,
            ArraySize = 1,
            MipLevels = 1,
            Format = Format.B8G8R8A8_UNorm,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            SampleDescription = new SampleDescription(1, 0)
        };

        var ds = DataStream.Create(pixels, true, false);
        var rect = new DataRectangle(ds.DataPointer, stride);
        var tex = new Texture2D(device, texDesc, rect);

        ds.Dispose();

        srv = new ShaderResourceView(device, tex);

        return tex;
    }
}