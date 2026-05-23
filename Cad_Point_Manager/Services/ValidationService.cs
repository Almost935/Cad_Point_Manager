using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;

namespace Cad_Point_Manager.Services
{
    public static class ValidationService
    {
        private static readonly System.Buffers.SearchValues<char> s_illegalGroupNameChars = System.Buffers.SearchValues.Create("@#%^&*");
        private static readonly char[] IllegalGroupNameChars = ['@', '#', '%', '^', '&', '*'];

        public static bool ValidateNewPointNumber(string input, CadManager cadManager, out string errorMessage)
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
            if (cadManager.PointExists(pointNumber))
            {
                errorMessage = $"Point number {pointNumber} is already in use.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool ValidatePointNumberChange(string input, CogoPoint editPoint, CadManager cadManager, out string errorMessage)
        {
            var isInt = int.TryParse(input, out int newNum);
            if (!isInt)
            {
                errorMessage = "Point number must be a valid integer.";
                return false;
            }
            if (newNum <= 0)
            {
                errorMessage = "Point number must be greater than zero.";
                return false;
            }
            if (!cadManager.ValidatePointNameChange(newNum, editPoint, out string cpmError))
            {
                errorMessage = cpmError;
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool ValidatePointGroupName(string input, IEnumerable<PointGroup> existingGroups, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Point group name is required.";
                return false;
            }

            if (input.AsSpan().IndexOfAny(s_illegalGroupNameChars) >= 0)
            {
                errorMessage = "Point group name must not contain illegal characters.";
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

        public static bool ValidateString(string input, out string errorMessage)
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

        public static bool ValidateSceneNameChange(string input, Scene scene, Camera camera, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Scene name is required.";
                return false;
            }

            if (input.AsSpan().IndexOfAny(s_illegalGroupNameChars) >= 0)
            {
                errorMessage = "Scene name must not contain illegal characters.";
                return false;
            }

            if (!camera.ValidateSceneNameChange(input, scene, out string cpmError))
            {
                errorMessage = cpmError;
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool ValidateLayoutNameChange(string input, Layout layout, CadManager cadManager, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Layout name is required.";
                return false;
            }

            if (input.AsSpan().IndexOfAny(s_illegalGroupNameChars) >= 0)
            {
                errorMessage = "Layout name must not contain illegal characters.";
                return false;
            }

            if (!cadManager.ValidateLayoutNameChange(input, layout, out string cpmError))
            {
                errorMessage = cpmError;
                return false;
            }

            errorMessage = null;
            return true;
        }

    }
}
