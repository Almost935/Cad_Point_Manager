using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class DxfPointTextVerticesDict
    {
        private const float _dictBaseTextSize = 50.00f;
        private const string _fontName = "Arial";
        private const float _flatteningTolerance = 0.001f;

        private readonly Dictionary<int, (TextVertex[] vertices, float width)> _numbersDict = [];
        private readonly D3dResCache _resCache;
        private TextFormat _textFormat;
        private FontFace _fontFace;
        private Vector4 _defaultColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        public DxfPointTextVerticesDict(D3dResCache resCache)
        {
            _resCache = resCache;
            _textFormat = new TextFormat(_resCache.WriteFactory, _fontName, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, _dictBaseTextSize);
            _fontFace = _resCache.GetFontFace(_fontName, FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
        }

        public TextVertex[] GetIntTextVertices(int integer, float textHeight, Vector2 basePoint)
        {
            List<TextVertex> verticesList = [];
            string text = integer.ToString();
            float xOffset = 0;
            float scale = _dictBaseTextSize / textHeight;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int num = Int32.Parse(c.ToString());

                if (_numbersDict.TryGetValue(num, out (TextVertex[] vertices, float width) tup))
                {
                    Vector2 offset = new(basePoint.X + xOffset, basePoint.Y);
                    for (int j = 0; j < tup.vertices.Length; j++)
                    {
                        tup.vertices[i] = tup.vertices[i].Translate(offset);
                    }
                    verticesList.AddRange(tup.vertices);
                    xOffset += tup.width;
                }
                else
                {
                    TextLayout textLayout = new(_resCache.WriteFactory, c.ToString(), _textFormat, 0.0f, 0.0f);
                    (var geometry, var bounds) = TextRenderingHelpers.CreateTextGeometry(_resCache, c.ToString(), textLayout, scale, textHeight, _fontFace, _flatteningTolerance);
                    var coordinates = TextRenderingHelpers.TessellateGeometry(geometry, _flatteningTolerance);

                    List<TextVertex> newVerticesList = [];
                    foreach (var coordinate in coordinates)
                    {
                        Vector3 vector = Vector3.TransformCoordinate(new Vector3(coordinate.X, coordinate.Y, 0.0f), Matrix.Scaling(1 / scale, 1 / scale, 1));
                        Vector3 translatedVector = new(vector.X + basePoint.X, vector.Y + basePoint.Y, vector.Z);
                        TextVertex vertex = new(vector, _defaultColor);
                        newVerticesList.Add(vertex);
                    }
                    _numbersDict.Add(num, (newVerticesList.ToArray(), bounds.Right - bounds.Left));
                    verticesList.AddRange(newVerticesList);

                    textLayout.Dispose();
                    geometry.Dispose();
                }
            }
            return verticesList.ToArray();
        }
    }
}
