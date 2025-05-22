using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class DxfPointTextVerticesDict
    {
        private const float _dictBaseTextSize = 3.00f;
        private const string _fontName = "Arial";
        private const float _flatteningTolerance = 0.001f;

        private readonly Dictionary<int, (List<TriangleVertex> vertices, float width)> _numbersDict = [];
        private readonly D3dResCache _resCache;
        private TextFormat _textFormat;
        private FontFace _fontFace;
        private Vector4 _defaultColor = new(0.0f, 0.0f, 0.0f, 1.0f);

        public DxfPointTextVerticesDict(D3dResCache resCache)
        {
            _resCache = resCache;
            _textFormat = new TextFormat(_resCache.WriteFactory, _fontName, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, _dictBaseTextSize);
            _fontFace = _resCache.GetFontFace(_fontName, FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
        }

        public TriangleVertex[] GetIntTextVertices(int integer, float textHeight, Vector2 basePoint, Vector4 color)
        {
            List<TriangleVertex> verticesList = [];
            string text = integer.ToString();
            float xOffset = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int num = Int32.Parse(c.ToString());

                if (_numbersDict.TryGetValue(num, out (List<TriangleVertex> vertices, float width) tup))
                {
                    bool colorChangeFlag = color != _defaultColor;
                    List<TriangleVertex> translated = new(tup.vertices.Count);
                    Vector2 offset = new(basePoint.X + xOffset, basePoint.Y);
                    for (int j = 0; j < tup.vertices.Count; j++)
                    {
                        var vertex = tup.vertices[j];
                        if (colorChangeFlag)
                        {
                            vertex = new TriangleVertex(vertex.Position, color, isVisible: 1, isMouseOver: 0, isSelected: 0);
                        }
                        translated.Add(vertex.Translate(offset));
                    }
                    verticesList.AddRange(translated);
                    xOffset += tup.width;
                }
                else
                {
                    TextLayout textLayout = new(_resCache.WriteFactory, c.ToString(), _textFormat, 0.0f, 0.0f);
                    (List<Vector2> coordinates, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(_resCache, textLayout, c.ToString(), textHeight, _fontFace);
                    List<TriangleVertex> newVerticesList = [];
                    List<TriangleVertex> dictVerticesList = [];

                    Vector2 offset = new(basePoint.X + xOffset, basePoint.Y);
                    
                    var spaceWidth = textLayout.Metrics.Width;

                    foreach (var coordinate in coordinates)
                    {
                        Matrix matrix = Matrix.Translation(offset.X, offset.Y, 0);
                        Vector3 dictVector = new(coordinate.X, coordinate.Y, 0.0f);
                        Vector3 vector = Vector3.TransformCoordinate(dictVector, matrix);
                        TriangleVertex vertex = new(vector, _defaultColor);
                        TriangleVertex dictVertex = new(dictVector, _defaultColor);
                        newVerticesList.Add(vertex);
                        dictVerticesList.Add(dictVertex);
                    }
                    verticesList.AddRange(newVerticesList);
                    _numbersDict.Add(num, (dictVerticesList, (bounds.Right - bounds.Left) + (spaceWidth * GlobalHelperProperties._textHeightToSpaceWidthFactor)));
                    xOffset += bounds.Right - bounds.Left;
                    textLayout.Dispose();
                }
            }
            return verticesList.ToArray();
        }
    }
}
