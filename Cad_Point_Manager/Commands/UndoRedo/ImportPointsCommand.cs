using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Importing;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class ImportPointsCommand : IUndoableCommand
    {
        #region Fields
        private readonly CadManager _cadManager;

        private readonly List<ParsedPointImportRow> _rows;

        private readonly CompositeCommand _composite;
        #endregion

        #region Properties
        public bool Succeeded => _composite.Succeeded;

        public string? ErrorMessage => _composite.ErrorMessage;

        public string Description => "Import Points";
        #endregion

        #region Constructor
        public ImportPointsCommand(
            CadManager cadManager,
            IEnumerable<ParsedPointImportRow> rows)
        {
            _cadManager = cadManager;

            _rows = rows.ToList();

            List<IUndoableCommand> commands = [];

            // Prevent duplicate group creation commands
            HashSet<string> createdGroups =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (var row in _rows)
            {
                var pg = _cadManager.GetPointGroup(row.PointGroup);

                Vector3 position = new(
                    (float)row.Easting,
                    (float)row.Northing,
                    0);

                commands.Add(
                    new CreatePointCommand(
                        _cadManager,
                        row.PointNumber,
                        position,
                        pg,
                        (float)(row.Elevation ?? 0),
                        row.Description ?? ""));
            }

            _composite = new CompositeCommand(
                _cadManager,
                "Import Points",
                commands);
        }
        #endregion

        #region Methods
        public void Execute()
        {
            _composite.Execute();
        }

        public void Undo()
        {
            _composite.Undo();
        }
        #endregion
    }
}
