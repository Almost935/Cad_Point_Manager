using Cad_Point_Manager.Models;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class CompositeCommand : IUndoableCommand
    {
        private readonly CadManager _cadManager;
        private readonly List<IUndoableCommand> _commands;

        private bool _succeeded;
        private string? _errorMessage;
        private bool _containsCogoPointCommands = false;

        public bool Succeeded => _succeeded;
        public string? ErrorMessage => _errorMessage;

        public string Description { get; }

        public CompositeCommand(
            CadManager cadManager,
            string description,
            IEnumerable<IUndoableCommand> commands)
        {
            _cadManager = cadManager;
            Description = description;
            _commands = commands.ToList();

            if (_commands.OfType<CreatePointCommand>().Any() ||
                _commands.OfType<DeletePointCommand>().Any() ||
                _commands.OfType<ImportPointsCommand>().Any())
            {
                _containsCogoPointCommands = true;
            }
        }

        public void Execute()
        {
            foreach (var cmd in _commands)
            {
                cmd.Execute();
            }

            if (_containsCogoPointCommands)
            {
                _cadManager.CogoPointCircleVerticesDirty = true;
                _cadManager.CogoPointTextVerticesDirty = true;
            }
        }

        public void Undo()
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }

            if (_containsCogoPointCommands)
            {
                _cadManager.CogoPointCircleVerticesDirty = true;
                _cadManager.CogoPointTextVerticesDirty = true;
            }
        }

        public void SetFailure(string errorMessage)
        {
            _succeeded = false;
            _errorMessage = errorMessage;
        }
    }
}
