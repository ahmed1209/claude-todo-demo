Revit Model Slimmer
===================

Cleans unused families, filters, empty views, view templates and worksets,
audits what makes a Revit file slow, and compacts the file.

INSTALL (Revit 2023, 2024, 2025 or 2026)
----------------------------------------
1. Extract this zip to any folder (do not run it from inside the zip).
2. Double-click Install.bat.
   It finds your Revit versions and copies the add-in to
   %AppData%\Autodesk\Revit\Addins\<version>\
3. Start Revit. If it asks about an unsigned add-in, click "Always Load".
4. Open a project: the "Model Slimmer" tab appears in the ribbon.

If Windows blocks the script, right-click Install.bat > Run as... is NOT needed;
it runs as your normal user. If you prefer to do it by hand, copy
  RevitModelSlimmer.addin         ->  %AppData%\Autodesk\Revit\Addins\<version>\
  <version>\RevitModelSlimmer.dll ->  %AppData%\Autodesk\Revit\Addins\<version>\RevitModelSlimmer\
then right-click the DLL > Properties > tick "Unblock".

UNINSTALL
---------
Double-click Uninstall.bat and restart Revit.

TOOLS
-----
Unused Families   - list of families with no instances/references; tick what to delete
Unused Filters    - filters not applied by any view or view template
Empty Views       - views with nothing in them (pre-checked) + views not on sheets (for review)
Unused Templates  - view templates not applied anywhere and not a view-type default
Clean Worksets    - choose worksets to delete; move or delete their elements
Performance Audit - imported CAD, unused CAD/image/group types, unplaced rooms, missing links,
                    point clouds, in-place families, heavy families, warnings; deletes after confirmation
Slim Model        - runs everything in sequence, then compacts the file
Compact Save      - save / synchronize with the Compact option (this is what actually shrinks the file)
Revit Purge       - opens Revit's own Purge Unused

Every deletion asks for confirmation, is undoable with Ctrl+Z, and writes a report to
%LocalAppData%\RevitModelSlimmer\Reports.
Work on a backup or a detached copy the first time you slim a model.
