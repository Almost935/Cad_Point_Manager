using netDxf;

namespace Cad_Point_Manager.Helpers
{
    public static class AutoCadColorConverter
    {
        /// <summary>
        /// Converts an AutoCAD C-number (ACI) to an RGBA color.
        /// </summary>
        public static Vector4 ConvertACINumberToRGBA(short aciNumber)
        {
            if (aciNumber >= 1 && aciNumber <= 255)
            {
                AciColor aciColor = AciColor.FromCadIndex(aciNumber);
                return new Vector4(aciColor.R / 255.0, aciColor.G / 255.0, aciColor.B / 255.0, 1);
            }
            else
            {
                return new Vector4(0, 0, 0, 1);
            }
        }

        public static Vector4 ConvertTrueColorToVector4(int trueColor)
        {
            byte[] bytes = BitConverter.GetBytes(trueColor);
            double r = bytes[0] / 255.0;  // Extract Red from correct position
            double g = bytes[1] / 255.0;  // Extract Green
            double b = bytes[2] / 255.0;  // Extract Blue
            double a = 1;                 // Assume fully opaque

            return new Vector4(r, g, b, a);
        }

    }
}