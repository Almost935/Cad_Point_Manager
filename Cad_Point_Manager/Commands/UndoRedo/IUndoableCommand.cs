namespace Cad_Point_Manager.Commands.UndoRedo
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();

        bool Succeeded { get; }
        string? ErrorMessage { get; }
        string Description { get; }
    }
}
