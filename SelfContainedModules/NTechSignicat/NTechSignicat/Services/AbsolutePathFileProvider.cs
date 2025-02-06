using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace NTechSignicat.Services
{
    public class AbsolutePathFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var fi = new System.IO.FileInfo(subpath);
            return new FileInfoImpl
            {
                Exists = fi.Exists,
                IsDirectory = false,
                LastModified = fi.LastWriteTime,
                Length = fi.Length,
                Name = fi.Name,
                PhysicalPath = fi.FullName
            };
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }

        public class FileInfoImpl : IFileInfo
        {
            public bool Exists { get; set; }

            public long Length { get; set; }

            public string PhysicalPath { get; set; }

            public string Name { get; set; }

            public DateTimeOffset LastModified { get; set; }

            public bool IsDirectory { get; set; }

            public Stream CreateReadStream()
            {
                return System.IO.File.OpenRead(PhysicalPath);
            }
        }
    }
}
