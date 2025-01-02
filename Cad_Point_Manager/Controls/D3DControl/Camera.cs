using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.Mathematics.Interop;
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
        private readonly float _zoomFactor;

        private Matrix _scaledInitialViewMatrix = Matrix.Identity;
        private Matrix _scaledViewMatrix = Matrix.Identity;
        #endregion

        #region Properties
        public Matrix InitialViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix InverseViewProjectionMatrix { get; private set; } = Matrix.Identity;

        public ViewportF Viewport { get; set; }
        public Bounds OverallBounds { get; set; } = Bounds.Empty;
        public Bounds CurrentBounds { get; set; } = Bounds.Empty;
        public Vector2 Translate { get; set; } = Vector2.Zero;
        public int CurrentZoomStep { get; set; } = 0;
        public float CurrentZoom => (float)Math.Pow(_zoomFactor, CurrentZoomStep);
        public Rotation CurrentRotation { get; set; } = Rotation.NoRotation;
        public bool IsIn3DView { get; set; } = false;
        #endregion

        #region Constructors
        public Camera(ViewportF viewport, Bounds dxfBounds, float zoomFactor)
        {
            Viewport = viewport;
            _zoomFactor = zoomFactor;

            UpdateBounds(dxfBounds);

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

        public void UpdateViewportSize(ViewportF viewport)
        {
            Viewport = viewport;

            UpdateProjection();
            UpdateViewProjection();
        }

        public void UpdateProjection()
        {
            //ProjectionMatrix = Matrix.OrthoOffCenterLH(
            //    CurrentBounds.Left, CurrentBounds.Right,
            //    CurrentBounds.Bottom, CurrentBounds.Top,
            //    0.1f, 1000f);

            ProjectionMatrix = Matrix.OrthoLH(Viewport.Width, Viewport.Height, 0.1f, 1000f);
        }

        public void UpdateInitialViewMatrix(Matrix newInitialView)
        {
            InitialViewMatrix = newInitialView;
            _scaledInitialViewMatrix = InitialViewMatrix;

            // Extract scale factors from ProjectionMatrix
            float scaleX = ProjectionMatrix.M11 * newInitialView.M11;
            float scaleY = ProjectionMatrix.M22 * newInitialView.M22;

            // Adjust M41 and M42 in the InitialViewMatrix
            _scaledInitialViewMatrix.M41 *= scaleX;
            _scaledInitialViewMatrix.M42 *= scaleY;
            
            // Update dependent matrices
            UpdateViewProjection();
        }
        public void UpdateView()
        {
            var zoom = CurrentZoom;
            ViewMatrix = Matrix.Scaling(zoom, zoom, 1) * Matrix.Translation(Translate.X, Translate.Y, 0);
            _scaledViewMatrix = Matrix.Scaling(zoom, zoom, 1) * Matrix.Translation(Translate.X * ProjectionMatrix.M11 * zoom, Translate.Y * ProjectionMatrix.M22 * zoom, 0);
        }
        private void UpdateViewProjection()
        {
            ViewProjectionMatrix = ProjectionMatrix * _scaledViewMatrix * _scaledInitialViewMatrix;
            InverseViewProjectionMatrix = Matrix.Invert(ViewProjectionMatrix);
        }
        public void ResetToDefaults()
        {
            CurrentZoomStep = 0;
            CurrentRotation.SetX(0);
            CurrentRotation.SetY(0);
            CurrentRotation.SetZ(0);
            IsIn3DView = false;

            UpdateProjection();
            UpdateView();
            UpdateViewProjection();
        }

        /// <summary>
        /// Translates the camera by the distance between screen space coordinates.
        /// </summary>
        /// <param name="screenSpaceStart">The start pan location in screen space coordinates.</param>
        /// <param name="screenSpaceEnd">The end pan location in screen space coordinates.</param>
        public void Pan(Vector2 screenSpaceStart, Vector2 screenSpaceEnd)
        {
            // Convert screen coordinates to normalized device coordinates (NDC)
            Vector2 startNDC = ScreenToNDC(screenSpaceStart);
            Vector2 endNDC = ScreenToNDC(screenSpaceEnd);

            // Convert NDC to world coordinates
            Vector3 startWorld = Unproject(startNDC);
            Vector3 endWorld = Unproject(endNDC);

            // Calculate the world-space delta
            Vector3 delta = endWorld - startWorld;

            // Update the translation vector
            Translate -= new Vector2(delta.X, delta.Y);

            // Update the view matrix
            UpdateView();
            UpdateViewProjection();
        }


        public void Zoom(int zoomStepDelta, Vector2 mousePosition)
        {
            // Update zoom step and calculate the scale
            CurrentZoomStep += zoomStepDelta;

            // Convert mouse position to NDC space
            Vector2 initialNDC = ScreenToNDC(mousePosition);

            // Unproject NDC to world space for the zoom pivot point
            Vector3 initialWorldPivot3D = Unproject(initialNDC);
            Vector2 initialWorldMousePos = new(initialWorldPivot3D.X, initialWorldPivot3D.Y);

            // Update matrices
            UpdateView();
            UpdateViewProjection();

            // Pan view so that zoom is towards mouse position
            Vector2 finalNDC = ScreenToNDC(mousePosition);
            Vector3 finalWorldPivot3D = Unproject(finalNDC);
            Vector2 finalWorldMousePos = new(finalWorldPivot3D.X, finalWorldPivot3D.Y);
            Vector2 worldDelta = finalWorldMousePos - initialWorldMousePos;

            Translate += worldDelta;

            // Update matrices
            UpdateView();
            UpdateViewProjection();
        }


        public void Rotate(float deltaX, float deltaY, bool shiftHeld)
        {

        }


        public RawMatrix3x2 Get2DTransformationMatrix()
        {
            var zoom = CurrentZoom;
            var scaleX = zoom * InitialViewMatrix.M11;
            var scaleY = zoom * InitialViewMatrix.M22;
            var translateX = (ViewMatrix.M41 + InitialViewMatrix.M41) * scaleX;
            var translateY = (ViewMatrix.M42 + InitialViewMatrix.M42) * scaleY;

            //var zoom = CurrentZoom;
            //var scaleX = zoom * InitialViewMatrix.M11;
            //var scaleY = zoom * InitialViewMatrix.M22;
            //var translateX = InverseViewProjectionMatrix.M41 * scaleX;
            //var translateY = InverseViewProjectionMatrix.M42 * scaleY;

            return new RawMatrix3x2(scaleX, 0, 0, scaleY, translateX, translateY);
        }

        public Vector2 ScreenToWorld(Vector2 screenSpace)
        {   
            // Convert screen coordinates to normalized device coordinates (NDC)
            Vector2 ndc = ScreenToNDC(screenSpace);

            // Unproject the NDC point into world space
            Vector3 world = Unproject(ndc);

            return new Vector2(world.X, world.Y);
        }

        public Vector2 ScreenToNDC(Vector2 screenSpace)
        {
            float x = (2.0f * screenSpace.X / Viewport.Width) - 1.0f;
            float y = 1.0f - (2.0f * screenSpace.Y / Viewport.Height);
            return new Vector2(x, y);
        }

        public Vector3 Unproject(Vector2 ndc)
        {
            // Add Z = 0 (near plane) and W = 1 for the unprojection calculation
            Vector4 ndcVec = new Vector4(ndc.X, ndc.Y, 0, 1);

            // Transform NDC to world space using the inverse of the view-projection matrix
            Vector4 worldVec = Vector4.Transform(ndcVec, InverseViewProjectionMatrix);

            // Perform perspective divide
            if (worldVec.W != 0)
            {
                worldVec.X /= worldVec.W;
                worldVec.Y /= worldVec.W;
                worldVec.Z /= worldVec.W;
            }

            return new Vector3(worldVec.X, worldVec.Y, worldVec.Z);
        }
        #endregion
    }
}
