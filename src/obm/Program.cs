using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace OsuBeatmapManager;

public class ExportCommand : Command<ExportCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[output_csv]")]
        [Description("Path to save the exported CSV file")]
        [DefaultValue("collections.csv")]
        public string OutputCsv { get; set; } = string.Empty;

        [CommandOption("-d|--osu-dir <DIR>")]
        [Description("Path to your osu! folder (containing client.realm)")]
        [DefaultValue("~/.local/share/osu/")]
        public string OsuDir { get; set; } = string.Empty;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        settings.OsuDir = settings.OsuDir.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        
        AnsiConsole.Write(
            new Panel($"[bold cyan]Exporting osu! collections[/]\nSource: {settings.OsuDir}\nDestination: {settings.OutputCsv}")
                .Expand()
        );

        Exporter.ExportCollections(settings.OsuDir, settings.OutputCsv);
        return 0;
    }
}

public class ImportCommand : AsyncCommand<ImportCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[input_csv]")]
        [Description("Path to the CSV file to import")]
        [DefaultValue("collections.csv")]
        public string InputCsv { get; set; } = string.Empty;
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        AnsiConsole.Write(
            new Panel($"[bold cyan]Importing osu! beatmaps[/]\nSource CSV: {settings.InputCsv}\n[italic]Beatmaps will be downloaded temporarily and sent directly to osu!lazer.[/]")
                .Expand()
        );

        await Importer.ImportCollectionsAsync(settings.InputCsv);
        return 0;
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("obm");
            config.AddCommand<ExportCommand>("export")
                .WithDescription("Parse local osu! databases and export collections to a CSV file.");
                
            config.AddCommand<ImportCommand>("import")
                .WithDescription("Read an exported CSV file, download the missing beatmap sets (.osz) from a mirror, and natively import them into osu!lazer.");
        });

        return app.Run(args);
    }
}
