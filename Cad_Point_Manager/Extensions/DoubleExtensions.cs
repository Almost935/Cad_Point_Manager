using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Extensions
{
    public static class DoubleExtensions
    {
        public static float ToFloat(this Double d)
        {
            return (float)d;
        }
    }
}
