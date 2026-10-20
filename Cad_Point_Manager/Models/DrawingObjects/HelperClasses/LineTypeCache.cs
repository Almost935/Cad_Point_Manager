using Cad_Point_Manager.Controls.D3DControl;
using ClosedXML.Excel;
using netDxf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects.HelperClasses
{
    public class LineTypeCache
    {
        #region Fields
        private readonly DxfDocument _dxfDocument;
        private readonly Dictionary<string, LineType> _lineTypes = [];
        #endregion

        #region Properties
        public LineType Continuous { get; }
        #endregion

        #region Constructor
        public LineTypeCache(DxfDocument dxfDocument)
        {
            _dxfDocument = dxfDocument;

            BuildCache();

            Continuous = GetLineType(_dxfDocument.Linetypes["Continuous"]);
        }
        #endregion

        #region Methods
        private void BuildCache()
        {
            foreach (var dxfLineType in _dxfDocument.Linetypes)
            {
                if (!_lineTypes.ContainsKey(dxfLineType.Handle))
                {
                    _lineTypes[dxfLineType.Handle] = new LineType(dxfLineType);
                }
            }
        }

        public LineType GetLineType(netDxf.Tables.Linetype dxfLineType)
        {
            if (_lineTypes.TryGetValue(dxfLineType.Handle, out var lineType))
            {
                return lineType;
            }
            else
            {
                // If the line type is not found, create a new one and add it to the cache
                lineType = new(dxfLineType);
                _lineTypes[dxfLineType.Handle] = lineType;
                return lineType;
            }
        }
        public LineType GetLineTypeOrContinuous(netDxf.Tables.Linetype? dxfLineType)
        {
            if (dxfLineType == null || string.IsNullOrEmpty(dxfLineType.Handle))
                return Continuous;

            return GetLineType(dxfLineType);
        }

        public void BuildGpuBuffers(out List<LineTypeInfo> infos, out List<float> patterns)
        {
            infos = [];
            patterns = [];

            foreach (var lt in _lineTypes.Values.OrderBy(x => x.Id))
            {
                infos.Add(new LineTypeInfo
                {
                    FirstPatternIndex = (uint)patterns.Count,
                    PatternCount = (uint)lt.Pattern.Count,
                    PatternLength = lt.PatternLength,
                    DashLength = lt.DashLength,
                });

                patterns.AddRange(lt.Pattern);
            }
        }
        #endregion
    }
}
