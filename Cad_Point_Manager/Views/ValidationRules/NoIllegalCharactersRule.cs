using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.ValidationRules
{
    public class NoIllegalCharactersRule : ValidationRule
    {
        // Set of characters considered illegal
        public string IllegalCharacters { get; set; } = "@#$%";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string ?? string.Empty;

            foreach (char c in IllegalCharacters)
            {
                if (input.Contains(c))
                {
                    return new ValidationResult(false, $"Input contains illegal character: {c}");
                }
            }

            return ValidationResult.ValidResult;
        }
    }
}
