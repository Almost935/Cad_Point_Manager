using System.IO;

namespace Cad_Point_Manager.Helpers
{
    public static class LastExportPaths
    {
        public static string LastPdfFullPath { get; set; } = "";

        public static string GetInitialDirectoryOrFallback(string fallbackDir)
        {
            // If we previously saved somewhere, start there
            if (!string.IsNullOrWhiteSpace(LastPdfFullPath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(LastPdfFullPath);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        return dir;
                }
                catch { /* ignore */ }
            }

            // Otherwise use caller fallback if valid
            if (!string.IsNullOrWhiteSpace(fallbackDir) && Directory.Exists(fallbackDir))
                return fallbackDir;

            // Finally fall back to Documents
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}
