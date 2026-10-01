# Predefined components

Open **Components** beside **Build** in the left panel. The library starts with a trapezoidal wall, a rounded wall and a straight wall. Select an entry, choose **Place selected component**, then click the floor grid.

## Create and manage presets

- **New trapezoidal wall** edits front/back widths, depth and height.
- **New rounded wall** edits radius, wall width, arc sweep, segment count and height.
- **New custom polygon** edits a footprint as local X/Z coordinates. Use **Insert after** and **Remove vertex** to change its corners. Concave outlines are supported; crossing edges and holes are rejected.
- **Draw custom footprint** lets you click at least three corners in the room. Press **Enter**, click the first point again, or choose **Finish footprint**. **Escape** cancels. The result is a placed object; select **Save as predefined component** in Object, or **Save selected object as preset** in Components, to add it to the library.
- **Edit / rename preset**, **Duplicate preset** and **Delete preset** manage the library. Editing or deleting a preset leaves its already placed instances unchanged.
- **Edit component geometry** in Object changes a placed shape. Design undo/redo restores these edits. Locked objects cannot be modified. Library edits are separate from design undo; the previous library file is retained as a backup.

The editor previews the footprint and saves height, geometry, material, density and applicable object properties. Custom shapes keep their dimensions when linked room walls resize. Presets are placed with new IDs and without the source object's group or protection state.

## JSON and persistence

**Export preset JSON** exports the selected entry; **Export entire library JSON** exports all entries. **Import component JSON** adds entries with fresh IDs, keeping the existing library intact. The library supports up to 100 presets, polygon footprints of 3–64 vertices, and imports up to 2 MB.

On Windows, files are stored under `%USERPROFILE%/AppData/LocalLow/RoomStudio/LINAC Room Studio/Components/`. Use **Open component files** to open that folder. The library is `components.json`, its previous version is `components.json.bak`, and exports go into `Exports/`. Paste the full path of a component JSON into the import field.

In WebGL, exports also download JSON through the browser and imports use a file picker. The library is saved in the browser's persistent filesystem for the current app location. Clearing site data removes that local library; downloaded JSON can restore it or transfer presets between Windows and WebGL.

Exports use the envelope `{"format":"RoomStudio.Components","version":1,"components":[...]}`. Each entry contains its ID, name and a native Room Studio item. The item carries either straight-wall data or custom component geometry. Native version-2 design files retain placed shapes; existing designs without the optional component field remain supported.

## Calculation boundary

Straight-wall presets use the existing reference QA path. Trapezoidal, rounded and polygon components are editable geometry. The current reference engine cannot calculate those shapes, so a design containing one returns an explicit unsupported-geometry error for reference QA and canonical ProShield export. Native design saves and component JSON exports preserve the complete geometry. The original numerical calculation source is unchanged.

## Verification — 2026-09-21

Editor geometry checks cover mesh volume, face winding, concave polygons, invalid geometry, independent instances, native serialization, JSON import/export and disk-library backups. Windows runtime checks cover placing all built-ins, MeshCollider selection, protection, custom drawing, save/edit/rename/delete/reuse, undo/redo and persistence. The complete Windows smoke suite passes, including QA and exact canonical fixture round-trip. Twelve real browser-worker checks pass, including explicit rejection of unsupported shapes for QA and canonical export.

Windows and WebGL were rebuilt with the final preview correction. The deployed-folder copy at `web/` passes `ROOM_STUDIO_COMPONENT_RUNTIME_PASSED` and `ROOM_STUDIO_WEB_FEATURE_CHECKS_PASSED`; all 22 launcher/build/worker files match `WebGL/` by SHA-256. Its build revision is `0863a2b722724ec393278c5014602b10`. No public deployment was performed.

Interactive browser verification created a concave six-corner footprint, saved it as a preset, renamed it to `Ltest`, changed its height to 4 m, downloaded its JSON, reloaded the app, reused the persisted preset, deleted the preset while retaining its placed object, and imported the downloaded JSON through the file picker. A second download retained the name, height and all six vertices. The temporary test preset was removed afterward. Normal key events verified naming; the browser automation tool's bulk text insertion does not enter text into Unity canvas fields. The download event listener timed out, but the actual downloaded file was found and its JSON contents verified.

Native Windows and browser visual checks confirm the corrected preview stays inside its panel at their different display scales. Dense curved-wall vertex labels are hidden. Browser QA reports the unsupported custom geometry explicitly. The final `web/` copy also calculates the normal sample design and displays all three barrier results successfully.

Evidence:

- `component-windows-build-verified.log`: `ROOM_STUDIO_BUILD_SUCCESS`, editor component geometry/library checks and prior regression checks.
- `Windows/component-runtime-smoke.log`: component, selection, shortcut, palette, QA popup, import and full runtime success markers. Smoke tests use an isolated temporary design folder, leaving normal user saves untouched.
- `component-webgl-build-verified.log`: `ROOM_STUDIO_WEBGL_BUILD_SUCCESS` and final revision.
- `component-web-runtime.log`: compiled runtime checks from the final `web/` copy.
- `component-web-package-verified.log`: SHA-256 equality for 22 files.
- `component-browser-worker-checks.log`: all 12 real browser-worker checks pass.
- `component-benchmarks.log`, `component-adapter-checks.log`, `component-workspace-checks.log`: all 17 numerical benchmarks and adapter/workspace regressions pass.
- `component-evidence/`: native and browser preview screenshots, verified downloaded JSON and file round-trip checks.

Repeat the builds with **Room Studio > Build Windows app** and **Build WebGL app**. Run the Windows executable with `--smoke-test -logFile <absolute-log-path>`. Serve the Unity folder with `python serve-webgl.py --directory .`, open `/web/?feature-tests=1` for compiled app checks and `/WebGL-tests/` for worker checks. Existing ZIP archives are older snapshots; use the current `Windows/`, `WebGL/` and `web/` folders.
