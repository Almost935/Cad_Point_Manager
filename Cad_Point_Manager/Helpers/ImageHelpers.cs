using System.IO;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public class ImageHelpers
    {
        public static byte[] LoadPackImage(string packUri)
        {
            var uri = new Uri(packUri, UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            if (info == null) { throw new InvalidOperationException($"Resource not found: {packUri}"); }

            using var s = info.Stream;
            using var ms = new MemoryStream();
            s.CopyTo(ms);

            return ms.ToArray();
        }

    }
}
