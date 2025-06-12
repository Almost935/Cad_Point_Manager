using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.ValidationRules
{
    public class DoubleValidationRule : ValidationRule
    {
        public DoubleValidationRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double d = 0;

            try
            {
                if (((string)value).Length > 0)
                    d = double.Parse((String)value);
            }
            catch (Exception)
            {
                return new ValidationResult(false, $"Invalid input.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
