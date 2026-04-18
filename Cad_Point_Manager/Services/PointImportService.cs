using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Importing;
using Cad_Point_Manager.Models.PointRendering;
using ClosedXML.Excel;
using System.IO;

namespace Cad_Point_Manager.Services
{
    public class PointImportService
    {
        public List<List<string>> ReadFile(string path)
        {
            var ext = Path.GetExtension(path).ToLower();

            return ext switch
            {
                ".csv" or ".txt" => ReadCsv(path),
                ".xlsx" or ".xls" => ReadExcel(path),
                _ => throw new Exception("Unsupported file type.")
            };
        }

        private List<List<string>> ReadCsv(string path)
        {
            return File.ReadAllLines(path)
                .Select(l => l.Split(',', '\t').ToList())
                .ToList();
        }

        private List<List<string>> ReadExcel(string path)
        {
            using var wb = new ClosedXML.Excel.XLWorkbook(path);
            var ws = wb.Worksheets.First();

            return ws.RangeUsed()
                     .Rows()
                     .Select(r => r.Cells().Select(c => c.GetString()).ToList())
                     .ToList();
        }

        public List<ColumnAnalysis> AnalyzeColumns(List<List<string>> rows)
        {
            int colCount = rows[0].Count;
            var result = new List<ColumnAnalysis>();

            for (int col = 0; col < colCount; col++)
            {
                var values = rows.Skip(1)
                                 .Select(r => r[col])
                                 .Where(v => !string.IsNullOrWhiteSpace(v))
                                 .ToList();

                bool allInt = values.All(v => int.TryParse(v, out _));
                bool allDouble = values.All(v => double.TryParse(v, out _));

                result.Add(new ColumnAnalysis
                {
                    Index = col,
                    Header = $"Column {col + 1}",
                    IsAllIntegers = allInt,
                    IsAllDoubles = allDouble,
                    DetectedType = allInt ? ColumnDataType.Integer :
                                   allDouble ? ColumnDataType.Double :
                                   ColumnDataType.String,
                    SampleValues = values.Take(5).ToList()
                });
            }

            return result;
        }

        public List<CogoPoint> CreatePoints(
            List<List<string>> rows,
            List<ColumnMapping> mappings,
            PointGroup group,
            CogoPointManager manager)
        {
            var result = new List<CogoPoint>();

            foreach (var row in rows.Skip(1))
            {
                int pointNum = 0;
                double northing = 0;
                double easting = 0;
                double elevation = 0;
                string description = "";

                foreach (var map in mappings)
                {
                    string val = row[map.ColumnIndex];

                    switch (map.AssignedField)
                    {
                        case CogoFieldType.PointNumber:
                            int.TryParse(val, out pointNum);
                            break;
                        case CogoFieldType.Northing:
                            double.TryParse(val, out northing);
                            break;
                        case CogoFieldType.Easting:
                            double.TryParse(val, out easting);
                            break;
                        case CogoFieldType.Elevation:
                            double.TryParse(val, out elevation);
                            break;
                        case CogoFieldType.Description:
                            description = val;
                            break;
                    }
                }

                var cp = new CogoPoint(
                    group,
                    pointNum,
                    new SharpDX.Vector3((float)easting, (float)northing, 0),
                    manager,
                    (float)elevation,
                    description
                );

                result.Add(cp);
            }

            return result;
        }
    }
}
