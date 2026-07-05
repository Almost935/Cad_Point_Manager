using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using System.Windows.Media;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class CreatePointGroupCommand : IUndoableCommand
    {
        private readonly CadManager _cadManager;
        private string _groupName;
        private Color _color;

        private PointGroup? _createdPointGroup;
        private bool _succeeded;
        private string? _errorMessage;

        public PointGroup? CreatedPointGroup => _createdPointGroup;
        public bool Succeeded => _succeeded;
        public string? ErrorMessage => _errorMessage;
        public string Description => "Create PointGroup";

        public CreatePointGroupCommand(CadManager cadManager, string groupName, Color color)
        {
            _cadManager = cadManager;
            _groupName = groupName;
            _color = color;
        }

        public void Execute()
        {
            _succeeded = _cadManager.TryCreatePointGroupInternal(_groupName, _color, out _createdPointGroup, out _errorMessage);
        }

        public void Undo()
        {
            if (_createdPointGroup is null) { return; }

            _cadManager.DeletePointGroup(_createdPointGroup);
        }
    }
}
