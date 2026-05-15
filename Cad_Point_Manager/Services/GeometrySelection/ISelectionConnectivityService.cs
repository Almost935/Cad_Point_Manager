using Cad_Point_Manager.Models.DrawingObjects;

namespace Cad_Point_Manager.Services.GeometrySelection
{
    public interface ISelectionConnectivityService
    {
        List<ChainPath> BuildChainsFromSelection(IEnumerable<DrawingObject> selected, double eps);
    }
}
