# WallFinishGlobal — Revit 2025

Revit 2025 add-in that previews room-rule-based wall finish type variants before applying approved changes. The add-in itself is a DLL loaded by Revit. A separate Windows EXE installer installs the DLL, rules file, and manifest for the current Windows user.

## Build and install
Requirements: Windows, .NET 8 SDK, and Revit 2025 API assemblies. By default the API path is `C:\Program Files\Autodesk\Revit 2025`.

```powershell
.\scripts\build-installer.ps1
```

Optional API path:
```powershell
.\scripts\build-installer.ps1 -RevitApiDir "D:\Autodesk\Revit 2025"
```

The script outputs `artifacts\installer\WallFinishInstaller.exe`. Run it, then restart Revit 2025. The installer targets `%AppData%\Autodesk\Revit\Addins\2025` and does not require admin rights. Autodesk Revit API assemblies are referenced but never copied into the package.

## Included behavior
- Ribbon buttons: Apply and read-only derived-type report.
- Rules JSON: room shared parameter `FinishRoomType`, falling back to room name, wildcard matching.
- Finish material lookup by string parameter `FinishCode`, then exact material name.
- Room boundary → wall mapping; divergent finish signatures on a wall are conflict/skip.
- In-memory plan and preview before a single Revit transaction.
- Derived wall types only; existing source types are not edited. Symmetric Finish1/Finish2 only.
- Missing materials/thickness and unequal existing finish widths are reported and skipped.
- No updater, purge, material creation, or autonomous/background model edits.

Edit `src/WallFinishGlobal/Resources/finish-rules.json` to configure the rules. Validate the add-in in a test Revit model before production use.
