using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPointTextVerticesDict
    {
        private const float _dictBaseTextSize = 10.00f;
        private const string _fontName = "Arial";

        private readonly Dictionary<int, (List<TextVertex> vertices, float width)> _numbersDict = [];
        private readonly Dictionary<char, (List<TextVertex> vertices, float width)> _charsDict = [];
        private readonly D3dResCache _resCache;
        private TextFormat _textFormat;
        private FontFace _fontFace;
        private Vector4 _defaultColor = new(0.0f, 0.0f, 0.0f, 1.0f);

        public CogoPointTextVerticesDict(D3dResCache resCache)
        {
            _resCache = resCache;
            _textFormat = new TextFormat(_resCache.WriteFactory, _fontName, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, _dictBaseTextSize);
            _fontFace = _resCache.GetFontFace(_fontName, FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
        }

        public TextVertex[] GetTextVertices(string text, float textHeight, Vector2 basePoint, Vector4 color)
        {
            List<TextVertex> verticesList = [];
            float xOffset = 0;
            bool colorChangeFlag = color != _defaultColor;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                Vector2 offset = new(basePoint.X + xOffset, basePoint.Y);
                float heightFactor = textHeight / _dictBaseTextSize;
                Matrix matrix = Matrix.Scaling(heightFactor, heightFactor, 1.0f) * Matrix.Translation(offset.X, offset.Y, 0);

                if (_charsDict.TryGetValue(c, out (List<TextVertex> vertices, float width) tup))
                {
                    List<TextVertex> translated = new(tup.vertices.Count);

                    for (int j = 0; j < tup.vertices.Count; j++)
                    {
                        TextVertex vertex = tup.vertices[j];
                        vertex.Transform(matrix);

                        if (colorChangeFlag)
                        {
                            vertex.Color = color;
                        }
                        translated.Add(vertex);
                    }
                    verticesList.AddRange(translated);
                    xOffset += (tup.width * heightFactor);
                }
                else
                {
                    TextLayout textLayout = new(_resCache.WriteFactory, c.ToString(), _textFormat, 0.0f, 0.0f);
                    (List<Vector2> coordinates, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(_resCache, textLayout, c.ToString(), _fontFace);
                    List<TextVertex> newVerticesList = [];
                    List<TextVertex> dictVerticesList = [];

                    var spaceWidth = textLayout.Metrics.Width;

                    foreach (var coordinate in coordinates)
                    {
                        Vector3 dictVector = new(coordinate.X, coordinate.Y, 0.0f);
                        Vector3 vector = Vector3.TransformCoordinate(dictVector, matrix);
                        TextVertex vertex = new(vector, color);
                        TextVertex dictVertex = new(dictVector, _defaultColor);
                        newVerticesList.Add(vertex);
                        dictVerticesList.Add(dictVertex);
                    }
                    _charsDict.Add(c, (dictVerticesList, (bounds.Right - bounds.Left) + (spaceWidth * GlobalHelperProperties._textHeightToSpaceWidthFactor)));

                    verticesList.AddRange(newVerticesList);

                    xOffset += bounds.Right - bounds.Left;
                    textLayout.Dispose();
                }
            }
            return verticesList.ToArray();
        }

        public TextVertex[] GetIntTextVertices(int integer, float textHeight, Vector2 basePoint, Vector4 color)
        {
            List<TextVertex> verticesList = [];
            string text = integer.ToString();
            float xOffset = 0;
            bool colorChangeFlag = color != _defaultColor;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int num = Int32.Parse(c.ToString());

                Vector2 offset = new(basePoint.X + xOffset, basePoint.Y);
                float heightFactor = textHeight / _dictBaseTextSize;
                Matrix matrix = Matrix.Scaling(heightFactor, heightFactor, 1.0f) * Matrix.Translation(offset.X, offset.Y, 0);

                if (_numbersDict.TryGetValue(num, out (List<TextVertex> vertices, float width) tup))
                {
                    List<TextVertex> translated = new(tup.vertices.Count);

                    for (int j = 0; j < tup.vertices.Count; j++)
                    {
                        TextVertex vertex = tup.vertices[j];
                        vertex.Transform(matrix);

                        if (colorChangeFlag)
                        {
                            vertex.Color = color;
                        }
                        translated.Add(vertex);
                    }
                    verticesList.AddRange(translated);
                    xOffset += (tup.width * heightFactor);
                }
                else
                {
                    TextLayout textLayout = new(_resCache.WriteFactory, c.ToString(), _textFormat, 0.0f, 0.0f);
                    (List<Vector2> coordinates, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(_resCache, textLayout, c.ToString(), _fontFace);
                    List<TextVertex> newVerticesList = [];
                    List<TextVertex> dictVerticesList = [];
                    
                    var spaceWidth = textLayout.Metrics.Width;

                    foreach (var coordinate in coordinates)
                    {
                        Vector3 dictVector = new(coordinate.X, coordinate.Y, 0.0f);
                        Vector3 vector = Vector3.TransformCoordinate(dictVector, matrix);
                        TextVertex vertex = new(vector, color);
                        TextVertex dictVertex = new(dictVector, _defaultColor);
                        newVerticesList.Add(vertex);
                        dictVerticesList.Add(dictVertex);
                    }
                    _numbersDict.Add(num, (dictVerticesList, (bounds.Right - bounds.Left) + (spaceWidth * GlobalHelperProperties._textHeightToSpaceWidthFactor)));

                    verticesList.AddRange(newVerticesList);

                    xOffset += bounds.Right - bounds.Left;
                    textLayout.Dispose();
                }
            }
            return verticesList.ToArray();
        }
    }
}
