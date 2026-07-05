
// Rendering/D3D/D3dStateBuffers.cs
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
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
        HasLeaderLine = 1u << 3,
        MouseOverAnchor = 1u << 4,
        AnchorPressed = 1u << 5,
        IsFlippedY = 1u << 6,
        IsFlippedX = 1u << 7
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
        public void InitializePointState(int count, CogoPoint cp, uint pId, uint gId)
        {
            EnsurePointCapacity(count);

            uint baseFlags = 0;
            baseFlags |= (uint)CogoPointFlags.Visible;
            if (cp.IsSelected) { baseFlags |= (uint)CogoPointFlags.Selected; }
            if (cp.IsMouseOver) { baseFlags |= (uint)CogoPointFlags.MouseOver; }
            if (cp.HasLeaderLine) { baseFlags |= (uint)CogoPointFlags.HasLeaderLine; }
            if (cp.IsFlippedY) { baseFlags |= (uint)CogoPointFlags.IsFlippedY; }
            if (cp.IsFlippedX) { baseFlags |= (uint)CogoPointFlags.IsFlippedX; }
            PointSpan[(int)pId] = new PointState
            {
                Offset = cp.Position.ToSharpDXVector2(),
                PointInfoOffset = Vector2.Zero,
                GroupId = gId,
                Flags = baseFlags
            };
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
        public void InitializeLayerState(int count, ObjectLayer layer, uint lId)
        {
            EnsureLayerCapacity(count);
            uint baseFlags = 0;
            if (layer.IsVisible) { baseFlags |= (uint)LayerFlags.Visible; }

            var color = layer.Color;
            if (color.X == 1f && color.Y == 1f && color.Z == 1f) { color = new Vector4(0, 0, 0, 1); } // white layers converted to black like autocad does

            LayerSpan[(int)lId] = new LayerState { Color = color, Flags = baseFlags };
        }
        public void InitializeObjectState(int count, DrawingObject obj, uint oId)
        {
            EnsureObjectCapacity(count);

            uint baseFlags = 0;
            baseFlags |= (uint)ObjectFlags.Visible;
            if (obj.IsSelected) { baseFlags |= (uint)ObjectFlags.Selected; }
            if (obj.IsMouseOver) { baseFlags |= (uint)ObjectFlags.MouseOver; }

            Vector4 color = obj.GetColor();
            if (obj.ColorType == ColorType.ByLayer) { color = obj.Layer.Color; baseFlags |= (uint)ObjectFlags.ColorByLayer; }
            else if (obj.ColorType == ColorType.ByBlock)
            {
                color = obj.BlockColor;
            }

            if (color.X == 1f && color.Y == 1f && color.Z == 1f) { color = new Vector4(0, 0, 0, 1); } // white objects converted to black like autocad doesS

            ObjectSpan[(int)oId] = new ObjectState { Color = color, Flags = baseFlags };
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
                BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource
                {
                    FirstElement = 0,
                    ElementCount = _pointCap
                }
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
            //if (_groupBuf is null || _labelBuf is null || _pointBuf is null) { return; }
            DataStream s;

            if (_layerBuf is not null)
            {
                // layers (few): discard whole
                _ctx.MapSubresource(_layerBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
                s.WriteRange(_layerCpu, 0, _layerCap);
                _ctx.UnmapSubresource(_layerBuf, 0);
            }

            if (_objectBuf is not null)
            {
                // objects (many): discard whole on full rebuild
                _ctx.MapSubresource(_objectBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
                s.WriteRange(_objectCpu, 0, _objectCap);
                _ctx.UnmapSubresource(_objectBuf, 0);
            }

            if (_groupBuf is not null)
            {
                // groups (few): discard whole
                _ctx.MapSubresource(_groupBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
                s.WriteRange(_groupCpu, 0, _groupCap);
                _ctx.UnmapSubresource(_groupBuf, 0);
            }

            if (_pointBuf is not null)
            {
                // points (many): discard whole on full rebuild
                _ctx.MapSubresource(_pointBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
                s.WriteRange(_pointCpu, 0, _pointCap);
                _ctx.UnmapSubresource(_pointBuf, 0);
            }

            if (_labelBuf is not null)
            {
                // labels (many): discard whole on full rebuild
                _ctx.MapSubresource(_labelBuf, 0, MapMode.WriteDiscard, MapFlags.None, out s);
                s.WriteRange(_labelCpu, 0, _labelCap);
                _ctx.UnmapSubresource(_labelBuf, 0);
            }
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

        // D3dStateBuffers.cs  (inside class)
        public void MaybeShrinkAllTo25PctOrLess(
            int labelUsed, int pointUsed, int groupUsed, int layerUsed, int objectUsed,
            Action<DeviceContext> unbindAllSrvs)
        {
            // We need SRVs unbound before recreating to avoid D3D warnings.
            unbindAllSrvs?.Invoke(_ctx);

            const float THRESH = 0.25f;

            if (_labelCap > 0 && labelUsed < _labelCap * THRESH) RecreateLabelCap(ToPow2AtLeast(labelUsed));
            if (_pointCap > 0 && pointUsed < _pointCap * THRESH) RecreatePointCap(ToPow2AtLeast(pointUsed));
            if (_groupCap > 0 && groupUsed < _groupCap * THRESH) RecreateGroupCap(ToPow2AtLeast(groupUsed));
            if (_layerCap > 0 && layerUsed < _layerCap * THRESH) RecreateLayerCap(ToPow2AtLeast(layerUsed));
            if (_objectCap > 0 && objectUsed < _objectCap * THRESH) RecreateObjectCap(ToPow2AtLeast(objectUsed));
        }
        private static int ToPow2AtLeast(int n)
        {
            if (n <= 0) return 0;
            int p = 1; while (p < n) p <<= 1; return p;
        }
        private void RecreateLabelCap(int newCap)
        {
            if (newCap == _labelCap) return;
            _labelSrv?.Dispose(); _labelSrv = null;
            _labelBuf?.Dispose(); _labelBuf = null;

            _labelCap = newCap;
            Array.Resize(ref _labelCpu, _labelCap);

            if (_labelCap == 0) return;

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
        private void RecreatePointCap(int newCap)
        {
            if (newCap == _pointCap) return;
            _pointSrv?.Dispose(); _pointSrv = null;
            _pointBuf?.Dispose(); _pointBuf = null;

            _pointCap = newCap;
            Array.Resize(ref _pointCpu, _pointCap);

            if (_pointCap == 0) return;

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
        private void RecreateGroupCap(int newCap)
        {
            if (newCap == _groupCap) return;
            _groupSrv?.Dispose(); _groupSrv = null;
            _groupBuf?.Dispose(); _groupBuf = null;

            _groupCap = newCap;
            Array.Resize(ref _groupCpu, _groupCap);

            if (_groupCap == 0) return;

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
        private void RecreateLayerCap(int newCap)
        {
            if (newCap == _layerCap) return;
            _layerSrv?.Dispose(); _layerSrv = null;
            _layerBuf?.Dispose(); _layerBuf = null;

            _layerCap = newCap;
            Array.Resize(ref _layerCpu, _layerCap);

            if (_layerCap == 0) return;

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
        private void RecreateObjectCap(int newCap)
        {
            if (newCap == _objectCap) return;
            _objectSrv?.Dispose(); _objectSrv = null;
            _objectBuf?.Dispose(); _objectBuf = null;

            _objectCap = newCap;
            Array.Resize(ref _objectCpu, _objectCap);

            if (_objectCap == 0) return;

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

        public void ResetFull()
        {
            Dispose();

            _labelCap = 0;
            _pointCap = 0;
            _groupCap = 0;
            _layerCap = 0;
            _objectCap = 0;

            _labelCpu = [];
            _pointCpu = [];
            _groupCpu = [];
            _layerCpu = [];
            _objectCpu = [];
        }


        public void Dispose()
        {
            _labelSrv?.Dispose();
            _labelSrv = null;

            _labelBuf?.Dispose();
            _labelBuf = null;

            _pointSrv?.Dispose();
            _pointSrv = null;

            _pointBuf?.Dispose();
            _pointBuf = null;

            _groupSrv?.Dispose();
            _groupSrv = null;

            _groupBuf?.Dispose();
            _groupBuf = null;

            _layerSrv?.Dispose();
            _layerSrv = null;

            _layerBuf?.Dispose();
            _layerBuf = null;

            _objectSrv?.Dispose();
            _objectSrv = null;

            _objectBuf?.Dispose();
            _objectBuf = null;
        }

        private static int NextPow2(int v)
        {
            v--; v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16; v++;
            return Math.Max(16, v);
        }
    }
}
