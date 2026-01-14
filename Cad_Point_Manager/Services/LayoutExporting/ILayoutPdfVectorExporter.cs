using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;

namespace Cad_Point_Manager.Services.LayoutExporting
{
    public interface ILayoutPdfVectorExporter
    {
        Task ExportAsync(Layout layout, CadManager cadManager3D, Scene scene, string outputPath, CancellationToken ct = default);
    }
}
