using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();

        string Description { get; }
    }
}
