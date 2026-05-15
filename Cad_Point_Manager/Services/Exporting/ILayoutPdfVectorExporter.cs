using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;
using System.Windows;

namespace Cad_Point_Manager.Services.Exporting
{
    public interface ILayoutPdfVectorExporter
    {
        Task ExportAsync(
            Layout layout, 
            CadManager cadManager3D,
            Scene scene,
            D3dStateController stateController, 
            SceneIdMap ids, ResCache resCache, 
            List<TbPrimitive> templatePrims, 
            string outputPath, 
            CancellationToken ct = default,
            bool openAfterExport = false);
    }
}
