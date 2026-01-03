using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using SharpDX;
using System.Collections.ObjectModel;
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
        private bool HasValidViewport => Viewport.Width > 0 && Viewport.Height > 0;

        private Matrix _scaledViewMatrix = Matrix.Identity;

        private Matrix3x2 _d2dMatrix = Matrix3x2.Identity;
        private System.Windows.Media.Matrix _windowsMatrix = System.Windows.Media.Matrix.Identity;
        private bool _isDirty = true;
        private ObservableCollection<Scene> _scenes = [];
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
        public System.Windows.Media.Matrix WindowsMatrix
        {
            get => _windowsMatrix;
            set
            {
                if (_windowsMatrix != value)
                {
                    _windowsMatrix = value;
                    OnPropertyChanged(nameof(WindowsMatrix));
                }
            }
        }
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }
        public ObservableCollection<Scene> Scenes
        {
            get => _scenes;
            set
            {
                if (_scenes != value)
                {
                    _scenes = value;
                    OnPropertyChanged(nameof(Scenes));
                }
            }
        }

        public Matrix InitialViewMatrix { get; set; } = Matrix.Identity;
        public Matrix ViewMatrix { get; private set; } = Matrix.Identity;
        public Matrix ProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix ViewProjectionMatrix { get; private set; } = Matrix.Identity;
        public Matrix InverseViewProjectionMatrix { get; private set; } = Matrix.Identity;
        public ViewportF Viewport { get; set; }
        //public RectangleF InitialViewportBounds { get; set; } = RectangleF.Empty;
        public Vector2 Translate { get; set; } = Vector2.Zero;
        public int CurrentZoomStep { get; set; } = 0;
        public float CurrentZoom => (float)Math.Pow(_zoomFactor, CurrentZoomStep);
        public bool IsIn3DView { get; set; } = false;
        public Rect Extents { get; set; } = RectExtensions.Zero;
        public Scene OverviewScene { get; set; }
        #endregion

        #region Constructors
        public Camera(ViewportF viewport, float zoomFactor, Rect extents)
        {
            Viewport = viewport;
            _zoomFactor = zoomFactor;
            Extents = extents;
            //InitialViewportBounds = new(
            //    Extents.Center().X.ToFloat() - viewport.Width / 2,
            //    Extents.Center().Y.ToFloat() - viewport.Height / 2,
            //    viewport.Width,
            //    viewport.Height);

            OverviewScene = new Scene() { Name = "Default", ZoomStep = 0, Translation = Vector2.Zero, Bounds = GetCurrentViewportBounds() };
            Scenes.Add(OverviewScene);

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

            if (!HasValidViewport)
            {
                ProjectionMatrix = Matrix.Identity;
                return;
            }

            float scaledViewWidth = Viewport.Width / InitialViewMatrix.M11;
            float scaledViewHeight = Viewport.Height / InitialViewMatrix.M11;
            ProjectionMatrix = Matrix.OrthoOffCenterLH(basePoint.X - scaledViewWidth / 2, basePoint.X + scaledViewWidth / 2, basePoint.Y - scaledViewHeight / 2, basePoint.Y + scaledViewHeight / 2, 0.0f, 1000f);
        }

        //public void UpdateProjection()
        //{
        //    if (!HasValidViewport)
        //    {
        //        ProjectionMatrix = Matrix.Identity;
        //        return;
        //    }

        //    var c = Extents.Center();
        //    float zoom = CurrentZoom;

        //    float worldW = Viewport.Width / zoom;
        //    float worldH = Viewport.Height / zoom;

        //    ProjectionMatrix = Matrix.OrthoOffCenterLH(
        //        c.X.ToFloat() - worldW / 2, c.X.ToFloat() + worldW / 2,
        //        c.Y.ToFloat() - worldH / 2, c.Y.ToFloat() + worldH / 2,
        //        0.0f, 1000f);
        //}



        public void ResetView(Matrix newInitialView, Rect newExtents)
        {
            ZeroViews();

            Extents = newExtents;
            InitialViewMatrix = newInitialView;
            UpdateProjection();
            UpdateViewProjection();

            //var width = Viewport.Width / D2dMatrix.M11;
            //var height = Viewport.Height / Math.Abs(D2dMatrix.M22);
            //InitialViewportBounds = new(
            //    Extents.Center().X.ToFloat() - width / 2,
            //    Extents.Center().Y.ToFloat() - height / 2,
            //    width,
            //    height);
            UpdateDefaultScene();
        }
        public void ZeroViews()
        {
            InitialViewMatrix = Matrix.Identity;
            ViewMatrix = Matrix.Identity;
            ProjectionMatrix = Matrix.Identity;
            ViewProjectionMatrix = Matrix.Identity;
            InverseViewProjectionMatrix = Matrix.Identity;
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
        //public void UpdateView()
        //{
        //    var zoom = CurrentZoom;

        //    ViewMatrix = Matrix.Scaling(zoom, zoom, 1) * Matrix.Translation(Translate.X, Translate.Y, 0);

        //    // keep projection in sync with zoom/translate
        //    UpdateProjection();

        //    _scaledViewMatrix =
        //        Matrix.Scaling(zoom, zoom, 1) *
        //        Matrix.Translation(Translate.X * ProjectionMatrix.M11 * zoom,
        //                           Translate.Y * ProjectionMatrix.M22 * zoom, 0);
        //}


        private void UpdateViewProjection()
        {
            ViewProjectionMatrix = ProjectionMatrix * _scaledViewMatrix;
            InverseViewProjectionMatrix = Matrix.Invert(ViewProjectionMatrix);
            Update2DTransformationMatrix();
        }
        public void ResetToDefaults()
        {
            CurrentZoomStep = 0;

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

        public void SetPanAndZoom(Vector2 translation, int zoomStep)
        {
            Translate = translation;
            CurrentZoomStep = zoomStep;
            UpdateView();
            UpdateViewProjection();
        }

        public void Update2DTransformationMatrix()
        {
            if (HasValidViewport)
            {
                var halfW = Viewport.Width / 2f;
                var halfH = Viewport.Height / 2f;
                var ndcToPixel = Matrix.Scaling(halfW, -halfH, 1) * Matrix.Translation(halfW, halfH, 0);
                Matrix final = ViewProjectionMatrix * ndcToPixel;
                D2dMatrix = new(
                   final.M11, final.M12,
                   final.M21, final.M22,
                   final.M41, final.M42
                   );
                WindowsMatrix = D2dMatrix.ToWindowsMatrix();
            }
            else
            {
                D2dMatrix = Matrix3x2.Identity;
                WindowsMatrix = D2dMatrix.ToWindowsMatrix();
            }
        }

        public Vector2 ScreenToWorld(Vector2 screenSpace)
        {
            Vector2 ndc = ScreenToNDC(screenSpace);
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
            Vector4 ndcVec = new(ndc.X, ndc.Y, 0, 1);

            var testMatrix = Matrix.Invert(ViewProjectionMatrix);

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

        public void ZoomToBounds(Rect bounds)
        {
            if (!HasValidViewport) return;
            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return;

            // Optional padding so it's not edge-to-edge
            const float paddingPct = 0.05f; // 5% padding
            float padX = (float)bounds.Width * paddingPct;
            float padY = (float)bounds.Height * paddingPct;

            float targetW = (float)bounds.Width + 2f * padX;
            float targetH = (float)bounds.Height + 2f * padY;

            // 1) Measure base viewport world size at zoom=1, translate=0
            int oldStep = CurrentZoomStep;
            Vector2 oldTranslate = Translate;

            CurrentZoomStep = 0;
            Translate = Vector2.Zero;
            UpdateView();
            UpdateViewProjection();

            RectangleF baseView = GetCurrentViewportBounds();
            if (baseView.Width <= 0 || baseView.Height <= 0)
            {
                // restore and bail
                CurrentZoomStep = oldStep;
                Translate = oldTranslate;
                UpdateView();
                UpdateViewProjection();
                return;
            }

            float baseW = baseView.Width;
            float baseH = baseView.Height;

            // 2) Compute the continuous zoom that would make bounds fit
            // zoom > 1 => zoom in (smaller world visible), zoom < 1 => zoom out (larger world visible)
            float desiredZoom = Math.Min(baseW / targetW, baseH / targetH);

            // Guard against weird values
            if (float.IsNaN(desiredZoom) || float.IsInfinity(desiredZoom) || desiredZoom <= 0)
                desiredZoom = 1f;

            // 3) Convert desiredZoom to a zoom step.
            // IMPORTANT: choose a zoom that does NOT exceed desiredZoom, so the bounds still fits.
            int step = ZoomStepFloor(desiredZoom);

            CurrentZoomStep = step;
            UpdateView();
            UpdateViewProjection();

            // 4) Center using the SAME convention as Pan(): Translate -= worldDelta
            Vector2 desiredCenter = new(
                (float)(bounds.X + bounds.Width / 2.0),
                (float)(bounds.Y + bounds.Height / 2.0));

            Vector2 screenCenter = new(Viewport.Width / 2f, Viewport.Height / 2f);
            Vector2 currentCenterWorld = ScreenToWorld(screenCenter);

            Vector2 worldDelta = desiredCenter - currentCenterWorld;

            // Pan uses: Translate -= delta; so do the same here
            Translate -= worldDelta;

            UpdateView();
            UpdateViewProjection();

        }

        private int ZoomStepFloor(float desiredZoom)
        {
            // desiredZoom = zoomFactor^step
            // step = log(desiredZoom) / log(zoomFactor)
            // We floor to ensure resulting zoom <= desiredZoom (keeps bounds fitting).
            double zf = _zoomFactor;
            if (zf <= 1.0) return 0;

            double raw = Math.Log(desiredZoom) / Math.Log(zf);

            // For negative values, Floor(-2.1) => -3 (more zoom-out), which is still <= desiredZoom, which we want.
            int step = (int)Math.Floor(raw);

            // Optional clamp (prevents insane zoom steps if bounds is microscopic/huge)
            step = Math.Clamp(step, -500, 500);

            return step;
        }

        public bool TrySaveScene(string sceneName, out Scene scene)
        {
            if (Scenes.Any(s => s.Name == sceneName))
            {
                scene = null;
                return false;
            }

            scene = new Scene() { Name = sceneName, ZoomStep = CurrentZoomStep, Translation = Translate, Bounds = GetCurrentViewportBounds() };
            Scenes.Add(scene);

            return true;
        }
        public bool TryGetScene(string sceneName, out Scene scene)
        {
            scene = Scenes.FirstOrDefault(s => s.Name == sceneName);
            return scene != null;
        }
        public bool TryDeleteScene(string sceneName)
        {
            return Scenes.Remove(Scenes.FirstOrDefault(s => s.Name == sceneName));
        }
        public bool TryDeleteScene(Scene scene)
        {
            return Scenes.Remove(scene);
        }
        public void LoadScene(Scene scene)
        {
            CurrentZoomStep = scene.ZoomStep;
            Translate = scene.Translation;
            UpdateView();
            UpdateViewProjection();
            IsDirty = true;
        }
        public string GetTempSceneName()
        {
            string baseName = "New Scene";
            int counter = 1;
            string sceneName = baseName + $" {counter}";
            while (SceneNameExists(sceneName))
            {
                sceneName = $"{baseName} {counter}"; counter++;
            }
            return sceneName;
        }
        public bool SceneNameExists(string name)
        {
            return Scenes.Any(pg => pg.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        public bool ValidateSceneNameChange(string newSceneName, Scene scene, out string? errorMessage)
        {
            errorMessage = null;

            if (newSceneName == scene.Name) { return true; }

            if (Scenes.Any(x => x.Name == newSceneName))
            {
                errorMessage = $"Scene name \"{newSceneName}\" already exists.";
                return false;
            }
            return true;
        }
        public RectangleF GetCurrentViewportBounds()
        {
            if (!HasValidViewport) { return RectangleF.Empty; }

            if (ViewProjectionMatrix == Matrix.Identity) { return new RectangleF(0, 0, Viewport.Width, Viewport.Height); }

            // Normal path:
            Vector2 screenTL = new(0, 0);
            Vector2 screenTR = new(Viewport.Width, 0);
            Vector2 screenBR = new(Viewport.Width, Viewport.Height);
            Vector2 screenBL = new(0, Viewport.Height);

            Vector2 worldTL = ScreenToWorld(screenTL);
            Vector2 worldTR = ScreenToWorld(screenTR);
            Vector2 worldBR = ScreenToWorld(screenBR);
            Vector2 worldBL = ScreenToWorld(screenBL);

            float minX = Math.Min(Math.Min(worldTL.X, worldTR.X), Math.Min(worldBL.X, worldBR.X));
            float maxX = Math.Max(Math.Max(worldTL.X, worldTR.X), Math.Max(worldBL.X, worldBR.X));
            float minY = Math.Min(Math.Min(worldTL.Y, worldTR.Y), Math.Min(worldBL.Y, worldBR.Y));
            float maxY = Math.Max(Math.Max(worldTL.Y, worldTR.Y), Math.Max(worldBL.Y, worldBR.Y));

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
        private void UpdateDefaultScene()
        {
            var defaultScene = Scenes.FirstOrDefault(s => s.Name == "Default");
            if (defaultScene != null)
            {
                defaultScene.ZoomStep = 0;
                defaultScene.Translation = Vector2.Zero;
                defaultScene.Bounds = GetCurrentViewportBounds();
            }
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
