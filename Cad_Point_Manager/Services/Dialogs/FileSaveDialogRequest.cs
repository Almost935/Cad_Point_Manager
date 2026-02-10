namespace Cad_Point_Manager.Services.Dialogs
{
    public sealed class FileSaveDialogRequest
    {
        public string Title { get; set; } = "Save File";
        public string Filter { get; set; } = "All files (*.*)|*.*";
        public string DefaultExtension { get; set; } = "";
        public string InitialDirectory { get; set; } = "";
        public string DefaultFileName { get; set; } = "";
        public bool OverwritePrompt { get; set; } = true;
    }
}
