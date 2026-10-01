# Migration checkpoint — 2026-09-14

## Current QA and WebGL update

Windows and WebGL builds completed on 2026-09-14. The fixed QA button, results popup, browser-worker engine and browser file handling are implemented. Current Windows runtime tests passed all three markers, including ROOM_STUDIO_QA_POPUP_SMOKE_PASSED. Both C# paths, 17 benchmarks, existing regressions and seven browser-worker tests pass. Linked room resizing passed nine geometry/persistence checks, native build checks and manual WebGL width/ceiling/wall-height checks. See [WEBGL.md](../WEBGL.md) for current evidence and build instructions.

## Completed
- Verified Windows build through Unity Hub using Unity 6000.6.0f1. The earlier license/startup blocker is resolved for this workflow.
- Integrated all nine supplied models, corrected mm shielding to metre rendering, native design validation/migration, occupancy regions, undo and offline reference QA.
- Added visual canonical-room/workspace JSON import and export, validation before project replacement, and reported repairs for supported missing optional fields.
- Preserved original source JSON and an editor baseline in native saves. Unchanged exports retain precise numbers, unknown fields, wrapper metadata, explicit source components and maze configuration.
- Merged supported geometric/shielding edits and additions/deletions into source data. Moving/rotating imported equipment transforms its explicit components with it.
- Fixed numeric controls silently rounding values merely by displaying them; imported source rotation accepts negative values.

## Verification
- Unity build completed with ROOM_STUDIO_BUILD_SUCCESS and model/project checks passed.
- Built Windows executable completed ROOM_STUDIO_IMPORT_SMOKE_PASSED and ROOM_STUDIO_RUNTIME_SMOKE_PASSED.
- Runtime test imported a two-source fixture, rebuilt the scene, saved and loaded through Unity JsonUtility, exported canonical JSON, and calculated nonempty reference QA.
- A separate deep comparison confirmed that the runtime export exactly equals the imported canonical fixture, including high-precision and unknown fields.
- Imported-room preview inspected in the built app.
- C# compilation without warnings and 18 standalone data validation checks passed during this implementation.
- Workspace regression checks passed for bare/wrapped rooms, float projection precision, source movement, ordered wall endpoints, deletion, defaults and invalid data.
- Existing adapter checks passed. The original 17 source benchmark cases passed in the preceding continuation; numerical formula source was not changed.

Runtime log and import-preview.png are in the sibling Windows folder. Run the built app with --smoke-test, or use the Unity **Room Studio > Test Windows app** menu, to repeat the integration test.

## Data and calculation boundaries
The bundled offline Node runtime executes the original ProShield TypeScript formulas from source commit c33e41d. The original source snapshot and hashes are retained. This is not yet a pure C# migration.

Imported source settings are used by reference QA; beam, diagnostic and maze fields remain preserved until dedicated editing controls are implemented. Imports validate supported enums, finite numbers, IDs and bounded geometry; this is not a universal schema editor. Missing optional fields receive documented defaults and an import summary.

New native designs use the existing four-field LINAC adapter. It translates centred Unity X/Z coordinates, yaw, rotated isocentre, workload and equivalent field size into source inputs. The orange beam is schematic and is hidden for imported source designs. Wall elevations and density remain visual/metadata where the source engine has no corresponding calculation.

QA rows represent barrier samples in mGy/week, not exact workstation-point doses or effective dose in Sv. Empty occupancy provides no compliance conclusion. Model scale/isocentre calibration and independent physics validation remain outstanding. GLB conversion preserves flat transforms and base colours, not embedded textures or advanced materials.

## Next steps
1. Add arbitrary source controls and matching beam cones, diagnostic configurations and explicit maze controls/report traces.
2. Port source catalog and engine into a Unity-independent C# assembly; compare broad frozen wall/floor/ceiling/maze fixtures before replacing the reference bridge.
3. Finish elevation editing, floor-plan image import, complete polygon editing/report presentation and independent physics validation.

See README.md for import/export and run instructions. Original Next.js source remains unchanged. Supplied documents were used as engineering references, not independent authorization.
