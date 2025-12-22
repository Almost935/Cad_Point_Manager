using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Printing
{
    public class PageSize
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public string Name { get; set; }

        public PageSize(int width, int height)
        {
            Width = width;
            Height = height;
            Name = $"{width} by {height}";
        }

        public static PageSize Get36x24 => new PageSize(36, 24);
        public static PageSize Get17x11 => new PageSize(17, 11);
    }
}
