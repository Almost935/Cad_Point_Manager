using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using System.Windows.Media;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class CreatePointGroupCommand : IUndoableCommand
    {
        private readonly CadManager _cadManager;
        private readonly string _initialGroupName;
        private string _finalGroupName;
        private readonly Color _color;

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
            _initialGroupName = groupName;
            _finalGroupName = groupName;
            _color = color;
        }

        public void Execute()
        {
            string name = _createdPointGroup == null ? _initialGroupName : _finalGroupName;
            _succeeded = _cadManager.TryCreatePointGroupInternal(
                name, _color, out _createdPointGroup, out _errorMessage);
        }

        public void Undo()
        {
            if (_createdPointGroup is null) { return; }

            _cadManager.DeletePointGroup(_createdPointGroup);
        }

        public void SetFinalName(string newName)
        {
            _finalGroupName = newName;

            if (_createdPointGroup != null)
            {
                _createdPointGroup.Name = newName;
            }
        }
    }
}
