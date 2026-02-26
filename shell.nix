{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  packages = with pkgs; [
    dotnet-sdk_8
  ];

  shellHook = ''
    echo "Run 'nix run nixpkgs#dotnet-sdk_8 -- run --project src/obm --' to test."
    echo "Run 'dotnet build src/obm/obm.csproj' to compile."
  '';
}
