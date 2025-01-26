using System;
using System.IO;
using SixLabors.Fonts;
using System.Runtime.InteropServices;
using System.Globalization;
using FreeTypeSharp;
using SharpDX.Direct3D11;
using SkiaSharp;
using Buffer = System.Buffer;

namespace Cad_Point_Manager.Models.TextRendering
{
    public unsafe class FreeTypeLoader : IDisposable
    {
        #region Fields
        private FreeTypeLibrary _library = new();
        private bool _disposed = false;
        #endregion

        #region Methods
        public FT_FaceRec_ GetFontFace(string style, int size)
        {
            var font = GetSystemFont(style);
            FT_FaceRec_* face;

            // Load the font face from the file
            FT_Error error = FT.FT_New_Face(_library.Native, (byte*)Marshal.StringToHGlobalAnsi(font), 0, &face);

            if (error != FT_Error.FT_Err_Ok)
            {
                throw new Exception("Error loading font face");
            }

            error = FT.FT_Set_Char_Size(face, 0, size * 64, 96, 96);

            if (error != FT_Error.FT_Err_Ok)
            {
                throw new Exception("Error setting font size.");
            }

            return *face;
        }

        public FT_GlyphSlotRec_ GetGlyph(FT_FaceRec_ face, char character)
        {
            uint glyphIndex = FT.FT_Get_Char_Index(&face, (uint)character);

            // Load the glyph for the character (ensure we're passing the pointer to FT_Face)
            FT_Error error = FT.FT_Load_Glyph(&face, glyphIndex, FT_LOAD.FT_LOAD_DEFAULT);
            if (error != FT_Error.FT_Err_Ok)
            {
                throw new Exception("Error loading glyph for character: " + character);
            }
            
            // Render the glyph into a bitmap (normal rendering mode)
            error = FT.FT_Render_Glyph(face.glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);
            if (error != FT_Error.FT_Err_Ok)
            {
                throw new Exception("Error rendering glyph for character: " + character);
            }

            var glyphPointer = face.glyph;
            FT_GlyphSlotRec_ glyph = *glyphPointer;

            return glyph;
        }

        //public Texture2D GenerateTextureAtlas(Device device)
        //{
        //    int atlasWidth = 1024;
        //    int atlasHeight = 1024;
        //    var atlasTexture = new Texture2D(device, atlasWidth, atlasHeight);

        //    int x = 0;
        //    int y = 0;
        //    int maxHeight = 0;

        //    // Store the glyphs in a list for later use
        //    List<Glyph> glyphs = [];

        //    for (int i = 32; i < 128; i++)  // Render ASCII characters
        //    {
        //        FT_Error error = FT_Load_Char(_face, i, FT_Load_Flags.FT_LOAD_RENDER);
        //        if (error != FT_Error.FT_Err_Ok)
        //            continue;

        //        var glyph = new Glyph
        //        {
        //            Character = (char)i,
        //            Bitmap = _face.glyph.bitmap,
        //            BitmapLeft = _face.glyph.bitmap_left,
        //            BitmapTop = _face.glyph.bitmap_top
        //        };

        //        glyphs.Add(glyph);

        //        // Pack this glyph into the texture atlas
        //        if (x + glyph.Bitmap.width >= atlasWidth)
        //        {
        //            x = 0;
        //            y += maxHeight;
        //            maxHeight = 0;
        //        }

        //        if (glyph.Bitmap.height > maxHeight)
        //        {
        //            maxHeight = glyph.Bitmap.height;
        //        }

        //        // Copy the bitmap to the atlas texture at the appropriate (x, y) position
        //        CopyGlyphToAtlas(glyph, atlasTexture, x, y);
        //        x += glyph.Bitmap.width;
        //    }

        //    return atlasTexture;
        //}
        #endregion

        #region Static Methods
        public static string? GetSystemFont(string fontName)
        {
            FontFamily search = new();
            bool found = false;
            foreach (var fontFamily in SystemFonts.Families)
            {
                if (fontFamily.Name == fontName)
                {
                    search = fontFamily;
                    found = true;
                }
                else
                {
                    if (fontFamily.TryGetMetrics(FontStyle.Regular, out var metrics))
                    {
                        if (metrics.Description.FontName(CultureInfo.CurrentCulture) == fontName)
                        {
                            search = fontFamily;
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                var font = search.CreateFont(0, FontStyle.Regular);
                if (font.TryGetPath(out var path))
                    return path;
            }

            return null;
        }
        #endregion

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _library?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FreeTypeLoader()
        {
            Dispose(false);
        }
        #endregion
    }
}
