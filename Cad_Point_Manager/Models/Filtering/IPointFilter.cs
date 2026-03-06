using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Filtering
{
    public interface IPointFilter
    {
        string DisplayText { get; }     // what you show on the chip
        bool IsMatch(CogoPoint p);
    }
}
