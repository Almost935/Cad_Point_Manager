using Cad_Point_Manager.Common;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.Printing;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using Matrix = System.Windows.Media.Matrix;
using D3dMatrix = SharpDX.Matrix;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class Camera : INotifyPropertyChanged
    {
        #region Fields
        private readonly double _zoomFactor;
        private bool HasValidViewport => Viewport.Width > 0 && Viewport.Height > 0;

        private Matrix _scaledViewMatrix = Matrix.Identity;

        private Matrix _windowsMatrix = Matrix.Identity;
        private bool _isDirty = true;
        private ObservableCollection<Scene> _scenes = [];
        #endregion

        #region Properties
        public Matrix WindowsMatrix
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
        public Vector2d Translate { get; set; } = Vector2d.Zero;
        public double CurrentZoomStep { get; set; } = 0.0;
        public double CurrentZoom => Math.Pow(_zoomFactor, CurrentZoomStep);
        public Rect Extents { get; set; } = RectExtensions.Zero;
        public Scene OverviewScene { get; set; }
        #endregion

        #region Constructors
        public Camera(ViewportF viewport, double zoomFactor, Rect extents)
        {
            Viewport = viewport;
            _zoomFactor = zoomFactor;
            Extents = extents;

            OverviewScene = new Scene()
            {
                Name = "Default",
                ZoomStep = 0,
                Translation = Vector2d.Zero,
                Bounds = GetCurrentViewportBounds()
            };
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
            if (!HasValidViewport || InitialViewMatrix.M11 <= 0)
            {
                ProjectionMatrix = Matrix.Identity;
                return;
            }

            double scale = InitialViewMatrix.M11;

            double centerX = Extents.X + Extents.Width * 0.5;
            double centerY = Extents.Y + Extents.Height * 0.5;

            double scaleX = 2.0 * scale / Viewport.Width;
            double scaleY = 2.0 * scale / Viewport.Height;

            ProjectionMatrix = new Matrix(
                scaleX, 0, 0, scaleY, -centerX * scaleX, -centerY * scaleY);
        }

        public void UpdateInitialView(Matrix newInitialView, Rect newExtents)
        {
            Extents = newExtents;
            InitialViewMatrix = newInitialView;

            UpdateProjection();
            UpdateViewProjection();
            UpdateDefaultScene();
        }

        public void ResetView(Matrix newInitialView, Rect newExtents)
        {
            ZeroViews();
            UpdateInitialView(newInitialView, newExtents);
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
            Translate = Vector2d.Zero;
        }

        public void UpdateView()
        {
            double zoom = CurrentZoom;

            double centerX = Extents.X + Extents.Width * 0.5;
            double centerY = Extents.Y + Extents.Height * 0.5;

            ViewMatrix = new Matrix(
                zoom, 0, 0, zoom, (1.0 - zoom) * centerX + zoom * Translate.X,
                (1.0 - zoom) * centerY + zoom * Translate.Y);

            _scaledViewMatrix = ViewMatrix;
        }

        public void UpdateViewProjection()
        {
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;

            InverseViewProjectionMatrix = ViewProjectionMatrix;

            if (InverseViewProjectionMatrix.HasInverse)
            {
                var inverse = InverseViewProjectionMatrix;
                inverse.Invert();
                InverseViewProjectionMatrix = inverse;
            }

            Update2DTransformationMatrix();

            IsDirty = true;
        }
        public void ResetToDefaults()
        {
            CurrentZoomStep = 0;
            Translate = Vector2d.Zero;

            UpdateProjection();
            UpdateView();
            UpdateViewProjection();

            IsDirty = true;
        }

        /// <summary>
        /// Translates the camera by the distance between screen space coordinates.
        /// </summary>
        /// <param name="screenSpaceStart">The start pan location in screen space coordinates.</param>
        /// <param name="screenSpaceEnd">The end pan location in screen space coordinates.</param>
        public void Pan(Vector2d screenSpaceStart, Vector2d screenSpaceEnd)
        {
            Vector2d deltaPixels = screenSpaceEnd - screenSpaceStart;

            double worldPerPixel = GetWorldUnitsPerPixel();

            Translate += new Vector2d(
                -deltaPixels.X * worldPerPixel,
                 deltaPixels.Y * worldPerPixel);

            UpdateView();
            UpdateViewProjection();

            IsDirty = true;
        }

        /// <summary>
        /// Sets the camera translation based on the total screen-space movement
        /// from the beginning of a pan.
        /// </summary>
        /// <param name="panStartTranslate">
        /// Camera Translate value when the pan began.
        /// </param>
        /// <param name="panStartScreen">
        /// Mouse position when the pan began, in screen pixels.
        /// </param>
        /// <param name="currentScreen">
        /// Current mouse position, in screen pixels.
        /// </param>
        public void PanFromStart(
            Vector2d panStartTranslate, Vector2d panStartScreen, Vector2d currentScreen, double worldPerPixel)
        {
            Vector2d deltaPixels = panStartScreen - currentScreen;

            Translate = panStartTranslate + new Vector2d(
                -deltaPixels.X * worldPerPixel,
                 deltaPixels.Y * worldPerPixel);

            UpdateView();
            UpdateViewProjection();

            IsDirty = true;
        }
        public void Zoom(double zoomStepDelta, Vector2d mousePosition)
        {
            if (zoomStepDelta == 0 || !HasValidViewport)
                return;

            Vector2d worldBefore = ScreenToWorld(mousePosition);

            CurrentZoomStep = Math.Clamp(
                CurrentZoomStep + zoomStepDelta, -100.0, 100.0);

            Vector2d worldAfter = ScreenToWorld(mousePosition);

            Translate += worldAfter - worldBefore;

            UpdateView();
            UpdateViewProjection();

            IsDirty = true;
        }
        public void SetPanAndZoom(Vector2d translation, int zoomStep)
        {
            Translate = translation;
            CurrentZoomStep = zoomStep;
            UpdateView();
            UpdateViewProjection();
        }

        public void Update2DTransformationMatrix()
        {
            if (!HasValidViewport)
            {
                WindowsMatrix = Matrix.Identity;
                return;
            }

            double halfW = Viewport.Width * 0.5;
            double halfH = Viewport.Height * 0.5;

            Matrix ndcToPixel = new(
                halfW, 0, 0, -halfH, halfW, halfH);

            WindowsMatrix = ViewProjectionMatrix * ndcToPixel;
        }

        public Vector2d ScreenToWorld(Vector2d screenPosition)
        {
            if (!HasValidViewport)
                return Vector2d.Zero;

            double scale = InitialViewMatrix.M11 * CurrentZoom;

            if (scale <= 0)
                return Vector2d.Zero;

            double worldPerPixel = 1.0 / scale;

            double centerX = Extents.X + Extents.Width * 0.5;
            double centerY = Extents.Y + Extents.Height * 0.5;

            double dx = screenPosition.X - Viewport.Width * 0.5;
            double dy = Viewport.Height * 0.5 - screenPosition.Y;

            return new Vector2d(
                centerX + dx * worldPerPixel - Translate.X,
                centerY + dy * worldPerPixel - Translate.Y);
        }

        public Vector2 ScreenToNDC(Vector2 screenSpace)
        {
            float x = (2.0f * screenSpace.X / Viewport.Width) - 1.0f;
            float y = 1.0f - (2.0f * screenSpace.Y / Viewport.Height);
            return new Vector2(x, y);
        }

        //public Vector3 Unproject(Vector2 ndc)
        //{
        //    Vector4 ndcVec = new(ndc.X, ndc.Y, 0, 1);
        //    Vector4 worldVec = Vector4.Transform(ndcVec, InverseViewProjectionMatrix);
        //    if (worldVec.W != 0)
        //    {
        //        worldVec.X /= worldVec.W;
        //        worldVec.Y /= worldVec.W;
        //        worldVec.Z /= worldVec.W;
        //    }

        //    return new Vector3(worldVec.X, worldVec.Y, worldVec.Z);
        //}

        /// <summary>
        /// Returns how many world units correspond to one screen-space pixel.
        /// </summary>
        public double GetWorldUnitsPerPixel()
        {
            if (!HasValidViewport)
                return 0.0;

            double scale = InitialViewMatrix.M11 * CurrentZoom;

            return scale > 0.0 ? 1.0 / scale : 0.0;
        }

        public void ZoomToBounds(Rect bounds)
        {
            if (!HasValidViewport || bounds.IsEmpty ||
                bounds.Width <= 0 || bounds.Height <= 0)
                return;

            const double paddingPct = 0.05;

            double targetW = bounds.Width * (1.0 + 2.0 * paddingPct);
            double targetH = bounds.Height * (1.0 + 2.0 * paddingPct);

            double initialScale = InitialViewMatrix.M11;

            if (initialScale <= 0)
                return;

            double baseW = Viewport.Width / initialScale;
            double baseH = Viewport.Height / initialScale;

            double desiredZoom = Math.Min(
                baseW / targetW,
                baseH / targetH);

            if (!double.IsFinite(desiredZoom) || desiredZoom <= 0)
                return;

            CurrentZoomStep = ZoomStepFloor(desiredZoom);

            Vector2d boundsCenter = new(
                bounds.X + bounds.Width * 0.5,
                bounds.Y + bounds.Height * 0.5);

            Vector2d extentsCenter = new(
                Extents.X + Extents.Width * 0.5,
                Extents.Y + Extents.Height * 0.5);

            Translate = extentsCenter - boundsCenter;

            UpdateView();
            UpdateViewProjection();

            IsDirty = true;
        }

        private int ZoomStepFloor(double desiredZoom)
        {
            double zf = _zoomFactor;

            if (zf <= 1.0) return 0;

            double raw = Math.Log(desiredZoom) / Math.Log(zf);
            int step = (int)Math.Floor(raw);
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

            scene = new Scene()
            {
                Name = sceneName,
                ZoomStep = CurrentZoomStep,
                ZoomFactor = _zoomFactor,
                Translation = Translate,
                Bounds = GetCurrentViewportBounds()
            };

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
            if (scene is null) { return; }
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
        public Rect GetCurrentViewportBounds()
        {
            if (!HasValidViewport)
                return Rect.Empty;

            Vector2d bottomLeft = ScreenToWorld(
                new Vector2d(0, Viewport.Height));

            Vector2d topRight = ScreenToWorld(
                new Vector2d(Viewport.Width, 0));

            return new Rect(
                bottomLeft.X,
                bottomLeft.Y,
                topRight.X - bottomLeft.X,
                topRight.Y - bottomLeft.Y);
        }
        public void UpdateDefaultScene()
        {
            var defaultScene = Scenes.FirstOrDefault(s => s.Name == "Default");
            if (defaultScene != null)
            {
                defaultScene.ZoomStep = 0;
                defaultScene.Translation = Vector2d.Zero;
                defaultScene.Bounds = Extents;
            }
        }

        public D3dMatrix GetRelativeViewProjectionMatrix(Vector2d renderOrigin)
        {
            if (!HasValidViewport || InitialViewMatrix.M11 <= 0)
                return D3dMatrix.Identity;

            double centerX = Extents.X + Extents.Width * 0.5;
            double centerY = Extents.Y + Extents.Height * 0.5;

            double scale = InitialViewMatrix.M11 * CurrentZoom;

            double scaleX = 2.0 * scale / Viewport.Width;
            double scaleY = 2.0 * scale / Viewport.Height;

            double translateX =
                (renderOrigin.X - centerX + Translate.X) * scaleX;

            double translateY =
                (renderOrigin.Y - centerY + Translate.Y) * scaleY;

            var matrix = D3dMatrix.Identity;

            matrix.M11 = (float)scaleX;
            matrix.M22 = (float)scaleY;
            matrix.M41 = (float)translateX;
            matrix.M42 = (float)translateY;

            return matrix;
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
