# Builds every supported Revit version and refreshes the dist folder. Run from the RevitModelSlimmer folder.
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\RevitModelSlimmer\RevitModelSlimmer.csproj'
foreach ($cfg in 'Release R23', 'Release R24', 'Release R25', 'Release R26') {
    $version = '20' + $cfg.Substring($cfg.Length - 2)
    dotnet build $project -c $cfg
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    New-Item -ItemType Directory -Force -Path (Join-Path $PSScriptRoot "dist\$version") | Out-Null
    Copy-Item (Join-Path $PSScriptRoot "build\$cfg\RevitModelSlimmer.dll") (Join-Path $PSScriptRoot "dist\$version\") -Force
}
Copy-Item (Join-Path $PSScriptRoot 'src\RevitModelSlimmer\RevitModelSlimmer.addin') (Join-Path $PSScriptRoot 'dist\') -Force
Compress-Archive -Path (Join-Path $PSScriptRoot 'dist\*') -DestinationPath (Join-Path $PSScriptRoot 'RevitModelSlimmer-Installer.zip') -Force
Write-Host 'dist folder and RevitModelSlimmer-Installer.zip refreshed.'
