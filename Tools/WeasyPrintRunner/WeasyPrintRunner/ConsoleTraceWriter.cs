using System;
using System.Collections.Generic;
using System.Text;
using Balbarak.WeasyPrint;

namespace WeasyPrintRunner
{
    public class ConsoleTraceWriter : ITraceWriter
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Verbose(string message)
        {

        }
    }
}
