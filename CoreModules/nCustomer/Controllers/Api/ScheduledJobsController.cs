using nCustomer.Code.Services.Aml.Cm1;
using nCustomer.DbModel;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Web.Mvc;

namespace nCustomer.Controllers.Api
{
    [NTechAuthorize]
    public class ScheduledJobsController : NController
    {
        [HttpPost]
        [Route("Api/Jobs/ImportRiskClassesFromCm1")]
        public ActionResult ImportRiskClassesFromCm1()
        {
            var ftpSettings = NEnv.Cm1FtpSettings;
            if (ftpSettings == null || !ftpSettings.Enabled)
            {
                var warning = ftpSettings == null ? "Cm1 export disabled since the settings file is missing." : "Cm1 export disabled by settings.";
                return Json2(new { warnings = new List<string>() { warning } });
            }

            var resolver = Service;

            return CustomersContext.RunWithExclusiveLock("ntech.scheduledjobs.importriskclassesfromcm1", () =>
                {
                    var user = GetCurrentUserMetadata().CoreUser;
                    var ftpClient = CreateFtpClient(ftpSettings);
                    Func<ISftpClient> createFtpClient = () => new RenciSftpClient(ftpClient);

                    var cm1Service = new CM1ImportService(Service.KeyValueStore, user,
                        resolver.CustomerContextFactory, resolver.Logging, createFtpClient, ftpSettings);
                    var errors = cm1Service.ImportRiskClassesFromCm1(
                        x => new CustomerWriteRepository(x, user, CoreClock.SharedInstance, resolver.EncryptionService, NEnv.ClientCfgCore));
                    return Json2(new { errors = errors });
                },
                () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));

        }

        private Lazy<SftpClient> CreateFtpClient(Cm1FtpSettings ftpSettings)
        {
            if (!string.IsNullOrEmpty(ftpSettings.PrivateKeyPathway))
            {
                var ftpClient = new Lazy<SftpClient>(() =>
                {
                    var privateKeyFile = new PrivateKeyFile(ftpSettings.PrivateKeyPathway, ftpSettings.PrivateKeyPassword);
                    var privateKeyAuthenticationMethod = new PrivateKeyAuthenticationMethod(ftpSettings.UserName, privateKeyFile);
                    var connectionInfo = new ConnectionInfo(ftpSettings.HostName,
                                                            ftpSettings.Port,
                                                            ftpSettings.UserName,
                                                            privateKeyAuthenticationMethod);

                    return new SftpClient(connectionInfo);
                });
                return ftpClient;
            }

            else
            {
                var ftpClient = new Lazy<SftpClient>(() => new SftpClient(new ConnectionInfo(ftpSettings.HostName,
                           ftpSettings.Port,
                           ftpSettings.UserName,
                           new PasswordAuthenticationMethod(ftpSettings.UserName, ftpSettings.Password))));
                return ftpClient;
            }
        }

        public class RenciSftpClient : ISftpClient
        {
            private readonly Lazy<SftpClient> client;

            public RenciSftpClient(Lazy<SftpClient> client)
            {
                this.client = client;
            }

            public IEnumerable<(string FullName, string Name, bool IsDirectory)> ListDirectory(string path) =>
                client.Value.ListDirectory(path).Select(x => (x.FullName, x.Name, x.IsDirectory));
            public Stream OpenRead(string path) => client.Value.OpenRead(path);
            public void Connect() => client.Value.Connect();
            public void DeleteFile(string path) => client.Value.DeleteFile(path);
            public void Disconnect() => client.Value.Disconnect();

            public void Dispose()
            {
                if (client.IsValueCreated)
                {
                    client.Value.Dispose();
                }
            }
        }
    }
}