using SharpDX;
using SharpDX.Direct2D1;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public class OffscreenBitmapManager 
    {
        #region Fields
        private int _bitmapCreationFactor = 2;

        private BitmapRenderTarget _bitmapRenderTarget;
        private Size2F _deviceContextSize;
        private float _zoomFactor;
        #endregion

        #region Properties
        public bool IsDisposed { get; private set; }
        public int CurrentZoomStep { get; set; }    
        public OffscreenBitmap CurrentOffscreenBitmap { get; set; }
        public OffscreenBitmap[] OffscreenBitmaps { get; set; }
        public (float x, float y) CenteringOffset { get; set; }
        public Vector UpdateDistance { get; set; }
        #endregion

        #region Constructors
        public OffscreenBitmapManager(BitmapRenderTarget bitmapRenderTarget, Size2F deviceContextSize, int currentZoomStep, float zoomFactor)
        {
            _bitmapRenderTarget = bitmapRenderTarget;
            _deviceContextSize = deviceContextSize;
            CurrentZoomStep = currentZoomStep;
            _zoomFactor = zoomFactor;

            UpdateDistance = new(((_bitmapRenderTarget.Size.Width / 2) - (_deviceContextSize.Width / 2)), ((_bitmapRenderTarget.Size.Height / 2) - (_deviceContextSize.Height / 2)));
            OffscreenBitmaps = new OffscreenBitmap[_bitmapCreationFactor * 2];
        }
        #endregion

        #region Methods

        #endregion
    }
}
