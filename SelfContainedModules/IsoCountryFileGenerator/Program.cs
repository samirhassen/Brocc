#pragma warning disable CS8602 // Dereference of a possibly null reference.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

if (args.Length != 1)
{
    Console.WriteLine(@"Usageexample: IsoCountryFileGenerator c:\temp\countries.json");
}

using HttpClient client = new();
var countriesRaw = await client.GetByteArrayAsync("https://restcountries.com/v3.1/all")!;
JArray countries = JArray.Parse(Encoding.UTF8.GetString(countriesRaw));
var result = new List<Country>();

var languagesToInclude = new Dictionary<string, string>
{
    ["swe"] = "sv",
    ["fin"] = "fi"
};

string? GetOpt(JToken t, params string[] names)
{
    JToken? local = t;
    foreach (var name in names)
    {
        local = local == null ? null : local[name];
    }
    return local?.Value<string>();
}

string GetReq(JToken t, params string[] names)
{
    var value = GetOpt(t, names);
    if (value == null)
    {
        throw new Exception();
    }
    return value;
}

foreach (var country in countries)
{
    try
    {
        var iso2 = GetReq(country, "cca2");
        var commonName = GetReq(country, "name", "common");

        var nativeName = country["name"]["nativeName"] != null
            ? country["name"]["nativeName"].Children().First().First()["common"]!.Value<string>()
            : commonName;

        var translations = new Dictionary<string, string>();
        translations["en"] = commonName;
        foreach (var lang in languagesToInclude)
        {
            translations[lang.Value] = GetOpt(country, "translations", lang.Key, "common") ?? commonName;
        }

        result.Add(new Country(
            commonName,
            nativeName!,
            GetReq(country, "cca2"),
            GetReq(country, "cca3"),
            translations));
    }
    catch (Exception)
    {
        Console.Error.WriteLine("Skipping: " + country.ToString());
    }
}

File.WriteAllText(args[0], JsonConvert.SerializeObject(result));

record Country(string commonName, string nativeName, string iso2Name, string iso3Name, Dictionary<string, string> translatedNameByLang2);
#pragma warning restore CS8602 // Dereference of a possibly null reference.