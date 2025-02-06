using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace nCredit.Code
{
    public class SecureMessageService : ISecureMessageService
    {

        private readonly DirectoryInfo TemplateFolder =
            NTechEnvironment.Instance.ClientResourceDirectory("ntech.securemessagetemplates.folder", "SecureMessageTemplates", false);

        private readonly ICustomerClient customerClient;

        public SecureMessageService()
        {
            customerClient = new CreditCustomerClient();
        }

        public SecureMessageService(ICustomerClient client)
        {
            customerClient = client;
        }

        public bool SendSecureMessageWithTemplate(string templateName, string channelType, int customerId, string creditNr, Dictionary<string, string> mines, bool throwIfError = false)
        {
            try
            {
                var templateFile = Path.Combine(TemplateFolder.FullName, templateName);
                if (!templateName.EndsWith(".txt")) templateFile += ".txt";

                var templateLines = File.ReadAllLines(templateFile);
                var messageText = string.Join("", templateLines); // Saved as string in database, cannot handle html. 

                if (mines != null)
                {
                    messageText = Nustache.Core.Render.StringToString(messageText, mines);
                }

                customerClient.SendSecureMessage(customerId, creditNr, channelType, messageText, true, null);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error when sending secure messages using template {templateName}. ");

                if (throwIfError)
                    throw ex;

                return false;
            }

            return true;

        }


    }
}