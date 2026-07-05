using netDxf;
using System.Globalization;

using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DxfImport
{
    public abstract class TagContainer
    {
        public string Handle => GetString(5) ?? string.Empty;
        public List<MLeaderTag> Tags { get; } = [];

        protected List<MLeaderTag> GetAll(int code)
        {
            return Tags
                .Where(x => x.Code == code)
                .ToList();
        }
        protected MLeaderTag? GetLast(int code)
        {
            return Tags
                .LastOrDefault(x => x.Code == code);
        }
        protected string? GetString(int code)
        {
            return GetLast(code)?.Value;
        }
        protected float? GetFloat(int code)
        {
            var value = GetString(code);

            if (value == null)
                return null;

            return float.Parse(
                value,
                System.Globalization.CultureInfo.InvariantCulture);
        }
        protected int? GetInt(int code)
        {
            var value = GetString(code);

            if (value == null)
                return null;

            return int.Parse(value);
        }
        protected short? GetShort(int code)
        {
            var tag = Tags
                .LastOrDefault(x => x.Code == code);

            if (tag == null)
                return null;

            return short.Parse(
                tag.Value,
                CultureInfo.InvariantCulture);
        }
        protected bool GetBool(int code)
        {
            return GetInt(code) == 1;
        }

        public static Vector4 ConvertTrueColorToVector4(int trueColor)
        {
            byte r = (byte)((trueColor >> 16) & 0xFF);
            byte g = (byte)((trueColor >> 8) & 0xFF);
            byte b = (byte)(trueColor & 0xFF);

            return new Vector4(
                r / 255f,
                g / 255f,
                b / 255f,
                1f);
        }

        public static Vector4 ConvertACINumberToRGBA(short aciNumber)
        {
            if (aciNumber >= 1 && aciNumber <= 255)
            {
                AciColor aciColor = AciColor.FromCadIndex(aciNumber);
                return new Vector4(aciColor.R / 255.0f, aciColor.G / 255.0f, aciColor.B / 255.0f, 1f);
            }
            else
            {
                return new Vector4(0, 0, 0, 1);
            }
        }
    }
}
