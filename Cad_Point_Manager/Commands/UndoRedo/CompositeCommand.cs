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
            CadManager cadManager, string description, IEnumerable<IUndoableCommand> commands)
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
            _succeeded = true;
            _errorMessage = null;

            void ExecuteCommands()
            {
                foreach (var cmd in _commands)
                {
                    cmd.Execute();

                    if (!cmd.Succeeded)
                    {
                        _succeeded = false;

                        _errorMessage ??= cmd.ErrorMessage ?? $"Command failed: {cmd.Description}";
                    }
                }
            }

            if (_containsCogoPointCommands)
            {
                using (_cadManager.CogoPoints.DeferNotifications())
                {
                    ExecuteCommands();
                }

                _cadManager.CogoPointCircleVerticesDirty = true;
                _cadManager.CogoPointTextVerticesDirty = true;
            }
            else
            {
                ExecuteCommands();
            }
        }

        public void Undo()
        {
            if (_containsCogoPointCommands)
            {
                using (_cadManager.CogoPoints.DeferNotifications())
                {
                    for (int i = _commands.Count - 1; i >= 0; i--)
                    {
                        _commands[i].Undo();
                    }
                }

                _cadManager.CogoPointCircleVerticesDirty = true;
                _cadManager.CogoPointTextVerticesDirty = true;
            }
            else
            {
                for (int i = _commands.Count - 1; i >= 0; i--)
                {
                    _commands[i].Undo();
                }
            }
        }

        public void SetFailure(string errorMessage)
        {
            _succeeded = false;
            _errorMessage = errorMessage;
        }
    }
}
