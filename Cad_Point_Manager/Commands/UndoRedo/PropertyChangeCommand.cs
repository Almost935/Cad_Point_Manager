namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class PropertyChangeCommand<T> : IUndoableCommand
    {
        #region Fields
        private readonly Action<T> _setter;

        private readonly T _oldValue;
        private readonly T _newValue;

        private readonly string _description;
        #endregion

        #region Properties
        public bool Succeeded { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string Description => _description;
        #endregion

        #region Constructor
        public PropertyChangeCommand(
            string description,
            Action<T> setter,
            T oldValue,
            T newValue)
        {
            _description = description;
            _setter = setter;
            _oldValue = oldValue;
            _newValue = newValue;
        }
        #endregion

        #region Methods
        public void Execute()
        {
            _setter(_newValue);

            Succeeded = true;
        }

        public void Undo()
        {
            _setter(_oldValue);
        }
        #endregion
    }
}
