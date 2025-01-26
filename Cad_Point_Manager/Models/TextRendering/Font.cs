using Cad_Point_Manager.Controls.D3DControl;
using System;
using System.Collections.Generic;
using SixLabors.Fonts;
using SharpDX;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.TextRendering
{
    public class Font
    {
        #region Fields
        private FreeTypeLoader _freeTypeLoader = new();
        #endregion

        #region Properties
        public FontFamily FontFamily { get; set; }
        public string FontName { get; set; }
        public int FontSize { get; set; }
        public GlyphTypeface GlyphTypeface { get; set; }
        public Dictionary<char, List<Vector3>> Characters { get; set; } = [];
        #endregion

        #region Constructors
        public Font(FontFamily fontFamily, int fontSize)
        {
            FontFamily = fontFamily;
            FontName = FontFamily.Name;
            FontSize = fontSize;
        }
        #endregion

        #region Methods
        public unsafe void LoadChar(char c)
        {
            if (!Characters.ContainsKey(c))
            { 
                var glyph = _freeTypeLoader.GetGlyph(_face, c);                
                Characters.Add(c, GenerateVerticesFromOutline(glyph));
            }
        }

        private unsafe List<Vector3> GenerateVerticesFromOutline(FT_GlyphSlotRec_ glyph)
        {
            var vertices = new List<Vector3>();

            //// Iterate through each contour in the outline
            //for (int i = 0; i < glyph.outline.n_contours; i++)
            //{
            //    var contour = glyph.outline.contours; // Assuming Contours is an array or list

            //    // Iterate through each point in the contour
            //    for (int j = 0; j < contour->; j++)
            //    {
            //        var point = contour[j]; // Assuming Points is accessible as an array

            //        // Add the point to the vertices list
            //        vertices.Add(new TextVertex(
            //            new Vector3(point.X, point.Y, 0),  // Position of the vertex
            //            color,                            // Color of the glyph
            //            new Vector3(0, 0, 0),             // TextCoord (UV mapping, can be set if required)
            //            isVisible,                        // IsVisible value
            //            rotationMatrix                   // Optional rotation matrix
            //        ));
            //    }
            //}

            return vertices;
        }
        #endregion
    }
}
