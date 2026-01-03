using Cad_Point_Manager.Models.DrawingObjects3D;

namespace Cad_Point_Manager.Services
{
    public interface ISelectionConnectivityService
    {
        List<ChainPath> BuildChainsFromSelection(IEnumerable<DrawingObject> selected, double eps);
    }
}
