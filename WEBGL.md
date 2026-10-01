# WebGL export - 2026-10-01

**Current CT source / energy setup correction:** point-first placement now exposes scanner configuration instead of leaving energy controls undiscoverable when the source list is empty. Point properties and results dialogs offer configuration recovery; explicit 120/140-kVp controls are at the top of CT settings. Source strength/calibration remain required external inputs. See the [CT setup guide](LinacRoomStudio/README.md#ct-point-of-interest-energy-and-materials).

Fresh Windows/WebGL builds and their complete editor gates pass, including 120 CT math and 121 integration assertions. Current WebGL revision: `6f9fdcd00e1d467a94e8e2af8ff84b20`; Wasm SHA-256: `74301de165e1a7f7b2ac1051143232b591f25f2a57abf5b7268ebd904c16a36a`. All 29 browser payloads match `web/`; refreshed Windows/WebGL archives have every file checked against the corresponding build/package by SHA-256. [Current handoff and receipts](PROJECT_HANDOFF.md#october-1-ct-source--energy-setup-correction---current-delivery) supersede older identities below. Interactive acceptance remains `In progress`; no screenshots, player/browser interaction, smoke workflows or public deployment were performed for this correction.

**Earlier 2026-10-01 Request 7 implementation/local delivery checkpoint:** equipment corner bubbles and plus/minus scale uniformly; preset selection arms room-click placement; only oversized door placements fit to 90% of wall length; Shift constrains wall drawing/endpoints to 45-degree directions; Project > Reset clears contents and associated guide/generation/CT state through one confirmed undoable command. Focused editor/numerical checks, all four source compilations and Windows/WebGL builds passed at that checkpoint. Both builds repeated the complete editor gate, including 120 CT math/76 CT data assertions. All 29 `web/` payload hashes matched revision `9c50d416fa804b609797d9af185b650a`; Wasm SHA-256 was `7cf003defccbc7d5111a48a96c727dded56df62c7abc86d0f0760dd923d5c26c`. Recorded static HTTP/MIME requests served that revision at http://127.0.0.1:8097/. Evidence and Windows identity are in [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md). Actual control/touch/history/cancellation/browser persistence acceptance remained `In progress`. No screenshots, smoke workflows, browser interaction or public deployment were performed.

**2026-10-01 earlier recorded Req5/Req6 build:** supported wall generation/joins/regeneration and CT points/scenarios/immutable results are implemented. Compact Scene/Build/Properties controls, active-pair full-3D cm labels, guarded double inputs and acknowledged browser saves/error feedback are included. CT result selection enforces 16 MiB before reading. Final Windows/WebGL builds and editor gates pass, including 120 CT math/76 data assertions. Existing final compiled runtime records also pass precision, authoring, physical connections, generation and CT checks; the browser-worker record reports all checks passed. Earlier unit/reference evidence includes eight bridge units, raster-quality fixtures, seven viewport rectangle fixtures, 17 LINAC benchmarks and seven reference suites.

Latest recorded WebGL revision: `82c62f8040ea4defbe67f58a17ebfa09`; Wasm SHA-256: `051f7c2ef958bf0a05be36d7aaa6b3fa76a91bbe84ef30829aea1c7af865aefc`. Windows assembly SHA-256: `c0d71301bc8541637fbcc7a1a17f72105960c0271e1a1cfddfa9cfe752f13a79`. The [acceptance package receipt](LinacRoomStudio/Logs/req5-req6-acceptance-package-2026-10-01.json) records SHA-256 verification of all 29 payloads copied into `web/`. The [saved static request](LinacRoomStudio/work/req56-acceptance/final-static-hosting.json) records HTTP 200 and `application/wasm` at http://127.0.0.1:8097/. No public deployment is recorded.

Evidence: [Windows runtime](LinacRoomStudio/Logs/req5-req6-acceptance-windows-runtime-final-2026-10-01.log), [Windows CT runtime](LinacRoomStudio/Logs/req5-req6-acceptance-windows-ct-final-2026-10-01.log), [compiled WebGL runtime](LinacRoomStudio/work/req56-acceptance/webgl-feature-results.json), [browser workers](LinacRoomStudio/work/req56-acceptance/browser-workers.txt) and [current handoff](PROJECT_HANDOFF.md). These are existing records from the earlier scoped acceptance pass, not newly executed checks.

**Remaining acceptance:** complete final-build browser reload/IndexedDB persistence and native/results download/reimport; comprehensive end-user control, desktop/mobile visual and actual-touch acceptance; maximum-size responsiveness/cancellation and broader noisy-image quality remain `In progress`. Export artifacts, compiled runtime passes and file hashes do not close every gate. This reconciliation changes documentation only; no new tests, builds, screenshots or application changes are performed.

**Verification restriction:** do not capture/upload screenshots or run player/browser smoke workflows, including the historical flags/URLs below. The earlier scoped Req5/Req6 authorization in [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md) explains the recorded acceptance pass; it is not new authorization for this documentation update. Older commands are retained as history only, not current instructions. Builds and mocked browser APIs do not establish complete browser acceptance.

**Earlier 2026-10-01 Req5-only build:** Request 5 extraction, editable/staged 2D/3D previews, physical multi-arm joins, category-colored union submeshes, explicit batch selection, independent auto-connect settings and guarded regeneration were included. X-split paths retained original detection identity. Editor checks and Windows/WebGL builds passed; 29 copied payloads matched at revision `1fb75dae1b4147ad8b3ed0f2a39e197b`. This checkpoint is superseded by the combined build above; its evidence remains historical.

**Previous 2026-09-29 panel/camera build:** the left palette's Linac HD/Dental OPG role note is removed, its wall-targeting toggle is labeled **Snap to wall**, and both side panels are 50 px wider. In 3D, holding the arrow keys smoothly moves the camera center forward/backward and sideways; in 2D, they still nudge selected objects. **V** remains the view toggle and **Ctrl+V** remains paste. Windows and WebGL builds passed Unity editor checks, and Windows runtime smoke passed the smooth camera walk, shortcut, text-focus and general runtime checks. All 28 `web/` payload files matched that WebGL output by SHA-256. Its revision was `cd691fa47e634a1bb4f9c24e06c0ef4a`; hashes and logs are in [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md). Browser interaction was not repeated for that build; no public deployment was performed.

**Previous 2026-09-29 build:** the WebGL output and `web/` package placed CathLab under **CT**, added **Room items** with the supplied Toilet, Basin, and Chair models, and included **V** to toggle 2D/3D view while **Ctrl+V** remained paste. They also showed **Linac HD**, **Dental OPG**, and short numbered object labels. Numeric inputs were limited to 12 characters, name/search fields to 24, and file paths wrapped in a bounded text area. Unity editor checks passed during Windows and WebGL builds; the Windows runtime smoke passed V/Ctrl+V checks, including paste in polygon mode, and all 12 palette entries. Its WebGL revision was `83dc98fef484405e9b9551dc3d65e621`. See [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md) for evidence and Request 5 status.

That earlier September 29 Windows runtime smoke passed the V view toggle, Ctrl+V paste, text-field focus guard, palette placement and native persistence for all 12 entries, plus QA and source-import checks. The September 28 WebGL feature and browser-worker checks remain evidence for a prior build; browser interaction was not repeated for either September 29 build.

Request 5 is implemented for supported straight-stroke plans, and final Windows/compiled WebGL generation and precision runtime checks are recorded as passing. The current packages include fractional editing, calibration/rules, cooperative masks and centerlines, physical joins and native batch regeneration. Full browser persistence/file exchange, comprehensive control/visual/touch and performance acceptance remains in progress. See [PLAN_AUTHORING.md](PLAN_AUTHORING.md), [PRECISION_EDITING.md](PRECISION_EDITING.md), the current [verification record](PROJECT_HANDOFF.md#request-5-automatic-color-coded-walls-connected-edges-and-fractional-editing) and the completed Request 4 [FLOOR_PLANS.md](FLOOR_PLANS.md).

## Implementation completed

- Request 5 uses the existing Floor plan tools for Generate connected walls, batch selection, independent Auto-connect/tolerance/cross-category controls and atomic preview/Apply/regeneration. Physical joined meshes preserve category colors and shared topology; QA/export explicitly reject joins or unsupported clipped geometry instead of approximating it.

- Floor-plan guides render in 2D below objects and remain outside shielding/QA. The browser picker accepts PNG/JPEG and guide JSON; browser saves retain embedded content and display settings. Export guide JSON downloads a reusable guide. F1 offers browser-native selectable/copyable text; R/Shift+R rotate in opposite directions with editing guards.

- Components library with editable shape parameters, custom footprint drawing, independent placed instances, named presets, library backups and JSON download/import. Native version-2 designs retain full custom geometry. Reference QA and canonical ProShield export return an explicit error for designs containing unsupported custom shapes; straight-wall presets remain supported.

- Ctrl+Z/Ctrl+Y undo/redo, Delete, Ctrl+L protection and F1 help are implemented. Text/numeric focus suppresses design commands; F1/Escape remain available. Native version-2 saves retain optional protection metadata. The visible Protect objects/Unlock objects button supports browsers that reserve Ctrl+L.
- Equipment appears in the requested Linac, CT and MRI groups with all original model IDs and calculation roles preserved.
- Multi-selection, box selection, shared dragging/rotation, persistent grouping/ungrouping, selection opacity and sliders paired with all numeric fields are implemented. See `web/README.md` for controls and deployment settings.
- Calculate reference QA stays in a fixed area below the properties panel.
- Results open in a scrollable popup with Recalculate, Export QA JSON, and Close.
- Closing the popup leaves the calculation button available. Design changes mark previous results stale.
- WebGL runs the existing reference engine in a browser module worker; it does not launch Node or require a calculation server.
- Browser file selection supports native saved designs and canonical ProShield JSON. Exports download to the device. Local saved designs also use the browser's persistent filesystem.
- **Room Studio > Build WebGL app** builds into `unity/WebGL` using a responsive page template.

## Historical checks completed

- Desktop/editor and WebGL C# compilation passed without warnings.
- Original 17 numerical benchmarks passed.
- Existing adapter and workspace round-trip regression checks passed.
- Eighteen real browser-worker checks passed, including guide QA/geometry invariance, canonical metadata precision, clearing and malformed-guide rejection, plus the previous checks: golden QA parity, repeated calculation, changed workload, exact source import/export, imported QA, protection/group/opacity export invariance, protection QA invariance, all seven visual palette models retaining source roles/export, explicit custom-shape QA/export rejection, invalid input, and recovery after error.

## Export status

2026-10-01 Req5/Req6 status reconciliation: the final acceptance build/runtime/worker records and 29-file receipt linked above supersede the earlier runtime-not-run statements. Latest recorded revision is `82c62f8040ea4defbe67f58a17ebfa09`. Only documentation is changed in this reconciliation; complete platform acceptance remains open.

2026-10-01 earlier Request 5 implementation build: `LinacRoomStudio/work/req5/windows-build-final-2026-10-01.log` and `webgl-build-final-2026-10-01.log` contain build-success markers and passing editor checks. `engine-regressions-2026-10-01.log` records 17 benchmarks and seven Node suites, including the final split-provenance bridge check. `package-hashes-2026-10-01.json` records all 29 payload hashes at revision `1fb75dae1b4147ad8b3ed0f2a39e197b`. That checkpoint did not claim runtime, screenshot or browser checks; later evidence is recorded above.

2026-09-26 Request 5 calibration/rules increment: `LinacRoomStudio/work/req5/authoring-windows-build.log`, `authoring-windows-smoke.log` and `authoring-webgl-build.log` record fresh builds and successful runtime suites. `authoring-web-runtime.json` captures the final packaged IL2CPP feature checks; `authoring-browser-workers.txt` records 22 checks against the packaged workers. `authoring-package-hashes.json` records equality of all 27 copied files. Build revision: `adde2367b21741088fa4b01b6d0e2b2f`. This supersedes the earlier licence/startup blocker. Full Request 5 acceptance remains open. No public deployment was performed.

2026-09-25 Request 4 release: `req4-windows-build-verified.log`, `req4-webgl-build-verified.log` and `Windows/req4-runtime-smoke-final.log` record successful builds and runtime checks. `req4-browser-runtime.json` and `req4-web-runtime.json` verify both compiled browser packages. All 18 worker checks pass. `req4-web-package-verified.log` records SHA-256 equality for all 24 package files. Interactive image picking, scale/tracing, persistent reload, JSON download/reimport and help copying pass. Build revision: `1f082c5347664e7586aebda05afb84c2`. The final Windows-only picker/path fixes do not change the WebGL compilation branch.

2026-09-21 components release: final Windows and WebGL success markers, component editor checks and both compiled runtime suites pass. Browser interaction verifies custom draw/save/reuse/edit/delete, persistent library after reload, actual JSON download and file-picker reimport. Preview rendering is verified at native and browser UI scales. `component-web-package-verified.log` records equality of all 22 copied files; `component-web-runtime.log` records runtime checks from the final `web/` copy. Build revision: `0863a2b722724ec393278c5014602b10`. Use current folders, not the older ZIP archives. No public deployment was performed.

2026-09-21 feature release: Windows and WebGL build-success markers and full feature runtime checks pass. `feature-web-package-verified.log` records SHA-256 equality for all 22 files copied into `web/`. The final package passes compiled selection/shortcut/palette smoke checks and browser QA. Interactive browser Ctrl+L, the unlock button, protection, text focus, Delete/undo/redo and F1 help were verified. Evidence and the browser-specific Ctrl+L limitation are documented in `FEATURE_VERIFICATION.md`. Use `web/` as the current deployment folder; existing ZIP files are historical snapshots. No public deployment was performed.

2026-09-19 selection release: rebuilt Windows and WebGL, then copied and SHA-256 verified all 22 build/worker/launcher files in `web/`. `Windows/selection-runtime-smoke.log` passes selection (including drag-release), QA popup, import and runtime checks. `web-selection-build-verified.log` records selection checks, build success and fresh launcher revision. Final packaged-browser checks pass multi-select dragging with selection retained, grouping/reselection, shared opacity, unlocking/independent selection and QA popup calculation. All 17 numerical benchmarks and adapter/workspace regressions pass; group/opacity metadata leaves QA and canonical exports unchanged. Use `web/` for the current Vercel deployment; older ZIP archives are not this release.

Incremental Unity builds can cache preprocessed template timestamps. `BuildStudio.RefreshWebGLRevision` now stamps fresh matching loader/data/framework/Wasm revisions after every successful build. Its menu action was compiled and run against the final output, then the launcher was recopied into `web/`.

Earlier verification history:

The final WebGL rebuild completed with `ROOM_STUDIO_WEBGL_BUILD_SUCCESS` on 2026-09-14. Windows was also rebuilt and passed `ROOM_STUDIO_QA_POPUP_SMOKE_PASSED`, `ROOM_STUDIO_IMPORT_SMOKE_PASSED` and `ROOM_STUDIO_RUNTIME_SMOKE_PASSED`. Unity Hub successfully opened the project; the earlier startup blocker is resolved for this workflow.

Linked walls/ceiling passed nine geometry/persistence checks and native build checks. Manual WebGL tests confirmed room-width scaling, ceiling height driving wall tops, a wall-height edit driving the shared ceiling, and ceiling alignment in 3D. Runtime sphere primitives now explicitly preserve SphereCollider against WebGL stripping.

Outputs: `WebGL/` and `Windows/`, with distribution archives `WebGL.zip` and `Windows.zip`. The desktop smoke log is `Windows/runtime-smoke.log`; the final WebGL build log is `webgl-build-verified.log`.

Final browser startup and QA popup passed. An older cached launcher initially mixed build assets; the delivered launcher now versions data/framework/Wasm URLs, and the source template generates a new identifier at build time. The local server requests cache revalidation. Configure static hosting to revalidate `index.html` when deploying updates.

## Build and run once Unity Editor is open

1. Open `LinacRoomStudio` in Unity 6000.6.0f1.
2. Choose **Room Studio > Build WebGL app** and wait for `ROOM_STUDIO_WEBGL_BUILD_SUCCESS` in the Console.
3. From this `unity` folder, run `python serve-webgl.py` and open http://127.0.0.1:8080/.
4. Calculate QA, close the popup, change workload, and calculate again. Confirm that the popup reopens with updated results and that the fixed button remains visible.

The preview server serves `.mjs` as JavaScript and `.wasm` as WebAssembly. Serve the output over HTTP/HTTPS; opening `index.html` directly as a file will not load Unity and module workers correctly. Static hosting must use these MIME types. The build disables Unity compression to avoid special Content-Encoding requirements.

Historical compiled feature check URLs were `/WebGL/?feature-tests=1` and `/web/?feature-tests=1`. Do not open them or repeat that prohibited smoke workflow.

Historical browser-worker execution used `/WebGL-tests/`. It was not repeated for the current build. Permitted reference regressions run with bundled Node; isolated bridge unit tests run `node.exe --test LinacRoomStudio/Tools/BrowserBridgeChecks.mjs` without loading a player or browser.

The existing pure C# migration and detailed source/beam/maze controls remain separate roadmap work.
