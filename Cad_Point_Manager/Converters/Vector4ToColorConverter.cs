using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Converters
{
    public class Vector4ToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Vector4 vector)
            {
                return Color.FromScRgb(
                 vector.W,
                 vector.X,
                 vector.Y,
                 vector.Z);
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new Vector4(color.ScR, color.ScG, color.ScB, color.ScA);
            }

            return Binding.DoNothing;
        }
    }
}
