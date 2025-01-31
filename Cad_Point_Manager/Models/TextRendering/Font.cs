using SharpDX;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.TextRendering
{
    public class Font
    {
        #region Fields
        private FreeTypeLoader _freeTypeLoader = new();
        #endregion

        #region Properties
        public string FontName { get; set; }
        public FontFamily FontFamily { get; set; }
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }
        public FontStretch FontStretch { get; set; }
        public Typeface Typeface { get; set; }
        public GlyphTypeface GlyphTypeface { get; set; }
        public Dictionary<char, List<Vector3>> Characters { get; set; } = [];
        #endregion

        #region Constructors
        public Font(string fontName, FontFamily fontFamily, FontStyle fontstyle, FontWeight fontweight, FontStretch fontStretch)
        {
            FontName = fontName;
            FontFamily = fontFamily;
            FontStyle = fontstyle;
            FontWeight = fontweight;
            FontStretch = fontStretch;

            LoadTypeFaces();
        }
        #endregion

        #region Methods
        public void LoadTypeFaces()
        {
            Typeface = new(FontFamily, FontStyle, FontWeight, FontStretch, new FontFamily("Arial"));

            GlyphTypeface glyphTypeFace = new();
            bool glyphTypeFaceSet = Typeface.TryGetGlyphTypeface(out glyphTypeFace);

            if (glyphTypeFaceSet)
            {
                GlyphTypeface = glyphTypeFace;
            }
            else
            {
                throw new Exception("Error loading GlyphTypeface");
            }
        }

        public List<Vector3> GetChar(char c)
        {
            if (Characters.TryGetValue(c, out var vectors))
            {
                return vectors;
            }
            else
            {
                var vertices = GetCharacterVertices(GlyphTypeface, c);
                Characters.Add(c, vertices);

                return vertices;
            }
        }
        #endregion

        #region StaticMethods
        public static List<Vector3> GetCharacterVertices(GlyphTypeface glyphTypeface, char character)
        {
            var vertices = new List<Vector3>();

            Debug.WriteLine($"\ncharacter: {character}");

            if (char.IsWhiteSpace(character))
            {
                return vertices;
            }

            int glyphIndex = glyphTypeface.CharacterToGlyphMap[character];
            Geometry geometry = glyphTypeface.GetGlyphOutline((ushort)glyphIndex, 1, 1);

            if (geometry != null)
            {
                PathGeometry pathGeometry = geometry.GetOutlinedPathGeometry();

                foreach (PathFigure figure in pathGeometry.Figures)
                {
                    foreach (PathSegment segment in figure.Segments)
                    {
                        if (segment is PolyLineSegment polyLineSegment)
                        {
                            foreach (Point point in polyLineSegment.Points)
                            {
                                vertices.Add(new Vector3((float)point.X, (float)point.Y, 0));
                            }
                        }
                        if (segment is LineSegment lineSegment)
                        {

                        }
                        if (segment is BezierSegment bezierSegment)
                        {
                           
                        }
                    }
                }
            }
            return vertices;
        }
        #endregion
    }
}
