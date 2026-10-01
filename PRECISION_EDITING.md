# Request 5 — first implementation increment

September 25, 2026 implementation checkpoint; historical verification updated September 26 and current status reconciled October 1. This first increment added the fractional-editing foundation in the existing editor. Calibration and color masks followed in [PLAN_AUTHORING.md](PLAN_AUTHORING.md). Generated walls, physical junction unions, connected topology and guarded regeneration are implemented in the October 1 builds. Editor precision checks and recorded final Windows/compiled WebGL precision runtime checks pass. Comprehensive end-user control, visual/actual-touch and performance acceptance remains open; see [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md#request-5-automatic-color-coded-walls-connected-edges-and-fractional-editing).

## Controls

- On screens below 1000 pixels wide or 600 pixels high, **Scene**, **Build** and **Properties** select one workspace surface at a time. Selecting a placement tool returns to the full-width scene. Properties and calculation controls share one scrollable panel; desktop side panels and fixed calculation controls are unchanged. Numeric rows fit the compact panel, and panel/modal clicks cannot become delayed scene gestures. Pure layout checks cover 320/390-pixel phones, 667-pixel landscape, 768-pixel tablets and 1024/1600/2560-pixel desktops. Actual visual/touch acceptance remains unverified.
- The main sidebar has independent **Snap to grid**, **Snap to wall**, and **Show display grid (1 m)** toggles. **Snap to wall** targets wall endpoints and edges. Both snap assists default to off. Changing them does not move anything or add a transform undo entry.
- **Precision settings** provides move and resize increments, each initially **0.01 m**, with 0.001 / 0.01 / 0.1 / 1 m presets and exact input. Thickness has its own **1 mm** initial step. Model scale uses a dimensionless **0.01** step.
- Numeric metre, millimetre and model-scale controls now have −/+ steppers. Position/point controls use the move step; dimensions use the resize step. Sliders are continuous, without the old range-dependent whole-unit quantization. Numeric text uses a round-trip float format and rejects nonfinite, out-of-range and incomplete entries.
- Arrow keys nudge selected objects on world X/Z. Object properties also provide X−/X+/Z−/Z+ buttons. These exact increments ignore snapping. Existing R/Shift+R rotation remains ±15 degrees. Text fields, popups, drawing, placement and pointer gestures suppress arrow nudges.
- **Length anchor: Start / Center / End** preserves the selected fixed point when changing wall length. In 2D, an individually selected unprotected wall has Start/End handles; dragging one changes its endpoint around the opposite fixed endpoint. Escape restores its starting geometry. Group selections use rigid movement rather than individual resize handles.
- Existing fractional controls for room dimensions, regions, guides, equipment and component geometry use the shared numeric implementation and retain their original limits and editing rules.

## Snapping and geometry contract

The snap grid is independent of the visible 1 m reference grid. Its default spacing is **0.1 m**, aligned to world X/Z and origin (0,0). Equal half cells round away from zero for positive and negative coordinates, with a bounded tolerance for float32 representation of decimal spacing.

Wall snapping uses straight-wall centerline endpoints and physical long side edges. Edge offsets convert wall thickness from mm to metres exactly once. Eligible endpoints take priority over side edges within each anchor search, and wall targets take priority over grid targets. The displayed tolerance defaults to **0.02 m** and is independent of zoom. Targets exclude all selected objects during a drag or resize. Protected target walls can be unchanged snap anchors.

Yellow markers and a target/coordinate label show the chosen target before release. Placement and drawing use the same target resolver. Rigid drag movement is computed from initial positions, not accumulated frame deltas; all group members receive one shared correction. Grid dragging snaps the picked object's initial center plus the gesture displacement. Wall snapping uses the selected walls' endpoints and other selected objects' origins. A stationary click does not snap or reposition anything. Exact typed values and steppers bypass both assists.

Scene press/release positions are captured from IMGUI events and processed in order. A quick drag whose press and release arrive before one update retains the actual press position. Selection modifiers are captured with the press. This fixes a missed-handle/accidental-marquee case found during native interaction testing. Shift/Ctrl selection gestures do not activate resize handles.

**Snapping is a positioning assist, not physical wall connection.** It does not trim or merge footprints, repair openings, record a junction, or guarantee a crack-free shielding union. The separate **Connect wall edges** and **Generate connected walls** workflows provide preview/Apply transactions and joined-geometry checks.

## Persistence and compatibility

Native schema/version stays 2. The optional `Design.editing` object stores `moveStep`, `resizeStep`, `thicknessStepMm`, `scaleStep`, `gridVisible`, `gridSnap`, `wallSnap`, `gridSpacing`, `wallSnapTolerance` and `lengthAnchor`. Absent/null settings use the new-design defaults. Older designs did not persist the former transient 0.5 m snap checkbox, so there is no saved setting to migrate from it.

Settings persist on native save/load and remain current across transform undo/redo. The active history baseline is refreshed without inserting a preference-only transform record. Movement and resize undo/redo retain surviving selected IDs and expand their saved groups. Existing deletion/import selection behavior is not a general selection-history implementation.

Move/resize/grid spacing accept 0.000001–100 m; wall-snap tolerance accepts 0.000001–1 m; thickness step accepts 0.000001–10000 mm; scale step accepts 0.000001–1. Existing float32 geometry and coordinate limits still apply: fine increments at extremely large coordinate magnitudes are limited by float32 resolution. Existing minimum wall length is 0.25 m.

Precision settings are native editor metadata; they do not enter canonical wall fields or reference QA. Canonical export retains its existing format and precise unknown source metadata. Use native project JSON to exchange editing preferences.

## Source owners

- `Assets/Scripts/PrecisionEditing.cs`: preferences, validation, parsing, stepping, grid/edge target resolution and anchored wall resize geometry.
- `Assets/Scripts/StudioPrecision.cs`: main precision controls, nudges, gesture state, handles and target feedback.
- `Assets/Scripts/PrecisionChecks.cs` and `StudioPrecisionChecks.cs`: shared editor/player numerical and interaction-path regression checks.
- Existing `StudioApp`, `StudioShortcuts`, `StudioComponents`, `ProjectData` and `BuildStudio` connect these paths to the current editor, native data and check entry points.

## Initial verification history

Evidence is under `LinacRoomStudio/work/req5/`. The initial work encountered a Package Manager IPC failure on direct startup (`baseline-editor.log`) and a Hub licence activation timeout. These blockers were resolved when the authoritative project opened through Hub. The historical managed-assembly checks below preceded the fresh builds described at the end of this document.

- Existing REQ4 Windows binary: `baseline-player.log` ends in `ROOM_STUDIO_RUNTIME_SMOKE_PASSED`, including floor-plan, component, shortcut, palette, selection, QA and import checks. This re-establishes the existing player baseline; it is not a fresh editor build.
- `compile.ps1` compiles current runtime scripts for editor, Windows Mono and WebGL/IL2CPP defines using Unity 6000.6.0f1's compiler and cached reference response files, then compiles the editor checks against the new reference assembly. It only writes under `work/req5`; it does not replace Unity's Library assemblies. Compiler success does not establish a new IL2CPP/WebGL build.
- `precision-player.log` runs the new Windows managed assembly in an isolated copy of the existing REQ4 Mono player and assets (`work/req5/player`). This is **managed-code runtime evidence, not a rebuilt Windows distribution**. The precision suite checks 100 fractional nudges, signed grid ties, exact values, fixed anchors, rigid group offsets, protection, invalid entries, old/native/null defaults, preference/history isolation, focus guards, cancellation and nudge/resize undo/redo. Existing feature and QA/import smoke tests run in the same process.
- Native interactive checks in that isolated player verified 0 → 0.01 m through the nudge button, toggling grid snap without geometry movement, typing 1.237 m with grid snap enabled, a free endpoint drag with the opposite end fixed, and Ctrl+Z restoring both the wall geometry and its selection. `interaction.log` records these observations. The queued-event regression injects captured samples directly because Unity does not expose `Event.type` outside an OnGUI callback.
- `benchmark.log`, `checks.log`, `workspace-checks.log` and `floor-plan-checks.log`: reference numerical, adapter, precise workspace and floor-plan bridge regressions. `precision-checks.mjs` adds explicit preference-only geometry, QA and canonical-export invariance checks.

Final source compilation records zero diagnostics for all four compilation paths in `compilation.log`. The final managed runtime run contains both `ROOM_STUDIO_PRECISION_CHECKS_PASSED` and `ROOM_STUDIO_PRECISION_RUNTIME_PASSED`, followed by `ROOM_STUDIO_RUNTIME_SMOKE_PASSED`. Its Windows assembly SHA-256 is `C7A77025B1A11126C83110528EA79F2A75E26E9FE646FAD7963B419726804E0F`; individual source hashes are recorded in `source-hashes.json`. Batch-mode screenshot capture messages are not interaction evidence; the separate interactive observations above were made in a visible test player.

## Fresh build verification — September 26

Real Unity editor checks, complete Windows and WebGL builds and both compiled runtime suites now pass, including the precision checks above. `authoring-windows-build.log`, `authoring-windows-smoke.log`, `authoring-webgl-build.log` and `authoring-web-runtime.json` record the fresh results. The browser suite runs against the final `web/` package with revision `adde2367b21741088fa4b01b6d0e2b2f`. All 29 build files match `WebGL/` by SHA-256 in `authoring-package-hashes.json`.

Native browser download/reimport and actual persistent reload were verified for this historical calibration/rules increment; saved designs also preserve optional precision preferences. These September observations do not establish complete final-build browser persistence acceptance. No public deployment was performed; older ZIP files remain historical snapshots.

## October 1 runtime evidence reconciliation

The [final Windows runtime record](LinacRoomStudio/Logs/req5-req6-acceptance-windows-runtime-final-2026-10-01.log) and [final compiled WebGL record](LinacRoomStudio/work/req56-acceptance/webgl-feature-results.json) contain `ROOM_STUDIO_PRECISION_RUNTIME_PASSED`: preference changes without transform history, grid display, nudge/resize undo-redo, focus guards, free drag, cancellation and native load. Editor precision evidence also covers 100 nudges, exact inputs, fixed anchors, rigid groups, protection and legacy defaults.

Latest recorded WebGL revision is `82c62f8040ea4defbe67f58a17ebfa09`; the [acceptance package receipt](LinacRoomStudio/Logs/req5-req6-acceptance-package-2026-10-01.json) verifies 29 local delivery payloads. Recorded runtime checks are complete for their stated coverage, while comprehensive end-user control/visual/actual-touch and maximum-size responsiveness/cancellation acceptance remains open. Complete final-build browser reload/download/reimport is also not conclusively recorded. This update changes documentation only; current status and limits are in [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md#request-5-automatic-color-coded-walls-connected-edges-and-fractional-editing).
