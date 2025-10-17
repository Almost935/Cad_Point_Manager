// Rendering/D3D/D3dStateBuffers.cs
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Diagnostics;
using System.Security.AccessControl;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    [Flags]
    public enum PointGroupFlags : uint
    {
        Visible = 1u << 0
    }

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

    [Flags]
    public enum LabelFlags : uint
    {
        Visible = 1u << 0,
    }

    [Flags]
    public enum LayerFlags : uint
    {
        Visible = 1u << 0,
    }

    [Flags]
    public enum ObjectFlags : uint
    {
        Visible = 1u << 0,
        Selected = 1u << 1,
        MouseOver = 1u << 2,
        ColorByLayer = 1u << 3
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
        private Buffer _layerBuf;
        private ShaderResourceView _layerSrv;
        private Buffer _objectBuf;
        private ShaderResourceView _objectSrv;

        private LabelState[] _labelCpu = [];
        private PointState[] _pointCpu = [];
        private GroupState[] _groupCpu = [];
        private LayerState[] _layerCpu = [];
        private ObjectState[] _objectCpu = [];
        private int _labelCap, _groupCap, _pointCap, _layerCap, _objectCap;

        public D3dStateBuffers(Device device, DeviceContext ctx)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        public ShaderResourceView LabelSRV => _labelSrv;
        public ShaderResourceView PointSRV => _pointSrv;
        public ShaderResourceView GroupSRV => _groupSrv;
        public ShaderResourceView LayerSRV => _layerSrv;
        public ShaderResourceView ObjectSRV => _objectSrv;
        public Span<LabelState> LabelSpan => _labelCpu.AsSpan(0, _labelCap);
        public Span<PointState> PointSpan => _pointCpu.AsSpan(0, _pointCap);
        public Span<GroupState> GroupSpan => _groupCpu.AsSpan(0, _groupCap);
        public Span<LayerState> LayerSpan => _layerCpu.AsSpan(0, _layerCap);
        public Span<ObjectState> ObjectSpan => _objectCpu.AsSpan(0, _objectCap);

        public void InitializeLabelState(int count, Vector2 offset, uint lId)
        {
            EnsureLabelCapacity(count);

            uint baseFlags = 0;
            //if (pg.IsVisible) { baseFlags |= (uint)LabelFlags.Visible; }
            baseFlags |= (uint)LabelFlags.Visible;

            LabelSpan[(int)lId] = new LabelState { Offset = offset, Flags = baseFlags };
        }
        public void InitializePointState(int count, CogoPoint pg, uint pId)
        {
            EnsurePointCapacity(count);

            uint baseFlags = 0;
            baseFlags |= (uint)CogoPointFlags.Visible;
            if (pg.IsSelected) { baseFlags |= (uint)CogoPointFlags.Selected; }
            if (pg.IsMouseOver) { baseFlags |= (uint)CogoPointFlags.MouseOver; }
            if (pg.HasLeaderLine) { baseFlags |= (uint)CogoPointFlags.HasLeaderLine; }
            PointSpan[(int)pId] = new PointState { Flags = baseFlags, Offset = Vector2.Zero, LeaderLineAngle = 0 };
        }
        public void InitializeGroupState(int count, PointGroup pg, uint gId)
        {
            EnsureGroupCapacity(count);

            uint baseFlags = 0;
            if (pg.IsVisible) { baseFlags |= (uint)PointGroupFlags.Visible; }
            GroupSpan[(int)gId] = new GroupState 
            { 
                Color = pg.Color.ToSharpDXVector4(), 
                Scale = (float)pg.PointScale, 
                Flags = baseFlags, 
                TextInfoBaseXoffset = pg.PointInfoBaseXoffset 
            };
        }
        public void InitializeLayerState(int count, ObjectLayer3D layer, uint lId)
        {
            EnsureLayerCapacity(count);
            uint baseFlags = 0;
            if (layer.IsVisible) { baseFlags |= (uint)LayerFlags.Visible; }
            LayerSpan[(int)lId] = new LayerState { Color = layer.Color, Flags = baseFlags };
        }
        public void InitializeObjectState(int count, DrawingObject3D obj, uint oId)
        {
            EnsureObjectCapacity(count);
            
            uint baseFlags = 0;
            baseFlags |= (uint)ObjectFlags.Visible;
            if (obj.IsSelected) { baseFlags |= (uint)ObjectFlags.Selected; }
            if (obj.IsMouseOver) { baseFlags |= (uint)ObjectFlags.MouseOver; }
            if (obj.ColorByLayer) { baseFlags |= (uint)ObjectFlags.ColorByLayer; }
            ObjectSpan[(int)oId] = new ObjectState { Color = obj.Color, Flags = baseFlags };
        }

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

        public void EnsureLayerCapacity(int count)
        {
            if (count <= _layerCap) return;
            _layerCap = NextPow2(count);
            Array.Resize(ref _layerCpu, _layerCap);

            _layerSrv?.Dispose(); _layerBuf?.Dispose();
            var desc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<LayerState>() * _layerCap,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = Utilities.SizeOf<LayerState>()
            };
            _layerBuf = new Buffer(_device, desc);
            _layerSrv = new ShaderResourceView(_device, _layerBuf, new ShaderResourceViewDescription
            {
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource { FirstElement = 0, ElementCount = _layerCap }
            });
        }
        public void EnsureObjectCapacity(int count)
        {
            if (count <= _objectCap) return;
            _objectCap = NextPow2(count);
            Array.Resize(ref _objectCpu, _objectCap);

            _objectSrv?.Dispose(); _objectBuf?.Dispose();
            var desc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<ObjectState>() * _objectCap,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = Utilities.SizeOf<ObjectState>()
            };
            _objectBuf = new Buffer(_device, desc);
            _objectSrv = new ShaderResourceView(_device, _objectBuf, new ShaderResourceViewDescription
            {
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource { FirstElement = 0, ElementCount = _objectCap }
            });
        }

        public void FlushAll()
        {
            if (_groupBuf is null || _labelBuf is null || _pointBuf is null) { return; }
            DataStream s;

            // layers (few): discard whole
            _ctx.MapSubresource(_layerBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
            s.WriteRange(_layerCpu, 0, _layerCap);
            _ctx.UnmapSubresource(_layerBuf, 0);

            // objects (many): discard whole on full rebuild
            _ctx.MapSubresource(_objectBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
            s.WriteRange(_objectCpu, 0, _objectCap);
            _ctx.UnmapSubresource(_objectBuf, 0);

            // groups (few): discard whole
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

        public void FlushLayerSubset(HashSet<uint> dirty)
        {
            if (dirty == null || dirty.Count == 0) return;

            DataStream s;
            _ctx.MapSubresource(_layerBuf, 0, MapMode.WriteNoOverwrite, MapFlags.None, out s);
            int stride = Utilities.SizeOf<LayerState>();
            foreach (var id in dirty)
            {
                s.Position = id * stride;
                s.Write(_layerCpu[id]);
            }
            _ctx.UnmapSubresource(_layerBuf, 0);
        }

        public void FlushObjectSubset(HashSet<uint> dirty)
        {
            if (dirty == null || dirty.Count == 0) { return; }

            DataStream s;
            _ctx.MapSubresource(_objectBuf, 0, MapMode.WriteNoOverwrite, MapFlags.None, out s);
            int stride = Utilities.SizeOf<ObjectState>();
            foreach (var id in dirty)
            {
                s.Position = id * stride;
                s.Write(_objectCpu[id]);
            }
            _ctx.UnmapSubresource(_objectBuf, 0);
        }

        public void FlushGroupSubset(HashSet<uint> dirty)
        {
            if (dirty == null || dirty.Count == 0) return;

            DataStream s;
            _ctx.MapSubresource(_groupBuf, 0, MapMode.WriteNoOverwrite, MapFlags.None, out s);
            int stride = Utilities.SizeOf<GroupState>();
            foreach (var id in dirty)
            {
                s.Position = id * stride;
                s.Write(_groupCpu[id]);
            }
            _ctx.UnmapSubresource(_groupBuf, 0);
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
            _layerSrv?.Dispose(); _layerBuf?.Dispose();
        }

        private static int NextPow2(int v)
        {
            v--; v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16; v++;
            return Math.Max(16, v);
        }
    }
}
