// Rendering/D3D/D3dStateBuffers.cs
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    [Flags]
    public enum CogoPointFlags : uint
    {
        Visible = 1u << 0,
        Selected = 1u << 1,
        MouseOver = 1u << 2,
        HasLeaderLine = 1u << 3, // if you add another flag, update the comment
        MouseOverAnchor = 1u << 4,
        AnchorPressed = 1u << 5,
    }

    public sealed class D3dStateBuffers : IDisposable
    {
        private readonly Device _device;
        private readonly DeviceContext _ctx;

        private Buffer _labelBuf;
        private ShaderResourceView _labelSrv;
        private Buffer _pointBuf;
        private ShaderResourceView _pointSrv;
        private Buffer _groupBuf;
        private ShaderResourceView _groupSrv;

        private LabelState[] _labelCpu = [];
        private PointState[] _pointCpu = [];
        private GroupState[] _groupCpu = [];
        private int _labelCap, _groupCap, _pointCap;

        public D3dStateBuffers(Device device, DeviceContext ctx)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        public ShaderResourceView LabelSRV => _labelSrv;
        public ShaderResourceView PointSRV => _pointSrv;
        public ShaderResourceView GroupSRV => _groupSrv;
        public Span<LabelState> LabelSpan => _labelCpu.AsSpan(0, _labelCap);
        public Span<PointState> PointSpan => _pointCpu.AsSpan(0, _pointCap);
        public Span<GroupState> GroupSpan => _groupCpu.AsSpan(0, _groupCap);

        public void EnsureLabelCapacity(int count)
        {
            if (count <= _labelCap) return;
            _labelCap = NextPow2(count);
            Array.Resize(ref _labelCpu, _labelCap);

            _labelSrv?.Dispose(); _labelBuf?.Dispose();
            var desc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<LabelState>() * _labelCap,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = Utilities.SizeOf<LabelState>()
            };
            _labelBuf = new Buffer(_device, desc);
            _labelSrv = new ShaderResourceView(_device, _labelBuf, new ShaderResourceViewDescription
            {
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource { FirstElement = 0, ElementCount = _labelCap }
            });
        }

        public void EnsureGroupCapacity(int count)
        {
            if (count <= _groupCap) return;
            _groupCap = NextPow2(count);
            Array.Resize(ref _groupCpu, _groupCap);

            _groupSrv?.Dispose(); _groupBuf?.Dispose();
            var desc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<GroupState>() * _groupCap,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = Utilities.SizeOf<GroupState>()
            };
            _groupBuf = new Buffer(_device, desc);
            _groupSrv = new ShaderResourceView(_device, _groupBuf, new ShaderResourceViewDescription
            {
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource { FirstElement = 0, ElementCount = _groupCap }
            });
        }

        public void EnsurePointCapacity(int count)
        {
            if (count <= _pointCap) return;
            _pointCap = NextPow2(count);
            Array.Resize(ref _pointCpu, _pointCap);

            _pointSrv?.Dispose(); _pointBuf?.Dispose();
            var desc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<PointState>() * _pointCap,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = Utilities.SizeOf<PointState>()
            };
            _pointBuf = new Buffer(_device, desc);
            _pointSrv = new ShaderResourceView(_device, _pointBuf, new ShaderResourceViewDescription
            {
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource { FirstElement = 0, ElementCount = _pointCap }
            });
        }

        public void FlushAll()
        {
            if (_groupBuf is null || _labelBuf is null || _pointBuf is null) { return; }
            // groups (few): discard whole
            DataStream s;
            _ctx.MapSubresource(_groupBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
            s.WriteRange(_groupCpu, 0, _groupCap);
            _ctx.UnmapSubresource(_groupBuf, 0);

            // points (many): discard whole on full rebuild
            _ctx.MapSubresource(_pointBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
            s.WriteRange(_pointCpu, 0, _pointCap);
            _ctx.UnmapSubresource(_pointBuf, 0);

            // labels (many): discard whole on full rebuild
            _ctx.MapSubresource(_labelBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
            s.WriteRange(_labelCpu, 0, _labelCap);
            _ctx.UnmapSubresource(_labelBuf, 0);
        }

        public void FlushPointSubset(HashSet<uint> dirty)
        {
            if (dirty == null || dirty.Count == 0) return;

            DataStream s;
            _ctx.MapSubresource(_pointBuf, 0, MapMode.WriteNoOverwrite, MapFlags.None, out s);
            int stride = Utilities.SizeOf<PointState>();
            foreach (var id in dirty)
            {
                s.Position = id * stride;
                s.Write(_pointCpu[id]);
            }
            _ctx.UnmapSubresource(_pointBuf, 0);
        }

        public void FlushLabelSubset(HashSet<uint> dirty)
        {
            if (dirty == null || dirty.Count == 0) return;

            DataStream s;
            _ctx.MapSubresource(_labelBuf, 0, MapMode.WriteNoOverwrite, MapFlags.None, out s);
            int stride = Utilities.SizeOf<LabelState>();
            foreach (var id in dirty)
            {
                s.Position = id * stride;
                s.Write(_labelCpu[id]);
            }
            _ctx.UnmapSubresource(_labelBuf, 0);
        }

        public void Dispose()
        {
            _labelSrv?.Dispose(); _labelBuf?.Dispose();
            _pointSrv?.Dispose(); _pointBuf?.Dispose();
            _groupSrv?.Dispose(); _groupBuf?.Dispose();
        }

        private static int NextPow2(int v)
        {
            v--; v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16; v++;
            return Math.Max(16, v);
        }
    }
}
