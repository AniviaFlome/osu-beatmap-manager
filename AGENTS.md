# AGENTS.md

## Project overview

`obm` — a .NET 8 CLI tool (Spectre.Console.Cli) for exporting osu!lazer beatmap collections from a local Realm database to CSV, and importing them back by downloading `.osz` files and sending them to osu!lazer.

## Build & run

```bash
dotnet build src/obm
dotnet run --project src/obm -- export          # export collections
dotnet run --project src/obm -- import file.csv # import from CSV
```

No solution file exists. The single csproj is at `src/obm/obm.csproj`.

**Required SDK**: `dotnet-sdk_8` specifically (not just `dotnet-sdk`). A Nix dev shell is provided in `shell.nix`.

## No tests

There is no test project or test runner configured.

## Key architectural notes

- **Realm + Fody weaving**: Models in `Models.cs` inherit from `RealmObject`. The Fody weaver (`FodyWeavers.xml`) runs at build time. Edits to `Models.cs` require a full rebuild for weaving to take effect.
- **Schema version hack in `Exporter.cs`**: The code intentionally opens the Realm with `SchemaVersion = 0` to trigger an exception whose message contains the real schema version. This is not a bug — do not "fix" it.
- **Download mirror**: Hardcoded to `catboy.best` (`/d/{setId}`). The `DownloadLink` field in the CSV uses this mirror.
- **osu! binary detection**: The `import` command searches PATH for executables named `osu!`, `osu-lazer`, or `osu-lazer-bin`.

## Gitignored outputs

`collections.csv` and `test.csv` are gitignored — they are program output, not source.