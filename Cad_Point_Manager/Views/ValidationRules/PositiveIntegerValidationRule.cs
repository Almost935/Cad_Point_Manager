using System.Globalization;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.ValidationRules
{
    public class PositiveIntegerValidationRule : ValidationRule
    {
        public PositiveIntegerValidationRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                int pointNum = 0;
                if (((string)value).Length > 0)
                    pointNum = Int32.Parse((String)value);
            }
            catch (Exception)
            {
                return new ValidationResult(false, $"Value must be a valid integer that is greater than zero");
            }

            return ValidationResult.ValidResult;
        }
    }
}
