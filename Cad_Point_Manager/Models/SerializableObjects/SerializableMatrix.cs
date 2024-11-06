using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.SerializableObjects
{
    public class SerializableMatrix
    {
        [JsonPropertyName("m11")]
        public double M11 { get; set; }

        [JsonPropertyName("m12")]
        public double M12 { get; set; }

        [JsonPropertyName("m21")]
        public double M21 { get; set; }

        [JsonPropertyName("m22")]
        public double M22 { get; set; }

        [JsonPropertyName("offsetX")]
        public double OffsetX { get; set; }

        [JsonPropertyName("offsetY")]
        public double OffsetY { get; set; }

        public SerializableMatrix() { }

        public SerializableMatrix(Matrix matrix)
        {
            M11 = matrix.M11;
            M12 = matrix.M12;
            M21 = matrix.M21;
            M22 = matrix.M22;
            OffsetX = matrix.OffsetX;
            OffsetY = matrix.OffsetY;
        }

        public Matrix ToMatrix()
        {
            return new Matrix(M11, M12, M21, M22, OffsetX, OffsetY);
        }
    }
}
