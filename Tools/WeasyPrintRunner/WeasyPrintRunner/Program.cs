using System;
using System.Diagnostics;
using System.IO;
using Balbarak.WeasyPrint;

namespace WeasyPrintRunner
{
    class Program
    {
        /// <summary>
        /// First version of using WeasyPrint is with an executable on the server that is called.
        /// Future improvements: possibly extract to App Service, 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args?.Length < 2)
            {
                throw new ArgumentException("Call this program using two parameters; file path of the template and the name and location of the targetfile. ");
            }

            var templateFilePath = args[0];
            var targetFile = args[1];

            // Wraps around another .NET Core wrapper for Python-made WeasyPrint. 
            // The .NET-wrapper can not handle header/footer-htmlpages currently. 
            using var client = new WeasyPrintClient(new ConsoleTraceWriter());
            client.GeneratePdf(templateFilePath, targetFile);
        }
    }
}
