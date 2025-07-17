using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.ValidationRules
{
    public class PositiveDoubleValidationRule : ValidationRule
    {
        public PositiveDoubleValidationRule() { }

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

            if (d <= 0.0)
            {
                return new ValidationResult(false, $"Value must be greater than 0.0, but was {d}.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
