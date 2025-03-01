using netDxf;
=using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

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
                return new Vector4(aciColor.R / 255, aciColor.G / 255, aciColor.B / 255, 1);
            }
            else
            {
                return new Vector4(0, 0, 0, 1);
            }
        }

        public static Vector4 ConvertTrueColorToVector4(int trueColor)
        {
            double r = ((trueColor >> 16) & 0xFF) / 255; // Extract Red and normalize
            double g = ((trueColor >> 8) & 0xFF) / 255;  // Extract Green and normalize
            double b = (trueColor & 0xFF) / 255;         // Extract Blue and normalize
            double a = 1;  // AutoCAD TrueColor doesn't store alpha, assume fully opaque

            return new Vector4(r, g, b, a);
        }
    }
}