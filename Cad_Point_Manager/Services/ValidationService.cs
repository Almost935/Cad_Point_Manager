using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Services
{
    public class ValidationService
    {
        private static readonly char[] IllegalGroupNameChars = ['@', '#', '%', '^', '&', '*'];

        public bool ValidatePointNumber(string input, CogoPointManager cogoPointManager, out string errorMessage)
        {
            if (!int.TryParse(input, out int pointNumber))
            {
                errorMessage = "Point number must be a valid integer.";
                return false;
            }

            if (pointNumber <= 0)
            {
                errorMessage = "Point number must be greater than zero.";
                return false;
            }

            if (cogoPointManager.PointExists(pointNumber))
            {
                errorMessage = $"Point number {pointNumber} is already in use.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidatePointNumber(int pointNumber, CogoPointManager cogoPointManager, out string errorMessage)
        {
            if (pointNumber <= 0)
            {
                errorMessage = "Point number must be greater than zero.";
                return false;
            }

            if (cogoPointManager.PointExists(pointNumber))
            {
                errorMessage = $"Point number {pointNumber} is already in use.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidatePointGroupName(string input, IEnumerable<PointGroup> existingGroups, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Point group name is required.";
                return false;
            }

            if (input.IndexOfAny(IllegalGroupNameChars) >= 0)
            {
                errorMessage = "Point group name contains illegal characters.";
                return false;
            }

            if (existingGroups.Any(pg => string.Equals(pg.Name, input, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"Point group name \"{input}\" already exists.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidateString(string input, out string errorMessage)
        {
            errorMessage = null;
            if (input == null) { return false; }

            string illegalChars = "";
            bool isValid = true;
            foreach (var character in IllegalGroupNameChars)
            {
                if (input.Contains(character))
                {
                    isValid = false;
                    if (string.IsNullOrEmpty(illegalChars))
                    {
                        illegalChars = $"{character}";
                    }
                    else
                    {
                        illegalChars += $", {character}";
                    }
                }
            }

            return isValid;
        }
    }
}
