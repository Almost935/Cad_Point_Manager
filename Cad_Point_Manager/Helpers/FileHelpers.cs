using System.IO;

namespace Cad_Point_Manager.Helpers
{
    public static class FileHelpers
    {
        public static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return "Untitled"; }

            foreach (var c in Path.GetInvalidFileNameChars()) { name = name.Replace(c, '_'); }

            return name.Trim();
        }
    }
}
