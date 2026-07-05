using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Models.Filtering
{
    public interface IPointFilter
    {
        string DisplayText { get; }     // what you show on the chip
        bool IsMatch(CogoPoint p);
    }
}
