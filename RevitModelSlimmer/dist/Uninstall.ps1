<#
    Revit Model Slimmer - uninstaller
    Removes the add-in from every per-user Revit add-ins folder.
#>
$ErrorActionPreference = 'Continue'
$addinsRoot = Join-Path $env:APPDATA 'Autodesk\Revit\Addins'
Write-Host ''
Write-Host '=== Revit Model Slimmer uninstaller ===' -ForegroundColor Cyan
$removed = 0
if (Test-Path $addinsRoot) {
    foreach ($dir in Get-ChildItem -Path $addinsRoot -Directory) {
        $manifest = Join-Path $dir.FullName 'RevitModelSlimmer.addin'
        $folder = Join-Path $dir.FullName 'RevitModelSlimmer'
        if ((Test-Path $manifest) -or (Test-Path $folder)) {
            Remove-Item -Path $manifest -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  Revit $($dir.Name): removed" -ForegroundColor Green
            $removed++
        }
    }
}
if ($removed -eq 0) { Write-Host '  The add-in was not installed for any Revit version.' }
Write-Host 'Restart Revit to complete the removal.'
