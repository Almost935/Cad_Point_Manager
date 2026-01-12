using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Services
{
    public sealed class D3dDxfRenderHost : ILayoutRenderHost
    {
        private readonly D3dDxfControl _renderer;

        public D3dDxfRenderHost(D3dDxfControl renderer)
            => _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

        public int MaxTextureSize => _renderer.MaxTextureSize;

        public void RenderSceneIntoWriteableBitmap(Scene scene, WriteableBitmap target)
            => _renderer.RenderSceneIntoWriteableBitmap(scene, target);
    }

}
