using SharpDX;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.SerializableObjects
{
    public class SerializableColor
    {
        public float Red { get; set; }
        public float Green { get; set; }
        public float Blue { get; set; }
        public float Alpha { get; set; }

        public SerializableColor() { }

        public SerializableColor(RawColor4 color)
        {
            Red = color.R;
            Green = color.G;
            Blue = color.B;
            Alpha = color.A;
        }
        public SerializableColor(byte red, byte green, byte blue, byte alpha)
        {
            Red = red / 255f;
            Green = green / 255f;
            Blue = blue / 255f;
            Alpha = alpha / 255f;
        }

        public RawColor4 ToColor4()
        {
            return new Color4(Red, Green, Blue, Alpha);
        }
        public (byte r, byte g, byte b, byte a) ToBytes()
        {
            return ((byte)(Red * 255), (byte)(Green * 255), (byte)(Blue * 255), (byte)(Alpha * 255));
        }
    }
}
