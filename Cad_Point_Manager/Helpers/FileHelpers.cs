using System.IO;

namespace Cad_Point_Manager.Helpers
{
    public static class FileHelpers
    {
        public static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return "Untitled"; }

            foreach (var c in Path.GetInvalidFileNameChars()) { name = name.Replace(c, '_'); }

            return name.Trim();
        }

        public static void TryOpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Could not open file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("File does not exist.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
