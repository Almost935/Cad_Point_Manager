using Cad_Point_Manager.Models.DxfImport;
using netDxf;

namespace Cad_Point_Manager.Services.DxfLoading
{
    public static class DxfImportService
    {
        public static DxfImportResult Load(string path)
        {
            var mleaderData = MleaderParser.Read(path);

            return new DxfImportResult
            {
                DxfDocument = DxfDocument.Load(path),

                MLeaders = mleaderData.MLeaders,

                MLeaderStyles = mleaderData.MLeaderStyles
            };
        }
    }
}
