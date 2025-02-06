using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroFileTypeDetector
    {
        public enum AgFileTypeCode
        {
            IncomingAgMedgivanden
        }

        public AgFileTypeCode? GetFileType(Stream file)
        {
            if (file == null)
                return null;
            if (AutogiroMedgivandeAviseringFileParser.CouldBeThisFiletype(file))
                return AgFileTypeCode.IncomingAgMedgivanden;
            else
                return null;
        }

        public AgFileTypeCode? GetFileType(string fileContent)
        {
            if (fileContent == null)
                return null;
            return GetFileType(new MemoryStream(Encoding.GetEncoding("iso-8859-1").GetBytes(fileContent)));
        }
    }
}
