using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using SharpDX;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingText : DrawingObject
    {
        #region Properties
        public List<TextVertex> TextVertices { get; set; } = [];
        public List<LineInstance> LineInstances { get; set; } = [];
        public TextRenderStyle TextRenderStyle { get; set; } = TextRenderStyle.Triangle;

        public string Text { get; set; }
        public float TextHeight { get; set; }
        public float MaxWidth { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; } = 0;
        public TextAttachmentPoint AttachmentPoint { get; set; }
        public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
        public Vector2 AttachmentOffset { get; set; } = new Vector2(0, 0);
        public float TextHeightScaleFactor { get; set; } = 1.0f;

        public Matrix LocalTranslationTransform => Matrix.Translation(AttachmentOffset.X, AttachmentOffset.Y, 0);
        public Matrix WorldTranslationTransform => Matrix.Translation(Position);
        public Matrix RotationTransform => Matrix.RotationZ(MathUtil.DegreesToRadians(Rotation));
        public Matrix ScaleTransform => Matrix.Scaling(TextHeightScaleFactor, TextHeightScaleFactor, 1.0f);
        public Matrix Transform => LocalTranslationTransform * ScaleTransform * RotationTransform * WorldTranslationTransform;
        #endregion

        #region Methods
        public abstract void UpdateVertices(ResCache d3DResCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers);
        #endregion
    }
}
