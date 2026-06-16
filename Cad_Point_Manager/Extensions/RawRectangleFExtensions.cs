using SharpDX;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Extensions
{
    public static class RawRectangleFExtensions
    {
        public static Rect ToRect(this RawRectangleF rectF)
        {
            return new Rect(rectF.Left, rectF.Top, rectF.Right - rectF.Left, rectF.Bottom - rectF.Top);
        }

        public static RectangleF ToRectangleF(this RawRectangleF rect)
        {
            return new RectangleF(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
    }
}
