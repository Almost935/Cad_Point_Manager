using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Commands.UndoRedo
{
    public class DeletePointCommand : IUndoableCommand
    {
        private readonly CogoPointManager _pointManager;

        private readonly CogoPoint _point;
        private readonly PointGroup _group;

        public string Description => "Delete Point";

        public DeletePointCommand(
            CogoPointManager pointManager,
            CogoPoint point)
        {
            _pointManager = pointManager;
            _point = point;
            _group = point.PointGroup;
        }

        public void Execute()
        {
            _pointManager.RemovePoint(_point);

            MarkDirty();
        }

        public void Undo()
        {
            _pointManager.TryAddPoint(_point, _group);

            MarkDirty();
        }

        private void MarkDirty()
        {
            var cad = _pointManager.CadManager;

            cad.CogoPointCircleVerticesDirty = true;
            cad.CogoPointTextVerticesDirty = true;
            cad.HitTestableObjectTreeDirty = true;

            cad.UpdateExtents();
        }
    }
}
