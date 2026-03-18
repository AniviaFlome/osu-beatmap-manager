using CsvHelper;
using Spectre.Console;
using System.Diagnostics;
using System.Globalization;

namespace OsuBeatmapManager;

public static class Importer
{
    public static async Task ImportCollectionsAsync(string inputCsv)
    {
        if (!File.Exists(inputCsv))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Could not find CSV file at {Markup.Escape(inputCsv)}");
            return;
        }

        List<CollectionCsvRow> records;
        try
        {
            using var reader = new StreamReader(inputCsv);
            using var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture));
            records = csv.GetRecords<CollectionCsvRow>().ToList();
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[bold red]Error reading CSV file:[/] {Markup.Escape(e.Message)}");
            return;
        }

        var uniqueSets = records
            .Where(r => r.BeatmapSetID > 0)
            .DistinctBy(r => r.BeatmapSetID)
            .ToList();

        AnsiConsole.MarkupLine($"[bold green]Found {uniqueSets.Count} unique beatmap sets to download.[/]");

        var successCount = 0;
        var outputDir = Path.Combine(Path.GetTempPath(), "osu-beatmap-manager-downloads", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "osu-beatmap-manager/1.0 (https://github.com/aniviaflome/osu-beatmap-manager)");

            await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn { Alignment = Justify.Right },
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new DownloadedColumn(),
                    new TransferSpeedColumn(),
                    new RemainingTimeColumn()
                )
                .StartAsync(async ctx =>
                {
                    foreach (var row in uniqueSets)
                    {
                        var setId = row.BeatmapSetID;
                        var url = row.DownloadLink;

                        var artist = SanitizeFileName(row.Artist);
                        var title = SanitizeFileName(row.Title);

                        var filename = $"{setId} {artist} - {title}.osz";
                        var destPath = Path.Combine(outputDir, filename);

                        if (File.Exists(destPath))
                        {
                            AnsiConsole.MarkupLine($"[bold yellow]Skipping:[/] {Markup.Escape(filename)} already exists.");
                            successCount++;
                            continue;
                        }

                        var task = ctx.AddTask($"Downloading {setId}...", autoStart: true);

                        try
                        {
                            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                            response.EnsureSuccessStatusCode();

                            var totalBytes = response.Content.Headers.ContentLength;
                            if (totalBytes.HasValue)
                            {
                                task.MaxValue = totalBytes.Value;
                            }

                            using var contentStream = await response.Content.ReadAsStreamAsync();
                            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                            var buffer = new byte[8192];
                            var isMoreToRead = true;

                            do
                            {
                                var bytesRead = await contentStream.ReadAsync(buffer.AsMemory());
                                if (bytesRead == 0)
                                {
                                    isMoreToRead = false;
                                    continue;
                                }

                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                task.Increment(bytesRead);
                            }
                            while (isMoreToRead);

                            successCount++;
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.MarkupLine($"[bold red]Failed to download {Markup.Escape(url)}:[/] {Markup.Escape(e.Message)}");
                            if (File.Exists(destPath)) File.Delete(destPath);
                        }
                        finally
                        {
                            task.StopTask();
                        }
                    }
                });

            AnsiConsole.MarkupLine($"[bold green]Finished![/] Successfully downloaded {successCount}/{uniqueSets.Count} beatmap sets.");

            var oszFiles = Directory.GetFiles(outputDir, "*.osz");
            if (oszFiles.Length == 0) return;

            var osuBin = GetOsuExecutable();

            if (!string.IsNullOrEmpty(osuBin))
            {
                AnsiConsole.MarkupLine($"[bold cyan]Auto-importing {oszFiles.Length} beatmaps into osu!lazer...[/]");
                var importCount = 0;

                foreach (var osz in oszFiles)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = osuBin,
                            Arguments = $"\"{osz}\"",
                            UseShellExecute = false,
                        });
                        importCount++;
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine($"[bold red]Failed to run osu! auto-import for {Markup.Escape(Path.GetFileName(osz))}:[/] {Markup.Escape(e.Message)}");
                    }
                }

                if (importCount > 0)
                {
                    AnsiConsole.MarkupLine($"\n[bold green]Successfully ran import command for {importCount} beatmap(s)![/] They are now processing in the background.");
                }

                await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new RemainingTimeColumn()
                    )
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[bold yellow]Waiting for osu! to finish copying files before cleanup...[/]", maxValue: 10);
                        for (int i = 0; i < 10; i++)
                        {
                            await Task.Delay(1000);
                            task.Increment(1);
                        }
                    });

                AnsiConsole.MarkupLine("[bold green]Temporary files cleaned up successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold yellow]Next Step:[/] Could not find osu! executable in PATH. Automatic import was aborted.");
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private static string SanitizeFileName(string input)
    {
        var value = string.IsNullOrEmpty(input) ? "Unknown" : input;
        return string.Concat(value.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c));
    }

    private static string? GetOsuExecutable()
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        var candidates = new[] { "osu!", "osu-lazer", "osu-lazer-bin" };

        foreach (var path in paths)
        {
            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(path, candidate);
                if (File.Exists(fullPath)) return fullPath;
            }
        }
        return null;
    }
}
