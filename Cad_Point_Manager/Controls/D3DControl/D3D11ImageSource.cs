using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3D11ImageSource : D3DImage, IDisposable
    {
        private Surface _surface;

        public void SetBackBuffer(Surface surface)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));

            _surface = surface;
            Lock();
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
            Unlock();
        }

        public void Dispose()
        {
            _surface?.Dispose();
        }
    }
}
