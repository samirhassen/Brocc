using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.WindowsAzure.Storage.Blob;
using nDocument.Code.Archive;
using nDocument.Code.FileExport;
using NTech.Services.Infrastructure;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nDocument.Controllers
{
    [NTechAuthorize]
    [NTechApi]
    public class FileExportController : Controller
    {
        public class ExportRequest
        {
            public string FileArchiveKey { get; set; }
            public string ProfileName { get; set; }
            public string Filename { get; set; } //Optional. If present, overrides any rules in the profile
        }

        private bool TryExport(ExportRequest request, string archiveFilename, Func<Stream> getFileStream, ExportProfileRepository.ExportProfile profile, out string errorMessage)
        {
            var filename = request.Filename ?? (profile.GetFileNameFromPattern(request.Filename, archiveFilename));

            errorMessage = null;
            try
            {
                switch (profile.ProfileCode)
                {
                    case ExportProfileRepository.ExportProfileCode.Sftp:
                        {
                            var p = profile as ExportProfileRepository.SftpExportProfile;
                            var connectionInfo = new Renci.SshNet.ConnectionInfo(p.Host,
                                                                    p.Port ?? 22,
                                                                    p.UserName,
                                                                    new Renci.SshNet.PasswordAuthenticationMethod(p.UserName, p.Password));
                            using (var client = new Renci.SshNet.SftpClient(connectionInfo))
                            {
                                client.Connect();
                                client.UploadFile(getFileStream(), filename);
                            }
                            return true;
                        }
                    case ExportProfileRepository.ExportProfileCode.SftpWithPrivateKeyAuth:
                        {
                            var p = profile as ExportProfileRepository.SftpWithPrivateKeyAuthExportProfile;

                            var privateKeyFile = new PrivateKeyFile(p.PrivateKeyPath, p.PrivateKeyPassword);

                            var privateKeyAuthenticationMethod = new PrivateKeyAuthenticationMethod(p.UserName, privateKeyFile);

                            var connectionInfo = new ConnectionInfo(p.Host,
                                                                    p.Port ?? 22,
                                                                    p.UserName,
                                                                    privateKeyAuthenticationMethod);

                            using (var client = new SftpClient(connectionInfo))
                            {
                                client.Connect();
                                client.UploadFile(getFileStream(), filename);
                            }
                            return true;
                        }
                    case ExportProfileRepository.ExportProfileCode.LocalFolder:
                        {
                            var p = profile as ExportProfileRepository.LocalFolderExportProfile;
                            Directory.CreateDirectory(p.FolderPath);
                            var fullName = Path.Combine(p.FolderPath, filename);

                            using (var fs = new FileStream(fullName, p.AllowOverwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
                            {
                                getFileStream().CopyTo(fs);
                                fs.Flush();
                            }

                            return true;
                        }
                    case ExportProfileRepository.ExportProfileCode.AzureBucket:
                        {
                            var p = profile as ExportProfileRepository.AzureBucketProfile;
                            var c = new CloudBlobContainer(new Uri(p.SasContainerUrl));
                            if (string.IsNullOrWhiteSpace(c.Name))
                                throw (new ApplicationException("Azure CloudBlobContainer is missing in profile: " + p.Name));
                            c.CreateIfNotExists();
                            var blockBlob = c.GetBlockBlobReference(filename);
                            blockBlob.UploadFromStream(getFileStream());

                            return true;
                        }

                    case ExportProfileRepository.ExportProfileCode.AmazonWebServicesBucket:
                        {
                            var p = profile as ExportProfileRepository.AmazonWebServicesBucketProfile;

                            if (string.IsNullOrWhiteSpace(p.AccessKeyId) || string.IsNullOrWhiteSpace(p.SecretAccessKey) || string.IsNullOrWhiteSpace(p.BucketName))
                                throw (new ApplicationException("AmazonWebServices-S3Bucket: One or more required setting (AccessKeyId, SecretAccessKey, BucketName) is missing in profile: " + p.Name));

                            using (var client = new AmazonS3Client(p.AccessKeyId, p.SecretAccessKey, p.RegionEndpoint))
                            {
                                var uploadRequest = new TransferUtilityUploadRequest
                                {
                                    InputStream = getFileStream(),
                                    BucketName = p.BucketName,
                                    Key = filename
                                };

                                var fileTransferUtility = new TransferUtility(client);
                                var response = fileTransferUtility.UploadAsync(uploadRequest);

                                return true;
                            }
                        }

                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Export failed for key: {archiveKey} with profile: {profileName}", request?.FileArchiveKey, request?.ProfileName);
                errorMessage = $"Export failed for key: {request?.FileArchiveKey} with profile: {request?.ProfileName}";
                return false;
            }
        }

        [HttpPost]
        public ActionResult Export(ExportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.FileArchiveKey))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing FileArchiveKey");
            if (string.IsNullOrWhiteSpace(request?.ProfileName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing ProfileName");

            var repo = new ExportProfileRepository();
            var profileSet = repo.Get(request.ProfileName);
            if (profileSet == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Export profile '{request.ProfileName}' does not exist");

            var archive = ArchiveProviderFactory.Create();
            var archiveResult = archive.Fetch(request.FileArchiveKey);
            if (archiveResult == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Archive key '{request.FileArchiveKey}' does not exist");

            bool? isSuccess = null;
            List<string> successProfileNames = new List<string>();
            List<string> errorMessages = new List<string>();
            List<string> failedProfileNames = new List<string>();
            var w = Stopwatch.StartNew();
            try
            {
                foreach (var profile in profileSet.Profiles)
                {
                    string errorMessage;
                    if (TryExport(request, archiveResult.FileName, () =>
                    {
                        if (archiveResult.Content.Position > 0)
                            archiveResult.Content.Position = 0;
                        return archiveResult.Content;
                    }, profile, out errorMessage))
                    {
                        successProfileNames.Add(profile.Name);
                        if (!isSuccess.HasValue)
                            isSuccess = true;
                    }
                    else
                    {
                        errorMessages.Add(errorMessage);
                        failedProfileNames.Add(profile.Name);
                        isSuccess = false;
                    }
                }
                if (errorMessages.Any())
                    throw new Exception(string.Join(Environment.NewLine, errorMessages));
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Export failed for key: {archiveKey} with profile: {profileName}", request?.FileArchiveKey, request?.ProfileName);
                isSuccess = false;
            }
            return Json(new { isSuccess = isSuccess.Value, timeInMs = w.ElapsedMilliseconds, profileName = request?.ProfileName, successProfileNames = successProfileNames, failedProfileNames = failedProfileNames });
        }

        [HttpPost]
        public ActionResult GetProfile(string profileName)
        {
            var repo = new ExportProfileRepository();
            var profile = repo.Get(profileName);
            if (profile == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Export profile '{profileName}' does not exist");

            var localFolderProfile = profile.ProfileCode == ExportProfileRepository.ExportProfileCode.LocalFolder ? (profile.Profiles.First() as ExportProfileRepository.LocalFolderExportProfile) : null;
            return Json(new
            {
                Name = profile.Name,
                ProfileCode = profile.ProfileCode.ToString(),
                LocalFolderPath = localFolderProfile?.FolderPath,
                IncludedProfileNames = profile.Profiles.Select(x => x.Name).ToList()
            });
        }
    }
}