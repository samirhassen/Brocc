using Dapper;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class CM1ImportService
    {
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly INTechCurrentUserMetadata currentUserMetaData;
        private readonly CustomerContextFactory customerContextFactory;
        private readonly ILoggingService loggingService;
        private readonly Func<ISftpClient> createFtpClient;
        private readonly Cm1FtpCommandSettings ftpSettings;
        private const string KeyValueItemKeyName = "ImportedCm1RiskClassFileNames";
        private const string BusinessEventName = "updatedRiskClassesFromCm1";

        //Used for integration testing to make tracking down errors easier
        public bool SupressSwallowErrors { get; set; } = false;

        public CM1ImportService(IKeyValueStoreService valueStoreService, INTechCurrentUserMetadata userMetaData,
            CustomerContextFactory customerContextFactory, ILoggingService loggingService, Func<ISftpClient> createFtpClient,
            Cm1FtpCommandSettings ftpSettings)
        {
            
            keyValueStoreService = valueStoreService;
            currentUserMetaData = userMetaData;
            this.customerContextFactory = customerContextFactory;
            this.loggingService = loggingService;
            this.createFtpClient = createFtpClient;
            this.ftpSettings = ftpSettings;
        }

        public List<string> ImportRiskClassesFromCm1(Func<ICustomerContextExtended, CustomerWriteRepository> createCustomerRepository)
        {
            var alreadyImportedFiles = GetAlreadyImportedFilesFromKeyValueStore();

            var filesToDownload = new HashSet<(string fileNameAndPath, string fileName)>();
            using (var ftpClient = createFtpClient())
            {
                ftpClient.Connect();

                foreach (var folder in ftpSettings.FoldersToScan)
                {
                    var filesInFolder = GetFileNamesFromFtp(ftpClient, folder).ToList();
                    var filesNotAlreadyImported = filesInFolder.Where(x => !alreadyImportedFiles.Contains(x.fileName)).ToList();
                    if (filesInFolder.Count() > filesNotAlreadyImported.Count())
                    {
                        loggingService.Information(
                            "There are files in the CM1 ftp that has already been read. " +
                            "Either the system failed to delete them, or CM1 added files that has already been read before. ");
                    }

                    filesToDownload.AddRange(filesNotAlreadyImported); // Oldest file first. 
                }

                foreach (var (fileNameAndPath, fileName) in filesToDownload.Where(x => !alreadyImportedFiles.Contains(x.fileName)))
                {
                    var customerData = DownloadAndParseFileFromFtp(ftpClient, fileNameAndPath, out var errors);
                    if (errors.Any())
                    {
                        return errors;
                    }

                    var propertiesToUpdate = customerData.Select(customer => ToCustomerPropertyModel(customer.Key, customer.Value)).ToList();

                    // Once per downloaded file from CM1. 
                    try
                    {
                        using (var context = customerContextFactory.CreateContext())
                        {
                            var customerRepository = createCustomerRepository(context);
                            context.BeginTransaction();
                            try
                            {

                                if (customerData.Any())
                                {
                                    var customerIdsForQuery = string.Join(", ", customerData.Keys.Select(x => $"('{x}')")); // ('123'),('456')
                                    var nonExistingCustomerIds = context.GetConnection().Query<int>(
                                        $@"SELECT customerId
                                FROM(VALUES{customerIdsForQuery}) E(customerId) 
                                EXCEPT 
                                SELECT DISTINCT CustomerId 
                                FROM CustomerProperty", transaction: context.CurrentTransaction).ToList();

                                    if (nonExistingCustomerIds.Any())
                                    {
                                        loggingService.Warning($"{nonExistingCustomerIds.Count} / {customerData.Keys.Count} customerids from CM1 that does not exist in Customer database: {string.Join(", ", nonExistingCustomerIds)}, file: {fileName}. ");
                                    }

                                    propertiesToUpdate = propertiesToUpdate
                                        .Where(x => !nonExistingCustomerIds.Contains(x.CustomerId))
                                        .ToList();

                                    customerRepository.UpdateProperties(propertiesToUpdate, true, BusinessEventName);
                                }

                                // Save the files that has been imported. 
                                alreadyImportedFiles.Add(fileName);
                                keyValueStoreService.SetValue(KeyValueItemKeyName, KeyValueItemKeyName,
                                    JsonConvert.SerializeObject(alreadyImportedFiles), currentUserMetaData);

                                context.SaveChanges();
                                context.CommitTransaction();

                                // We remove the file from the ftp in accordance with CM1. 
                                ftpClient.DeleteFile(fileNameAndPath);                                
                            }
                            catch
                            {
                                context.RollbackTransaction();
                                throw;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (SupressSwallowErrors)
                            throw;

                        loggingService.Error(ex, $"Error in CM1 risk class import from FTP for file {fileName}. ");
                        errors.Add($"Error saving data from CM1 risk class import for file {fileName}");
                    }
                }

                ftpClient.Disconnect();
            }

            return new List<string>();
        }

        private static CustomerPropertyModel ToCustomerPropertyModel(int customerId, string value) =>
            new CustomerPropertyModel
            {
                CustomerId = customerId,
                Group = "sensitive",
                IsSensitive = true,
                Name = "amlRiskClass",
                Value = value
            };

        private List<string> GetAlreadyImportedFilesFromKeyValueStore()
        {
            var alreadyImportedFilesJson = keyValueStoreService.GetValue(KeyValueItemKeyName, KeyValueItemKeyName);
            if (alreadyImportedFilesJson == null)
            {
                var emptyList = new List<string>();
                keyValueStoreService.SetValue(KeyValueItemKeyName, KeyValueItemKeyName, JsonConvert.SerializeObject(emptyList), currentUserMetaData);
                return emptyList;
            }

            return JsonConvert.DeserializeObject<List<string>>(alreadyImportedFilesJson);
        }

        private Dictionary<int, string> DownloadAndParseFileFromFtp(ISftpClient client, string fileNameAndPath, out List<string> errors)
        {
            var data = new Dictionary<int, string>();
            errors = new List<string>();
            using (var stream = client.OpenRead(fileNameAndPath))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    try
                    {
                        var row = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(row)) // An empty .csv gives an empty string here
                            return data;

                        var values = row.Split(','); // Values in the form of "1001,Låg"

                        // Note: should the format in the file be wrong, below will fail and that is correct. 
                        var customerId = Convert.ToInt32(values[0].Trim());
                        var risk = values[1].ToString().Trim();
                        data.Add(customerId, risk);
                    }
                    catch (ArgumentException ex)
                    {
                        if (SupressSwallowErrors)
                            throw;

                        errors.Add($"File {fileNameAndPath} contains duplicate customers and could not be parsed, contact CM1. ");
                        loggingService.Error(ex, $"Error parsing file {fileNameAndPath}; contains duplicate values. ");
                    }
                    catch (Exception ex)
                    {
                        if (SupressSwallowErrors)
                            throw;

                        errors.Add($"There were errors parsing file {fileNameAndPath}, contact CM1. ");
                        loggingService.Error(ex, $"Error parsing file {fileNameAndPath} from CM1. ");
                    }
                }
            }

            return data;
        }

        private IEnumerable<(string fileNameAndPath, string fileName)> GetFileNamesFromFtp(ISftpClient ftpClient, string folder)
        {
            var filesFromFtp = ftpClient.ListDirectory(folder);
            var regex = new Regex(ftpSettings.FileNamePattern);
            // ListDirectory also returns directories, so we select all that is not a directory here. 
            // Fullname = /prod/upload/file.csv, name = file.csv
            return filesFromFtp.Where(x => !x.IsDirectory && regex.IsMatch(x.Name))
                .OrderBy(x => x.Name) // Oldest file first. 
                .Select(x => (x.FullName, x.Name));
        }
    }

    public interface ISftpClient : IDisposable
    {
        /// <summary>
        /// Example
        /// FullName: /prod/upload/file.csv
        /// Name:     file.csv
        /// </summary>
        /// <returns></returns>
        IEnumerable<(string FullName, string Name, bool IsDirectory)> ListDirectory(string path);
        Stream OpenRead(string path);
        void DeleteFile(string path);
        void Connect();
        void Disconnect();
    }
}