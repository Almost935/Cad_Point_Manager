using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;

namespace Cad_Point_Manager.Services.LayoutExporting
{
    public interface ILayoutPdfVectorExporter
    {
        Task ExportAsync(Layout layout, CadManager cadManager3D, Scene scene, D3dStateController stateController, SceneIdMap ids, ResCache resCache, string outputPath, CancellationToken ct = default);
    }
}
