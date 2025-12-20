using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.Printing
{
    public class LayoutViewport
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid SceneId { get; set; }
        public Rect LocalRectIn { get; set; }
        public bool ShowBorder { get; set; } = true;
    }
}
