using Cad_Point_Manager.Models.Printing;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Services
{
    public interface ILayoutRenderHost
    {
        int MaxTextureSize { get; }
        void RenderSceneIntoWriteableBitmap(Scene scene, WriteableBitmap target);
    }
}
