#!/usr/bin/env bash
# Builds every supported Revit version on Linux/macOS (no Revit needed) and refreshes dist/.
set -euo pipefail
cd "$(dirname "$0")"
for cfg in "Release R23" "Release R24" "Release R25" "Release R26"; do
  version="20${cfg: -2}"
  dotnet restore src/RevitModelSlimmer/RevitModelSlimmer.csproj -p:Configuration="$cfg" -p:EnableWindowsTargeting=true
  dotnet build src/RevitModelSlimmer/RevitModelSlimmer.csproj -c "$cfg" -p:EnableWindowsTargeting=true --no-restore
  mkdir -p "dist/$version"
  cp "build/$cfg/RevitModelSlimmer.dll" "dist/$version/"
done
cp src/RevitModelSlimmer/RevitModelSlimmer.addin dist/
rm -f RevitModelSlimmer-Installer.zip
(cd dist && zip -q -r ../RevitModelSlimmer-Installer.zip .)
echo "dist folder and RevitModelSlimmer-Installer.zip refreshed."
