using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class CreatePointCommand : IUndoableCommand
    {
        private readonly CogoPointManager _pointManager;

        private readonly int _pointNumber;
        private readonly Vector3 _position;
        private readonly PointGroup _group;
        private readonly float _elevation;
        private readonly string _description;

        private CogoPoint? _createdPoint;

        public string Description => "Create Point";

        public CreatePointCommand(
            CogoPointManager pointManager,
            int pointNumber,
            Vector3 position,
            PointGroup group,
            float elevation,
            string description)
        {
            _pointManager = pointManager;
            _pointNumber = pointNumber;
            _position = position;
            _group = group;
            _elevation = elevation;
            _description = description;
        }

        public void Execute()
        {
            _pointManager.TryAddPoint(
                _pointNumber,
                _position,
                _group,
                out _createdPoint,
                _elevation,
                _description);

            MarkDirty();
        }

        public void Undo()
        {
            if (_createdPoint is null)
                return;

            _pointManager.RemovePoint(_createdPoint);

            MarkDirty();
        }

        private void MarkDirty()
        {
            var cad = _pointManager.CadManager;

            cad.CogoPointCircleVerticesDirty = true;
            cad.CogoPointTextVerticesDirty = true;
            cad.HitTestableObjectTreeDirty = true;

            cad.UpdateExtents();
        }
    }
}
