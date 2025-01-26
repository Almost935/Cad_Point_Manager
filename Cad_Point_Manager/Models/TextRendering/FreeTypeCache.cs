using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cad_Point_Manager.Models.DrawingObjects3D;
using FreeTypeSharp;
using SharpDX;
using SharpDX.Direct3D11;
using SkiaSharp;

namespace Cad_Point_Manager.Models.TextRendering
{
    public unsafe class FreeTypeCache() : IDisposable
    {
        #region Fields
        //private Device _device = device;
        private Dictionary<string, FT_FaceRec_> _fontCache = [];
        private FreeTypeLoader _loader = new();
        private bool _disposed = false;

        #endregion

        #region Constructors
        #endregion


        #region Methods
        public FT_GlyphSlotRec_ GetGlyph(FT_FaceRec_ face, char character)
        {
            return _loader.GetGlyph(face, character);
        }

        public bool TryGetFont(string style, int size, out FT_FaceRec_? font)
        {
            string cacheKey = CreateCacheKey(style, size);

            if (_fontCache.TryGetValue(cacheKey, out FT_FaceRec_ value))
            {
                font = value;
                return true;
            }
            else
            {
                font = null;
                return false;
            }
        }

        public FT_FaceRec_ GetFont(string style, int size)
        {
            string cacheKey = CreateCacheKey(style, size);

            if (_fontCache.TryGetValue(cacheKey, out FT_FaceRec_ face))
            {
                return face;
            }
            else
            {
                FT_FaceRec_ newFace = LoadFont(style, size);
                _fontCache[cacheKey] = newFace;
                return newFace;
            }
        }

        // Load a new font face from the font file
        private FT_FaceRec_ LoadFont(string style, int size)
        {
            // Initialize FreeType and load the font (you can add more logic here for style)
            return _loader.GetFontFace(style, size);
        }
        #endregion

        #region Static Methods
        public static string CreateCacheKey(string style, int size)
        {
            return $"{style}_{size}";
        }

        public static SKBitmap ConvertFreetypeBitmapToSKBitmap(FT_Bitmap_ ftBitmap)
        {
            int width = (int)ftBitmap.width;
            int height = (int)ftBitmap.rows;
            int pitch = Math.Abs(ftBitmap.pitch); // Ensure pitch is positive

            // Create an SKBitmap with the same dimensions as the FT_Bitmap
            var skBitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque);

            // Access the FreeType bitmap buffer
            var buffer = ftBitmap.buffer;

            // Copy the buffer data to the SKBitmap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte gray = buffer[y * pitch + x];
                    skBitmap.SetPixel(x, y, new SKColor(gray, gray, gray));
                }
            }

            return skBitmap;
        }

        
        #endregion

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var font in _fontCache.Values)
                    {
                        FT.FT_Done_Face(&font);
                    }
                    // Dispose managed resources
                    _fontCache.Clear();
                    _loader.Dispose();
                }

                // Free unmanaged resources (if any)

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FreeTypeCache()
        {
            Dispose(false);
        }
        #endregion
    }
}
