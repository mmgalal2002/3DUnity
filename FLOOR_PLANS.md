# Floor-plan tracing and shortcut refinements

Request 4 release, verified September 24–25, 2026. Use the current `Windows/`, `WebGL/` and `web/` folders; older ZIP files are historical snapshots. Request 5 was completed on September 29; see [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md#request-5-automatic-color-coded-walls-connected-edges-and-fractional-editing).

## Controls and workflow

1. Open **Floor plan → Choose floor-plan file** and select PNG, JPEG or `RoomStudio.FloorPlan` v1 JSON. Windows also offers **Load from path**. Importing replaces only the guide and is undoable.
2. Image import starts at **0.01 metres per pixel**. Set a measured scale: a 600 × 400 image at **0.02 m/px** spans **12 × 8 m**. Changing metres per pixel scales both dimensions uniformly. Width and height can also be adjusted independently to correct scan distortion; the displayed scale follows horizontal width.
3. Adjust visibility, opacity, X/Z position and rotation. **Fit guide** centres the top view. **Trace walls over guide** starts ordinary wall drawing; click each wall's endpoints. **Escape** returns to selection. Turn off the 0.5 m snap for finer tracing. Existing component placement can also be used over the guide.
4. **Save design** embeds the image/vector data and display settings. The original source path is informational; reopening the native design does not require that source file. Browser saves persist for the same site and browser storage.
5. **Export guide JSON** writes/downloads a reusable guide with its dimensions. It omits the current position, rotation, opacity and visibility; native design saves retain those settings. Canonical ProShield export stores the full supported guide under `room.floorPlan` as editor metadata and preserves unrelated source precision and unknown fields.

The guide is a visual layer below walls/equipment in 2D. It has no collider, is absent from the scene-object list and shielding geometry, and is hidden in 3D. QA results are unchanged by guide-only edits. This release does not automatically detect or generate walls.

**F1** opens help; F1, Escape and the close button dismiss it. Windows text can be selected and copied with Ctrl+C; **Copy / select help** also copies the full help. In WebGL that button opens browser-native selectable text, with its own Close/F1/Escape controls. **R** rotates the selection clockwise 15°, **Shift+R** counter-clockwise 15°. Rotation is suppressed in text/numeric fields, popups, dragging, drawing, component editing and placement modes. Protected objects retain their existing restrictions.

## Supported files

PNG/JPEG: non-empty, up to 18 MiB compressed, 8192 pixels per side and 16,777,216 total pixels. Dimensions are checked before decoding, and image content is decoded and verified before the active guide is replaced. JSON: non-empty UTF-8 (optional BOM), up to 25 MiB. Invalid imports preserve the current design and history.

Vector JSON uses metres in a centred extent. Segment `x` maps to world X; segment `y` maps to world Z. Every endpoint must fit within ±width/2 and ±height/2. There must be 1–2000 nonzero segments. Resizing a vector guide scales its endpoints.

```json
{
  "format": "RoomStudio.FloorPlan",
  "version": 1,
  "widthMeters": 12,
  "heightMeters": 8,
  "segments": [
    { "start": { "x": -6, "y": -4 }, "end": { "x": 6, "y": -4 } },
    { "start": { "x": 6, "y": -4 }, "end": { "x": 6, "y": 4 } }
  ]
}
```

For an image JSON guide, replace `segments` with `pixelWidth`, `pixelHeight` and `imageBase64` (raw base64 PNG/JPEG bytes, without a data-URI prefix). Pixel dimensions must match the image. Width and height specify metres; horizontal metres per pixel is derived from width/pixelWidth. The file cannot combine image and vector content. Extents must be 0.001–10000 m; image scale 0.000001–10 m/px. Native guide opacity is 0–1, position ±10000 m and rotation ±3600°. Values must be finite.

This guide-file schema is separate from native Room Studio v2 and canonical ProShield workspace JSON. Native designs with an omitted/empty guide remain compatible, including Unity's default optional-object serialization.

## Repairs made during the audit

- Replaced the incomplete Request 4 parser, renderer and UI implementation; removed the Texture2D/byte-array compilation error.
- Corrected pixel-derived initial dimensions, live settings refresh and undo/redo commits for imports/edits/clearing.
- Added bounded image validation, a shader included explicitly in player builds, and cached decoding so display edits do not repeatedly decode images.
- Preserved guide metadata separately from QA geometry, with float32-aware canonical merging that retains original source precision and unknown annotations.
- Added browser PNG/JPEG selection and isolated browser-native help copying without clipboard permissions or an extra Unity module.
- Fixed the native picker marshalling recursion found during Windows interaction, and constrained the absolute-path field so long paths cannot widen the sidebar.
- Preserved the existing material textures and viewport label clipping; reduced scene lighting to keep the floor material readable.

## Verification and evidence

- `req4-windows-build-verified.log`: editor checks and `ROOM_STUDIO_BUILD_SUCCESS` in Unity 6000.6.0f1.
- `Windows/req4-runtime-smoke-final.log`: selection, shortcuts, palette, components, floor plan, QA popup, canonical import and overall runtime checks pass. The suite uses a separate temporary save directory.
- `req4-webgl-build-verified.log`: all editor checks and `ROOM_STUDIO_WEBGL_BUILD_SUCCESS`; revision `1f082c5347664e7586aebda05afb84c2`.
- `req4-browser-runtime.json`: compiled WebGL selection, shortcut, palette, component and floor-plan checks, ending in `ROOM_STUDIO_WEB_FEATURE_CHECKS_PASSED`.
- `req4-web-runtime.json`: the copied `web/` package passes the same compiled runtime suite.
- `req4-browser-download.log`: the actual downloaded guide retains the embedded 600 × 400 image and 12 × 8 m dimensions; reimport through the browser picker passes.
- `req4-browser-workers.txt`: all 18 real browser-worker checks pass, including guide QA/geometry invariance, precision preservation, clearing and malformed metadata.
- `req4-numerical-benchmarks.log`, `req4-adapter-checks.log`, `req4-workspace-checks.log`, `req4-floor-plan-bridge.log`: 17 numerical benchmarks and adapter/workspace/guide regressions pass.
- `req4-web-package-verified.log`: all 24 launcher/build/worker files in `web/` match `WebGL/` by SHA-256.

Interactive browser verification used `Req4-fixtures/trace-600x400.png`: file selection, correct orientation, 0.02 m/px → 12 × 8 m, wall tracing and undo, saving `Req4-check`, page reload and saved-design recovery, 3D hiding, and malformed-file rejection without replacing the guide. Browser help copied all 2766 characters and closed through Escape/F1. Windows interaction verified the corrected native file dialog, image import, path fallback and stable sidebar layout with a long absolute path; selectable/copyable help was checked in the prior build with unchanged help code.

The last two corrections are restricted to the Windows/editor compilation branch; the delivered WebGL code path is unchanged. The original Next.js TypeScript check passed. Its build compiled successfully but this session's sandbox blocked spawning a worker during page-data collection (`EPERM`); no Next.js source was changed.

## Repeating checks and builds

Open the authoritative Unity project through Hub. Its **Room Studio** menu provides editor checks, Windows build/test and WebGL build. Direct command-line startup can fail at Unity Package Manager IPC in this environment.

An open editor also consumes `LinacRoomStudio/work/editor-build/request.json`, containing `{"command":"checks"}`, `{"command":"windows"}` or `{"command":"webgl"}`. Results are written beside that request as `<command>.log`. Queue one request at a time and close the test player before rebuilding Windows.

Run `benchmark.mjs`, `checks.mjs`, `workspace-checks.mjs` and `floor-plan-checks.mjs` with Node from `Assets/StreamingAssets/ProShield`. Serve the `unity` folder over HTTP and open `/WebGL-tests/` for worker checks, or `/WebGL/?feature-tests=1` and `/web/?feature-tests=1` for compiled player checks. Use fresh test pages because the feature suite exercises design/history and writes a smoke-test save. Fixtures are in `Req4-fixtures/`.
