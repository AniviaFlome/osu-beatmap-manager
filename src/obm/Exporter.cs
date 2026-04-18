using CsvHelper;
using CsvHelper.Configuration;
using Realms;
using Spectre.Console;
using System.Globalization;

namespace OsuBeatmapManager;

public static class Exporter
{
    public static void ExportCollections(string osuDir, string outputCsv)
    {
        var realmPath = Path.Combine(osuDir, "client.realm");

        if (!File.Exists(realmPath))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Could not find client.realm at {Markup.Escape(realmPath)}");
            return;
        }

        int missingBeatmapsCount = 0;
        int parsedCollectionsCount = 0;
        int foundMapsCount = 0;
        int successfulExportsCount = 0;
        string errorMessage = string.Empty;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Opening client.realm and extracting metadata...", ctx =>
            {
                try
                {
                    ulong currentVersion = 51; // Default
                    try
                    {
                        // Purposefully trigger an exception with version 0 to read what version the database actually is
                        using var triggerRealm = Realm.GetInstance(new RealmConfiguration(realmPath) { IsReadOnly = true, SchemaVersion = 0 });
                    }
                    catch (Exception ex)
                    {
                        // Message looks like: "Provided schema version 0 does not equal last set version 51."
                        var msg = ex.Message;
                        if (msg.Contains("last set version")) 
                        {
                            var parts = msg.Split(new[] { "last set version" }, StringSplitOptions.None);
                            if (parts.Length == 2)
                            {
                                var versionString = parts[1].Trim().TrimEnd('.');
                                if (ulong.TryParse(versionString, out ulong parsedVersion))
                                {
                                    currentVersion = parsedVersion;
                                }
                            }
                        }
                    }

                    var config = new RealmConfiguration(realmPath)
                    {
                        IsReadOnly = true,
                        SchemaVersion = currentVersion,
                        Schema = new[] {
                            typeof(BeatmapCollection),
                            typeof(RealmUser),
                            typeof(BeatmapMetadata),
                            typeof(BeatmapSetInfo),
                            typeof(BeatmapInfo)
                        }
                    };
                    
                    using var realm = Realm.GetInstance(config);

                    var collections = realm.All<BeatmapCollection>().ToList();
                    var beatmaps = realm.All<BeatmapInfo>().ToList();

                    var md5ToCols = new Dictionary<string, List<string>>();

                    foreach (var col in collections)
                    {
                        var name = col.Name;
                        if (name == null) continue;

                        foreach (var hash in col.BeatmapMD5Hashes)
                        {
                            if (hash == null) continue;

                            if (!md5ToCols.TryGetValue(hash, out var list))
                            {
                                list = new List<string>();
                                md5ToCols[hash] = list;
                            }
                            list.Add(name);
                        }
                    }

                    var rows = new List<CollectionCsvRow>();
                    var exportedMd5s = new HashSet<string>();

                    foreach (var bm in beatmaps)
                    {
                        var md5 = !string.IsNullOrEmpty(bm.MD5Hash) ? bm.MD5Hash : bm.Hash;
                        if (string.IsNullOrEmpty(md5)) continue;

                        var setInfo = bm.BeatmapSet;
                        var metadata = bm.Metadata;

                        var onlineSetId = setInfo?.OnlineID ?? 0;
                        if (onlineSetId <= 0) continue;

                        foundMapsCount++;

                        var cols = md5ToCols.TryGetValue(md5, out var colList) && colList.Count > 0
                            ? colList
                            : new List<string> { "None" };

                        if (colList != null)
                        {
                            exportedMd5s.Add(md5);
                        }

                        var dlLink = $"https://catboy.best/d/{onlineSetId}";
                        var artist = metadata?.Artist ?? "Unknown";
                        var title = metadata?.Title ?? "Unknown";
                        var diff = string.IsNullOrEmpty(bm.DifficultyName) ? "Unknown" : bm.DifficultyName;
                        var starRating = bm.StarRating;

                        foreach (var c in cols)
                        {
                            rows.Add(new CollectionCsvRow
                            {
                                Collection = c,
                                Artist = artist,
                                Title = title,
                                Difficulty = diff,
                                StarRating = starRating,
                                BeatmapID = bm.OnlineID,
                                BeatmapSetID = onlineSetId,
                                MD5 = md5,
                                DownloadLink = dlLink
                            });
                            successfulExportsCount++;
                        }
                    }

                    using var writer = new StreamWriter(outputCsv);
                    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
                    csv.WriteRecords(rows);

                    missingBeatmapsCount = md5ToCols.Count - exportedMd5s.Count;
                    parsedCollectionsCount = collections.Count;
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                }
            });

        if (!string.IsNullOrEmpty(errorMessage))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(errorMessage)}");
            return;
        }

        AnsiConsole.MarkupLine($"[bold green]Parsed {parsedCollectionsCount} collections and {foundMapsCount} total online map difficulties.[/]");

        if (missingBeatmapsCount > 0)
        {
            AnsiConsole.MarkupLine($"[bold yellow]Warning:[/] {missingBeatmapsCount} beatmaps were in collections but not found in the realm dataset (they might lack Online IDs). They were skipped.");
        }

        AnsiConsole.MarkupLine($"[bold green]Successfully exported {successfulExportsCount} Beatmap mappings to {Markup.Escape(outputCsv)}[/]");
    }
}
