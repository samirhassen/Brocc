using System.Collections.Generic;
using System.IO;

namespace NTech.Banking.Autogiro
{
    public abstract class AbstractAutogiroFileToBgcBuilder
    {
        public static System.Text.Encoding Encoding { get; private set; } = System.Text.Encoding.GetEncoding("iso-8859-1");
        public const string NewLine = "\r\n";
        public List<string> Rows { get; private set; } = new List<string>();

        public abstract string GetFileName();

        protected virtual void BeforeWrite()
        {

        }

        public void WriteToStream(System.IO.Stream s, AutogiroHmacSealer alsoSealWith = null)
        {
            BeforeWrite();

            List<string> lines;
            if (alsoSealWith == null)
                lines = Rows;
            else
                lines = alsoSealWith.CreateSealedFile(Rows);

            var f = string.Join(NewLine, lines);
            var b = Encoding.GetBytes(f);
            s.Write(b, 0, b.Length);
            s.Flush();
        }

        public byte[] ToByteArray(AutogiroHmacSealer alsoSealWith = null)
        {
            using (var ms = new MemoryStream())
            {
                WriteToStream(ms, alsoSealWith: alsoSealWith);
                return ms.ToArray();
            }
        }

        public void SaveToFolderWithCorrectFilename(DirectoryInfo directory, AutogiroHmacSealer alsoSealWith = null)
        {
            var b = ToByteArray(alsoSealWith: alsoSealWith);
            File.WriteAllBytes(Path.Combine(directory.FullName, GetFileName()), b);
        }

        protected AutogiroRowBuilder NewRow(string postType)
        {
            return AutogiroRowBuilder.Start(postType, onEnd: Rows.Add);
        }
    }
}
