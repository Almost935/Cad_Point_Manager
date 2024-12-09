using Cad_Point_Manager.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class TestCamera
    {
        #region Fields
        private Vector3 _position;   // Camera position
        private Vector3 _target;     // Camera target
        private Vector3 _up;         // Up direction
        #endregion

        #region Properties
        public Matrix ViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewProjectionMatrix { get; private set; }
        public Matrix InverseViewProjectionMatrix { get; private set; }

        public float ScreenWidth { get; set; }
        public float ScreenHeight { get; set; }
        public Bounds Bounds { get; set; } = Bounds.Empty;

        public float CurrentZoom { get; set; } = 1;
        public Rotation CurrentRotation { get; set; } = Rotation.NoRotation;
        public bool IsIn3DView { get; set; } = false;
        #endregion

        #region Constructors
        public TestCamera(float screenWidth, float screenHeight)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            ResetToDefaults();
        }
        #endregion

        #region Methods
        public void UpdateProjection()
        {
            if (IsIn3DView)
            {
                ProjectionMatrix = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, ScreenWidth / ScreenHeight, 0.1f, 1000f);
            }
            else
            {
                ProjectionMatrix = Matrix.OrthoLH(ScreenWidth, ScreenHeight, 0.1f, 1000f);
            }
        }
        public void UpdateView()
        {
            ViewMatrix = Matrix.LookAtLH(_position, _target, _up);
        }
        private void UpdateViewProjection()
        {
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
            InverseViewProjectionMatrix = Matrix.Invert(ViewProjectionMatrix);
        }
        public void ResetToDefaults()
        {
            _position = new Vector3(0, 0, 100);
            _target = new Vector3(0, 0, 0);
            _up = Vector3.UnitY;
            CurrentZoom = 1.0f;
            CurrentRotation.SetX(0);
            CurrentRotation.SetY(0);
            CurrentRotation.SetZ(0);
            IsIn3DView = false;

            UpdateProjection();
            UpdateView();
            UpdateViewProjection();
        }

        public void Toggle3DView(bool enable)
        {
            IsIn3DView = enable;

            if (IsIn3DView)
            {
                _position = new Vector3(0, 50, 50); // Position camera for a 3D view
                _target = new Vector3(0, 0, 0);
                _up = Vector3.UnitY;
            }
            else
            {
                ResetToDefaults();
            }
        }

        public void Pan(float deltaX, float deltaY)
        {
            Vector3 panDirection = IsIn3DView ? new Vector3(-deltaX, deltaY, 0) : new Vector3(deltaX, deltaY, 0);
            //position += panDirection * zoomFactor;
            //target += panDirection * zoomFactor;
            _position += panDirection;
            _target += panDirection;
        }

        public void Zoom(float zoom, Vector2 mousePosition)
        { 
            var originalPos = _position;

            // Update zoom factor
            CurrentZoom *= zoom;

            // Adjust position and target for zoom-to-mouse
            Vector3 mouseWorldPosition = ScreenToWorld(mousePosition);
            Vector3 zoomDirection = mouseWorldPosition - _position;
            _position += zoomDirection * zoom;
        }

        public void Rotate(float deltaX, float deltaY, bool shiftHeld)
        {
            if (!IsIn3DView || !shiftHeld) return;

            CurrentRotation.SetX(deltaY * 0.01f);
            CurrentRotation.SetX(deltaX * 0.01f);

            // Apply rotations around the target
            Matrix rotationMatrix = Matrix.RotationYawPitchRoll(CurrentRotation.Y, CurrentRotation.X, 0);
            Vector3 direction = Vector3.Normalize(_position - _target);
            direction = Vector3.TransformNormal(direction, rotationMatrix);

            _position = _target + direction * (_position - _target).Length();
        }

        public Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            // Assume an orthographic projection for 2D and near plane for picking in 3D
            if (!IsIn3DView)
            {
                return new Vector3(screenPosition.X, screenPosition.Y, 0) * CurrentZoom + _target;
            }
            else
            {
                // Placeholder: Implement screen-to-world conversion for 3D
                return Vector3.Zero;
            }
        }

        public RectangleF GetViewBounds(float viewportWidth, float viewportHeight)
        {
            // Calculate half-dimensions of the view in world space
            float halfWidth = (viewportWidth / 2) * CurrentZoom;
            float halfHeight = (viewportHeight / 2) * CurrentZoom;

            // Determine bounds in world space
            float left = _position.X - halfWidth;
            float right = _position.X + halfWidth;
            float bottom = _position.Y - halfHeight;
            float top = _position.Y + halfHeight;

            return new RectangleF(left, bottom, right - left, top - bottom);
        }

        public void FitToScreen2D(Bounds boundingBox, float viewportWidth, float viewportHeight)
        {
            if (IsIn3DView)
            {
                throw new InvalidOperationException("FitToScreen is only supported in 2D mode.");
            }

            // Calculate the bounding box center and size
            Vector2 boxCenter = new(
                boundingBox.Left + boundingBox.Width / 2,
                boundingBox.Top + boundingBox.Height / 2
            );

            float boxWidth = boundingBox.Width;
            float boxHeight = boundingBox.Height;

            // Adjust zoom to fit the bounding box
            float zoomX = viewportWidth / boxWidth;
            float zoomY = viewportHeight / boxHeight;

            CurrentZoom = Math.Min(zoomX, zoomY); // Fit both dimensions

            // Update position and target to center the view
            _target = new Vector3(boxCenter.X, boxCenter.Y, 0);
            _position = new Vector3(boxCenter.X, boxCenter.Y, 100 / CurrentZoom);
        }
        #endregion


        #region Static Methods
        public static Vector2 ScreenToNDC(Vector2 screenPos, float screenWidth, float screenHeight)
        {
            return new Vector2(
                (screenPos.X / screenWidth) * 2.0f - 1.0f, // Map x from [0, screenWidth] to [-1, 1]
                1.0f - (screenPos.Y / screenHeight) * 2.0f  // Map y from [0, screenHeight] to [1, -1]
            );
        }

        public static Vector3 Unproject(Vector2 ndc, Matrix inverseViewProjectionMatrix)
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
        #endregion
    }
}
