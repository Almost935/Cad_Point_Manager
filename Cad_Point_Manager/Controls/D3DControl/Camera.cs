using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public Matrix ViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewProjectionMatrix { get; private set; }
        public Matrix InverseViewProjectionMatrix { get; private set; }
        public float CurrentZoom { get; set; } = 1;
        public Bounds InitialBounds { get; set; } = new(-1, 1, 1, -1);
        public float InitialNearPlane { get; set; } = 0.1f;
        public float InitialFarPlane { get; set; } = 100.0f;
        public Bounds Bounds { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }

        public Camera(float rotationSpeed, float screenWidth, float screenHeight)
        {
            _rotationSpeed = rotationSpeed;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            UpdateBounds(InitialBounds);
        }

        public void SetOrthographic()
        {
            ProjectionMatrix = Matrix.OrthoOffCenterLH(Bounds.Left, Bounds.Right, Bounds.Bottom, Bounds.Top, NearPlane, FarPlane);
            UpdateViewProjection();
        }
        public void SetProjection(float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            ProjectionMatrix = Matrix.PerspectiveFovLH(fov, aspectRatio, nearPlane, farPlane);
            UpdateViewProjection();
        }
        public void UpdateView()
        {
            ViewMatrix = Matrix.LookAtLH(Position, Target, Up);
            UpdateViewProjection();
        }
        private void UpdateViewProjection()
        {
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
            InverseViewProjectionMatrix = Matrix.Invert(ViewProjectionMatrix);
        }

        public void PanCamera(Vector2 startPanPos, Vector2 endPanPos, Matrix viewProjectionMatrix, Matrix inverseViewProjectionMatrix)
        {
            // Convert screen positions to normalized device coordinates (NDC)
            Vector2 ndcCurrent = MathHelpers.ScreenToNDC(startPanPos, ScreenWidth, ScreenHeight);
            Vector2 ndcLast = MathHelpers.ScreenToNDC(endPanPos, ScreenWidth, ScreenHeight);

            // Unproject the NDC points into world space
            Vector3 worldCurrent = Unproject(ndcCurrent, inverseViewProjectionMatrix);
            Vector3 worldLast = Unproject(ndcLast, inverseViewProjectionMatrix);

            // Calculate the world space delta
            Vector3 worldDelta = (worldCurrent - worldLast) / CurrentZoom;

            //// Apply the delta to both the camera position and target
            //Position = new(Position.X + worldDelta.X, Position.Y - worldDelta.Y, Position.Z + worldDelta.Z);
            //Target = new(Target.X + worldDelta.X, Target.Y - worldDelta.Y, Target.Z + worldDelta.Z);

            Bounds.Translate(worldDelta.X, -worldDelta.Y);
            SetOrthographic();

            //UpdateView();
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

            UpdateView();
            UpdateViewProjection();
        }

        //public void ZoomCamera(float zoomAmount, Vector2 mousePosition, Matrix viewProjectionMatrix, Matrix inverseViewProjectionMatrix)
        //{
        //    // Convert the mouse position to NDC
        //    Vector2 ndcMouse = MathHelpers.ScreenToNDC(mousePosition, ScreenWidth, ScreenHeight);

        //    // Unproject the mouse NDC position into world space
        //    Vector3 worldMouse = Unproject(ndcMouse, inverseViewProjectionMatrix);

        //    // Calculate the zoom direction (from the camera position to the mouse world point)
        //    Vector3 zoomDirection = Vector3.Normalize(worldMouse - Position);

        //    // Adjust the camera position and target based on the scroll delta
        //    Position += zoomDirection * zoomAmount;

        //    // Optionally adjust the camera target to keep the scene centered
        //    // This depends on your use case; remove the line below if not desired
        //    Target += zoomDirection * zoomAmount;
        //}
        public void ZoomCamera(float zoomAmount, Vector2 mousePosition, float screenWidth, float screenHeight)
        {
            CurrentZoom *= zoomAmount;

            var mouseNDC = MathHelpers.ScreenToNDC(mousePosition, screenWidth, screenHeight);
            var worldMouse = Unproject(mouseNDC, InverseViewProjectionMatrix);
            
            float width = (Bounds.Right - Bounds.Left) / zoomAmount;
            float height = (Bounds.Top - Bounds.Bottom) / zoomAmount;

            float left = worldMouse.X - (mouseNDC.X + 1) / 2.0f * width;
            float right = left + width;
            float bottom = worldMouse.Y - (mouseNDC.Y + 1) / 2.0f * height;
            float top = bottom + height;

            UpdateBounds(left, right, bottom, top);
            
            SetOrthographic();
        }

        public Vector3 Unproject(Vector2 ndc, Matrix inverseViewProjectionMatrix)
        {
            // Create a homogeneous clip space position
            Vector4 clipPos = new(ndc.X, ndc.Y, 0.0f, 1.0f);

            // Transform from clip space to world space
            Vector4 worldPos = Vector4.Transform(clipPos, inverseViewProjectionMatrix);

            // Perform perspective division to get 3D world position
            if (worldPos.W != 0)
            {
                worldPos /= worldPos.W;
            }

            return new Vector3(worldPos.X, worldPos.Y, worldPos.Z);
        }

        public void UpdateBounds(float left, float right, float bottom, float top)
        {
            Bounds = new(left, right, top, bottom);
            
            NearPlane = InitialNearPlane;
            FarPlane = InitialFarPlane;
        }
        public void UpdateBounds(Bounds bounds)
        {
            Bounds = bounds;

            NearPlane = InitialNearPlane;
            FarPlane = InitialFarPlane;
        }
    }
}
