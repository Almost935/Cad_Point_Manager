using Cad_Point_Manager.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class Camera
    {
        private readonly float _rotationSpeed;

        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; } = Vector3.UnitY;
        public float ScreenWidth { get; set; }
        public float ScreenHeight { get; set; }

        public Matrix ViewMatrix => Matrix.LookAtLH(Position, Target, Up);
        public Matrix ProjectionMatrix { get; private set; }

        public Camera(float rotationSpeed, float screenWidth, float screenHeight)
        {
            _rotationSpeed = rotationSpeed;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
        }

        public void SetProjection(float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            ProjectionMatrix = Matrix.PerspectiveFovLH(fov, aspectRatio, nearPlane, farPlane);
        }

        public void PanCamera(Vector2 startPanPos, Vector2 endPanPos, float screenWidth, float screenHeight, Matrix viewProjectionMatrix, Matrix inverseViewProjectionMatrix)
        {
            // Convert screen positions to normalized device coordinates (NDC)
            Vector2 ndcCurrent = MathHelpers.ScreenToNDC(startPanPos, screenWidth, screenHeight);
            Vector2 ndcLast = MathHelpers.ScreenToNDC(endPanPos, screenWidth, screenHeight);

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

        public void ZoomCamera(float zoomAmount, Vector2 mousePosition, Matrix viewProjectionMatrix, Matrix inverseViewProjectionMatrix)
        {
            // Convert the mouse position to NDC
            Vector2 ndcMouse = MathHelpers.ScreenToNDC(mousePosition, ScreenWidth, ScreenHeight);

            // Unproject the mouse NDC position into world space
            Vector3 worldMouse = Unproject(ndcMouse, inverseViewProjectionMatrix);

            // Calculate the zoom direction (from the camera position to the mouse world point)
            Vector3 zoomDirection = Vector3.Normalize(worldMouse - Position);

            // Adjust the camera position and target based on the scroll delta
            Position += zoomDirection * zoomAmount;

            // Optionally adjust the camera target to keep the scene centered
            // This depends on your use case; remove the line below if not desired
            Target += zoomDirection * zoomAmount;
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
