using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace PEI_Doctors.Services;

public class DoctorMonitorService
{
    private static readonly string Url = "https://cpspei.alinityapp.com/Client/PublicDirectory/Registrants";
    private const string QuerySID = "1000608";

    public async Task RunScrapeAsync()
    {
        string dataDir = Path.Combine(GetProjectRoot(), "data");
        Directory.CreateDirectory(dataDir);

        string todayKey = DateTime.Now.ToString("yyyyMMdd");
        string todayFile = Path.Combine(dataDir, $"{todayKey}.json");
        string baselineFile = Path.Combine(dataDir, "baseline.json");

        // Fetch all data in a single call
        Console.WriteLine("Fetching data for all regions...");
        var rawJson = await FetchDataAsync("", "[not entered]");
        
        // Normalize formatting for consistent diffs
        var currentFormatted = NormalizeJson(rawJson);

        // Save today's snapshot
        await File.WriteAllTextAsync(todayFile, currentFormatted, Encoding.UTF8);

        if (!File.Exists(baselineFile))
        {
            Console.WriteLine("No baseline found. Saving today as baseline.");
        }
        else
        {
            // Load formatted baseline (already normalized)
            var baselineFormatted = await File.ReadAllTextAsync(baselineFile, Encoding.UTF8);
            // Print per-doctor diffs
            bool changesFound = PrintRecordDiffs(baselineFormatted, currentFormatted);
            if (!changesFound)
            {
                Console.WriteLine("No changes detected. Data is up to date.");
            }
        }

        // Update baseline with today's normalized JSON
        await File.WriteAllTextAsync(baselineFile, currentFormatted, Encoding.UTF8);

        Console.WriteLine("Process completed successfully.");
    }

    public async Task<string> FetchDataAsync(string regionSid, string regionLabel)
    {
        using var client = new HttpClient();
        string queryParameters = "{\"Parameter\":[{" +
            "\"ID\":\"TextOptionA\",\"Value\":\"\",\"ValueLabel\":\"[not entered]\"}," +
            "{\"ID\":\"RegionSID\",\"Value\":\"" + regionSid + "\",\"ValueLabel\":\"" + regionLabel + "\"}," +
            "{\"ID\":\"IsCheckedOptionA\",\"Value\":\"\",\"ValueLabel\":\"[not entered]\"}," +
            "{\"ID\":\"TextOptionB\",\"Value\":\"\",\"ValueLabel\":\"[not entered]\"}," +
            "{\"ID\":\"CitySID\",\"Value\":\"\",\"ValueLabel\":\"[not entered]\"}," +
            "{\"ID\":\"TextOptionC\",\"Value\":\"\",\"ValueLabel\":\"[not entered]\"}," +
            "{\"ID\":\"SpecializationSID\",\"Value\":\"\",\"ValueLabel\":\"[not entered]\"}]}";

        var values = new Dictionary<string, string>
        {
            ["queryParameters"] = queryParameters,
            ["querySID"] = QuerySID
        };
        using var content = new FormUrlEncodedContent(values);

        var response = await client.PostAsync(Url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public static string NormalizeJson(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(doc.RootElement, options);
    }

    public bool PrintRecordDiffs(string oldJson, string newJson)
    {
        bool changesFound = false;
        var options = new JsonSerializerOptions { WriteIndented = true };
        using var oldDoc = JsonDocument.Parse(oldJson);
        using var newDoc = JsonDocument.Parse(newJson);

        var oldRecs = oldDoc.RootElement.GetProperty("Records");
        var newRecs = newDoc.RootElement.GetProperty("Records");

        var oldMap = new Dictionary<string, string>();
        foreach (var rec in oldRecs.EnumerateArray())
        {
            var id = rec.GetProperty("rg").GetString() ?? string.Empty;
            oldMap[id] = JsonSerializer.Serialize(rec, options);
        }

        var newMap = new Dictionary<string, string>();
        foreach (var rec in newRecs.EnumerateArray())
        {
            var id = rec.GetProperty("rg").GetString() ?? string.Empty;
            newMap[id] = JsonSerializer.Serialize(rec, options);
        }

        var allIds = new SortedSet<string>(oldMap.Keys);
        allIds.UnionWith(newMap.Keys);

        var diffBuilder = new InlineDiffBuilder(new Differ());

        foreach (var id in allIds)
        {
            var existsOld = oldMap.ContainsKey(id);
            var existsNew = newMap.ContainsKey(id);

            if (!existsOld)
            {
                changesFound = true;
                Console.WriteLine($"+++ Added record for {id}");
                Console.WriteLine(newMap[id]);
                continue;
            }
            if (!existsNew)
            {
                changesFound = true;
                Console.WriteLine($"--- Removed record for {id}");
                Console.WriteLine(oldMap[id]);
                continue;
            }

            var oldText = oldMap[id];
            var newText = newMap[id];
            if (oldText == newText)
                continue;

            changesFound = true;
            Console.WriteLine($"=== Changes for {id}");
            var diff = diffBuilder.BuildDiffModel(oldText, newText);
            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Deleted:
                        Console.WriteLine($"- {line.Type} {line.Text}"); break;
                    case ChangeType.Inserted:
                        Console.WriteLine($"+ {line.Type} {line.Text}"); break;
                }
            }
        }
        return changesFound;
    }

    public string GetProjectRoot([CallerFilePath] string callerPath = "")
    {
        return Path.GetDirectoryName(callerPath) ?? Directory.GetCurrentDirectory();
    }
}
