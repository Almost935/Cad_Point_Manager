using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.ValidationRules
{
    public class PointNumberValidationRule : ValidationRule
    {
        public PointNumberValidationRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int pointNum = 0;

            try
            {
                if (((string)value).Length > 0)
                    pointNum = Int32.Parse((String)value);
            }
            catch (Exception)
            {
                return new ValidationResult(false, $"Point number must be a positive integer");
            }

            return ValidationResult.ValidResult;
        }
    }
}
