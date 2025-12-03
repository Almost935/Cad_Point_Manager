using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl.Buffers
{
    public class ResizableVertexBuffer<T> : IDisposable where T : struct
    {
        private readonly Device _device;
        private Buffer _buffer;
        private int _capacity; // current number of T elements it can hold

        public Buffer Buffer => _buffer;
        public int Capacity => _capacity;
        public int Stride => Utilities.SizeOf<T>();

        public ResizableVertexBuffer(Device device, int initialCapacity)
        {
            _device = device;
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

            var desc = new BufferDescription
            {
                SizeInBytes = newCapacity * Utilities.SizeOf<T>(),
                BindFlags = BindFlags.VertexBuffer,
                Usage = ResourceUsage.Dynamic,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = Utilities.SizeOf<T>()
            };

            _buffer = new Buffer(_device, desc);
            _capacity = newCapacity;
        }

        public void Update(DeviceContext context, ReadOnlySpan<T> data)
        {
            EnsureCapacity(data.Length);

            var box = context.MapSubresource(_buffer, 0, MapMode.WriteDiscard, MapFlags.None);
            unsafe
            {
                fixed (T* srcPtr = data)
                {
                    Utilities.CopyMemory(box.DataPointer, (nint)srcPtr, data.Length * Utilities.SizeOf<T>());
                }
            }
            context.UnmapSubresource(_buffer, 0);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }
    }

}
