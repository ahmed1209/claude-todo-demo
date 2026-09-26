# Revit Model Slimmer

A Revit add-in that makes project files smaller and faster. It adds a **Model Slimmer** ribbon tab with these tools:

| Button | What it does |
| --- | --- |
| **Unused Families** | Lists loadable families that have no placed instances and are not referenced by any element or type (nested/shared families, profiles, balusters, tags in legends, etc.). On Revit 2024+ the result is cross-checked with Revit's own purge engine and marked *verified*. You tick the families to delete and untick the ones to keep. |
| **Unused Filters** | Lists rule-based and selection filters that no view and no view template applies. |
| **Empty Views** | Lists views with nothing visible in them (levels, grids, reference planes and view markers are ignored) pre-checked, plus views that are not on any sheet for review. Sheets, schedules, view templates, the active view and the starting view are never listed. |
| **Unused Templates** | Lists view templates that are not applied to any view and are not the default template of a view family type. |
| **Clean Worksets** | Workshared models only. Shows every user workset with its element count, owner and editability. You choose which to delete and whether their elements are moved to another workset or deleted with it. |
| **Performance Audit** | Finds what makes the file big and slow, then deletes what you confirm. See below. |
| **Slim Model** | Wizard that runs all of the above in sequence and finishes with a compact save. |
| **Compact Save** | Saves (or synchronizes with central) with the *Compact* option. Deleting content alone does not shrink an RVT; the compact save rewrites the file and reclaims the space. |
| **Revit Purge** | Opens Revit's built-in Purge Unused for materials, patterns, text types and other leftovers. |

Every tool follows the same pattern: **analyse -> show a check-list -> confirm (Delete / Cancel) -> delete inside one undoable transaction -> write a report**. Reports are saved to `%LocalAppData%\RevitModelSlimmer\Reports`. Elements that refuse to be deleted are skipped individually and listed in the report; they never abort the batch.

## Performance audit checks

| Check | Default | Notes |
| --- | --- | --- |
| Imported (not linked) CAD | delete | The most common cause of bloated, slow files. |
| Unused CAD link/import types | delete | Definitions without instances still live in the file. |
| Unused raster image types | delete | |
| Unused model/detail group types | delete | |
| Unplaced rooms / areas / spaces | delete | Not-enclosed (zero-area) ones are listed for review. |
| Revit links that are *Not Found* | delete | Merely unloaded links are listed for review. |
| Point clouds | review | |
| In-place families | review | Deleting removes geometry; usually you want to re-model them as loadable families. |
| Families with 100+ types | review | |
| Heavy families (deep audit only) | review, delete if unplaced | Opens every loaded family and measures its saved size. Families over 1 MB are flagged, over 3 MB as high severity. Slow on big models. |

The audit summary also reports file size, element/type/view/sheet counts, detail-line count, design options, the ten most-placed families, the heaviest families (deep audit) and the most frequent warnings.

## Supported versions

| Configuration | Revit | Framework |
| --- | --- | --- |
| `Debug R23` / `Release R23` | 2023 | .NET Framework 4.8 |
| `Debug R24` / `Release R24` | 2024 | .NET Framework 4.8 |
| `Debug R25` / `Release R25` | 2025 | .NET 8 |
| `Debug R26` / `Release R26` | 2026 | .NET 8 |

Revit 2023 is the first version whose API can delete worksets, which is why older versions are not targeted.

## Install without building (recommended)

1. Download **[RevitModelSlimmer-Installer.zip](RevitModelSlimmer-Installer.zip)** (or the `dist` folder) and extract it.
2. Double-click `Install.bat`. It detects the Revit versions on your computer (2023 to 2026) and copies the add-in to `%AppData%\Autodesk\Revit\Addins\<version>\`. No administrator rights are needed.
3. Start Revit and click **Always Load** when it asks about the unsigned add-in. The **Model Slimmer** tab appears once a project is open.

`Uninstall.bat` removes it again. The binaries in `dist` are built from this repository with `build-all.sh` (Linux/macOS) or `build-all.ps1` (Windows).

## Build from source

Requirements: Visual Studio 2022 (17.8+) or the .NET SDK 8 with the .NET Framework 4.8 targeting pack, on Windows. The Revit API assemblies are pulled from the `Nice3point.Revit.Api.*` NuGet packages, so a Revit installation is not required to compile.

```powershell
cd RevitModelSlimmer
dotnet build -c "Release R24"     # or "Release R25", "Debug R26", ...
```

Output goes to `RevitModelSlimmer\build\<Configuration>\`. A **Debug** build additionally copies the add-in into `%AppData%\Autodesk\Revit\Addins\<version>\` so you can start Revit straight away.

## Install manually (from a build)

1. Copy `RevitModelSlimmer.addin` to `%AppData%\Autodesk\Revit\Addins\<version>\`.
2. Copy `RevitModelSlimmer.dll` to `%AppData%\Autodesk\Revit\Addins\<version>\RevitModelSlimmer\` (the manifest points to that sub-folder).
3. Start Revit and open a project. The **Model Slimmer** tab appears in the ribbon.

## Safety notes

* Nothing is deleted without the confirmation dialog, and every deletion is a single named transaction, so **Ctrl+Z** undoes a whole step.
* Work on a detached copy or make a backup before slimming a central model, and synchronize afterwards so other users receive the clean-up.
* "Unused" for families is determined from instances plus every ElementId parameter in the model. On Revit 2023, where Revit's purge engine is not exposed through the API, rows are marked *heuristic*; on 2024+ they are *verified*. Keep families you intend to place later.
* Schedules are never listed as empty views because an empty schedule is usually intentional.
* Deleting a view template that a view relies on is impossible (only unused templates are listed), but filters used only by an unused template will appear as unused after the template is gone, so run **Unused Templates** before **Unused Filters** (the wizard does this for you).

## Project layout

```
RevitModelSlimmer/
  RevitModelSlimmer.sln
  src/RevitModelSlimmer/
    App.cs                       ribbon tab and buttons
    RevitModelSlimmer.addin      manifest
    Analysis/                    usage analysers and the performance auditor
    Commands/                    one IExternalCommand per button + the shared cleanup flow
    Services/                    deletion transaction, failure handling, reports, helpers
    UI/                          WPF check-list window and workset window
    Resources/Icons/             ribbon icons
```
