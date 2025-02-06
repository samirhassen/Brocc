using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace StagingDatabaseTransformer
{
    public class ParameterFile
    {
        public DictionaryIgnoreCaseWithKeyNameInErrorMessage Parameters { get; set; }
        public List<Tuple<FileInfo, string>> StaticIncludeFilesAndNames { get; set; }
        
        public static ParameterFile ParseFromFile(FileInfo f)
        {
            if (!File.Exists(f.FullName))
                throw new Exception($"File does not exist: {f.FullName}");

            var lines = File
                .ReadAllLines(f.FullName)
                .Where(x => !x.StartsWith("#") || string.IsNullOrWhiteSpace(x))
                .Select(x => 
                {
                    var i = x.IndexOf('=');
                    if (i < 0)
                        throw new Exception("Invalid params file");
                    var name = x.Substring(0, i).Trim();
                    var value = x.Substring(i + 1).Trim();
                    return new { K = name, V = value, IsStaticInclude = name == "staticFileToInclude" };
                });

            var d = new DictionaryIgnoreCaseWithKeyNameInErrorMessage();
            lines
                .Where(x => !x.IsStaticInclude)
                .ToList()
                .ForEach(x => d.Add(x.K, x.V));

            var staticIncludes = lines
                .Where(x => x.IsStaticInclude)
                .Select(x => Tuple.Create(new FileInfo(x.V.Split(';')[0]), x.V.Split(';')[1]))
                .ToList();

            return new ParameterFile
            {
                Parameters = d,
                StaticIncludeFilesAndNames = staticIncludes
            };
        }

        public static IDictionary<string, string> PaseServiceRegistry(string fileName)
        {
            var result = File.ReadAllLines(fileName).Where(x => !x.StartsWith("#") || string.IsNullOrWhiteSpace(x)).Select(x =>
            {
                var ss = x.Split('=');
                return new { K = ss[0].Trim(), V = ss[1].Trim() };
            }).ToDictionary(x => x.K, x => x.V, StringComparer.InvariantCultureIgnoreCase);
            return result;
        }
    }
}
