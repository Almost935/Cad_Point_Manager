using Cad_Point_Manager.Extensions;
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
        private readonly CadManager _cadManager;

        private readonly int _pointNumber;
        private readonly Vector3 _position;
        private readonly PointGroup _group;
        private readonly float _elevation;
        private readonly string _description;

        private CogoPoint? _createdPoint;
        private bool _succeeded;
        private string? _errorMessage;

        public bool Succeeded => _succeeded;
        public string? ErrorMessage => _errorMessage;
        public string Description => "Create Point";

        public CogoPoint? CreatedPoint => _createdPoint;

        public CreatePointCommand(
            CadManager cadManager,
            int pointNumber,
            Vector3 position,
            PointGroup group,
            float elevation,
            string description)
        {
            _cadManager = cadManager;
            _pointNumber = pointNumber;
            _position = position;
            _group = group;
            _elevation = elevation;
            _description = description;
        }

        public CreatePointCommand(
            CadManager cadManager,
            CogoPoint cogoPoint)
        {
            _cadManager = cadManager;
            _pointNumber = cogoPoint.PointNumber;
            _position = cogoPoint.Position.ToSharpDXVector3();
            _group = cogoPoint.PointGroup;
            _elevation = cogoPoint.Elevation.ToFloat();
            _description = cogoPoint.Description;
        }

        public void Execute()
        {
            _succeeded = _cadManager.TryCreatePointInternal(
                _pointNumber,
                _position,
                _group,
                out _createdPoint,
                out _errorMessage,
                _elevation,
                _description);
        }

        public void Undo()
        {
            if (_createdPoint is null) { return; }

            _cadManager.TryDeletePointInternal(_createdPoint);
        }

        //public void MarkDirty()
        //{
        //    _cadManager.CogoPointCircleVerticesDirty = true;
        //    _cadManager.CogoPointTextVerticesDirty = true;
        //    //_cadManager.HitTestableObjectTreeDirty = true;
        //    _cadManager.UpdateExtents();
        //}
    }
}
