using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class Camera
    {
        private readonly float _zoomSpeed = 0.1f;
        private readonly float _rotationSpeed = 0.005f;

        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; } = Vector3.UnitY;

        public Matrix ViewMatrix => Matrix.LookAtLH(Position, Target, Up);
        public Matrix ProjectionMatrix { get; private set; }

        public Camera(float zoomSpeed, float rotationSpeed)
        {
            _zoomSpeed = zoomSpeed;
            _rotationSpeed = rotationSpeed;
        }

        public void SetProjection(float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            ProjectionMatrix = Matrix.PerspectiveFovLH(fov, aspectRatio, nearPlane, farPlane);
        }

        public void PanCamera(Vector2 dis, Matrix viewProjectionMatrix, Matrix inverseViewProjectionMatrix)
        {
            // Convert screen positions to normalized device coordinates (NDC)
            Vector2 ndcCurrent = ScreenToNDC(currentMousePosition, screenWidth, screenHeight);
            Vector2 ndcLast = ScreenToNDC(_lastMousePosition, screenWidth, screenHeight);

            // Compute the delta in NDC
            Vector2 ndcDelta = ndcCurrent - ndcLast;

            // Unproject the NDC points into world space
            Vector3 worldCurrent = Unproject(ndcCurrent, inverseViewProjectionMatrix);
            Vector3 worldLast = Unproject(ndcLast, inverseViewProjectionMatrix);

            // Calculate the world space delta
            Vector3 worldDelta = worldCurrent - worldLast;

            // Apply the delta to both the camera position and target
            Position -= worldDelta;
            Target -= worldDelta;
        }

        public void RotateCamera(Vector2 delta)
        {
            var targetDirection = Target - Position;

            // Horizontal rotation (around the Y-axis)
            var horizontalRotation = Matrix.RotationY(delta.X * _rotationSpeed);
            targetDirection = Vector3.TransformCoordinate(targetDirection, horizontalRotation);

            // Vertical rotation (around the right vector)
            var right = Vector3.Cross(Up, targetDirection);
            var verticalRotation = Matrix.RotationAxis(right, delta.Y * _rotationSpeed);
            targetDirection = Vector3.TransformCoordinate(targetDirection, verticalRotation);

            Target = Position + targetDirection;
        }

        public void ZoomCamera(float scrollDelta)
        {
            var direction = Vector3.Normalize(Target - Position);
            Position += direction * scrollDelta * _zoomSpeed;
        }

        public Vector3 Unproject(Vector2 ndc, Matrix inverseViewProjectionMatrix)
        {
            // Create a homogeneous clip space position
            Vector4 clipPos = new Vector4(ndc.X, ndc.Y, 0.0f, 1.0f);

            // Transform from clip space to world space
            Vector4 worldPos = Vector4.Transform(clipPos, inverseViewProjectionMatrix);

            // Perform perspective division to get 3D world position
            if (worldPos.W != 0)
            {
                worldPos /= worldPos.W;
            }

            return new Vector3(worldPos.X, worldPos.Y, worldPos.Z);
        }
    }
}
