using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Importing;
using Cad_Point_Manager.Models.PointRendering;
using ClosedXML.Excel;
using CsvHelper;
using System.Globalization;
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

        public List<List<string>> ReadCsv(string path)
        {
            var rows = new List<List<string>>();

            using (var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    while (csv.Read())
                    {
                        var row = new List<string>();

                        for (int i = 0; csv.TryGetField(i, out string field); i++)
                        {
                            row.Add(field);
                        }

                        rows.Add(row);
                    }
                }
            }

            return rows;
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

        public List<ColumnAnalysis> AnalyzeColumns(List<List<string>> rows, bool hasHeader)
        {
            int colCount = rows[0].Count;

            var result = new List<ColumnAnalysis>();

            for (int col = 0; col < colCount; col++)
            {
                var dataRows = hasHeader ? rows.Skip(1) : rows;

                var values = dataRows
                    .Select(r => r[col])
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                result.Add(new ColumnAnalysis
                {
                    Index = col,
                    Header = hasHeader ? rows[0][col] : $"Column {col + 1}",
                    DetectedType = values.All(v => int.TryParse(v, out _)) ? ColumnDataType.Integer :
                                   values.All(v => double.TryParse(v, out _)) ? ColumnDataType.Double :
                                   ColumnDataType.String,
                    SampleValues = values.Take(5).ToList()
                });
            }

            return result;
        }

        public bool DetectHeaderRow(List<List<string>> rows)
        {
            var firstRow = rows.First();
            var secondRow = rows.Skip(1).FirstOrDefault();

            if (secondRow == null) return false;

            int headerScore = 0;

            for (int i = 0; i < firstRow.Count; i++)
            {
                bool firstIsNumber = double.TryParse(firstRow[i], out _);
                bool secondIsNumber = double.TryParse(secondRow[i], out _);

                if (!firstIsNumber && secondIsNumber)
                    headerScore++;
            }

            return headerScore >= firstRow.Count / 2;
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

        public CogoPoint CreatePoint(
        List<string> row,
            List<ColumnMapping> mappings,
            PointGroup group,
            CogoPointManager manager)
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

            return cp;
        }

        public (int num, double n, double e, double? elev, string? desc, string? pg) ParseRow(
            List<string> row,
            List<ColumnMapping> mappings)
        {
            int pointNum = 0;
            double northing = 0;
            double easting = 0;
            double? elevation = null;
            string description = "";
            string? pointGroup = null;
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
                        if (double.TryParse(val, out double elev))
                            elevation = elev;
                        break;
                    case CogoFieldType.Description:
                        description = val;
                        break;
                    case CogoFieldType.PointGroup:
                        pointGroup = val;
                        break;
                }
            }
            return (pointNum, northing, easting, elevation, description, pointGroup);
        }

        public string? ValidatePointNumber(int num, CogoPointManager manager)
        {
            if (num <= 0) { return "Point number must be greater than zero."; }
            if (manager.PointExists(num)) { return $"Point number already exists"; }

            return null;
        }

        public string? GetMappedValue(
            List<string> row,
            List<ColumnMapping> mappings,
            CogoFieldType field)
        {
            var mapping = mappings.FirstOrDefault(m => m.AssignedField == field);

            if (mapping == null) { return null; }

            if (mapping.ColumnIndex < 0 || mapping.ColumnIndex >= row.Count) { return null; }

            var value = row[mapping.ColumnIndex];

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
