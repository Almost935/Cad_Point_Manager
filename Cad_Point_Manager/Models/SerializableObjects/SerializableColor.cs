using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Models.SerializableObjects
{
    public class SerializableColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public SerializableColor() { }

        public SerializableColor(RawColor4 color)
        {
            R = (byte)(color.R * 255);
            G = (byte)(color.G * 255);
            B = (byte)(color.B * 255);
            A = (byte)(color.A * 255);
        }
        public SerializableColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public RawColor4 ToColor4()
        {
            return new RawColor4(R / 255, G / 255, B / 255, A / 255);
        }
    }
}
