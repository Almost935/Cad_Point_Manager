using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class DeletePointCommand : IUndoableCommand
    {
        private readonly CadManager _cadManager;

        private readonly CogoPoint _point;
        private readonly PointGroup _group;

        private bool _succeeded;
        private string? _errorMessage;

        public bool Succeeded => _succeeded;
        public string? ErrorMessage => _errorMessage;
        public string Description => "Delete Point";

        public DeletePointCommand(CadManager cadManager, CogoPoint point)
        {
            _cadManager = cadManager;
            _point = point;
            _group = point.PointGroup;
        }

        public void Execute()
        {
            _succeeded = _cadManager.TryDeletePointInternal(_point);

            if (!_succeeded)
            {
                _errorMessage = $"Failed to delete point {_point.PointNumber}.";
            }
        }

        public void Undo()
        {
            _succeeded = _cadManager.RestorePointInternal(_point, _group);

            if (!_succeeded)
                _errorMessage = $"Could not restore point {_point.PointNumber}.";
        }
    }
}
