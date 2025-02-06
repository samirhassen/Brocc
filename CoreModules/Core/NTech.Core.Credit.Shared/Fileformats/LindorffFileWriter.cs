using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace nCredit.Code.Fileformats
{
    public class LindorffFileWriter : IDisposable
    {
        private Stream stream;
        private bool ownsStream;

        private LindorffFileWriter()
        {

        }

        public static LindorffFileWriter BeginCreate(string fileName)
        {
            var w = new LindorffFileWriter();
            w.stream = File.Create(fileName);
            w.ownsStream = true;
            return w;
        }

        public static LindorffFileWriter BeginCreate(Stream target)
        {
            var w = new LindorffFileWriter();
            w.stream = target;
            w.ownsStream = false;
            return w;
        }

        public class Record
        {
            private LindorffFileWriter writer;
            private Stream stream;
            private static Lazy<Encoding> enc = new Lazy<Encoding>(() => Encoding.GetEncoding("iso-8859-1"));

            private void Write(string s)
            {
                var b = enc.Value.GetBytes(s);
                stream.Write(b, 0, b.Length);
            }

            public Record(LindorffFileWriter writer, Stream s)
            {
                this.writer = writer;
                this.stream = s;
            }

            public Record WriteString(string value, int length, string label)
            {
                if (value == null)
                    value = "";
                if (value.Length < length)
                    value = value.PadLeft(length, ' ');
                if (value.Length > length)
                    value = value.Substring(0, length);

                Write(value);

                return this;
            }

            public Record WriteUnsignedInt(int value, int length, string label)
            {
                if (value < 0)
                    throw new Exception($"{label} cannot be negative");

                var sValue = value.ToString();

                if (sValue.Length < length)
                    sValue = sValue.PadLeft(length, '0');
                if (sValue.Length > length)
                    throw new Exception($"Value in {label} cannot exceed length {length}");

                Write(sValue);

                return this;
            }

            public Record WriteUnsignedDecimal(decimal value, int length, int nrOfDecimals, string label)
            {
                var prefixLength = length - nrOfDecimals;
                if (prefixLength <= 0 || nrOfDecimals <= 0)
                    throw new Exception($"{label}: A decimal {length}.{nrOfDecimals} is not valid");
                if (value < 0)
                    throw new Exception($"{label} cannot be negative");
                var sValue = value.ToString($"F{nrOfDecimals}", CultureInfo.InvariantCulture).Replace(".", "");
                if (sValue.Length < length)
                    sValue = sValue.PadLeft(length, '0');
                if (sValue.Length > length)
                    throw new Exception($"Value in {label} cannot exceed length {length}");

                Write(sValue);

                return this;
            }

            public Record WriteDate(DateTime d, int length, string label)
            {
                //Length 8: using yyyymmdd since at least original due date at least so hopefully true everywhere
                //Length 6: using yymmdd
                if (length == 6)
                {
                    Write(d.ToString("yyMMdd"));
                }
                else if (length == 8)
                {
                    Write(d.ToString("yyyyMMdd"));
                }
                else
                {
                    throw new Exception($"Date field {label} has length {length} different from 6(yymmdd) or 8 (yyyymmdd)");
                }
                return this;
            }

            public LindorffFileWriter EndRecord()
            {
                Write("\r\n");
                return this.writer;
            }
        }

        public Record BeginRecord(int code, string label)
        {
            return new Record(this, this.stream);
        }

        public void Dispose()
        {
            if (ownsStream)
            {
                this.stream.Dispose();
            }
        }
    }
}