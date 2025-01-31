using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.TextRendering
{
    public class FontCache
    {
        #region Properties
        public Dictionary<string, Font> Fonts { get; set; } = [];
        #endregion

        #region Methods
        public Font GetFont(string fontName, int fontSize)
        {
            if (Fonts.TryGetValue(fontName, out Font font))
            {
                return font;
            }
            else
            {
                FontFamily fontFamily = new(fontName);
                Font newFont = new(fontName, fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                Fonts.Add(fontName, newFont);

                return newFont;
            }
        }
        #endregion
    }
}
