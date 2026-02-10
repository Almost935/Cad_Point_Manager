namespace Cad_Point_Manager.Services.Dialogs
{
    public interface IFileSaveDialogService
    {
        bool TryPickSavePath(FileSaveDialogRequest request, out string fullPath);
    }
}
