using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public readonly struct DxfTag(int code, string value)
    {
        public int Code { get; } = code;
        public string Value { get; } = value;

        public override string ToString()
        {
            return $"{Code}: {Value}";
        }
    }
}
