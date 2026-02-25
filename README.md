# Osu Beatmap Manager

Import and export your osu!lazer beatmaps.

## What it does
- **Exporting**: Reads your local `client.realm` database and creates csv file containing beatmaps.
- **Importing**: Download `.osz` files from beatmap mirrors and import them into osu!lazer.


## Installation
1. Clone the repository: 
    ```bash
    git clone https://github.com/aniviaflome/osu-beatmap-manager.git
    cd osu-beatmap-manager
    ```
2. Build the project:
    ```bash
    dotnet build src/OsuBeatmapManager
    ```

## Usage
Run the application natively or via Nix.

### Exporting your Collections
```bash
# Uses default ~/.local/share/osu path
dotnet run --project src/OsuBeatmapManager -- export 

# Or via Nix Shell:
nix run nixpkgs#dotnet-sdk_8 -- run --project src/OsuBeatmapManager -- export
```

You can optionally specify a custom database location:
```bash
dotnet run --project src/OsuBeatmapManager -- export /path/to/my/osu/database output_collections.csv
```
The resulting `collections.csv` provides metadata like Artist, Title, Difficulty, Set ID, and a mirror download link.

### Importing `.osz` from a CSV
```bash
# Parses the CSV and sends downloads directly to Lazer
dotnet run --project src/OsuBeatmapManager -- import collections.csv
```
