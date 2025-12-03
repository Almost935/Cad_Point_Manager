using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl.Buffers
{
    public class ResizableConstantBuffer<T> : IDisposable where T : struct
    {
        private readonly Device _device;
        private Buffer _buffer;

        public Buffer Buffer => _buffer;

        public ResizableConstantBuffer(Device device)
        {
            _device = device;
            Initialize();
        }

        private void Initialize()
        {
            var desc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<T>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _buffer = new Buffer(_device, desc);
        }

        public void Update(DeviceContext context, ref T data)
        {
            context.UpdateSubresource(ref data, _buffer);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }
    }

}
