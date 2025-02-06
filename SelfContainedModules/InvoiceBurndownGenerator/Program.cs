// See https://aka.ms/new-console-template for more information
using nDocument.Code.Excel;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;

/*
 Publish single file:
 dotnet publish -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
 */

var forMonth = args.Length > 0 ? DateTime.Today.AddMonths(int.Parse(args[0])) : DateTime.Today;
var pathName = args.Length > 1 ? args[1] : Environment.CurrentDirectory;

var sums = new Dictionary<string, Dictionary<string, decimal>>();

void HandleItem(string userName, DateTime date, string clientName, decimal hours, string desc)
{
    var include = date.Year == forMonth.Year && date.Month == forMonth.Month;
    if (include)
    {
        if (!sums.ContainsKey(userName))
            sums[userName] = new Dictionary<string, decimal>();

        var sumPerClient = sums[userName];

        if (!sumPerClient.ContainsKey(clientName))
            sumPerClient[clientName] = 0m;
        sumPerClient[clientName] += hours;
    }
}

foreach (var file in new DirectoryInfo(pathName).GetFiles("*.xlsx"))
{
    if (!file.Name.Contains("Timredovisning", StringComparison.OrdinalIgnoreCase) || file.Name.StartsWith("~", StringComparison.OrdinalIgnoreCase))
        continue;

    var userName = Path.GetFileNameWithoutExtension(file.Name).Replace("Timredovisning", "", StringComparison.OrdinalIgnoreCase).Replace("-", "").Trim();
    var fileName = file.FullName;

    try
    {
        var p = new ExcelParser(true);

        using var s = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var result = p.ParseExcelFile(fileName, s);
        if (!result.Item1)
            throw new Exception($"Broken: {fileName}");

        var timeSheet = result.Item2.ContainsKey(forMonth.Year.ToString())
            ? result.Item2[DateTime.Today.Year.ToString()]
            : result.Item2.OrderByDescending(x => x.Value.Count).First().Value; //Use the longest sheet as source.

        var headerRow = timeSheet[0];

        string GetCellValue(List<string> row, int columnIndex) => row.Count < columnIndex + 1 ? "" : row[columnIndex];

        foreach (var (row, rowIndex) in timeSheet.Select((x, i) => (x, i)).Skip(1))
        {
            var desc = GetCellValue(row, 1);
            var date = GetCellValue(row, 0)?.Trim();
            if (string.IsNullOrWhiteSpace(date))
            {
                //Scan up for a date
                var i = rowIndex - 1;
                while (i > 0 && string.IsNullOrWhiteSpace(date))
                    date = GetCellValue(timeSheet[i--], 0)?.Trim();
            }

            if (string.IsNullOrWhiteSpace(desc))
                continue;

            if (row.Count > 2 && !string.IsNullOrWhiteSpace(date) && DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                foreach (var cellIndex in Enumerable.Range(2, row.Count - 2))
                {
                    var clientName = GetCellValue(headerRow, cellIndex)?.ToLowerInvariant()?.Trim();
                    var value = GetCellValue(row, cellIndex).Replace(",", ".");
                    if (!string.IsNullOrWhiteSpace(clientName) && !string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue))
                    {
                        HandleItem(userName, parsedDate, clientName, parsedValue, desc);
                    }
                }
            }
        }
    }
    catch(Exception ex)
    {
        throw new Exception($"Failed on {fileName}", ex);
    }
}

var totalPerClient = new Dictionary<string, decimal>();
foreach(var userName in sums.Keys)
{
    var sumPerClient = sums[userName];
    foreach(var clientName in sumPerClient.Keys)
    {
        if (!totalPerClient.ContainsKey(clientName))
            totalPerClient[clientName] = 0m;
        totalPerClient[clientName] += sumPerClient[clientName];
    }
}
var infoFilename = Path.Combine(Environment.CurrentDirectory, $"invoicing-summary-{forMonth.ToString("yyyy-MM")}.txt");
using var infoFile = File.CreateText(Path.Combine(Environment.CurrentDirectory, $"invoicing-summary-{forMonth.ToString("yyyy-MM")}.txt"));

void PrintDataLine(string line)
{
    Console.WriteLine(line);
    infoFile.WriteLine(line);
}
PrintDataLine($"Totalt {forMonth.ToString("yyyy-MM")}: {totalPerClient.Sum(x => x.Value)}");
PrintDataLine("");
foreach (var kvp in totalPerClient)
{
    PrintDataLine($" {kvp.Key}: {kvp.Value}");
}
PrintDataLine("");
if (DateTime.Today.Year == forMonth.Year && DateTime.Today.Month == forMonth.Month)
{
    PrintDataLine($"Varning, data för nuvarande månad så kan vara ofullständiga. Rapporten togs ut {DateTime.Today.ToString("yyyy-MM-dd HH:mm")}");
    PrintDataLine("");
}
PrintDataLine("------------------------------------------------");
foreach (var userName in sums.Keys)
{
    PrintDataLine("");
    PrintDataLine($"{userName}:" );    
    var sumPerClient = sums[userName];
    foreach (var clientName in sumPerClient.Keys)
    {
        PrintDataLine($" {clientName}: {sumPerClient[clientName]}");        
    }
}

infoFile.Close();
infoFile.Dispose();

Process.Start(new ProcessStartInfo
{
    UseShellExecute = true,
    FileName = infoFilename
});