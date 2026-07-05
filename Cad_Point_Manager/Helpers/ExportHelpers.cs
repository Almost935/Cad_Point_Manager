using Cad_Point_Manager.Models.PointRendering;
using System.IO;
using System.Text;

namespace Cad_Point_Manager.Helpers
{
    public static class ExportHelpers
    {
        public static void ExportPointsToCsv(string path, IEnumerable<CogoPoint> points)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("PointNumber,Northing,Easting,Elevation,Description,PointGroup");

            foreach (var p in points)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(p.PointNumber.ToString()),
                    EscapeCsv(p.Northing.ToString("N3")),
                    EscapeCsv(p.Easting.ToString("N3")),
                    EscapeCsv(p.Elevation.ToString("N3")),
                    EscapeCsv(p.Description),
                    EscapeCsv(p.PointGroup?.Name ?? string.Empty)));
            }
            
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            bool mustQuote = value.Contains(',') ||
                             value.Contains('"') ||
                             value.Contains('\n') ||
                             value.Contains('\r');

            if (value.Contains('"'))
                value = value.Replace("\"", "\"\"");

            return mustQuote ? $"\"{value}\"" : value;
        }
    }
}
