using SharpDX.Direct3D11;
using SharpDX;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl.Buffers
{
    public class ResizableStructuredBuffer<T> : IDisposable where T : struct
    {
        private readonly Device _device;
        private Buffer _buffer;
        private ShaderResourceView _srv;
        private UnorderedAccessView _uav;
        private int _capacity;

        public Buffer Buffer => _buffer;
        public ShaderResourceView SRV => _srv;
        public UnorderedAccessView UAV => _uav;
        public int Capacity => _capacity;

        private readonly bool _allowUAV;

        public ResizableStructuredBuffer(Device device, int initialCapacity, bool allowUav = false)
        {
            _device = device;
            _allowUAV = allowUav;
            Resize(initialCapacity);
        }

        public void EnsureCapacity(int requiredCount)
        {
            if (requiredCount <= _capacity)
                return;

            int newCapacity = Math.Max(requiredCount, _capacity * 2);
            Resize(newCapacity);
        }

        private void Resize(int newCapacity)
        {
            _buffer?.Dispose();
            _srv?.Dispose();
            _uav?.Dispose();

            var sizeOfStruct = Utilities.SizeOf<T>();
            var desc = new BufferDescription
            {
                SizeInBytes = newCapacity * sizeOfStruct,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | (_allowUAV ? BindFlags.UnorderedAccess : 0),
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = sizeOfStruct
            };

            _buffer = new Buffer(_device, desc);
            _srv = new ShaderResourceView(_device, _buffer);
            if (_allowUAV)
                _uav = new UnorderedAccessView(_device, _buffer);

            _capacity = newCapacity;
        }

        public void Update(DeviceContext context, ReadOnlySpan<T> data)
        {
            EnsureCapacity(data.Length);
            context.UpdateSubresource(data.ToArray(), _buffer);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
            _srv?.Dispose();
            _uav?.Dispose();
        }
    }

}
