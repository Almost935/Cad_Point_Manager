using SharpDX.Direct3D11;
using SharpDX;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl.Buffers
{
    public class ResizableBuffer<T> : IDisposable where T : unmanaged
    {
        private Device _device;
        private Buffer _buffer;
        private int _capacity;

        public Buffer Buffer => _buffer;
        public int Capacity => _capacity;
        public int Stride => Utilities.SizeOf<T>();

        public ResizableBuffer(Device device, int initialCapacity)
        {
            _device = device;
            _capacity = initialCapacity;
            CreateBuffer(_capacity);
        }

        private void CreateBuffer(int elementCount)
        {
            _buffer?.Dispose();

            var desc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<T>() * elementCount,
                BindFlags = BindFlags.VertexBuffer,
                Usage = ResourceUsage.Dynamic,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = Utilities.SizeOf<T>()
            };
            _buffer = new Buffer(_device, desc);
        }

        public void EnsureCapacity(int requiredElements)
        {
            if (requiredElements > _capacity)
            {
                int newCapacity = Math.Max(requiredElements, _capacity * 2);
                CreateBuffer(newCapacity);
                _capacity = newCapacity;
            }
        }

        public void Update(DeviceContext context, Span<T> data)
        {
            EnsureCapacity(data.Length);
            var box = context.MapSubresource(_buffer, 0, MapMode.WriteDiscard, MapFlags.None);
            unsafe
            {
                fixed (T* srcPtr = data)
                {
                    Utilities.CopyMemory(box.DataPointer, (nint)srcPtr, data.Length * sizeof(T));
                }
            }
            context.UnmapSubresource(_buffer, 0);
        }
        public void Update(DeviceContext context, ReadOnlySpan<T> data)
        {
            EnsureCapacity(data.Length);
            var box = context.MapSubresource(_buffer, 0, MapMode.WriteDiscard, MapFlags.None);
            unsafe
            {
                fixed (T* srcPtr = data)
                {
                    Utilities.CopyMemory(box.DataPointer, (nint)srcPtr, data.Length * sizeof(T));
                }
            }
            context.UnmapSubresource(_buffer, 0);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
            _buffer = null;
        }
    }

}
