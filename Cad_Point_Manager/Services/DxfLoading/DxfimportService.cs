using Cad_Point_Manager.Models.DxfImport;
using netDxf;
using System.IO;

namespace Cad_Point_Manager.Services.DxfLoading
{
    public static class DxfImportService
    {
        public static DxfImportResult Load(string path)
        {
            return new DxfImportResult
            {
                DxfDocument = DxfDocument.Load(path),
                MLeaders = MleaderParser.Read(path)
            };
        }
    }
}
