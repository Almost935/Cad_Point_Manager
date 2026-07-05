using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class DeletePointCommand : IUndoableCommand
    {
        private readonly CadManager _cadManager;

        private readonly CogoPoint _point;
        private readonly PointGroup _group;

        private bool _disposed;

        private bool _succeeded;
        private string? _errorMessage;

        public bool Succeeded => _succeeded;
        public string? ErrorMessage => _errorMessage;
        public string Description => "Delete Point";
        public bool Disposed => _disposed;

        public DeletePointCommand(
            CadManager cadManager,
            CogoPoint point)
        {
            _cadManager = cadManager;
            _point = point;
            _group = point.PointGroup;
        }

        public void Execute()
        {
            _cadManager.TryDeletePointInternal(_point);

            MarkDirty();
        }

        public void Undo()
        {
            _cadManager.TryAddPoint(_point, _group);

            MarkDirty();
        }

        private void MarkDirty()
        {
            //_cadManager.CogoPointCircleVerticesDirty = true;
            //_cadManager.CogoPointTextVerticesDirty = true;
            //_cadManager.HitTestableObjectTreeDirty = true;

            //_cadManager.UpdateExtents();
        }
    }
}
