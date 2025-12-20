using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.Printing;
using SharpDX.Direct3D9;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Services
{
    public sealed class ScenePreviewService
    {
        private readonly D3dDxfControl _renderer;
        private readonly ConcurrentDictionary<(Guid sceneId, int w, int h), BitmapSource> _cache = new();

        public ScenePreviewService(D3dDxfControl renderer)
        {
            _renderer = renderer;
        }

        public async Task<BitmapSource> GetPreviewAsync(Scene scene, int pixelW, int pixelH)
        {
            var key = (scene.SceneId, pixelW, pixelH);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            // Render must run on UI thread (same thread as D3D control)
            var bmp = await _renderer.Dispatcher.InvokeAsync(() =>
                _renderer.RenderSceneToBitmapSource(scene, pixelW, pixelH));

            _cache[key] = bmp;
            return bmp;
        }
    }
}
