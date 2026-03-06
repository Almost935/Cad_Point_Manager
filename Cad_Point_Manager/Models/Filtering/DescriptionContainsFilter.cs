using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Filtering
{
    public sealed class DescriptionContainsFilter(string text) : PointFilterBase
    {
        public string Text { get; } = text ?? "";

        public override string DisplayText => $"Desc contains \"{Text}\"";

        public override bool IsMatch(CogoPoint p)
        {
            if (Text.Length == 0) { return true; }
            return p.Description.Contains(Text);
        }
    }
}
