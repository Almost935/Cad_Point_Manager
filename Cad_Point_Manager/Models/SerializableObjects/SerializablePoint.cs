using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.SerializableObjects
{
    public class SerializablePoint
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        public SerializablePoint() { }

        public SerializablePoint(Point point)
        {
            X = point.X;
            Y = point.Y;
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }
    }
}
