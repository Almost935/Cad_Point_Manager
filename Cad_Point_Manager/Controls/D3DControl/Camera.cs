using Cad_Point_Manager.Helpers;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class Camera
    {
        #region Fields
        private const float _zoomFactor = 1.25f;

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
        public Bounds OverallBounds { get; set; } = Bounds.Empty;
        public Bounds CurrentBounds { get; set; } = Bounds.Empty;

        public int CurrentZoomStep { get; set; } = 0;
        public float CurrentZoom => (float)Math.Pow(_zoomFactor, CurrentZoomStep);
        public Rotation CurrentRotation { get; set; } = Rotation.NoRotation;
        public bool IsIn3DView { get; set; } = false;
        #endregion

        #region Constructors
        public Camera(float screenWidth, float screenHeight, Bounds bounds)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            OverallBounds = bounds;
            CurrentBounds = bounds;

            ResetToDefaults();
        }
        #endregion

        #region Methods
        public void UpdateBounds(Bounds bounds)
        {
            OverallBounds = bounds;
            CurrentBounds = bounds;
            ResetToDefaults();
        }
        public void UpdateProjection()
        {
            ProjectionMatrix = Matrix.OrthoOffCenterLH(CurrentBounds.Left, CurrentBounds.Right, CurrentBounds.Bottom, CurrentBounds.Top, 0.1f, 1000f);
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
            _position = new Vector3(ScreenWidth, 0, 100);
            _target = new Vector3(ScreenWidth, 0, 0);
            _up = Vector3.UnitY;
            CurrentZoomStep = 0;
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


        public void Pan(Vector2 startPanPos, Vector2 endPanPos)
        {
            // Convert screen positions to normalized device coordinates (NDC)
            Vector2 ndcCurrent = ScreenToNDC(startPanPos, ScreenWidth, ScreenHeight);
            Vector2 ndcLast = ScreenToNDC(endPanPos, ScreenWidth, ScreenHeight);

            // Unproject the NDC points into world space
            Vector3 worldCurrent = Unproject(ndcCurrent, InverseViewProjectionMatrix);
            Vector3 worldLast = Unproject(ndcLast, InverseViewProjectionMatrix);

            // Calculate the world space delta
            Vector3 worldDelta = (worldCurrent - worldLast);

            CurrentBounds = Bounds.Translate(CurrentBounds, worldDelta.X, -worldDelta.Y);

            UpdateProjection();
            UpdateViewProjection();
        }


        public void Pan(Vector2 distance)
        {
            CurrentBounds = Bounds.Translate(CurrentBounds, distance.X, -distance.Y);

            UpdateProjection();
            UpdateViewProjection();
        }

        public void Zoom(int zoomStepDelta, Vector2 mousePosition)
        {
            // Update zoom step and calculate the scale
            CurrentZoomStep += zoomStepDelta;
            float scale = (float)Math.Pow(_zoomFactor, zoomStepDelta);

            // Convert mouse position to NDC space
            Vector2 initialNDC = Camera.ScreenToNDC(mousePosition, (float)ScreenWidth, (float)ScreenHeight);

            // Unproject NDC to world space for the zoom pivot point
            Vector3 initialWorldPivot3D = Camera.Unproject(initialNDC, InverseViewProjectionMatrix);
            Vector2 initialWorldMousePos = new(initialWorldPivot3D.X, initialWorldPivot3D.Y);

            // Compute new bounds around the pivot
            //Bounds scaledBounds = Bounds.ScaleTo(CurrentBounds, scale, worldMousePos);
            Bounds scaledBounds = Bounds.Scale(CurrentBounds, scale);

            // Correct for bounds centering
            CurrentBounds = scaledBounds;

            // Update matrices
            UpdateProjection();
            UpdateViewProjection();

            Vector2 finalNDC = Camera.ScreenToNDC(mousePosition, (float)ScreenWidth, (float)ScreenHeight);

            // Unproject NDC to world space for the zoom pivot point
            Vector3 finalWorldPivot3D = Camera.Unproject(finalNDC, InverseViewProjectionMatrix);
            Vector2 finalWorldMousePos = new(finalWorldPivot3D.X, finalWorldPivot3D.Y);

            // Calculate the difference in world space
            Vector2 worldDelta = finalWorldMousePos - initialWorldMousePos;
            Pan(worldDelta);
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

        public void FitToScreen2D(Bounds boundingBox, float viewportWidth, float viewportHeight)
        {
            //if (IsIn3DView)
            //{
            //    throw new InvalidOperationException("FitToScreen is only supported in 2D mode.");
            //}

            //// Calculate the bounding box center and size
            //Vector2 boxCenter = new(
            //    boundingBox.Left + boundingBox.Width / 2,
            //    boundingBox.Top + boundingBox.Height / 2
            //);

            //float boxWidth = boundingBox.Width;
            //float boxHeight = boundingBox.Height;

            //// Adjust zoom to fit the bounding box
            //float zoomX = viewportWidth / boxWidth;
            //float zoomY = viewportHeight / boxHeight;

            //CurrentZoom = Math.Min(zoomX, zoomY); // Fit both dimensions

            //// Update position and target to center the view
            //_target = new Vector3(boxCenter.X, boxCenter.Y, 0);
            //_position = new Vector3(boxCenter.X, boxCenter.Y, 100 / CurrentZoom);
        }

        //public void UpdateMouseCoords(Vector2 mousePosition)
        //{
        //    var ndcCoords = ScreenToNDC(mousePosition, ScreenWidth, ScreenHeight);
        //    var vector3MouseCoords = Unproject(ndcCoords, InverseViewProjectionMatrix);
        //    MouseCoords = new(vector3MouseCoords.X, vector3MouseCoords.Y);
        //}
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
