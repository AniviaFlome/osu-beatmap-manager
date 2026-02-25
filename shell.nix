{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  packages = with pkgs; [
    dotnet-sdk_8
  ];

  shellHook = ''
    echo "Run 'nix run nixpkgs#dotnet-sdk_8 -- run --project src/OsuBeatmapManager --' to test."
    echo "Run 'dotnet build src/OsuBeatmapManager/OsuBeatmapManager.csproj' to compile."
  '';
}
