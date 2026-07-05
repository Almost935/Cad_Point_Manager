using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class CreateLayoutCommand : IUndoableCommand
    {
        private readonly CadManager _cadManager;
        private string _layoutName;
        private LayoutViewport _layoutViewport;

        private Layout? _createdLayout;
        private bool _succeeded;
        private string? _errorMessage;

        public Layout? CreatedLayout => _createdLayout;
        public bool Succeeded => _succeeded;
        public string? ErrorMessage => _errorMessage;
        public string Description => "Create Layout";

        public CreateLayoutCommand(CadManager cadManager, string layoutName, LayoutViewport layoutViewport)
        {
            _cadManager = cadManager;
            _layoutName = layoutName;
            _layoutViewport = layoutViewport;
        }

        public void Execute()
        {
            _succeeded = _cadManager.TryCreateLayoutInternal(_layoutName, _layoutViewport, out _createdLayout, out _errorMessage);
        }

        public void Undo()
        {
            if (_createdLayout is null) { return; }

            _cadManager.TryDeleteLayout(_createdLayout);
        }
    }
}
