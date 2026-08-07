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
        public LineType Continuous => _lineTypes.Values.FirstOrDefault(lt => lt.Name == "Continuous") ?? throw new InvalidOperationException("Continuous line type not found in the cache.");
        #endregion

        #region Constructor
        public LineTypeCache(DxfDocument dxfDocument)
        {
            _dxfDocument = dxfDocument;
            BuildCache();
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
        public bool TryGetLineType(netDxf.Tables.Linetype dxfLineType, out LineType lineType)
        {
            return _lineTypes.TryGetValue(dxfLineType.Handle, out lineType);
        }
        public bool TryGetLineTypeNyHandle(string handle, out LineType lineType)
        {
            return _lineTypes.TryGetValue(handle, out lineType);
        }
        public bool TryGetLineTypeByName(string name, out LineType lineType)
        {
            lineType = _lineTypes.Values.FirstOrDefault(lt => lt.Name == name);
            return lineType != null;
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
                });

                patterns.AddRange(lt.Pattern);
            }
        }
        #endregion
    }
}
