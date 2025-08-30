using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class AdvanceWidthCache
    {
        private readonly float[] _advDU; // index by glyphId
        public AdvanceWidthCache(SharpDX.DirectWrite.FontFace face, int maxGlyphId)
        {
            _advDU = new float[maxGlyphId + 1];
            var ids = new short[maxGlyphId + 1];
            for (short i = 0; i <= maxGlyphId; i++) ids[i] = i;
            var gm = face.GetDesignGlyphMetrics(ids, false);
            for (int i = 0; i < gm.Length; i++) _advDU[i] = gm[i].AdvanceWidth;
        }
        public float this[int glyphId] => (glyphId >= 0 && glyphId < _advDU.Length) ? _advDU[glyphId] : 0f;
    }
}
