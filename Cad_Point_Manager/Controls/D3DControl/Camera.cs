using Cad_Point_Manager.Models;
using Cad_Point_Manager.Extensions;
using SharpDX;
using SharpDX.Mathematics.Interop;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class Camera : INotifyPropertyChanged
    {
        #region Fields
        private readonly float _zoomFactor;

        private Matrix _scaledInitialViewMatrix = Matrix.Identity;
        private Matrix _scaledViewMatrix = Matrix.Identity;

        private Matrix3x2 _d2dMatrix = Matrix3x2.Identity;
        #endregion

        #region Properties
        public Matrix3x2 D2dMatrix
        {
            get => _d2dMatrix;
            set
            {
                if (_d2dMatrix != value)
                {
                    _d2dMatrix = value;
                    OnPropertyChanged(nameof(D2dMatrix));
                }
            }
        }

        public Matrix InitialViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix InverseViewProjectionMatrix { get; private set; } = Matrix.Identity;
        public ViewportF Viewport { get; set; }
        public Vector2 Translate { get; set; } = Vector2.Zero;
        public int CurrentZoomStep { get; set; } = 0;
        public float CurrentZoom => (float)Math.Pow(_zoomFactor, CurrentZoomStep);
        public Rotation CurrentRotation { get; set; } = Rotation.NoRotation;
        public bool IsIn3DView { get; set; } = false;
        public Rect Extents { get; set; } = Rect.Empty;
        #endregion

        #region Constructors
        public Camera(ViewportF viewport, float zoomFactor, Rect extents)
        {
            Viewport = viewport;
            _zoomFactor = zoomFactor;
            Extents = extents;

            ResetToDefaults();
        }
        #endregion

        #region Methods
        public void UpdateViewportSize(ViewportF viewport)
        {
            Viewport = viewport;

            UpdateProjection();
            UpdateViewProjection();
        }

        public void UpdateProjection()
        { 
            Vector2 basePoint = new(Extents.Center().X.ToFloat(), Extents.Center().Y.ToFloat());
            
            float scaledViewWidth = Viewport.Width / InitialViewMatrix.M11;
            float scaledViewHeight = Viewport.Height / InitialViewMatrix.M11;
            ProjectionMatrix = Matrix.OrthoOffCenterLH(basePoint.X - scaledViewWidth / 2, basePoint.X + scaledViewWidth / 2, basePoint.Y - scaledViewHeight / 2, basePoint.Y + scaledViewHeight / 2, 0.1f, 1000f);
        }

        public void ResetView(Matrix newInitialView, Rect newExtents)
        {
            ZeroViews();

            Extents = newExtents;
            InitialViewMatrix = newInitialView;

            UpdateProjection();
            UpdateViewProjection();
        }
        public void ZeroViews()
        {
            InitialViewMatrix = Matrix.Identity;
            ViewMatrix = Matrix.Identity;
            ProjectionMatrix = Matrix.Identity;
            ViewProjectionMatrix = Matrix.Identity;
            InverseViewProjectionMatrix = Matrix.Identity;
            _scaledInitialViewMatrix = Matrix.Identity;
            _scaledViewMatrix = Matrix.Identity;
            CurrentZoomStep = 0;
            Translate = Vector2.Zero;
        }
        public void UpdateView()
        {
            var zoom = CurrentZoom;
            ViewMatrix = Matrix.Scaling(zoom, zoom, 1) * Matrix.Translation(Translate.X, Translate.Y, 0);
            _scaledViewMatrix = Matrix.Scaling(zoom, zoom, 1) * Matrix.Translation(Translate.X * ProjectionMatrix.M11 * zoom, Translate.Y * ProjectionMatrix.M22 * zoom, 0);
        }
        private void UpdateViewProjection()
        {
            ViewProjectionMatrix = ProjectionMatrix * _scaledViewMatrix;
            InverseViewProjectionMatrix = Matrix.Invert(ViewProjectionMatrix);
            D2dMatrix = Get2DTransformationMatrix();
        }
        public void ResetToDefaults()
        {
            CurrentZoomStep = 0;
            CurrentRotation.SetX(0);
            CurrentRotation.SetY(0);
            CurrentRotation.SetZ(0);

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


        public Matrix3x2 Get2DTransformationMatrix()
        {
            var halfW = Viewport.Width / 2f;
            var halfH = Viewport.Height / 2f;
            var ndcToPixel = Matrix.Scaling(halfW, -halfH, 1) * Matrix.Translation(halfW, halfH, 0);
            Matrix final = ViewProjectionMatrix * ndcToPixel;
            Matrix3x2 matrix = new(
                final.M11, final.M12,
                final.M21, final.M22,
                final.M41, final.M42
            );

            return matrix;
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

        /// <summary>
        /// Returns how many world units correspond to one screen-space pixel.
        /// </summary>
        public float GetWorldUnitsPerPixel()
        {
            // Unproject two screen points that are 1 pixel apart in X (screen space)
            Vector2 screenCenter = new(Viewport.Width / 2f, Viewport.Height / 2f);
            Vector2 screenRight = new(screenCenter.X + 1, screenCenter.Y);

            Vector2 worldCenter = ScreenToWorld(screenCenter);
            Vector2 worldRight = ScreenToWorld(screenRight);

            float worldUnitsPerPixel = Vector2.Distance(worldCenter, worldRight);
            return worldUnitsPerPixel;
        }

        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
