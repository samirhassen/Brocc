using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.NTechWsDoc
{
    public class ServiceMethodDocumentationGenerator
    {
        public class FileStreamResponseMarkerType
        {

        }

        public ServiceMethodDocumentation Generate(string path, string method, Type requestType, Type responseType)
        {
            try
            {
                var d = new ServiceMethodDocumentation()
                {
                    Path = path,
                    Method = method
                };
                var typeParser = new TypeParser();
                d.RequestType = typeParser.ParseType(requestType, "RequestType");
                var isFileResponse = (responseType.FullName == typeof(FileStreamResponseMarkerType).FullName);
                if (isFileResponse)
                {
                    d.ResponseType = new CompoundType
                    {
                        Name = "FileResponseType",
                        CompoundProperties = new List<CompoundProperty>(),
                        PrimtiveProperties = new List<PrimtiveProperty>()
                    };
                }
                else
                {
                    d.ResponseType = typeParser.ParseType(responseType, "ResponseType");
                }

                d.OtherTypes = typeParser.CompoundTypeByName.Keys.Where(x => x != d.RequestType.Name && x != d.ResponseType.Name).Select(x => typeParser.CompoundTypeByName[x]).OrderBy(x => x.Name).ToList();

                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    d.RequestExample = JsonConvert.SerializeObject(CreateSampleObject(d.RequestType), Formatting.Indented);
                }
                else if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    d.RequestExample = CreateSampleQueryString(path, d.RequestType);
                }
                else
                    throw new NotImplementedException();

                if (isFileResponse)
                {
                    var n = Environment.NewLine;
                    d.ResponseExample = $"Content-Disposition: attachment; filename=somefile.pdf{n}Content-Length: 9999{n}Content-Type: application/pdf{n}[...binary data...]";
                }
                else
                {
                    d.ResponseExample = JsonConvert.SerializeObject(CreateSampleObject(d.ResponseType), Formatting.Indented);
                }

                return d;
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not generate api docs for {path}.{method}", ex);
            }
        }

        private object CreateSamplePrimitiveValue(PrimitiveTypeCode code, bool isArray, bool isNullable)
        {
            switch (code)
            {
                case PrimitiveTypeCode.Boolean:
                    return isArray ? (object)new[] { true, false } : true;
                case PrimitiveTypeCode.Date:
                    return isArray ? (object)new DateTime?[] { new DateTime(2099, 12, 24), new DateTime(2099, 12, 25) } : new DateTime(2099, 12, 24);
                case PrimitiveTypeCode.Decimal:
                    return isArray ? (object)new decimal?[] { 99.88m, 99m } : 99.99m;
                case PrimitiveTypeCode.Int:
                    return isArray ? (object)new int?[] { 99, 98 } : 99;
                case PrimitiveTypeCode.String:
                    return isArray ? (object)new string[] { "abc", "def" } : "abc";
                default:
                    throw new NotImplementedException();
            }
        }

        private Dictionary<string, object> CreateSampleObject(CompoundType t)
        {
            var d = new Dictionary<string, object>();

            foreach (var p in t.PrimtiveProperties)
            {
                PrimitiveTypeCode code;
                if (!Enum.TryParse<PrimitiveTypeCode>(p.TypeCode, out code))
                    throw new Exception($"Unknown typecode '{p.TypeCode}'");
                d.Add(p.Name, CreateSamplePrimitiveValue(code, p.IsArray, p.IsNullable));
            }

            foreach (var p in t.CompoundProperties)
            {
                d.Add(p.Name, p.IsArray ? (object)new object[] { CreateSampleObject(p.Type), CreateSampleObject(p.Type) } : CreateSampleObject(p.Type));
            }

            return d;
        }

        private string CreateSampleQueryString(string context, CompoundType t)
        {
            if (t.CompoundProperties.Any())
                throw new Exception($"{context}: Query string request types can only have primitive properties");
            if (t.PrimtiveProperties.Count == 0)
                return "";
            else
            {
                var s = CreateSampleObject(t);
                Func<object, string> getExampleValue = x => new Newtonsoft.Json.Linq.JValue(x).ToString(Formatting.None)?.Replace("\"", "");
                return "?" + string.Join("&", s.Select(x => $"{x.Key}={Uri.EscapeDataString(getExampleValue(x.Value))}"));
            }
        }
    }
}
