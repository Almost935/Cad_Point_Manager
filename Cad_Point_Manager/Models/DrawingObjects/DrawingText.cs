using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;

using Matrix = System.Windows.Media.Matrix;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingText : DrawingObject
    {
        #region Properties
        public List<TextVertex> TextVertices { get; set; } = [];
        public List<LineInstance> LineInstances { get; set; } = [];
        public TextRenderStyle TextRenderStyle { get; set; } = TextRenderStyle.Triangle;

        public string Text { get; set; }
        public double TextHeight { get; set; }
        public double MaxWidth { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }
        public Vector2d Position { get; set; }
        public double Rotation { get; set; } = 0;
        public TextAttachmentPoint AttachmentPoint { get; set; }
        public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
        public Vector2d AttachmentOffset { get; set; } = Vector2d.Zero;
        public double TextHeightScaleFactor { get; set; } = 1.0f;

        public Matrix LocalTranslationTransform => new Matrix(1, 0, 0, 1, AttachmentOffset.X, AttachmentOffset.Y);
        public Matrix WorldTranslationTransform => new(1, 0, 0, 1, Position.X, Position.Y);
        public Matrix RotationTransform
        {
            get
            {
                Matrix m = Matrix.Identity;
                m.Rotate(Rotation);
                return m;
            }
        }
        public Matrix ScaleTransform => new(TextHeightScaleFactor, 0, 0, TextHeightScaleFactor, 0, 0);
        public Matrix Transform =>
            LocalTranslationTransform *
            ScaleTransform *
            RotationTransform *
            WorldTranslationTransform;
        #endregion

        #region Methods
        public abstract void UpdateVertices(
            ResCache d3DResCache, uint layerId, uint lineTypeId, SceneIdMap sceneIdMap, 
            D3dStateBuffers stateBuffers, Vector2d renderOrigin);
        #endregion
    }
}
