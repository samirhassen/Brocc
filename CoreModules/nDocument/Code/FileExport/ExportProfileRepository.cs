using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace nDocument.Code.FileExport
{
    public class ExportProfileRepository
    {
        private readonly List<ExportProfileSet> profiles;

        public ExportProfileRepository()
        {
            var path = NEnv.FileExportProfilesFile;
            var doc = XDocuments.Load(path);
            var profiles = new Dictionary<string, ExportProfile>();

            Func<XElement, string, string> req = (e, n) => e.Descendants().Single(x => x.Name.LocalName.ToLowerInvariant() == n.ToLowerInvariant()).Value;
            Func<XElement, string, string> opt = (e, n) => e.Descendants().SingleOrDefault(x => x.Name.LocalName.ToLowerInvariant() == n.ToLowerInvariant())?.Value;

            foreach (var sftp in doc.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "sftpprofile"))
            {
                var p = new SftpExportProfile
                {
                    Name = sftp.Attribute("name").Value,
                    ProfileCode = ExportProfileCode.Sftp,
                    Host = req(sftp, "Host"),
                    Port = new Func<int?>(() =>
                    {
                        var v = opt(sftp, "Port");
                        return string.IsNullOrWhiteSpace(v) ? new int?() : new int?(int.Parse(v));
                    })(),
                    UserName = req(sftp, "UserName"),
                    Password = req(sftp, "Password"),
                    FileNamePattern = opt(sftp, "FileNamePattern")
                };
                profiles[p.Name] = p;
            }
            foreach (var sftp in doc.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "sftpprofilewithprivatekeyauth"))
            {
                var p = new SftpWithPrivateKeyAuthExportProfile
                {
                    Name = sftp.Attribute("name").Value,
                    ProfileCode = ExportProfileCode.SftpWithPrivateKeyAuth,
                    Host = req(sftp, "Host"),
                    Port = new Func<int?>(() =>
                    {
                        var v = opt(sftp, "Port");
                        return string.IsNullOrWhiteSpace(v) ? new int?() : new int?(int.Parse(v));
                    })(),
                    UserName = req(sftp, "UserName"),
                    PrivateKeyPath = req(sftp, "PrivateKeyPath"),
                    PrivateKeyPassword = req(sftp, "PrivateKeyPassword"),
                    FileNamePattern = opt(sftp, "FileNamePattern")
                };
                profiles[p.Name] = p;
            }

                foreach (var f in doc.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "localfolderprofile"))
            {
                var p = new LocalFolderExportProfile
                {
                    Name = f.Attribute("name").Value,
                    ProfileCode = ExportProfileCode.LocalFolder,
                    FolderPath = req(f, "Folder"),
                    FileNamePattern = opt(f, "FileNamePattern"),
                    AllowOverwrite = (opt(f, "AllowOverwrite") ?? "false").ToLowerInvariant().Trim() == "true"
                };
                profiles[p.Name] = p;
            }

            foreach (var az in doc.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "azurebucketprofile"))
            {
                var p = new AzureBucketProfile
                {
                    SasContainerUrl = req(az, "SasContainerUrl"),
                    Name = az.Attribute("name").Value,
                    ProfileCode = ExportProfileCode.AzureBucket,
                    FileNamePattern = opt(az, "FileNamePattern")
                };
                profiles[p.Name] = p;
            }

            foreach (var awsProfile in doc.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "amazonwebservicesbucketprofile"))
            {
                var p = new AmazonWebServicesBucketProfile
                {
                    AccessKeyId = req(awsProfile, "AccessKeyId"),
                    SecretAccessKey = req(awsProfile, "SecretAccessKey"),
                    BucketName = req(awsProfile, "BucketName"),
                    RegionEndpoint = !string.IsNullOrWhiteSpace(opt(awsProfile, "RegionEndpoint")) ? Amazon.RegionEndpoint.GetBySystemName(opt(awsProfile, "RegionEndpoint")) : Amazon.RegionEndpoint.EUNorth1,
                    Name = awsProfile.Attribute("name").Value,
                    ProfileCode = ExportProfileCode.AmazonWebServicesBucket,
                    FileNamePattern = opt(awsProfile, "FileNamePattern")
                };
                profiles[p.Name] = p;
            }

            var profileSets = new List<ExportProfileSet>();

            profileSets.AddRange(profiles.Select(x => new ExportProfileSet { Name = x.Value.Name, ProfileCode = x.Value.ProfileCode, Profiles = new List<ExportProfile> { x.Value } }));

            foreach (var f in doc.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "compositeprofile"))
            {
                var profilesNames = f.Descendants().Where(x => x.Name.LocalName.ToLowerInvariant() == "includedprofilename").Select(x => x.Value).ToList();
                profileSets.Add(new ExportProfileSet
                {
                    Name = f.Attribute("name").Value,
                    ProfileCode = ExportProfileCode.Composite,
                    Profiles = profilesNames.Select(x => profiles[x]).ToList()
                });
            }

            this.profiles = profileSets;
        }

        public enum ExportProfileCode
        {
            Sftp,
            SftpWithPrivateKeyAuth,
            LocalFolder,
            Composite,
            AzureBucket,
            AmazonWebServicesBucket
        }

        public class ExportProfileSet
        {
            public string Name { get; set; }
            public ExportProfileCode ProfileCode { get; set; }
            public List<ExportProfile> Profiles { get; set; }
        }

        public class ExportProfile
        {
            public string Name { get; set; }
            public string FileNamePattern { get; set; }

            private Regex FileNamePatternRegex { get; } = new Regex(@"\{([^\}]+)\}");
            private Regex FileNamePatternMine { get; } = new Regex(@"([A-Za-z]+)(\:(.+))?");

            public string GetFileNameFromPattern(string requestFilename, string archiveFilename)
            {
                return GetFileNameFromPattern(requestFilename, archiveFilename, () => DateTimeOffset.Now, () => Guid.NewGuid());
            }

            public string GetFileNameFromPattern(string requestFilename, string archiveFilename, Func<DateTimeOffset> getNow, Func<Guid> newGuid)
            {
                if (FileNamePattern == null)
                    return null;

                return FileNamePatternRegex.Replace(FileNamePattern, m =>
                {
                    var mine = m.Groups[1].Value;
                    var mm = FileNamePatternMine.Match(mine);
                    var mineName = mm.Groups[1].Value;
                    var mineFormat = mm.Groups[3].Success ? mm.Groups[3].Value : null;
                    if (mineName.Equals("exportDate", StringComparison.OrdinalIgnoreCase))
                    {
                        return getNow().ToString(mineFormat ?? "yyyyMMddHHmmss");
                    }
                    else if (mineName.Equals("guid", StringComparison.OrdinalIgnoreCase))
                    {
                        return newGuid().ToString();
                    }
                    else if (mineName.Equals("requestFilename", StringComparison.OrdinalIgnoreCase))
                    {
                        return requestFilename ?? "";
                    }
                    else if (mineName.Equals("archiveFilename", StringComparison.OrdinalIgnoreCase))
                    {
                        return archiveFilename ?? "";
                    }
                    else if (mineName.Equals("filename", StringComparison.OrdinalIgnoreCase))
                    {
                        return requestFilename ?? archiveFilename;
                    }
                    else
                        throw new Exception($"Unsupported format mine '{mine}'");
                });
            }

            public ExportProfileCode ProfileCode { get; set; }
        }

        public class SftpExportProfile : ExportProfile
        {
            public string Host { get; set; }
            public int? Port { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public class SftpWithPrivateKeyAuthExportProfile : ExportProfile
        {
            public string Host { get; set; }
            public int? Port { get; set; }
            public string UserName { get; set; }
            public string PrivateKeyPath { get; set; }
            public string PrivateKeyPassword { get; set; }
        }

        public class LocalFolderExportProfile : ExportProfile
        {
            public string FolderPath { get; set; }
            public bool AllowOverwrite { get; set; }
        }

        public class AzureBucketProfile : ExportProfile
        {
            public string SasContainerUrl { get; set; }
        }

        public class AmazonWebServicesBucketProfile : ExportProfile
        {
            public string AccessKeyId { get; set; }
            public string SecretAccessKey { get; set; }
            public string BucketName { get; set; }
            public Amazon.RegionEndpoint RegionEndpoint { get; set; }
        }

        public ExportProfileSet Get(string profileName)
        {
            return profiles.SingleOrDefault(x => x.Name.ToLowerInvariant() == profileName.ToLowerInvariant());
        }
    }
}