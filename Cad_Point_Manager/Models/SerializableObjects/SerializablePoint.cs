using System.Text.Json.Serialization;
using System.Windows;

namespace Cad_Point_Manager.Models.SerializableObjects
{
    [Serializable]
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
        public SerializablePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }
    }
}
