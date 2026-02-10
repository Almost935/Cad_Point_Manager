using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Cad_Point_Manager.Services.Dialogs
{
    public class FileSaveDialogService : IFileSaveDialogService
    {
        public bool TryPickSavePath(FileSaveDialogRequest request, out string fullPath)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }

            var dlg = new SaveFileDialog
            {
                Title = request.Title ?? "Save File",
                Filter = string.IsNullOrWhiteSpace(request.Filter) ? "All files (*.*)|*.*" : request.Filter,
                DefaultExt = request.DefaultExtension ?? "",
                AddExtension = true,
                OverwritePrompt = request.OverwritePrompt,
                FileName = request.DefaultFileName ?? ""
            };

            if (!string.IsNullOrWhiteSpace(request.InitialDirectory) && Directory.Exists(request.InitialDirectory))
            { dlg.InitialDirectory = request.InitialDirectory; }

            var ok = dlg.ShowDialog(Application.Current.MainWindow) == true;
            fullPath = ok ? dlg.FileName : null;
            return ok;
        }
    }
}
