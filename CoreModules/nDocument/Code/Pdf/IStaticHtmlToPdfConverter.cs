namespace nDocument.Pdf
{
    public interface IStaticHtmlToPdfConverter
    {
        bool TryRenderToTempFile(string templateFilePath, string targetFile, string logFile);
    }
}
