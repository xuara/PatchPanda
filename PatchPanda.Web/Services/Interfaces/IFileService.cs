namespace PatchPanda.Web.Services.Interfaces
{
    internal interface IFileService
    {
        bool Exists(string? path);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
    }
}
