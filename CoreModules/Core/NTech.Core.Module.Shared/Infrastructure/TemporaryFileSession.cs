using System;
using System.IO;

namespace NTech.Services.Infrastructure
{
    /// <summary>
    /// Returns a temporary directory that will be deleted on dispose but where failures
    /// to delete will be ignored.
    /// </summary>
    public class TemporaryDirectory : InnerTemporaryDirectory, IDisposable
    {
        public TemporaryDirectory() : base(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
        {

        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (Directory.Exists(FullDirectory.FullName))
                            Directory.Delete(FullDirectory.FullName, true);
                    }
                    catch
                    {
                        /* Ignored. If tempfiles are held for some reason we let the OS deal with them later */
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class InnerTemporaryDirectory
    {
        internal InnerTemporaryDirectory(string fullDirectoryName)
        {
            Directory.CreateDirectory(fullDirectoryName);
            FullDirectory = new DirectoryInfo(fullDirectoryName);
        }
        public DirectoryInfo FullDirectory { get; set; }
        public string FullName => FullDirectory.FullName;

        /// <summary>
        /// So let's say FullDirectoryName is c:\temp\abc123
        /// GetRelativeTempFile("kitten.txt") -> c:\temp\abc123\kitten.txt
        /// GetRelativeTempFile(@"test\kitten.txt") -> c:\temp\abc123\test\kitten.txt
        /// All intermediate directories (like \test\ will be created) but the actual file will not.
        /// </summary>
        public FileInfo GetRelativeTempFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || Path.IsPathRooted(filename))
                throw new ArgumentException("filename must non null and relative");
            var fullFilename = Path.Combine(FullDirectory.FullName, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(fullFilename));
            return new FileInfo(fullFilename);
        }

        /// <summary>
        /// So let's say FullDirectoryName is c:\temp\abc123
        /// GetRelativeTempDirectory("test") -> c:\temp\test
        /// GetRelativeTempDirectory(@"test\moretest") -> c:\temp\test\moretest
        /// The directories will be created.
        /// </summary>
        public InnerTemporaryDirectory GetRelativeTempDirectory(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName) || Path.IsPathRooted(directoryName))
                throw new ArgumentException("directoryName must be non null and relative");
            var dir = Path.Combine(FullDirectory.FullName, directoryName);
            return new InnerTemporaryDirectory(dir);
        }
    }
}
