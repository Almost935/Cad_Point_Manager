using Cad_Point_Manager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class UndoRedoManager : BaseModel
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Execute(IUndoableCommand command)
        {
            command.Execute();

            _undoStack.Push(command);
            _redoStack.Clear();

            RaiseStateChanged();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) { return; }

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            RaiseStateChanged();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) { return; }

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            RaiseStateChanged();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();

            RaiseStateChanged();
        }

        private void RaiseStateChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }
    }
}
