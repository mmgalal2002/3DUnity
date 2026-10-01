# Project handoff for future chats

Last updated: 2026-10-01. Start here, then read the repository README and `unity/WEBGL.md`.

## October 1 CT source / energy setup correction - current delivery

The reported ROI/patient result was `MissingRequiredInput: No CT source model configured`. Before this correction, point placement could leave the source list empty while energy controls only appeared for an existing source. Point properties did not expose scanner setup, and the results dialog had no configuration recovery action. Earlier compilation/numerical completion evidence did not establish this first-run workflow.

The supported correction is implemented in `StudioCt`, `StudioApp`, `CtShieldingData`, `CtShieldingCalculation` and `CtShieldingChecks`. Point placement opens CT settings, including compact Properties. Point properties expose source setup before position fields; a missing scanner has an explicit placement action. Scanner configuration creates one owned, unreviewed scatter marker, links existing editable ROI/patient points and reuses an existing source without duplicating workload. Protected points are not mutated. The selected source's explicit 120/140-kVp controls are at the top of CT settings. Results offer source setup outside the scroll area and point-specific input editing, without changing saved snapshots. Empty-source calculation controls route to setup instead of saving a meaningless scenario.

Absolute air kerma still requires supplied and reviewed scanner/protocol source strength, normalization, component coverage and calibration. kVp chooses attenuation fits, not source strength. No energy, kerma, workload, occupancy or design-goal default was invented. Native version-2/CT version-1 formats and the separate LINAC calculation remain unchanged. The [CT setup guide](LinacRoomStudio/README.md#ct-point-of-interest-energy-and-materials) documents both normalization modes and recovery actions.

Permitted verification passes: 120 CT math assertions and 121 integration assertions, including ROI-first source setup, supplied reference inputs reaching ROI/patient kerma, exact lead/concrete fits at 120/140 kVp, protected-point nonmutation, native persistence, duplicate prevention and atomic source/point/object capacity failures. Fresh Windows and WebGL builds both repeat the complete editor regression gate.

Current Windows assembly SHA-256: `5c230dd41f754390d3785d70d132a0d1ffd2f9b5d4ebf3f6427d5c181f961179`. Current WebGL revision: `6f9fdcd00e1d467a94e8e2af8ff84b20`; Wasm SHA-256: `74301de165e1a7f7b2ac1051143232b591f25f2a57abf5b7268ebd904c16a36a`. All 29 browser launcher/build/worker payloads match `web/` by SHA-256. `Windows.zip` (382 files) and `WebGL.zip` (32 files, including launcher instructions) were refreshed; every archived file was compared by SHA-256 with its build/package source.

Evidence: [focused CT editor checks](LinacRoomStudio/Logs/ct-source-setup-editor-2026-10-01.log), [Windows build](LinacRoomStudio/Logs/ct-source-setup-windows-2026-10-01.log), [WebGL build](LinacRoomStudio/Logs/ct-source-setup-webgl-2026-10-01.log), [29-payload receipt](LinacRoomStudio/Logs/ct-source-setup-package-2026-10-01.json), [Windows archive receipt](LinacRoomStudio/Logs/ct-source-setup-windows-archive-2026-10-01.json) and [WebGL archive receipt](LinacRoomStudio/Logs/ct-source-setup-webgl-archive-2026-10-01.json).

Interactive acceptance remains `In progress`: no player/browser interaction, screenshots or smoke workflows were run for this correction. Prior runtime records do not accept these changed routes. Full control/touch/history/browser persistence acceptance and qualified scientific/facility review remain open; neither Request 6 nor Request 7 is declared fully accepted. The build identities below describe earlier checkpoints, not this current delivery.

## October 1 Request 7 - earlier implementation and delivery checkpoint

R7.1-R7.6 supported implementation and local delivery are complete: uniform equipment corner scaling, default component-select/room-click placement, oversized-door fitting, Shift-held 45-degree wall directions, proportional plus/minus scaling and confirmed atomic room reset. All four source compilation paths, focused numerical/editor checks and fresh Windows/WebGL builds pass. Both builds repeat the complete editor gate, including 120 CT math/76 CT data assertions and existing authoring/geometry/native compatibility checks.

Final Windows assembly SHA-256: `8abe4eb9eb06195370336863fe6788f5a413c6b27d6fc207c95fa740c2212191`. Final WebGL revision: `9c50d416fa804b609797d9af185b650a`; Wasm SHA-256: `7cf003defccbc7d5111a48a96c727dded56df62c7abc86d0f0760dd923d5c26c`. All 29 launcher/build/worker payloads match `web/` by SHA-256; the current package manifest records this Request 7 output. Static HTTP HTML/Wasm/data/module requests pass at http://127.0.0.1:8097/, with the final launcher revision present.

Evidence: [Windows build](LinacRoomStudio/Logs/req7-windows-final-2026-10-01.log), [WebGL build](LinacRoomStudio/Logs/req7-webgl-final-2026-10-01.log), [four-path compilation](LinacRoomStudio/Logs/req7-compilation-final-2026-10-01.log), [build identity](LinacRoomStudio/Logs/req7-build-identity-2026-10-01.json), [package hashes](LinacRoomStudio/Logs/req7-package-hashes-2026-10-01.json) and [static hosting](LinacRoomStudio/Logs/req7-static-hosting-2026-10-01.json). The [Request 7 tracker](#request-7-proportional-scaling-direct-placement-wall-editing-and-room-reset) records each implementation/check step and remaining acceptance.

Overall Request 7 remains `In progress` for actual pointer/keyboard/touch, history/cancellation and browser persistence/file acceptance. No screenshots, player/browser smoke workflows, browser interaction or public deployment were performed. Earlier Req5/Req6 scoped authorization/evidence below does not authorize Request 7 live checks or verify its changed controls. No clinical/facility approval is implied.

## Verification authorization - October 1 Requests 5 and 6 completion

For this completion pass, the user explicitly answered: **"Authorize remaining live checks, including screenshots if needed"**. This authorizes the remaining Windows/WebGL interaction, smoke, persistence/file-exchange and screenshot checks for Requests 5 and 6. It supersedes the earlier restriction below for this scoped completion pass only; it does not authorize work on Request 7 or change preferences for other projects. Mark completion only from the resulting evidence, not from this authorization alone.

This records the earlier scoped authorization and explains the existing acceptance evidence. The current Req5/Req6 follow-up is documentation-only; no new live checks, builds or screenshots are being performed.

### Earlier verification restriction - historical context

Do not take/capture or upload/share screenshots. Do not run smoke tests in this project or any other project. This is a global user preference recorded in persistent memory and supersedes earlier screenshot/smoke-test commands and acceptance instructions in this handoff. Do not repeat or rename those workflows to bypass the restriction. Existing evidence is historical; it is not permission to run them again.

Use permitted compilation/build, static, unit/numerical and file-integrity checks only as needed. Checked progress items mean the described implementation is done; unchecked unfinished work is labeled `In progress`. Full acceptance, deployment and clinical review must not be implied where they have not been established.

## October 1 Requests 5 and 6 - current status reconciliation

Supported implementation and local delivery are complete. Existing final Windows and compiled WebGL runtime records now show passing Req5/Req6 checks. Overall Request 5 and Request 6 remain `In progress` because complete final-build platform acceptance is not conclusively recorded. This entry supersedes older statements that current runtime/browser-worker checks have not run; it does not turn incomplete acceptance into a passing result.

- Req5 implementation complete: fractional movement/resizing and independent snapping; calibrated PNG/JPEG color rules and extraction; physical wall joins; staged 2D/3D previews and Apply/Cancel; history, native provenance and manual/deleted-override-preserving regeneration. Final Windows/WebGL runtime checks record precision, focus/free-drag/cancellation, authoring invariance, existing-wall mesh/collider connections, generation, native roundtrip and duplicate-free regeneration passes.
- Req6 R6.0-R6.9 implementation complete: Dot and typed CT points, source ownership/anchors, shielding states, supported CT material/fits and source/path providers, declared workload/occupancy/goal scenarios, immutable history/comparisons, native/result-file helpers and full 3D centimetre measurements. Final Windows/WebGL CT runtime checks record annotation/collider, source/ROI, guard/history, on/off and best/nominal/worst snapshot, native/results-file and atomic-deletion passes. The final editor gates retain 120 CT math and 76 CT data assertions.
- Browser-worker evidence records `ALL BROWSER WORKER CHECKS PASSED`, including Req5 provenance/unsupported joins and CT annotations, metadata, shielding-state and material boundaries. This is distinct from interactive browser reload/download acceptance.
- The saved Req5 export summary records eight walls, eight paths, six junctions, 100/150 mm category thicknesses, embedded source data and confirmed calibration. CT native/result artifacts also exist, but artifact presence and internal roundtrips alone do not prove final-build browser reload or successful picker reimport.

Latest recorded Windows assembly SHA-256: `c0d71301bc8541637fbcc7a1a17f72105960c0271e1a1cfddfa9cfe752f13a79`. WebGL revision: `82c62f8040ea4defbe67f58a17ebfa09`; Wasm SHA-256: `051f7c2ef958bf0a05be36d7aaa6b3fa76a91bbe84ef30829aea1c7af865aefc`. The acceptance package record verifies 29 launcher/build/worker payloads from `WebGL/` to `web/` at 2026-10-01T13:31:31Z. The recorded static Wasm request returns HTTP 200 with `application/wasm`. No public deployment is recorded. Older package manifests and hashes below describe their dated checkpoints, not this latest identity.

Evidence: [final Windows build](LinacRoomStudio/Logs/req5-req6-acceptance-windows-final-2026-10-01.log), [final WebGL build](LinacRoomStudio/Logs/req5-req6-acceptance-webgl-final-2026-10-01.log), [Windows Req5 runtime](LinacRoomStudio/Logs/req5-req6-acceptance-windows-runtime-final-2026-10-01.log), [Windows CT runtime](LinacRoomStudio/Logs/req5-req6-acceptance-windows-ct-final-2026-10-01.log), [compiled WebGL runtime](LinacRoomStudio/work/req56-acceptance/webgl-feature-results.json), [browser workers](LinacRoomStudio/work/req56-acceptance/browser-workers.txt), [recorded build hashes](LinacRoomStudio/work/req56-acceptance/final-build-hashes.json), [acceptance package hashes](LinacRoomStudio/Logs/req5-req6-acceptance-package-2026-10-01.json), [static hosting](LinacRoomStudio/work/req56-acceptance/final-static-hosting.json) and [Req5 export summary](LinacRoomStudio/work/req56-acceptance/req5-downloaded-summary.json).

**In progress:** complete final-build browser reload/IndexedDB persistence and native/results download/reimport; comprehensive end-user focus/history/cancellation/Apply acceptance; desktop/mobile visual and actual-touch acceptance; maximum-size responsiveness/cancellation and broader noisy-image quality beyond the documented fixtures. R6.10/R6.11 remain open. Existing runtime passes close the recorded checks, not every interaction scenario. Qualified scanner/source/material/facility review remains external. This reconciliation changes documentation only and does not repeat tests, capture screenshots or modify application code.

## October 1 Requests 5 and 6 - implementation and delivery follow-up (prior output)

At this earlier checkpoint, supported source implementation and local delivery were complete, while actual platform acceptance remained `In progress` and live checks were excluded. The current reconciliation above records the later scoped acceptance evidence. Request 7 was not implemented or changed.

- Req5: added deterministic anti-aliased/JPEG decode fixtures, transparent/noise checks and documented rule settings. The clean calibrated PNG pipeline still produces eight walls, six physical joins, stable regeneration and a native roundtrip. JPEG qualities 90/75 stay within two original pixels with RGB tolerance 0.1 and a 24-pixel minimum component area; anti-aliasing stays within 1.5 pixels at tolerance 0.2. These fixtures are not arbitrary noisy-image or maximum-size performance acceptance.
- Req5/Req6 UI: screens below 1000 px wide or 600 px high use Scene/Build/Properties surfaces, readable compact controls and scrollable calculation controls. Desktop panel dimensions are preserved. Captured panel/modal clicks remain blocked even after a panel closes. Pure rectangle tests cover seven phone/tablet/desktop sizes; no screenshot or actual touch/layout acceptance is claimed.
- Req6: double-precision CT text entry now rejects incomplete decimals/exponents. Only the active ROI pair displays a label; its measurement is highlighted and other lines remain selectable. Plan-view labels explicitly identify full 3D distance when elevations differ. Exact 100 cm and 3-4-5 spatial checks pass.
- Persistence: native Save waits for `FS.syncfs` acknowledgment before reporting browser persistence success. Concurrent writes require their own flush; quota/filesystem failures are surfaced without claiming persistence. CT result selection rejects files over 16 MiB before reading. Eight isolated Node VM bridge tests pass; these mock filesystem callbacks and do not establish actual browser reload/IndexedDB/download acceptance.
- Checks: all four source compilation paths passed without diagnostics; final Windows/WebGL builds repeat the complete editor gate, including 120 CT math assertions, 76 CT data assertions, Dot assets, precision, authoring, generation, connections, components and native compatibility. All 17 LINAC benchmarks and seven reference suites pass; regenerated fixture bytes were restored.

Final Windows assembly SHA-256: `b430282da8ad286813f9a3e0ae6d15b9edc8d407d86b928e8361b9c68d3a2699`. Final WebGL revision: `c3e54ff8a61949afbe36f25937508157`; Wasm SHA-256: `d749365a55ca48a64f1382cffdfaed3becd14d4f6275faaff76771cd5d8f69fb`. All 29 launcher/build/worker payload files in `web/` match `WebGL/` by SHA-256. Deployment configuration and package README are preserved. Local preview: http://127.0.0.1:8097/; static HTML/Wasm/data/module MIME checks pass. No public deployment was performed.

Evidence: [Windows build](LinacRoomStudio/Logs/req5-req6-windows-final-2026-10-01.log), [WebGL build](LinacRoomStudio/Logs/req5-req6-webgl-final-2026-10-01.log), [CT input/distance checks](LinacRoomStudio/Logs/req6-input-data-2026-10-01.log), [raster checks](LinacRoomStudio/Logs/req5-raster-quality-2026-10-01.log), [layout checks](LinacRoomStudio/Logs/req5-req6-layout-2026-10-01.log), [bridge units](LinacRoomStudio/Logs/req5-req6-browser-bridge-units-2026-10-01.log), [reference regressions](LinacRoomStudio/Logs/req5-req6-engine-regressions-2026-10-01.log), [package hashes](LinacRoomStudio/Logs/req5-req6-package-hashes-2026-10-01.json) and [static hosting](LinacRoomStudio/Logs/req5-req6-static-hosting-2026-10-01.json). `work/req5/current-package-hashes.json` records this earlier combined checkpoint, not the latest acceptance identity above. [Refresh-WebPackage.ps1](LinacRoomStudio/Tools/Refresh-WebPackage.ps1) refreshes generated payloads and supports `-VerifyOnly`; [BrowserBridgeChecks.mjs](LinacRoomStudio/Tools/BrowserBridgeChecks.mjs) runs with bundled Node `--test` without a player/browser.

**In progress:** actual native/results browser download/reimport and persistent reload, full interactive focus/history/cancellation/Apply acceptance, actual visual/touch acceptance and maximum-size responsiveness. Compilation, unit checks, mocked browser APIs and file hashes do not close these gates. No screenshots, player/browser smoke tests, browser interaction tests or browser-worker runtime checks were run for this follow-up. Clinical source/material/facility review remains external.

## October 1 Request 6 CT initial implementation - prior output

CT-scanner-only scope is confirmed. Request 6 source now includes the supplied Dot resource, typed scatter/ROI/patient points, reviewed source/anchor inputs, independent CT wall shielding states, all six reference authoring materials, separate CT/primary coefficient datasets, best/nominal/worst workload scenarios, finite homogeneous barrier paths, immutable result history and comparisons, portable CT result files, and full 3D centimetre measurement overlays.

Completed: 120 scientific math assertions (including all 20 CT forward benchmarks), 59 CT integration assertions, Dot prefab checks, the full pre-existing Unity editor gate, 17 LINAC benchmarks, all seven reference regression suites, and corrected Windows/WebGL builds. Final WebGL revision: `b17645fec90c4312a2919ae5047b698f`. Windows assembly SHA-256: `a38e36e5fb1617a5eca464d8b8403097147686d333fa3b2eeae5918300842d76`; WebGL Wasm SHA-256: `fe55a524d62ea2452d0ea5465fe6b0c01e045730790b82e9a2ea90ed72d312f5`. Build evidence: `LinacRoomStudio/Logs/req6-windows-build-final-2026-10-01.log` and `req6-webgl-build-final-2026-10-01.log`.

At that checkpoint, interactive browser persistence/file acceptance, layout acceptance and the `web/` refresh were unfinished. The follow-up above completes local package delivery and adds permitted unit coverage; actual interactive acceptance remains `In progress`. No further screenshots or smoke tests will be used to close these items. Request 7's requirements are unchanged.

Historical verification, completed before the new restriction: compiled Windows and corrected WebGL CT checks passed, including marker resources, scene/history operations and native/result-file round trips; the corrected browser reported no console errors. All 33 source browser-worker checks passed. The initial WebGL revision `98cfe3f8bc794670ad13fdb9370aaba3` failed a precision check; promoting scientific `/2000` and wall-centre arithmetic to double resolved it in the corrected build. These earlier checks are retained as evidence only and must not be rerun.

Scientific boundaries: only 120/140-kVp CT secondary lead/concrete fits are available. Required scanner source data, reviewed path/anchor/material applicability, occupancy conventions and goals are user-supplied; no clinical constants are invented. Existing `Glass` is legacy lead glass and is preserved; new `PlateGlass` is distinct and cannot inherit its fit. Unsupported composites, doors, joined/custom paths, CT canonical export and LINAC-QA substitution reject explicitly. Software arithmetic checks are not clinical or regulatory approval.

## October 1 Request 5 initial implementation and builds - prior output

The supported raster-to-wall workflow is implemented: calibrated color-mask centerlines, editable path previews, physical wall unions and shared junctions, explicit generation-batch selection, independent **Auto-connect walls** settings, and atomic Apply/regeneration. Exact three/four-arm endpoint meetings now produce T/X junctions instead of false ambiguity. Joined meshes retain category colors in disjoint material submeshes with one collider; unjoined generated walls retain their saved display colors. Regeneration preserves manual/deleted overrides and reports conflicts for changed calibration, transforms, dimensions, density, thickness or appearance. X-split paths retain their original detection IDs/endpoints so unchanged regeneration preserves child IDs and topology. Pending jobs cannot relabel a retained preview, and Apply uses the staged geometry that was previewed.

Unity editor checks, all four source compilation paths, all 17 numerical benchmarks and seven reference-engine regression suites passed. Windows and WebGL builds passed with their editor checks. All **29** copied launcher/build/worker files match `unity/web` by SHA-256. WebGL revision: `1fb75dae1b4147ad8b3ed0f2a39e197b`. Windows `Assembly-CSharp.dll` SHA-256: `555e154c8f3f7cb7090f71e7ded7237927b5f7ef04aaa697393f1366e43cfb82`; WebGL `Build/WebGL.wasm` SHA-256: `4ae1934f3575ae7605ecc858e5350f8a0e9a73203d59ab0192972b71d0a8ddc9`.

Evidence is under `LinacRoomStudio/work/req5`: `editor-checks-2026-10-01.log`, `generation-pipeline-final-2026-10-01.log`, `generation-model-settings-2026-10-01.log`, `generation-split-regeneration-2026-10-01.log`, `connection-category-checks-2026-10-01.log`, `engine-regressions-2026-10-01.log`, `windows-build-final-2026-10-01.log`, `webgl-build-final-2026-10-01.log`, and `package-hashes-2026-10-01.json`. Earlier same-day build logs are interim outputs. `current-package-hashes.json` records the final package, not the September 29 package.

**Verification boundary:** no screenshots, Windows/player smoke tests, compiled WebGL feature tests or browser interaction/worker tests were performed for this delivery, as requested. Fresh browser reload/download, focus/history/cancellation interaction, maximum-size responsiveness and noisy/JPEG extraction acceptance remain unverified. Request 5 is **implemented for supported straight-stroke plans, with final runtime acceptance pending**, not acceptance-complete. No public deployment was performed. Joined/clipped geometry remains explicitly unsupported by reference QA and canonical ProShield export; use native project JSON.

## September 29 panel and 3D camera build - prior output

The latest requested UI change removes the note in the left palette that describes Linac HD as the QA source and Dental OPG as visual-only. The left precision control is labeled **Snap to wall**; its endpoint/edge targeting behavior and independence from grid snapping remain the same. The left and right side panels each gain 50 px of width to reduce horizontal clipping and scrolling while keeping a usable center view.

In 3D, holding the arrow keys smoothly moves the camera's look-at center through the scene: up/down move forward/backward relative to the camera's ground-plane heading, and left/right move sideways. In 2D, the same keys retain their existing selected-object nudge behavior. **V** still switches between 2D and 3D; **Ctrl+V** still pastes. Text-entry and modal focus guards continue to apply.

Windows and WebGL builds passed Unity editor checks. The Windows runtime smoke passed smooth 3D arrow movement without editing the design, V view toggle, Ctrl+V paste, text focus and the general runtime workflow. WebGL revision: `cd691fa47e634a1bb4f9c24e06c0ef4a`. All 28 copied WebGL payload files match the `unity/web` package by SHA-256 in `LinacRoomStudio/work/req5/current-package-hashes.json`. Windows `Assembly-CSharp.dll` SHA-256: `73a611d78876de689e7550c78b16296a08d9b210f5029c9a10bffc9fd9e08ecb`; WebGL `Build/WebGL.wasm` SHA-256: `09dfe0ffbcc29e1c3a939339bd702272bb7bae99ee46a45c33c1aff9e65827fb`. Evidence logs: `work/req5/windows-build-panel-camera-2026-09-29.log`, `windows-runtime-panel-camera-2026-09-29.log`, and `webgl-build-panel-camera-2026-09-29.log`. Browser interaction was not repeated for this build. No public deployment was performed.

## September 29 room items and view shortcut build — prior output

CathLab moved from Linac to CT in the equipment palette. A new **Room items** section contains the supplied Toilet, Basin, and Chair GLBs. They are visual-only `Model` items with stable IDs `Toilet`, `Basin`, and `Chair`; existing source roles and IDs are unchanged. The GLBs are retained verbatim, and Unity imported 2, 13, and 4 mesh parts respectively. The generated Basin prefab is centered horizontally for accurate placement while its original GLB and converted ModelData remain unchanged. See [MODEL_ASSETS.md](MODEL_ASSETS.md) for source hashes and dimensions.

Pressing **V** switches between 2D plan and 3D view. **Ctrl+V** continues to paste copied objects. The shortcut is listed in the help and beside the view controls; keyboard shortcuts remain disabled while text fields are active.

Windows and WebGL builds passed Unity editor checks. The Windows runtime smoke passed the V camera toggle, Ctrl+V paste while the polygon tool was active, text-field focus guard, and full palette, QA, and source-import checks. WebGL revision: `83dc98fef484405e9b9551dc3d65e621`. All 28 copied WebGL payload files matched that build's `unity/web` package by SHA-256; `current-package-hashes.json` now records the newer build above. Windows `Assembly-CSharp.dll` SHA-256: `9672b27b403c2e31044d17f136b99369c590b1ea262e24bc9af26c84b4f9c71b`; WebGL `Build/WebGL.wasm` SHA-256: `49dfd80f0593ff5e4741f7951650e9475f036d66a8f8e66dd911faba2859ef62`. Evidence logs: `work/req5/windows-build-view-shortcut-final-2026-09-29.log`, `webgl-build-view-shortcut-final-2026-09-29.log`, and `windows-runtime-view-shortcut-final-2026-09-29.log`. Browser interaction was not repeated for this build. No public deployment was performed.

## September 29 labels and text-field build

The supplied Versa GLB has SHA-256 `dad7df348409818f1ae0c479f0526b3fa828667d01f53c5486afabf100added9`, identical to the existing project source, so no other model assets were changed. The palette now calls it **Linac HD** and calls the existing `PlanmecaViso` model **Dental OPG** while preserving both internal model IDs and the QA-source role. Scene lists and object headers use short type labels with a three-digit sequence (for example, `WALL 001` and `LINAC HD 001`) instead of exposing GUID IDs. Numeric entries accept at most 12 characters; name/search entries at most 24; file paths wrap in a 260-character text area.

The earlier label/input build passed `BuildStudio` editor checks at WebGL revision `47250cc86341439b84790b32c00fd2a0`. Its logs are `work/req5/windows-build-2026-09-29-final.log` and `webgl-build-2026-09-29-final.log`. The panel and 3D camera build above is the current output.

## September 28 build — supplied models and scene editing

At the September 28 checkpoint, `unity/Windows`, `unity/WebGL`, and `unity/web` contained the then-current user-requested changes. Unity Editor checks, Windows runtime smoke, hosted WebGL feature checks, browser-worker checks, and all seven ProShield Node regressions passed. That WebGL revision was `a529c8344c7e414eaef15ae77c6dc563`; the current build and package hashes are recorded above. No public deployment was performed.

Build identity: Windows `LinacRoomStudio.exe` SHA-256 `a4c71b1bfc3e42d02f2f129fe04adef3ebf2343a662884e4b99598e9dfc90e7d`; WebGL `Build/WebGL.wasm` SHA-256 `f943092345ca6710b1468df523e8e6811fc26176b7e0d92bf6c07e00165635eb` (identical in `WebGL` and `web`).

- The visual LINAC uses the supplied `versa hd without patient.glb`. CT, Cyberknife, and MRI use the latest supplied patient-free GLBs; Planmeca Viso appears under CT. Existing model IDs and calculation roles are preserved. The Planmeca file later supplied in Downloads is byte-identical to the one already imported. `BuildStudio.EnsureModels` tracks the converted source hashes and all five model prefabs were imported. [MODEL_ASSETS.md](MODEL_ASSETS.md) records exact hashes, geometry counts, and static-material limits.
- Walls own optional version-2 `DoorOpening` records with wall-local offset, width, height, panel material/thickness/density, and explicit lead lining in mm (default 0). Select one wall in Object, use **Click wall to cut / place door** in 2D, then edit or remove it there. Segmented wall cubes leave a physical aperture, with a panel/frame/lining and plan marker. Native saves and undo/redo retain doors. Reference QA and canonical export explicitly reject designs with doors because the current engine cannot calculate apertures; there is no shielding adequacy claim.
- Object includes **Lock in place (no movement or editing)**, persisted with `Item.locked` and undo/redo. A locked wall also protects its doors. Ctrl+C/Ctrl+V and visible Object buttons copy selected scene objects/groups and paste unlocked copies with fresh object/group/door IDs and repeated 0.5 m offsets. Duplicate LINAC and invalid placement attempts reject before editing history.
- The gantry-angle preview rotates the treatment head and its source and imaging arms around the configured isocentre; the gantry rings, bore, and LINAC root stay fixed. The beam follows the moving `Beam_Window` and points at the isocentre. Saved gantry angle remains the existing reference QA input, while runtime mesh transforms remain outside QA geometry.
- Evidence: `work/req5/unity-integration-check-elevated.log`, `windows-build-latest.log`, `windows-runtime-latest.log`, and `webgl-build-latest.log` under `LinacRoomStudio`; browser feature checks passed on both `/WebGL/?feature-tests=1` and `/web/?feature-tests=1`, with all 22 browser-worker checks passing at `/WebGL-tests/`. In the packaged browser UI, selecting West wall, using the visible Copy and Paste buttons, and checking Lock in place produced a new selected wall at X=-5.5 m with editing controls disabled. `work/req5/compile.ps1` passed Editor/Windows/WebGL/editor-check compilations, and `work/door-release/proshield-node-results.json` records the seven Node suites.

This is a historical build checkpoint. Request 5 extraction, joins, previews and regeneration are now implemented; October 1 evidence and the outstanding runtime acceptance checks are recorded above and in the Request 5 tracker below.

## Authoritative locations

- Repository: `D:\0-ProCare\radiation-shielding-software`.
- Unity project: `unity/LinacRoomStudio`, Unity `6000.6.0f1`.
- Original Next.js reference application: repository root.
- Windows output: `unity/Windows`; WebGL output: `unity/WebGL`.
- Older Codex output folders and ZIPs are snapshots, not the authoritative project. Inspect current Git changes and logs before editing; preserve unrelated material/model/scene changes.

## Implemented features

The current verified package contains ten supplied model resources, 2D/3D room editing, wall doors, shielding, workstations and occupied regions, copy/paste, object locking, undo/redo, native design save/load and legacy migration, canonical ProShield JSON import/export for supported geometry, reference QA with a persistent button and results popup, browser-worker QA, file selection/downloads and persistent browser saves.

Components update (2026-09-21): the **Components** tab manages trapezoidal, rounded and straight-wall presets plus custom polygon footprints. Create/draw, save from selection, place, edit/rename, duplicate, delete, persist and JSON export/import are implemented in Windows and WebGL. Native designs preserve full component geometry. Custom shapes are explicitly rejected by reference QA and canonical ProShield export because the current engine cannot represent them; straight-wall presets remain supported. See [COMPONENTS.md](COMPONENTS.md) for controls, limits and verification.

Geometry is in metres. Shielding thickness is stored/edited in millimetres and converted for rendering. Native designs use schema/version 2. Original source JSON and an editor baseline are retained to preserve precise numbers, unknown fields, components and maze settings through canonical round-trip.

Selection update (2026-09-19): Shift/Ctrl-click, a Multi-select toggle, box selection, shared dragging and rotation, persistent Lock together/Unlock group, and selection-wide opacity are implemented. Every numeric property now has a slider and exact text input. `SelectionEditing.cs` contains group expansion and transform operations; `SelectionChecks.cs` covers geometry and persistence. Native item fields `groupId` and `transparency` retain version-2 compatibility (absent transparency means opaque). Canonical ProShield JSON remains unchanged by display metadata. Scene lists can select zero-opacity objects; selection outlines remain visible. Linked-wall selections require unlinking room dimensions before a rigid vertical move. Desktop runtime checks include the IMGUI mouse-release/commit regression and pass in `Windows/selection-runtime-smoke.log`. Group locking means moving together; it is separate from the implemented object-protection lock in Request 1 below.

Room linkage: `Design.linkWallsToRoom` defaults to true. Width/depth edits scale all wall centres and endpoint vectors, including maze and diagonal walls. Ceiling edits set wall height to ceiling elevation minus wall base. Editing a linked wall height moves the shared ceiling and other wall tops. Equipment and occupied regions retain their chosen world positions. Unlinking restores independent wall editing; this preference is saved. Invalid room resizing is rejected before mutation. One design has one room and one shared ceiling; independently grouped buildings are not implemented.

## Source map (relative to Unity project)

| File                                                  | Purpose                                                                                                         |
| ----------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `Assets/Scripts/StudioApp.cs`                         | Runtime scene, editor UI, QA popup, file operations and runtime smoke tests                                     |
| `Assets/Scripts/StudioShortcuts.cs`                   | Shared keyboard dispatch, persisted protection controls, F1 help and shortcut runtime checks                    |
| `Assets/Scripts/PrecisionEditing.cs` | REQ5 optional precision preferences, validation, grid/edge targeting and anchored wall resizing |
| `Assets/Scripts/StudioPrecision.cs` | Precision UI, queued scene pointer input, nudges, drag/resize gestures and target feedback |
| `Assets/Scripts/PrecisionChecks.cs`, `StudioPrecisionChecks.cs` | REQ5 numerical, compatibility, history and interaction-path checks |
| `Assets/Scripts/PlanAuthoring.cs`, `PlanColorMask.cs` | Calibration, source/world coordinates, configurable rules and cooperative original-resolution masks |
| `Assets/Scripts/PlanPathExtraction.cs`, `PlanPathExtractionChecks.cs` | Bounded thinning/centerlines, opening guards and deterministic image-to-wall pipeline checks |
| `Assets/Scripts/WallGenerationData.cs`, `WallGenerationDiff.cs` | Optional batch/options/provenance, pure staging, manual/deleted overrides and regeneration conflicts |
| `Assets/Scripts/WallConnections.cs`, `WallUnionGeometry.cs` | World-space joins, shared junction propagation, disjoint physical unions and category submeshes |
| `Assets/Scripts/StudioWallGeneration.cs`, `StudioWallConnections.cs` | Batch selection, preview corrections, staged rendering, atomic Apply/connect/detach controls |
| `Assets/Scripts/WallGenerationChecks.cs`, `WallConnectionChecks.cs` | Native compatibility, connection geometry, protection and regeneration unit checks |
| `Assets/Scripts/EquipmentPalette.cs`                  | Ordered Linac/CT/MRI display catalog with unchanged model IDs and calculation roles                             |
| `Assets/Scripts/DoorGeometry.cs`, `StudioDoors.cs`     | Wall-local door validation, true cutouts, panel/lining rendering and Object controls                           |
| `Assets/Scripts/StudioDoorChecks.cs`, `Assets/Scripts/DoorChecks.cs` | Door geometry, persistence, lock and runtime checks                                                |
| `Assets/Scripts/StudioLock.cs`                         | Visible Lock in place checkbox for the persisted protection state                                              |
| `Assets/Scripts/SceneClipboard.cs`, `StudioSceneClipboard.cs`, `SceneClipboardChecks.cs` | Scene copy/paste, Object buttons, domain and runtime checks |
| `Assets/Scripts/StudioBeam.cs` | Beam illustration anchored to the supplied Versa `Beam_Window` |
| `Assets/Scripts/StudioPalette.cs`                     | Grouped palette UI and placement/persistence runtime checks                                                     |
| `Assets/Scripts/StudioComponents.cs`                  | Components tab, shape editor/preview, drawing, placement and JSON file controls                                 |
| `Assets/Scripts/StudioFloorPlan.cs` | Floor-plan controls, cached image/vector rendering and import/export |
| `Assets/Scripts/FloorPlanCodec.cs` | Bounded parsing/decoding, guide validation, resizing and portable JSON |
| `Assets/Scripts/FloorPlanFilePicker.cs` | Windows Unicode file dialog and editor picker |
| `Assets/Scripts/StudioFloorPlanChecks.cs` | Shared Windows/WebGL floor-plan runtime workflow checks |
| `Assets/Resources/FloorPlanGuide.shader` | Included unlit alpha guide material |
| `Assets/StreamingAssets/ProShield/floor-plan.mjs` | Canonical guide validation and source-precision merge |
| `Assets/Editor/BuildRequestRunner.cs` | Repeatable checks/build queue in an already-open editor |
| `Assets/Scripts/ComponentGeometry.cs`                 | Validated trapezoid/arc/polygon footprints, concave triangulation and extruded meshes                           |
| `Assets/Scripts/ComponentLibrary.cs`                  | Independent presets, versioned component JSON and persistent library backups                                    |
| `Assets/Scripts/StudioComponentChecks.cs`             | Shared Windows/WebGL component workflow runtime checks                                                          |
| `Assets/Editor/ComponentChecks.cs`                    | Geometry volume/winding, invalid inputs and library round-trip checks                                           |
| `Assets/Editor/FloorPlanChecks.cs`                    | Legacy compatibility, vector persistence and guide validation checks                                            |
| `Assets/Scripts/ProjectData.cs`                       | DTOs, validation/migration, linked room resizing                                                                |
| `Assets/Scripts/SelectionEditing.cs`                  | Saved groups, selection expansion and rigid multi-object transforms                                             |
| `Assets/Editor/SelectionChecks.cs`                    | Group, movement, rotation, opacity, validation and persistence regression checks                                |
| `Assets/Scripts/ReferenceQa.cs`                       | Desktop Node process or WebGL bridge selection                                                                  |
| `Assets/Scripts/BrowserBridge.cs`                     | WebGL callbacks and asynchronous operations                                                                     |
| `Assets/Plugins/WebGL/RoomStudio.jslib`               | Browser workers, file picker/downloads and filesystem sync                                                      |
| `Assets/StreamingAssets/ProShield/engine.mjs`         | Shared adapter and operation dispatch                                                                           |
| `Assets/StreamingAssets/ProShield/runner.mjs`         | Desktop Node CLI                                                                                                |
| `Assets/StreamingAssets/ProShield/browser-worker.mjs` | Browser worker entry point                                                                                      |
| `Assets/StreamingAssets/ProShield/workspace.mjs`      | Import validation, projection and source-preserving merge                                                       |
| `Assets/StreamingAssets/ProShield/radiation-*.mjs`    | Original reference catalog/formulas from c33e41d                                                                |
| `Assets/Editor/BuildStudio.cs`                        | Model preparation, builds and native validation checks                                                          |
| `Assets/WebGLTemplates/RoomStudio/index.html`         | Responsive WebGL loading page                                                                                   |
| `Assets/link.xml`                                     | Preserves SphereCollider and MeshCollider used by runtime primitives and custom shapes in stripped WebGL builds |

## Build and verification

Open the authoritative project through Unity Hub for interactive work. In this managed session a default-sandbox batch launch failed at Package Manager IPC; the September 28 Editor checks and builds succeeded with local IPC access. Inspect current state rather than assuming a previous license/startup blocker still applies.

- **Room Studio > Run editor checks** validates native persistence, protection, selection, the palette, component geometry/library behavior and floor-plan metadata.
- **Room Studio > Build Windows app** produces the complete Windows folder.
- **Room Studio > Test Windows app** historically invoked `--smoke-test`; it is prohibited under the current user restriction. Do not run it.
- **Room Studio > Build WebGL app** produces the WebGL folder and logs `ROOM_STUDIO_WEBGL_BUILD_SUCCESS`.
- Run `python serve-webgl.py` from `unity`, then open http://127.0.0.1:8080/. The preview server supplies correct `.mjs` and `.wasm` MIME types. Serve through HTTP/HTTPS, not `file://`.
- In `Assets/StreamingAssets/ProShield`, run bundled `node.exe` with `benchmark.mjs`, `checks.mjs`, `workspace-checks.mjs` and `floor-plan-checks.mjs`. These regenerate regression fixture files.
- Historical browser-worker regression execution used `/WebGL-tests/`; it was not repeated for the current delivery.
- Historical compiled player/browser checks used `--smoke-test`, `--ct-smoke-test`, `/WebGL/?feature-tests=1` and `?ct-tests=1`. These workflows are prohibited; do not open/run them or rename equivalent tests. Use the permitted editor/unit checks and isolated bridge units recorded above.
- In the actual app, calculate, close the popup, change workload, and recalculate. Check updated results and the fixed calculation button. Test room resizing in 3D with cutaway disabled and ceiling enabled, then undo and save/load.

## Verification history

2026-10-01 Req5/Req6 documentation reconciliation: existing final Windows and compiled WebGL records pass precision, authoring, physical connections, generation and CT runtime checks; browser-worker records also pass. Latest recorded WebGL revision is `82c62f8040ea4defbe67f58a17ebfa09`, with 29 verified local delivery payloads. This update performs no application changes or new verification. Full final-build browser persistence/file exchange, comprehensive control/visual/touch acceptance and maximum-size responsiveness remain `In progress`; see the current status reconciliation at the top.

2026-10-01 Request 5 implementation delivery: editor/unit checks, four source compilation paths, Windows/WebGL builds, 17 numerical benchmarks and seven Node suites pass. The clean two-color PNG unit pipeline generates eight walls and six physical joins with no manual tracing or regeneration duplicates; original-pixel endpoint tolerance is 1 px. All 29 local deployment payload files match the build. Screenshots and smoke tests were expressly excluded; current player/browser acceptance is not claimed. See the October 1 build record above and the Request 5 verification record below.

2026-09-25 Request 4 release: floor-plan tracing and shortcut refinements are complete. Rebuilt Windows/WebGL and final web-package checks pass; all 17 numerical benchmarks and 18 browser-worker checks pass. Native chooser/path import, browser save/reload/download/reimport and F1 copying were verified interactively. Evidence and the supported guide format are in [FLOOR_PLANS.md](FLOOR_PLANS.md). Request 5 remains pending.

2026-09-21 components release: Request 3 is complete. Windows and WebGL builds pass, including component geometry/library checks and compiled runtime create/save/reuse/edit/delete/export/import checks. Browser interaction confirms custom drawing, naming, height edits, persistence after reload, independent placed objects, JSON download and reimport through the picker. Corrected previews are visually verified in Windows and WebGL. All 17 numerical benchmarks and 12 real browser-worker checks pass. The final `web/` copy passes runtime checks and all 22 launcher/build/worker files match `WebGL/` by SHA-256. Evidence and the custom-shape QA/export limitation are recorded in [COMPONENTS.md](COMPONENTS.md). No public deployment was performed.

2026-09-21 Request 4 initial audit: the source implementation now includes the Floor plan tab, embedded image/vector guide data, 2D-only guide rendering, F1/R/Shift+R dispatch and a Windows floor-plan smoke marker in [Windows/runtime-smoke.log](Windows/runtime-smoke.log). The current code diagnostics are clean. Req 4 is not acceptance-complete: the existing WebGL/browser logs predate the floor-plan changes, canonical ProShield export does not yet carry guide metadata, the browser picker advertises JSON but not images, help-copy behavior is not verified and the modal is drawn while GUI controls are disabled, and shortcut/file-validation guards still need focused checks. Do not use the older WebGL feature logs as Req 4 evidence.

2026-09-21 completion of the September 20 feature builds: Requests 1 and 2 are complete. Editor checks, Windows build/runtime smoke tests, 17 numerical benchmarks and adapter/workspace regressions pass. The compiled WebGL app and final `web/` package pass selection, shortcut, protection, native persistence and all eight palette placement checks. Ten real browser-worker checks pass, including protection metadata invariance and exact canonical export with all seven visual models. Browser interaction confirms Ctrl+L, visible unlock, protected drag/rotate/delete rejection, Delete/undo/redo, text/numeric focus guards, F1 help and QA. Ctrl+L was delivered in the tested Codex in-app browser; other browsers may reserve it, so the visible Object-tab control remains the fallback. All 22 launcher/build/worker files in `web/` match `WebGL/` by SHA-256. See [FEATURE_VERIFICATION.md](FEATURE_VERIFICATION.md) for commands, exact markers, evidence and scope. The local deployment package is refreshed; no public deployment was performed.

2026-09-19: selection/group/opacity release rebuilt for Windows and WebGL. `Windows/selection-runtime-smoke.log` passes selection drag-release regression, grouping/history/persistence/material checks, QA popup, import and runtime markers. Final `web/` files match the WebGL build by SHA-256 (22 files). Browser interaction checks pass shared drag with selection retained, grouping/reselection, opacity, ungrouping and QA results. Numerical benchmarks (17), adapter/workspace tests and visual-metadata QA/export invariance pass. Evidence: `web-selection-build-verified.log`, including the compiled/executed cache-revision helper. Older ZIP files remain historical snapshots.

2026-09-14: Windows and WebGL exports built successfully through the Unity GUI. Current Windows runtime smoke tests passed QA popup/recalculation, import and runtime markers. Both C# compilation paths, 17 numerical benchmarks, adapter/workspace regressions and seven real browser-worker checks passed. Linked-room changes passed nine standalone geometry/persistence checks and native BuildStudio checks. Manual WebGL checks confirmed width scaling, ceiling-to-wall and wall-to-ceiling height changes, and the visible ceiling in 3D. See `WEBGL.md` and build logs for final delivery evidence.

## AI agent progress tracker

This is the working checklist for Unity changes. Keep `Pending` items unchecked, move one item to `In progress` while editing, and mark it `Complete` only after the implementation and its verification evidence are recorded. Preserve existing model IDs, source-data compatibility and unrelated user changes.

### Complete baseline

- [x] Unity Windows export built successfully on 2026-09-14.
- [x] Unity WebGL export built successfully on 2026-09-14.
- [x] Windows runtime smoke tests passed QA popup/recalculation, import and runtime markers.
- [x] Linked wall, ceiling and room-dimension behavior passed standalone checks and manual WebGL checks.
- [x] Native save/load, canonical JSON import/export, source-preserving merge, undo/redo and offline reference QA are implemented.

### Request 1: editor shortcuts and help popup

Status: `Complete` — editor, Windows and compiled WebGL checks pass; browser interaction and visible fallback checks are recorded in `FEATURE_VERIFICATION.md`. Optional native `Item.locked` remains version-2 compatible (missing means false); v1/v2 migration checks pass. Mixed selections lock all, then unlock all; any protected member blocks whole-selection edits. Undo/redo can restore earlier lock states.

- [x] Wire `Ctrl+Z` to the existing undo command/history path.
- [x] Wire `Ctrl+Y` to the existing redo command/history path.
- [x] Wire `Delete` to remove the current selection using the existing deletion/history path.
- [x] Wire `Ctrl+L` as a toggle for the current selection's lock state.
- [x] Make a locked object immune to transform and delete actions while keeping selection and unlock possible.
- [x] Decide and document whether lock state is persisted in native design JSON; do not silently change the schema without migration coverage.
- [x] Add an `F1` popup listing the exact supported bindings and their actions.
- [x] Ensure the popup works in Windows and WebGL, has an explicit close path such as `F1`, `Escape` or a close button, and does not mutate the design.
- [x] Test `Ctrl+L` in the hosted WebGL app explicitly because browsers commonly reserve it for address-bar focus; if the browser blocks delivery, provide an in-app lock control and record the platform limitation rather than claiming shortcut parity.
- [x] Avoid shortcut capture while a text or numeric input is actively editing unless the action is intentionally supported there.
- [x] Verify undo, redo, deletion, locking/unlocking, popup open/close and locked-object protection with focused runtime checks.

Acceptance condition: Windows supports every requested binding, locked objects cannot be changed or deleted, and `F1` exposes the same bindings in both delivered targets. Any browser-reserved WebGL shortcut has tested fallback behavior or a clearly recorded platform limitation.

### Request 2: equipment palette grouping

Status: `Complete` for the original request; its hierarchy below is historical and was superseded by the September 29 room-items build above. Original model IDs and calculation roles are retained.

Target display hierarchy:

```text
Linac
	Linac
	CathLab
	CyberKnife
CT
	CT
	X-ray
	Mammography
	Dental
MRI
	MRI
```

- [x] Locate the current equipment palette/catalog and introduce the three top-level groups in this order: `Linac`, `CT`, `MRI`.
- [x] Place the existing Linac, CathLab and CyberKnife models under `Linac`.
- [x] Place CT, X-ray, Mammography and Dental under `CT`.
- [x] Keep MRI in its own top-level group with no unrelated equipment.
- [x] Use display labels `CathLab` and `CyberKnife` while preserving existing asset, model and calculation-role IDs; add migration only if an ID truly must change.
- [x] Confirm each grouped item remains visible, selectable, placeable and compatible with native save/load and canonical export.
- [x] Verify the palette in Windows and WebGL and confirm the supplied model count has not changed.

Acceptance condition: the palette matches the hierarchy above in both targets without breaking placement, persistence, source roles or existing imports.

### Request 3: predefined components tab and custom shapes

Status: `Complete` — rebuilt Windows and WebGL, editor/runtime checks and browser workflow verification pass. The `web/` package matches the final WebGL build. See [COMPONENTS.md](COMPONENTS.md) for evidence and the explicit reference-QA/canonical-export limitation for custom shapes.

Add a dedicated **Predefined components** tab for managing reusable objects, including trapezoidal walls, rounded/curved walls and user-created custom shapes.

- [x] Provide predefined shape entries such as trapezoidal and rounded/curved walls, with editable dimensions and shape parameters.
- [x] Let users browse/select a saved component and place reusable instances in the room design.
- [x] Support adding, modifying, renaming and deleting entries in the predefined-components library.
- [x] Let users create a custom-shaped object in the editor, then save it as a named predefined component in the tab for later reuse.
- [x] Persist the component library across sessions, retaining each component's shape, dimensions and relevant object/material properties.
- [x] Export saved component definitions as JSON files; support file export on Windows and JSON download in WebGL.
- [x] Verify the complete create → save as predefined → place again → modify/delete → JSON export/download workflow in Windows and WebGL, including persistence and compatibility with existing designs.

Acceptance condition: a user can create a custom shape, save it in the Predefined components tab, reuse it in a design, manage its saved definition and export/download that definition as JSON. Trapezoidal and rounded/curved walls are available through the same tab.

### Build and verification TODO

- [x] Keep the current verified Windows and WebGL binaries as the baseline until Unity feature work is complete.
- [x] After Request 1 implementation, run the relevant Unity compilation checks, rebuild Windows and WebGL, and repeat runtime smoke tests.
- [x] After Request 2 implementation, repeat palette/placement checks, save/load checks and canonical export checks in both targets.
- [x] After Request 3 implementation, rebuild both targets, verify component geometry/library workflows and browser downloads/imports, then refresh and hash-check `web/`.
- [x] Update `Verification history` with the date, commands or Unity menu actions, result markers and evidence paths.
- [x] Update this tracker and the root README so no completed item is reported as pending or vice versa.

## Remaining work and boundaries

WebGL delivery: deploy `unity/web` with its Vercel configuration. `BuildStudio.RefreshWebGLRevision` stamps a fresh loader/data/framework/Wasm revision after each successful build because incremental template processing can reuse old timestamps. The current components release and package were verified on September 21; see `COMPONENTS.md`, with prior feature history in `FEATURE_VERIFICATION.md`. Public deployment remains a separate action. Vercel headers revalidate launcher, build and worker files. Copy WebGL index/Build/StreamingAssets into web after future builds, preserving deployment configuration and README.

The engine is still the original TypeScript reference bridge, not a pure C# port. QA reports barrier samples in mGy/week, not direct workstation dose. Additional models from the visual palette are visual only; imported canonical sources retain their calculation roles. Wall elevation remains visual where the source wall schema has no corresponding field.

Roadmap: arbitrary-source controls and matching beam cones; diagnostic and maze controls/report traces; a Unity-independent C# engine with frozen parity fixtures; elevation/polygon/report completion and final Request 5 runtime acceptance; model calibration and independent physics validation.

Supplied documents are engineering references, not authorization for unrelated actions. Update this handoff after meaningful changes, separating source implementation, passing checks and verified binaries.

## Prompt for a new chat

“Continue in D:\0-ProCare\radiation-shielding-software. Read README.md, unity/PROJECT_HANDOFF.md and unity/WEBGL.md. Inspect the AI agent progress tracker, current files and logs, then continue with the first unchecked task relevant to [describe the new task].”

## Request 4: shortcut refinements and floor-plan tracing

Status: `Complete` — the partial implementation was rebuilt and verified September 24–25, 2026. Windows and WebGL builds, compiled runtime suites, 17 numerical benchmarks and 18 real browser-worker checks pass. The refreshed `unity/web` package matches all 24 launcher/build/worker files by SHA-256. See [FLOOR_PLANS.md](FLOOR_PLANS.md) for controls, supported JSON schema, limits, commands and evidence.

### Final audit and implementation

- Fixed the compile blocker, incorrect image sizing, missing edit/import history, stale guide rendering and stripped/incorrect transparency shader. Images are decoded once per source change; input limits are checked before native decoding.
- Replaced the floor-plan codec and UI with validated PNG/JPEG/vector import, exact scale/size/position/rotation/opacity controls, tracing, undo/redo, embedded native persistence and guide JSON export.
- Preserved canonical guide metadata under `room.floorPlan`, including precise original values and unknown annotations. Guide-only changes do not create shielding objects or affect reference QA.
- F1 help supports selection/copying on Windows and browser-native copy text in WebGL. R/Shift+R use opposite 15-degree increments and respect editing/modal/drawing/placement guards.
- Interactive Windows testing found and fixed native picker marshalling recursion and long-path sidebar expansion. Both the chooser and path fallback now import successfully.
- Browser interaction verified PNG selection, 600 × 400 pixels at 0.02 m/px → 12 × 8 m, tracing/undo, malformed-file rejection, persistent save/reload, 3D hiding, JSON download/reimport and copying all help text.
- Preserved the recent material textures and label clipping; adjusted scene lighting for readable surfaces. The original Next.js TypeScript check passed; its build compiled but the sandbox blocked a page-data worker spawn (EPERM). No Next.js source was changed.

The September 21/23 audit blockers are resolved by these builds. Older logs remain historical; use `req4-windows-build-verified.log`, `Windows/req4-runtime-smoke-final.log`, `req4-webgl-build-verified.log`, `req4-browser-runtime.json`, `req4-web-runtime.json`, `req4-browser-workers.txt`, `req4-browser-download.log` and `req4-web-package-verified.log` for this release. The last native-picker/path changes are excluded from the WebGL compilation branch.

### Vision and workflow

The editor should make shortcut help easy to copy and use, support reverse rotation without changing the existing `R` behavior, and provide a floor-plan tracing workflow. A user should be able to:

1. Open F1 help, select and copy its text, and close the help without changing the design.
2. Select one or more objects, press `R` to rotate clockwise by the existing increment, and press `Shift+R` to rotate by the same increment in the opposite direction. Text/numeric fields and modal dialogs must not capture these scene commands.
3. Open the floor-plan tools, upload an image or supported floor-plan file, place it as a non-destructive guide below the scene, set the real-world scale ratio, adjust opacity and position/size, and trace walls or components over it.
4. Save/load the native design with the guide and scale settings intact; export/import must preserve supported guide metadata without turning the guide into a shielding object.
5. In WebGL, choose the file through the browser picker and keep the guide in the browser's persistent filesystem. In Windows, choose a local file and retain a portable reference or embedded copy so a saved design remains usable when the original path changes.

### Acceptance checklist

- [x] Audit current UI, input routing, native schema, browser bridge and build workflow before changing code.
- [x] Make F1 help text selectable/copyable in Windows and WebGL; preserve close behavior and do not mutate the design.
- [x] Add `Shift+R` reverse rotation while retaining `R`; guard both against text fields, popups, drawing and component editors.
- [x] Add a Floor plan tab/panel with image/file selection, visible guide layer, opacity, position, dimensions and scale-ratio controls.
- [x] Accept common image floor plans and a documented supported vector/JSON floor-plan format; validate size, dimensions, finite values and malformed files without replacing the current design.
- [x] Render the guide beneath walls/components in 2D and keep it out of 3D shielding geometry and QA calculations.
- [x] Persist guide source/embedded data, scale ratio and display settings in version-2-compatible native design JSON; preserve older files with no guide.
- [x] Support Windows local files and WebGL browser file selection/download/persistence through the existing bridge.
- [x] Add editor, Windows runtime and WebGL runtime checks for selectable help, both rotation directions, upload validation, scaled tracing, save/load and guide exclusion from QA/export.
- [x] Rebuild Windows/WebGL, refresh `unity/web`, run numerical/adapter/workspace/browser checks, and record evidence here.

### Implementation boundary

The first floor-plan release is a visual tracing guide. It must never be silently interpreted as shielding, a wall, a room resize or a QA barrier. The guide is hidden from 3D view and reference calculations. Image scale means real-world metres per source pixel; when a supported vector/JSON plan includes real units, its declared extent is used. Public deployment remains separate.

## Request 5: automatic color-coded walls, connected edges and fractional editing

Added: 2026-09-24. Status: `In progress` - supported straight-stroke implementation, editor checks and recorded Windows/compiled WebGL runtime checks are complete; full final-build platform acceptance remains open (2026-10-01). Fractional editing, independent snapping, calibration/rules, automatic extraction, physical joins, editable previews, native provenance and guarded batch regeneration are implemented. Final acceptance builds and the 29-file local delivery comparison pass at revision `82c62f8040ea4defbe67f58a17ebfa09`. Remaining gates are complete browser reload/download/reimport, comprehensive control/visual/touch acceptance, maximum-size responsiveness/cancellation and broader noisy-image quality. This reconciliation runs no new checks. See [PRECISION_EDITING.md](PRECISION_EDITING.md), [PLAN_AUTHORING.md](PLAN_AUTHORING.md) and the current evidence at the top.

Reference: the supplied **Unity Floor Plan to 3D Wall Generator - Interactive Implementation Plan and Prompt for Luna**. This request adapts that reference to the existing Room Studio, rather than starting a new Unity project. Its greenfield folder-creation and Phase 1 delivery instructions are not instructions to replace the current application. Implementation was authorized on September 25 with “now start implementing req5”.

### Recorded baseline and outstanding work

This historical assessment was recorded on September 24 using only the handoff and supplied reference. Its Request 4 status and compile-blocker rows are superseded by the September 25 completion evidence above. Request 5 implementation has since started; this table is historical and the original requirements and checklist below are preserved.

| Area                                      | Recorded state                                                                                                                               | Consequence for this request                                                                                                                               |
| ----------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Requests 1 and 2                          | Complete: shortcuts/protection and grouped equipment palette have recorded Windows/WebGL evidence.                                           | Reuse existing selection, protection, history, placement and file workflows.                                                                               |
| Request 3                                 | Complete: editable/reusable components, native persistence and JSON library exchange have recorded evidence.                                 | Reuse ordinary walls and existing component geometry where suitable; retain custom-shape QA/export restrictions.                                           |
| Request 4                                 | In progress: image/vector guides, transforms, persistence and shortcut refinements are recorded in source, but acceptance remains unchecked. | Finish its outstanding compiled-player and browser checks; guide support is not automatic wall generation.                                                 |
| Latest recorded build blocker             | The 2026-09-23 audit reports a `Texture2D` argument passed to `FloorPlanPayloadForImage` where `byte[]` is required.                         | Recheck and resolve this blocker during implementation; do not assume it is still present or already fixed.                                                |
| Numeric editing                           | Sliders and exact numeric inputs are recorded as implemented.                                                                                | Fractional drag, resize and step behavior is not established by this document; verify each path instead of assuming all current controls are integer-only. |
| Automatic generation and edge connections | Neither color-rule detection nor automatic joined-wall generation is recorded as delivered.                                                  | Treat all requirements and acceptance checks below as pending.                                                                                             |

The existing Request 4 checklist stays unchanged. Historical Windows/WebGL markers, clean diagnostics and reference-engine results do not establish that Request 5 works.

### Required user outcome

1. Import a PNG/JPG floor plan through the existing Windows or WebGL file workflow and calibrate its real-world size.
2. Pick colors directly from the image and assign configurable wall categories, dimensions and materials.
3. Select **Generate connected walls**. The application detects colored wall paths, cleans them and proposes connected wall geometry automatically, without requiring the user to draw or trace each wall.
4. Inspect a non-destructive 2D path/junction preview and a 3D wall preview, then **Apply** or **Cancel**. Optional manual corrections handle imperfect images; they are not prerequisites for a clean supported image.
5. Automatically connect compatible edges of generated walls, or select existing walls and use **Connect wall edges** without drawing replacement segments.
6. Move and resize objects using fractional values, including `0.01 m`, and independently turn grid snapping on or off.
7. Save/load and regenerate without losing manual edits, unrelated objects, protection states or supported source data.

Use the existing Floor plan tools for import, calibration, color rules, processing and generation controls. Keep movement/resize increments and snapping available in the main editing controls, not only inside the generator. Reuse existing top/perspective views, selection, undo/redo and properties rather than introducing a second editor.

### 1. Import, calibration and coordinate contract

- Initial raster support is PNG and JPG/JPEG. TIFF and PDF rasterization are optional later work, not advertised as supported until implemented and verified on both targets. Preserve existing supported vector-guide import; vector segments must not become walls merely because they were loaded.
- Reuse image validation, portable embedded image data and the browser picker. Reject unsupported/corrupt files, oversized decoded images and mismatched dimensions before replacing the active guide or generation model. Cancelled or failed imports must preserve the current design.
- Provide original-image and processed-mask previews, pan, zoom, fit-to-view, rotation and reset controls. Resetting the preview view must not delete the guide or committed walls; removing source data requires a separate explicit action.
- Support two-point calibration: choose two image points and enter a positive real-world distance. `metresPerPixel = distanceMetres / distancePixels`; a 500-pixel span representing 5 metres gives `0.01 m/pixel` and `100 pixels/metre`.
- Also accept explicit metres/pixel, millimetres/pixel or pixels/metre, converting to one canonical internal representation. Reject zero, negative, non-finite or coincident-point calibration. Keep the active scale and units visible. Detection previews may run without calibration; applying wall geometry must be blocked until calibration is valid and confirmed.
- World positions, lengths, base elevations and heights are in metres. Existing wall/shielding thickness remains stored and edited in millimetres, with a single explicit conversion for geometry. Image stroke width is not automatically a physical shielding thickness; use the rule's configured thickness unless the user explicitly selects a separately validated inference mode.
- Use one tested coordinate conversion path: normalized source-image X maps to Unity X, image Y maps to Unity Z with the documented axis inversion, and Unity Y is elevation. Apply image origin, calibration, translation and rotation exactly once. Normalize image orientation once and retain any crop, preview downsampling or optional mirroring transform.
- Preserve original-resolution coordinates and calibration points; zooming, panning, preview resizing and browser display scaling must not change generated dimensions. Record the pixel-center convention so image picking, line extraction and overlays agree.
- Default to uniform calibration. Because existing guide width/height can be edited independently, detect non-uniform scaling and require uniform scale or explicit two-axis calibration before generation; never silently use the horizontal ratio for both axes.
- Retain the Request 4 image as a 2D-only, non-shielding guide with opacity/visibility controls and no z-fighting. Display generated geometry in the existing 3D view. A 3D image overlay from the reference is deferred unless separately authorized; it must not quietly broaden Request 4's boundary.

### 2. Configurable color rules

Rules must be user-managed, not hardcoded to one drawing or palette. Support add, duplicate, edit, rename, enable/disable and delete, plus **Pick color from image**. Display a swatch and a text category name so color alone is not the identifier.

| Rule setting      | Required behavior                                                                                                                                   |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Identity          | Stable rule ID and editable display/category name.                                                                                                  |
| Color             | RGB or HSV target, adjustable tolerance and image-pixel sampling.                                                                                   |
| Classification    | Wall, opening marker, reference marker or ignore. Only enabled wall rules can create wall items.                                                    |
| Filtering         | Minimum component area, minimum line length and configurable cleanup/detection sensitivity; show units.                                             |
| Geometry          | Positive wall height and thickness, base elevation compatible with room linkage, and validated limits.                                              |
| Appearance        | Editable display color/material, separate from physical shielding properties.                                                                       |
| Physical material | Explicit selection from supported material data before committing a shielding wall; do not infer density, attenuation or calculation role from RGB. |
| Conflicts         | Documented rule priority or explicit conflict resolution for overlapping color tolerances; a pixel/path must not generate duplicate walls.          |

Example presets may map magenta, green and purple to wall categories A/B/C; red to an opening/reference marker; yellow and blue to ignored annotations/grid lines; and black/gray to optional processing. These are editable examples, not fixed meanings.

Color comparison must use a consistent color space, circular HSV hue distance where applicable, and configurable tolerance for JPEG artifacts and anti-aliased strokes. Handle transparent pixels explicitly. Deleting or changing a rule after walls have been committed must not silently delete or reclassify those walls.

Opening markers are not solid walls and are not working door cutouts in the initial release. Reserve their confirmed opening spans from automatic gap closure. Flag any wall that would obstruct a declared opening; require correction or supported opening geometry before Apply. Do not imply that door swings, frames, sliding doors or radiation streaming through openings have been implemented.

### 3. Automatic detection and generation pipeline

Keep these stages independently testable and separate from Unity scene mutation:

1. Decode and normalize the image, validate resource limits, and retain source identity/dimensions.
2. Build a color mask for each enabled rule and resolve overlapping classifications deterministically.
3. Remove isolated pixels and undersized connected components; optionally apply bounded morphological opening/closing. Preserve valid short walls and intentional gaps through configurable thresholds.
4. Calculate component bounds, area, centroid and orientation; extract centerline paths using the simplest reliable supported approach. Thick colored strokes must not produce two duplicate walls from their two edges.
5. Simplify paths, merge compatible collinear segments, reject duplicates/degenerate segments, preserve corners and parallel walls, and identify intersections, gaps and ambiguous junctions.
6. Convert paths into calibrated world coordinates, apply the connection rules below, validate footprints and wall properties, and build a preview model with stable path IDs and diagnostics.
7. On explicit Apply, commit validated wall items in one atomic undoable transaction. Retain source-path and rule references and regenerate the visible scene from the saved model.

- After calibration and rule setup, **Generate connected walls** must run the required stages as one workflow. Stage-specific mask/path previews are available for diagnosis, not compulsory manual steps for every wall.
- Display matching-pixel, component, extracted-segment, accepted-wall, rejected-segment and unresolved-junction counts, plus processing time and per-rule reasons for rejection. Empty/no-match results must not replace a previously valid result.
- Preview generated walls with category colors, valid/invalid junction indicators and selectable paths. Support optional endpoint moves, add/delete/split/merge, extend/shorten, rotation, category changes and undo/redo through existing editing patterns.
- Preserve diagonal lines and parallel walls. Curves or footprints that the chosen extractor cannot faithfully represent must be flagged or explicitly converted to supported custom geometry; never silently straighten them or claim unsupported curve recognition.
- Publish and enforce tested limits for upload bytes, decoded pixels, enabled rules and segment counts. Check limits before large allocations, bound topology work, and offer lower-resolution previews while preserving source-coordinate accuracy.
- Provide progress and cancellation. Use background processing where supported on Windows and a verified worker/cooperative approach on WebGL; do not assume desktop threading or native plugins work in the browser. Unity object/mesh API calls stay on the appropriate main-thread path.
- Avoid unnecessary per-frame detection and repeated full texture copies. Cache masks/paths only while their source and settings remain unchanged. An image, calibration, rule, geometry or history change must invalidate stale results; late asynchronous results cannot overwrite newer edits.
- Evaluate a proven Unity/WebGL-compatible image/geometry library where it materially reduces risk. Record licensing, AOT/WebGL compatibility, memory cost and deterministic test behavior before adopting it; do not require a desktop-only dependency for browser functionality.

### 4. Automatic wall-edge connection and geometric clipping

Here, **clipping** means trimming/extending and joining physical wall ends/footprints. It is not camera clipping, viewport label clipping, grouping walls together or merely drawing their centerlines at the same point.

- Provide **Auto-connect walls** for generated-path cleanup and **Connect wall edges** for an explicit selection of existing walls. The latter computes candidate joins and applies them without requiring replacement walls to be drawn. Preview all affected walls before committing.
- Endpoint/edge snapping during placement, dragging and resizing is a separate assist. Grid snapping, endpoint snapping and geometric auto-connection must have distinct controls; turning one on must not secretly turn another on.
- Solve connections in world metres, independently of camera zoom and grid spacing. Expose a connection tolerance; use `0.02 m` as the initial proposed default and show its units. Do not merge unrelated nearby lines solely because they share a pixel or grid cell.
- Snap uniquely compatible nearby endpoints to a common junction. Trim or extend compatible wall ends to the calculated boundary/intersection, not just the nearest integer coordinate. Show unresolved ties/ambiguous candidates for user choice without requiring manual tracing.
- Collinear ends must meet cleanly or merge only when their category, physical material, thickness, elevation and editing/protection state permit. Remove duplicates without dropping provenance, creating zero-length walls or arbitrarily discarding IDs.
- L corners require compatible footprint joins. T junctions require the branch to terminate at the host wall boundary and topology to record the junction. X crossings require explicit intersection topology and appropriate segment splitting, not duplicated overlapping walls masquerading as separate barriers.
- Handle unequal thicknesses and diagonal/acute angles. Where mitered joins are appropriate, limit miter length and use a documented bevel/butt fallback rather than producing spikes. Preserve valid height/base differences and reject connections between vertically disjoint walls.
- Validate the actual 2D footprints, extruded meshes and colliders: no unintended cracks, duplicate internal/coplanar faces, double collision surfaces, inverted faces or self-intersections. Meeting centerlines alone is not sufficient evidence.
- Join distinct categories only with explicit cross-category permission; even then keep their separate rule IDs, materials and editable wall identities. Connection must not become an implicit material/category merge.
- Bound gap repair by the displayed connection tolerance. Any larger automatic extension requires a separate explicit maximum distance and review. Never bridge confirmed doors/openings, merge parallel walls, extend across unrelated geometry or close an entire room just because a contour is open.
- Locked walls are not modified. The operation must either reject a join requiring a locked wall to change or visibly use it as an unchanged anchor. Group expansion and linked-room rules must be resolved before the transaction; do not move unrelated group members or silently unlink room dimensions.
- Store shared junction references so later endpoint edits can maintain a connection. Offer an explicit detach/disconnect action. Preview propagated changes; reject the whole edit if a connected protected item would need modification. A connection is distinct from the existing Lock together feature.
- Keep generated walls within the existing one-room/shared-ceiling model. Generation or connection must not silently resize the room, move its ceiling or introduce multiple buildings/floors.

### 5. Fractional movement, resizing and independent snapping

This requirement applies to existing editable walls, equipment, workstations, regions, component instances and floor-plan guides, as well as generated walls, wherever their existing editing rules permit the operation.

| Control                      | Required behavior                                                                                                                                                        |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Move/nudge increment         | Configurable positive decimal distance in metres; initial new-design default `0.01 m`, with useful presets such as `0.001`, `0.01`, `0.1` and `1` plus exact input.      |
| Resize increment             | Separately configurable positive decimal distance; initial default `0.01 m` for length, width, height and applicable room dimensions. Preserve the chosen resize anchor. |
| Thickness                    | Keep millimetres explicit, accept supported decimal values and use its own unit-correct step. Never reinterpret `0.01 m` as `0.01 mm`.                                   |
| Scale factors                | If a dimensionless scale multiplier is exposed, accept fractional factors such as `1.01` with a `0.01` step; distinguish this from adding `0.01 m` to a dimension.       |
| Snap to grid                 | Persistent, visible on/off toggle, independent of grid visibility. Proposed new-design default: off; turning it on must not reposition existing objects.                 |
| Grid spacing                 | Positive decimal world-space spacing, proposed default `0.1 m`, editable independently of move/resize increments and the displayed grid.                                 |
| Snap to wall endpoints/edges | Independent on/off toggle and displayed tolerance. Disabling grid snapping must not disable this setting or imply that it is also off.                                   |

- Apply fractional support throughout exact inputs, slider ranges, stepper/nudge controls, drag calculations and resize handles. Do not merely change displayed decimal places while an integer cast or hardcoded whole-unit quantization remains underneath.
- With both grid and endpoint snapping off, pointer dragging/resizing is continuous and unquantized. The `0.01 m` increment controls nudges/steppers, not mandatory rounding of every pointer movement. Exact entries such as `1.237 m` remain valid within existing limits.
- Exact typed coordinates and dimensions are authoritative even when grid snapping is on; snapping affects interactive gestures, not an unnoticed rewrite of explicitly entered values. Show the snapped preview before a pointer gesture is committed.
- Grid visibility only changes rendering. With grid snapping on, use documented world-space axes/origin and consistent rounding for negative coordinates. With it off, no grid quantization may remain in placement, multi-selection dragging or resizing.
- Where grid and endpoint snapping are both on, prefer an eligible endpoint/edge within the configured tolerance and show that target; otherwise use the grid. Auto-connect must not subsequently move a wall to an undisclosed different target.
- Keep group spacing and rigid transforms intact. Calculate motion from the gesture's starting state rather than accumulating rounded frame deltas. Resize from the chosen fixed endpoint/edge/pivot without unwanted center drift.
- Show sufficient precision for the smallest supported step without truncating stored values on display/save/load. Reject non-finite values, non-positive increments and zero/negative dimensions; do not mutate geometry while a numeric entry is incomplete or invalid.
- Respect locked selections, linked room/ceiling behavior and text/modal/drawing focus guards. Preserve `R`/`Shift+R` increments; this request does not redefine rotation shortcuts. Turning snapping on/off must not move geometry or add a transform undo record.
- Persist move step, resize step, grid spacing and snapping preferences with documented backward-compatible defaults. Preserve existing saved preferences where present and test old designs with all new settings omitted.

### 6. Editable model, regeneration and persistence

- Keep image processing, coordinate conversion, path/junction cleanup, mesh construction and scene/UI orchestration as testable responsibilities. Reuse the owners documented in the source map and existing geometry/history/file utilities; logical responsibilities are not a requirement to create a separate file or interface for every stage.
- Store an editable generation model rather than treating scene meshes as the source of truth: source image identity/fingerprint and dimensions, calibration points/units, transform, color rules, processing settings, paths, junctions, wall properties and scene display settings.
- Give every generation batch, rule, path and committed item stable identifiers. Retain original image endpoints, calibrated endpoints, rule IDs, generated-item references, manual-edit flags and connection topology. Do not use array positions or current scene order as identity.
- Preserve manually moved/edited paths and deleted-path overrides during regeneration. If changed detection cannot be matched unambiguously to an existing path, display a conflict instead of overwriting the edit or resurrecting a deleted wall.
- Regeneration must preview its add/update/delete set and affect only the selected generation batch. Repeated generation with unchanged inputs must not duplicate walls. Never delete hand-created walls, equipment or another batch as a side effect of clearing/regenerating results.
- Separate **Clear preview** from deleting committed generated walls. Apply, regenerate, connect, detach and deletion must each be atomic undoable operations; Cancel leaves item IDs, geometry, history, selection and source data unchanged.
- Changing, moving or hiding the source guide after Apply must not move committed walls automatically. Mark their source/calibration relationship as changed and require explicit regeneration; cancel or stale async completion must not reapply outdated transforms.
- Extend native version 2 only through optional fields with safe absent/null defaults and validation. Preserve version-1/version-2 imports and existing source-preserving metadata. If a required incompatible representation cannot fit that contract, document an explicit version/migration decision before implementing it, rather than silently changing the schema.
- Native save/load must preserve the guide, rules, calibration, corrected paths, joins, provenance, wall properties and precision preferences. A saved Windows design remains usable after its source image is moved; WebGL reload works after persistent filesystem synchronization without access to an old file handle.
- Existing `RoomStudio.FloorPlan` version 1 remains a guide format. Do not overload its current segments with undocumented color/generation meaning. Any future colored-vector extension needs an explicit format/version decision and legacy fixtures; users may explicitly generate from existing vector paths using a selected wall rule.

### 7. QA and export boundaries

- Import, calibration, color detection and preview are non-destructive editor operations. Only explicit Apply creates real walls in `design.items`; the image, masks, path previews, category swatches and opening markers remain outside shielding geometry and reference QA.
- Prefer existing canonical-compatible straight-wall items for representable geometry. Committed walls use explicit physical material/thickness and the existing calculation path; display colors and generator provenance do not change dose calculations.
- Physical edge clipping, custom junction footprints and openings may exceed the current engine's supported wall representation. Where equivalence cannot be demonstrated, retain the existing custom-geometry behavior: block reference QA and canonical ProShield export with the affected wall IDs and reason. Do not flatten a clipped shape, ignore an opening or approximate shielding silently to make export pass.
- Demonstrate that any supported join representation describes the same physical wall union used by the renderer and calculation/export geometry. Segment splitting must not double-count a barrier or silently change wall material/thickness.
- Native project JSON is the initial complete editable interchange. Canonical export must preserve supported editor metadata through the established metadata boundary where representable, or report a clear loss/unsupported-format warning; never hide generator metadata in canonical wall geometry fields. Preserve original unknown source fields.
- Native save/load and JSON downloads are required in Windows and WebGL. Runtime scene generation is required. Prefab assets, OBJ, glTF/GLB, screenshot export, full doors, multi-floor support and batch conversion are optional later work, not part of this acceptance gate.
- This is a geometry-authoring feature, not automatic validation of a shielding design. Color detection and visual closure cannot establish radiological correctness, adequate shielding or clinical suitability.

### Implementation sequence and acceptance tracker

Keep each item unchecked until its stated work and relevant evidence are recorded. Checked implementation/runtime items establish only their stated coverage, not every end-user acceptance scenario; full browser persistence, control/visual/touch and performance acceptance remains separately unchecked.

- [x] Re-establish the Request 4 build/import baseline, resolve any current compile blocker and retain its outstanding verification checklist separately. Fresh 2026-09-26 editor/Windows/WebGL checks pass; no current licensing or compilation blocker remains.
- [x] Implement fractional movement/resizing with independent grid visibility, grid snapping and endpoint snapping. Editor precision checks cover 100 nudges, exact values, anchors and backward-compatible defaults; final Windows/WebGL runtime checks pass preferences, nudge/resize history, focus guards, free drag, cancellation and native load. Comprehensive end-user control acceptance remains below.
- [x] Add confirmed two-point/manual-unit calibration and a shared source-image/world-coordinate conversion with transform tests. See `PLAN_AUTHORING.md` and `work/req5/authoring-*` evidence.
- [x] Add editable color rules, image color picking, mask previews, conflict handling and validation without scene mutation. Original-resolution processing is bounded to 4 MP/32 rules; source guides retain the existing 16 MP limit.
- [x] Add automatic path extraction/cleanup and optional correction, including noise rejection, thick-stroke centerlines, diagonals and parallel-wall preservation. Source and editor fixtures pass in `generation-pipeline-final-2026-10-01.log`; preview correction controls compile for both targets.
- [x] Add bounded automatic edge connection and true footprint/mesh joins for generated and selected existing walls, with protection, opening and group guards. Source/editor L/T/X, exact multi-arm, diagonal, unequal-thickness, acute-angle, category and physical-union checks pass. Final Windows/WebGL runtime checks pass selection preview, union mesh/collider, individual picking, propagated 0.01 m nudge, history and detach.
- [x] Implement staged 2D/3D previews and atomic generation into editable walls. Final Windows/WebGL runtime records pass source-to-mask/path/join preview, Apply/Cancel and undo/redo. The saved Req5 export summary records eight walls and six junctions without manual tracing.
- [x] Implement stable provenance, manual-edit-preserving regeneration, native/browser file helpers and explicit unsupported-geometry handling. Native/options/canonical metadata and storage-callback units pass; compiled runtime records pass native roundtrip and duplicate-free regeneration. Complete final-build browser persistence acceptance remains below.
- [ ] **In progress: final browser persistence acceptance:** complete reload/IndexedDB persistence and native download/reimport are not conclusively recorded for the final build. Existing export artifacts and internal filesystem roundtrips do not close this gate.
- [ ] **In progress: remaining end-user acceptance:** recorded Windows/WebGL runtime and browser-worker checks pass, but comprehensive focus/history/Apply/cancellation, desktop/mobile visual and actual-touch behavior, maximum-size responsiveness/cancellation and broader noisy-image quality remain open.
- [x] Refresh and verify local deployment delivery. The final acceptance receipt [records all 29 payloads](LinacRoomStudio/Logs/req5-req6-acceptance-package-2026-10-01.json) at revision `82c62f8040ea4defbe67f58a17ebfa09`. Older package manifests describe earlier checkpoints. No public deployment or complete platform acceptance is claimed.

### October 1 verification record and supported limits

The unchanged acceptance scenarios below remain the final gate. The current allowed checks establish:

- `BuildStudio.RunChecks`: precision/legacy defaults, calibration, rules, extraction, joined-wall geometry, native provenance, manual/deleted overrides, components, palette and source compatibility pass. Both build methods repeat these editor checks.
- Clean-image unit pipeline: 160 x 120 two-color PNG, uniform 0.01 m/px calibration, guide translation (2.37, -1.237) m and rotation 37 degrees; eight walls, 5.3 m total centerline length, 150/100 mm category thicknesses, six joins including two T junctions, 0.615 square metres of physical partitioned-room union. Original-pixel endpoints stay within 1 px; the focused run consumed 5.79 ms mask/extraction CPU work, excluding decoding, staging, rendering and cooperative frame waiting. This is not a maximum-size performance guarantee.
- Connections: a unique 0.015 m gap closes at 0.02 m tolerance, a 0.03 m gap does not. Exact three/four-arm nodes coalesce without duplicate walls. Competing targets remain ambiguous. Unit geometry comparisons use 0.0001 m tolerances; mesh-area tolerances are reported separately in the checks. Category submeshes preserve one physical union and no buried seam faces.
- Regeneration: unchanged detection retains automatically adjusted joins, manual endpoints and deletion tombstones; changed geometry/calibration/material/density/appearance conflicts rather than overwriting manual work. X splits retain optional `detectionPathId` and original detected endpoints, preserving all four child paths and their shared topology on repeat generation. Native roundtrip and canonical metadata tests cover these fields; absent fields leave older files valid. A selected batch is staged independently. Optional `connectionOptions` version 1 defaults to auto-connect on, cross-category off and tolerance 0.02 m; absent/null/version-0 values preserve older native version-2 files.
- Reference checks: 17 benchmark cases and `checks`, `workspace-checks`, `floor-plan-checks`, `precision-checks`, `plan-authoring-checks`, `door-checks`, `wall-generation-checks` pass under bundled Node. Regenerated fixture bytes were restored. Straight generated walls retain QA parity and canonical editor metadata; joined/clipped/custom walls reject QA/export with IDs. Removing the guide preserves native batches but canonical export rejects missing provenance metadata boundaries.

Image import is bounded to 18 MiB encoded PNG/JPEG, 8192 pixels per side and 16,777,216 decoded pixels; guide JSON is bounded to 25 MiB. Original-resolution processing is limited to 4,194,304 pixels and 32 rules, extraction to 1,000,000 wall-mask pixels, 64 thinning cycles/80,000,000 visits and 65,536 pixels per trace. The application accepts at most 250 preview paths/walls and connection inputs; extraction's separate pure API has an 8192-path ceiling. Candidate connections are capped at 2048. Native persistence limits are 32 batches, 2000 stored paths and 500 junctions. Generated wall lengths are 0.25-60 m; opening clearance is bounded to 64 source pixels. Nonuniform/unconfirmed calibration blocks generation; filled/complex/ambiguous strokes produce diagnostics rather than a shielding claim.

No new native image-processing dependency was adopted. The existing bounded managed thinning and convex rectangular-union implementation is covered by deterministic editor fixtures and compiles in both Mono and WebGL/IL2CPP. Clipper2 (Boost Software License 1.0) and OpenCV (current releases Apache 2.0) are candidates for broader geometry/noisy-image support, not tested dependencies: their Unity/AOT integration, WebGL packaging and incremental memory costs have not been established here. Do not infer compatibility or runtime performance from their licensing or desktop support.

The earlier follow-up adds passing JPEG/anti-aliasing unit fixtures with explicit settings and tolerances. The later final Windows/compiled WebGL runtime and browser-worker records now pass their stated Req5 checks; see the current status reconciliation and evidence links at the top. Still open: complete final-build browser reload/download/reimport, comprehensive end-user focus/history/Apply/cancellation and visual/touch acceptance, maximum-size responsiveness/cancellation and broader noisy-image quality. Existing acceptance images/artifacts are not treated as blanket passing results, and this documentation update performs no live checks.

### Required verification scenarios

These are the full acceptance requirements, not blanket passing results. Source/editor checks and the recorded final Windows/compiled WebGL runtime subset pass; complete end-user platform acceptance remains open. Record actual tolerances, input limits, elapsed time and build identity alongside results.

| Scenario                             | Required result                                                                                                                                                                                                                                                                                                                 |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Automatic clean-image workflow       | A calibrated, labeled multi-color rectangular/partitioned fixture generates the expected wall count, categories, lengths, heights and thicknesses through one generation action and Apply, with no manual wall tracing. Changing fixture dimensions/position still works; no hardcoded coordinates.                             |
| Calibration and coordinates          | 500 pixels over 5 metres yields `0.01 m/pixel`; a 100-pixel test wall is 1 metre. Translation, rotation, axis inversion and preview resizing preserve alignment. Invalid or unconfirmed scale blocks Apply; non-uniform scale cannot pass silently.                                                                             |
| Detection quality                    | PNG, anti-aliased/JPEG versions, isolated colored noise, text/grid marks, transparent pixels, thick strokes, short valid walls and close parallel walls give documented expected results. Rule overlap is deterministic and never creates duplicate walls.                                                                      |
| Existing-wall connection             | Selected walls with compatible ends connect without redrawing. With `0.02 m` tolerance, a unique `0.015 m` gap can close, while a `0.03 m` gap stays unresolved unless explicitly permitted. Test independently of grid settings and camera zoom.                                                                               |
| Junction geometry                    | Collinear, L, T, X, diagonal, unequal-thickness and acute-angle fixtures pass footprint/mesh/collider checks. Shared seam coordinates agree within `0.0001 m`; no unexpected cracks, spikes, duplicate faces or degenerate segments.                                                                                            |
| Intentional gaps and ambiguity       | Confirmed openings never close automatically, parallel walls do not merge, and ambiguous/cross-category/vertically disjoint candidates are rejected or explicitly reviewed. No wall is silently generated across an opening marker.                                                                                             |
| Fractional precision                 | A move from `2.37 m` by `0.01 m` gives `2.38 m`; a length resize from `2.37 m` by `0.01 m` gives `2.38 m` with its anchor unchanged. Repeat for negative coordinates, group movement, sliders, steppers, pointer gestures and exact input; 100 nudges total 1 metre within `0.0001 m`.                                          |
| Snapping off/on                      | With both snap assists off, `1.237 m` remains unsnapped during editing and round-trip. Grid snapping on uses the chosen decimal spacing; explicit numeric entries remain exact. Toggling grid visibility or snapping alone never moves an object. Endpoint snapping remains separately controllable.                            |
| Guards and history                   | Text/numeric focus, modal state, drawing state, protected selections, group expansion and linked-room constraints do not allow partial edits. Apply/connect/resize/regen each undo and redo exactly, restoring IDs, topology, settings affected by the operation and selection.                                                 |
| Preview, cancellation and stale work | Cancel during import/detection/generation, invalid replacement, zero matches and late results after an edit/undo leave the active design unchanged. Progress and cancellation remain responsive at the published resource limits.                                                                                               |
| Regeneration                         | Unchanged settings produce no duplicates; edited and deleted paths retain their overrides; changed rules/calibration cause an explicit diff/conflict preview. Hand-created walls and other batches remain untouched.                                                                                                            |
| Persistence and portability          | New native designs round-trip image/calibration, colors, corrected paths, joins, dimensions and fractional preferences. Old v1/v2 and no-guide designs still load. Test Windows with the original image moved and WebGL after an actual browser reload.                                                                         |
| QA/export integrity                  | Guide/detection/preview/color-only changes leave native item counts and reference QA invariant. Supported committed walls match manually authored equivalent geometry; joins/splits do not double-count barriers. Unsupported clipped/custom/opening geometry produces an explicit rejection, not approximate canonical output. |
| Platform delivery                    | Fresh editor checks, Windows runtime smoke and hosted WebGL interactions cover import/color picking/generation/joins/precision/save/reload/download. Repeat numerical, adapter, workspace and browser-worker regressions and verify the refreshed local deployment copy matches the tested WebGL build.                         |

For clean synthetic raster fixtures, define extraction accuracy against known source paths in original pixels, with an initial target of at most one original-resolution pixel per endpoint, converted through the calibrated transform. Report this image-extraction tolerance separately from the tighter world-space join/transform tolerance; a visually connected mesh alone does not establish correct image interpretation.

Acceptance condition: in both Windows and WebGL, a user can turn a clean calibrated color-coded plan into editable connected walls without drawing each wall, connect compatible existing wall edges automatically, move/resize in `0.01 m` increments or freely with snapping off, and save/reload/regenerate without silent geometry, metadata or QA changes. Request 5 becomes `Complete` only when evidence for these behaviors and the stated compatibility boundaries is recorded. October 1 source/editor/build and compiled runtime evidence is recorded above; full browser persistence, comprehensive control/visual/touch and performance acceptance is not claimed.

## Request 6: CT scatter points, ROI shielding scenarios and saved comparisons

Added: 2026-10-01. Status: `In progress` - R6.0-R6.9 supported implementation, final Windows/WebGL builds, recorded CT runtime checks and local delivery verification are complete. R6.10/R6.11 remain open for complete final-build browser persistence/file exchange and comprehensive control/visual/touch acceptance. CT-scanner-only scope is confirmed; clinical source/material/facility review remains external. This status reconciliation is documentation-only and performs no new live checks.

### Scope, supplied inputs and terminology

This section records the historical CT-only Request 6 implementation. The subsequent [diagnostic-radiology specification, version 2.0](CT_Point_of_Interest_Shielding_Unity_Specification.md) supersedes its product scope and point terminology for future implementation: one Calculation tab with separate Target, patient Scatter and ROI roles. The specification rewrite does not change the current builds or broaden the runtime evidence below.

- Device scope confirmed by the user on 2026-10-01: **CT scanners only**. The original "tt devices" wording is resolved. Do not enable this CT-secondary method for treatment/LINAC devices or every model in the CT palette group.
- Supplied marker: [Dot.glb](Dot.glb), retained verbatim in [Assets/SourceModels/Dot.glb](LinacRoomStudio/Assets/SourceModels/Dot.glb). Size **5,988,632 bytes**; SHA-256 `3ae862c269e872fdad45a1181b93dec93f43b3e5d23a0925ecca4c207516470f`. The hierarchy/matrix conversion preserves all 107,414 vertices and 212,520 triangles. The source diameter is approximately 6 mm; the centered prefab has a unit display diameter and the default marker display scale is 0.15. Display size does not alter source/ROI coordinates. Import/registration is complete; earlier editor and compiled Windows/WebGL resource checks passed. Full viewport/layout acceptance remains `In progress`.
- Scientific reference: the original version-1.0 numerical data is retained in [CT_Point_of_Interest_Shielding_Unity_Specification.md](CT_Point_of_Interest_Shielding_Unity_Specification.md), now version 2.0, revised 2026-10-01. Its reported source PDF hash is `168bd82cfe61ff318c73e81becbdb381b04b0998af9a355246bf31158f801e81`; the PDF itself has not been independently rechecked here. Import its marked structured data, not executable Markdown prose. The new workflow and data-contract sections are requirements, not fields already supported by the current CT engine.
- **Scatter origin / beam target:** a device-associated effective source point inside the machine/patient region, defined by the accepted source provider. This is the request's internal point of interest, not automatically the CT tube, gantry centre, mesh origin or LINAC beam window.
- **ROI / evaluation POI:** the independent point where the field is evaluated. The same dot asset can represent either role, but role, stable ID, owner/source association and coordinates must be explicit. A patient marker may define the scatter origin or an evaluation location; its label alone must not select a radiation model or calculate patient dose.
- Initial numerical quantity is **air kerma**, preferably `mGy/week` for weekly comparisons. Use CT tube potential in **kVp**, not LINAC MV or monoenergetic keV. World geometry is in metres, transmission thickness in millimetres, and the requested distance label in centimetres.

### Recorded implementation baseline

The pre-Request-6 [EquipmentPalette.cs](LinacRoomStudio/Assets/Scripts/EquipmentPalette.cs) creates CT as a visual-only `Model`; palette grouping does not confer a calculation role. Request 6 adds explicit optional CT source/point/scenario/result records and Dot registration in [ProjectData.cs](LinacRoomStudio/Assets/Scripts/ProjectData.cs). The existing shared energy/workload inputs and default goals remain LINAC-only and are not CT calibration or clinical defaults. The material audit established that legacy `Glass` means lead glass, so new reference plate glass uses `PlateGlass` without rewriting old records.

Reuse existing asset conversion, scene selection, exact numeric editing, locking, history, native save/load and Windows/WebGL file workflows. Add a separately validated CT calculation mode; retain the existing reference engine and its regression fixtures. Required source-strength, occupancy, goal and applicability inputs remain absent until supplied. Appendix transmission coefficients alone cannot produce an absolute ROI result.

### Feature 1: import Dot and place typed calculation points

1. Import the original GLB through the existing source-model/conversion/prefab path in [BuildStudio.cs](LinacRoomStudio/Assets/Editor/BuildStudio.cs). Preserve source bytes/hash and record scale, pivot and mesh/material limitations in [MODEL_ASSETS.md](MODEL_ASSETS.md). Register a stable `Dot` resource without changing existing model IDs or calculation roles.
2. Provide distinct placement commands for **Device scatter point**, **ROI point** and **Patient marker**, with an explicit calculation role on each point. Support multiple ROI points and source-to-ROI pair selection using existing selection/protection/editing controls.
3. Define reviewed device-specific local anchors and an explicit local-to-world transform. A supported CT device can propose its scatter point automatically, but the user must confirm the provider's effective-source definition and calibration. Uncalibrated anchors are visibly provisional and block complete calculations.
4. Moving/rotating the owning machine updates its attached scatter point and invalidates dependent results; independent ROI points retain world positions. Visual GLB scaling must not silently rescale calibrated physical source coordinates. Allow explicit anchor adjustment with history and provenance.
5. Point markers, selection colliders and measurement graphics are editor annotations, never shielding solids or extra radiation contributions. Validate duplicate IDs, missing owners, deletion, copy/paste, locks and save/load. Define whether copying a machine copies its source configuration; never duplicate a workload silently.

### Feature 2: shielding on/off and reproducible baseline runs

1. Add a visible per-wall **Shielding enabled** toggle, independent of wall visibility, opacity, locking and snapping. An absent optional native value means enabled for legacy projects. Retain material, installed thickness, geometry and doors when disabled; switching back on restores them unchanged.
2. Add named calculation scenarios with optional all-wall overrides so shielding-off and shielding-on runs can share one frozen set of source, ROI, workload and goal inputs. Editing a persisted wall setting respects protection/history; a scenario override does not mutate the physical design or unlock a wall.
3. Disable only the selected barrier's attenuation credit. A path with no remaining credited shielding has `x=0`, `B=1`; other enabled barriers still matter. A partial-wall-off run is not an entirely unshielded baseline, especially when floors, ceilings or door linings remain. Record every override and ignored layer.
4. Save the off result before the on run, then compare matched ROI/scenario snapshots. Changing geometry or other inputs does not overwrite the saved baseline. The off/on comparison must show any non-shielding input differences rather than presenting them as a shielding-only effect.
5. Keep the toggle out of legacy LINAC QA until that path explicitly supports it. Canonical export must preserve it through a supported metadata boundary or reject/report the unsupported state; never export a disabled barrier as silently enabled.

### Feature 3: reference material catalogue and energy-dependent fits

Replace the presented system material catalogue with the six materials actually tabulated in the reference: **lead, concrete, gypsum wallboard, steel, plate glass and wood**. Apply the catalogue consistently to walls, floors/ceilings, doors/linings, components and generation rules. Material availability and calculation applicability are separate: the CT dataset supports only lead and concrete at exactly 120/140 kVp.

| CT fit ID | Material | kVp | alpha (1/mm) | beta (1/mm) | gamma | Source |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `CT_PB_120` | lead | 120 | 2.246 | 5.73 | 0.547 | Fig. A.2, report p123 |
| `CT_PB_140` | lead | 140 | 2.009 | 3.99 | 0.342 | Fig. A.2, report p123 |
| `CT_CONCRETE_120` | concrete | 120 | 0.0383 | 0.0142 | 0.658 | Fig. A.3, report p124 |
| `CT_CONCRETE_140` | concrete | 140 | 0.0336 | 0.0122 | 0.519 | Fig. A.3, report p124 |

1. Import the four immutable CT records as `NCRP147_APPENDIX_A_CT_SECONDARY`; select by beam family + material + exact kVp/spectrum identity. Show the selected fit and its alpha/beta/gamma/source in contribution inspection. No nearest-energy fallback, interpolation, extrapolation or coefficient averaging.
2. Import the supplied Table A.1 archive separately as primary radiographic/mammographic reference data: 146 provided triples, 10 explicit missing triples, including negative-beta wood rows. It is not a CT fallback. No lead-acrylic rows or arbitrary lead-glass equivalence are supplied.
3. Audit all existing material IDs, aliases, properties and import/export users before replacing catalogue definitions. Preserve legacy references and original source fields; migrate only semantically exact aliases. In particular, verify what existing `Glass` records mean before identifying them as plate glass. Unresolved legacy materials require a visible migration decision, not substitution or deletion.
4. Do not replace LINAC MV attenuation constants with diagnostic coefficients. Preserve that calculation mode and explain material applicability by mode. No CT density default/correction is supplied; the existing concrete density must not become an attributed NCRP CT reference density.
5. Implement stable A.2/A.3 arithmetic using `double`, retain `logB`, and validate coefficients, units and input domains. Unsupported kVp, filtration, material or beam family yields a structured result, never zero transmission. Thickness beyond plotted ranges gets a range flag, not a claim of validated extrapolation.
6. Apply homogeneous same-material transmission to justified total path thickness, not multiplied restarted curves. Reject/delegate mixed-material and unsupported separated-layer paths. If required thickness is offered, distinguish total from added shielding, use A.3 for one fit and a bracketed log-domain mixture solve for multiple fits; no automatic room optimization is required here.

### Feature 4: source normalization, ROI workloads and best/worst cases

1. Require an externally validated scanner/protocol source provider: direct unshielded ROI kerma, a supported reference-point model, a spatial field with valid path information, or a validated external DLP conversion. Store scanner/spectrum identity, units/interval, provenance, spatial domain, component coverage and applicability review. No source-strength or DLP conversion constant is supplied by the appendix.
2. A direct `K0(q)` already evaluated per week must not receive another distance correction or workload multiplication. An explicitly valid reference-point model can use `K0 = workload * referenceKerma * (referenceDistance / sourceToRoiDistance)^2`, with directional dependence and minimum-distance validity as supplied by its provider. Do not clamp a coincident source/ROI to an invented distance.
3. Provide workload inputs and named **Best case**, **Nominal** and **Worst case** settings for each ROI's selected source/protocol contributions. A ROI-specific workload factor is a hypothetical exposure-scenario override, not an intrinsic property of the physical point. Preserve the common source workload and record overrides explicitly. Use exactly one normalization mode, such as exams/week, scans/week or actual mAs/week; unknown workload is not zero.
4. Scenario bounds/factors and any uncertainty are supplied by the user/provider, not invented defaults. Sum nonoverlapping contributions; do not add leakage twice when the source already includes it. Missing relevant protocols, localizers or source components make the total incomplete.
5. Calculate physical `K = sum(K0_i * B_i)` separately from occupancy-weighted kerma. A ROI's occupancy and its exposure convention are separate inputs, not the workload factor. Apply occupancy once and require a goal in the same quantity/interval, distinguishing an occupied-person goal from an already occupancy-adjusted field limit.
6. Show each scenario's physical/occupied values, goal, utilization, margin and **Pass / Fail / Not evaluated** for the selected numeric criterion. A pass requires valid, complete contributions and compatible inputs; zero occupancy is not an intrinsic-safe verdict. Missing inputs, unsupported models and underflow retain explicit flags and a nullable verdict. Air kerma is not automatically patient dose, effective dose or mSv.
7. Keep best/worst outcomes tied to declared scenario assumptions. Distance annotations alone do not establish inverse-square applicability, source intensity, spectrum validity or shielding continuity. A qualified medical physicist must review real source/material applicability and facility decisions.

### Feature 5: save results and compare them later

1. Store immutable, versioned calculation snapshots with a stable result ID, timestamp, scenario name and project/input fingerprint. Recalculating creates a new result; editing inputs marks the current result stale while retaining its historical snapshot.
2. Save source/protocol/model IDs and citations, units/time basis, normalization/workload, source and ROI coordinates, distance, path entry/exit segments, barrier states/materials/thicknesses, fit IDs and actual coefficients, `K0`, `logB`, `B`, per-contribution kerma, totals, occupancy/goal conventions, scenarios, missing inputs, approximations, solver settings and software/dataset versions.
3. Provide a results history and side-by-side ROI comparison for shielding off/on and best/nominal/worst runs. Show absolute differences and ratios/percent changes only with valid nonzero denominators. Clearly distinguish changed coordinates, source models, workload, occupancy, goals, datasets or units from a matched shielding-only comparison.
4. Persist history portably in native project JSON with bounded record sizes/counts and explicit schema migration. Support Windows result-file export/import and WebGL JSON download/import, persistent filesystem synchronization and actual reload. Publish limits before implementation; never silently drop snapshots on save.
5. Keep native CT calculations/results separate from the current canonical LINAC contract. Unsupported canonical export/QA must identify the affected records and preserve the native design, not omit CT points, overrides or results without warning.

### Feature 6: scatter-to-ROI line and centimetre label

1. Draw a selectable measurement line from the chosen effective scatter/beam-target point to the chosen ROI point. Multiple ROIs retain explicit source pairing; show the selected pair clearly in both existing 2D and 3D views.
2. Calculate full 3D Euclidean distance from authoritative point coordinates: `distanceCm = 100 * metersPerUnityUnit * length(roiWorld - scatterWorld)`. Store the unit conversion explicitly. A 2D projected line must still identify that its label is the full 3D distance when elevations differ.
3. Place a numeric **cm** label above the line's projected midpoint, with readable text, no panel overlap and stable placement across zoom, camera movement and desktop/mobile WebGL sizes. Display rounding must not change calculations or saved coordinates.
4. Update the line/label after point or owner transforms, height changes, pair selection, undo/redo and load. Missing/deleted points remove or flag the measurement; a valid coincident pair displays zero distance but blocks a source model that requires positive separation.
5. The line is a measurement/path overlay, not a simulated beam or shielding object. Provide contribution inspection for the actual geometry path and intersections; the illustration must not imply a validated photon-transport simulation.

### Shared calculation, geometry and compatibility requirements

- Use a deterministic scientific service independent of animation/scene rendering. Resolve finite source-to-ROI segments against supported closed solids with explicit metre tolerances, source/ROI-inside handling, overlap detection, entry/exit audit data and intersections clipped before the ROI. Oblique `normalThickness / abs(dot(ray, normal))` is a declared full-slab approximation only; do not clamp grazing angles or ignore finite edges/openings.
- Scalar scatter fields require a supported effective-source/path approximation or path-resolved contributions before one barrier transmission can be applied. Unsupported doors, openings, custom/joined geometry and mixed layers remain explicit errors until independently implemented and tested; existing Request 5 exclusions do not disappear because CT mode is added.
- Extend native version 2 with optional typed CT source/point/scenario/result records, dataset references and wall-toggle metadata only where backward-compatible representation is demonstrated. Use presence flags/nullability for unknown scientific values. Do not deserialize missing workload, occupancy, goals or calibration to zero/default clinical values. If compatibility cannot be maintained, document a version/migration decision before code changes.
- Validate unique IDs, references, roles, finite/nonnegative values, quantities, intervals, fit families and schema versions before replacing a design. Bound stored records and calculation work; cancelled/stale jobs cannot overwrite a newer design or saved comparison.
- Invalidate dependent current calculations when source normalization, protocol/workload, point/owner geometry, material, installed thickness, shielding state, door state, occupancy or goal changes. Reuse existing focus/modal guards, locks, history and asynchronous/file ownership rules.

### Implementation sequence and ownership

| Stage | Needed steps and nearest existing owner | Exit condition |
| --- | --- | --- |
| 1. Resolve contracts | Confirm tt/CT scope, scatter-anchor/source definition, ROI/patient roles, catalogue migration, required external inputs and result limits; extend [ProjectData.cs](LinacRoomStudio/Assets/Scripts/ProjectData.cs) with validated optional records. | Accepted CT contract; v1/v2/no-CT imports preserve existing behavior and unknown source fields. |
| 2. Register asset and points | Reuse [BuildStudio.cs](LinacRoomStudio/Assets/Editor/BuildStudio.cs), [EquipmentPalette.cs](LinacRoomStudio/Assets/Scripts/EquipmentPalette.cs), [StudioApp.cs](LinacRoomStudio/Assets/Scripts/StudioApp.cs), selection and clipboard owners for Dot and typed point tools. | Windows/WebGL point rendering, calibrated ownership, transforms, protection, history and native roundtrip checks pass. |
| 3. Import catalogue and math | Add the separate immutable CT/primary datasets and pure scientific services; connect validated source-normalization and geometry providers without replacing [ReferenceQa.cs](LinacRoomStudio/Assets/Scripts/ReferenceQa.cs). | Reference benchmarks, fit selection, units, log-domain arithmetic, source and finite-path checks pass. |
| 4. Add scenarios and overlays | Add wall shielding toggles, ROI workload/occupancy/goal controls, best/nominal/worst evaluation and centimetre measurements in existing Object/results views. | Matched shielding-off/on results and scenario verdicts are correct; independent physical field and exact distance are verified. |
| 5. Persist and compare | Reuse native file/history owners and [BrowserBridge.cs](LinacRoomStudio/Assets/Scripts/BrowserBridge.cs) for immutable snapshots, comparisons, exports and browser saves. | Portable native/results files, old-design migration, actual browser reload/download/reimport and stale-result guards pass. |
| 6. Verify and deliver | Run focused scientific/editor checks, existing LINAC/authoring regressions, fresh Windows/WebGL builds and interaction tests; refresh/hash-check `web/` and update this handoff. | Exact build identity and evidence recorded; every implemented feature's platform acceptance established. No public deployment is implied. |

These are logical responsibilities, not a requirement for a new file for every service. Keep edits local to existing owners and add pure CT modules only where the calculation/data boundary warrants them. Do not mark Request 5 or 6 complete from shared build success alone.

### Feature progress checklist

Checked entries below record completed supported implementation and existing editor/compiled runtime evidence, not blanket interactive or clinical acceptance. Unfinished acceptance work is unchecked and labeled `In progress`. The scoped authorization at the top explains the later recorded checks; this documentation update does not execute them again.

- [x] **R6.0 - Scope and contracts:** CT-scanner-only scope confirmed; only model ID `CT` can own a CT source. Explicit scatter/ROI/patient roles, reviewed anchors/providers, missing-input flags, one-metre geometry, native version-2 extension and distinct `PlateGlass`/legacy lead-glass contracts are implemented and checked. Real scanner calibration/review remains external.
- [x] **R6.1 - Dot asset:** supplied GLB imported/registered with original bytes/hash and stable `Dot` ID; dimensions, pivot/display normalization and complete mesh are documented. Final Windows/WebGL CT runtime records pass Dot mesh/annotation/collider checks on both targets.
- [x] **R6.2 - Point placement implementation:** CT-owned scatter points and independent ROI/patient tools, source pairing, anchor transforms, protection, copy/delete guards, history and native records are implemented. Full interactive acceptance remains with R6.10/R6.11.
- [x] **R6.3 - Shielding toggle:** independent saved wall states and current/walls-off/all-off scenarios are implemented; old files default enabled. Restoration, matched calculations, history and explicit legacy-QA/export rejection are checked.
- [x] **R6.4 - Material catalogue:** six reference authoring materials and separate primary archive are imported. `PlateGlass` is distinct from retained legacy lead glass; existing LINAC coefficients are unchanged and unsupported substitutions reject explicitly.
- [x] **R6.5 - CT coefficients and arithmetic:** four exact CT fits, stable double/log-domain math, all 20 forward benchmarks, inverse/domain/underflow checks and unsupported-selection guards are implemented. The IL2CPP unit-conversion precision defect is fixed.
- [x] **R6.6 - Supported source and path providers:** direct weekly ROI fields and reviewed isotropic reference-exam models, component coverage, finite homogeneous paths and explicit composite/opening/custom-geometry rejection are implemented and checked. Native spatial-map/DLP/composite engines are outside the supported delivery.
- [x] **R6.7 - ROI workloads and scenarios:** declared best/nominal/worst factors, independent occupancy/goal conventions, physical/occupied outputs and nullable verdicts are implemented; scaling, completeness and zero/missing-input cases are checked.
- [x] **R6.8 - Saved comparison implementation:** immutable hashed snapshots, input provenance, matched comparisons, stale indicators and native/result JSON import/export are implemented; incomplete results and undefined ratios are guarded. Actual browser file acceptance remains R6.10.
- [x] **R6.9 - Distance overlay implementation:** selectable scatter-to-ROI lines and full 3D double-precision cm labels update with transforms/history/load. The active pair is highlighted, only its label is shown, and projected distances with elevation differences are identified as 3D. Pure phone/tablet/desktop layout and exact distance tests pass; actual visual/touch acceptance remains R6.11.
- [ ] **R6.10 - In progress: interactive persistence acceptance:** native compatibility, file helpers, acknowledged filesystem sync, pre-read CT limits and eight bridge units pass; final Windows/WebGL CT runtime records also pass internal native/results-file and guard/history checks. Complete final-build browser reload/IndexedDB/download/reimport and end-user guard/cancellation acceptance remain unclosed. Existing result files alone do not prove these browser workflows.
- [ ] **R6.11 - In progress: remaining platform acceptance:** final Windows/WebGL builds, editor/reference checks, compiled CT runtime checks, browser-worker checks and the 29-file local delivery receipt are recorded. Comprehensive desktop/mobile visual, actual-touch and file/control interaction acceptance remains unfinished. The latest recorded revision is `82c62f8040ea4defbe67f58a17ebfa09`; no facility-design approval is claimed and this documentation update runs no new verification.

### Required verification scenarios

| Scenario | Required result before the corresponding feature is checked |
| --- | --- |
| Asset and device-role selection | Dot is visible/selectable in both views/targets; only confirmed supported device IDs get CT scatter configuration. CathLab, Dental OPG, LINAC and other palette neighbours do not acquire it merely by group membership. Marker-only placement leaves existing QA geometry invariant. |
| Anchors and independent ROI | A reviewed local anchor follows machine translation/rotation exactly; independent ROI positions do not move. Invalid/provisional anchors cannot produce a complete result. Lock, copy, delete, undo/redo and native reload preserve intended roles and associations. |
| Distance and units | A 1 m separation displays 100 cm; a 3-4-5 m spatial offset displays 500 cm, including height in 2D. 0.002 m of lead becomes 2 mm exactly once. Camera/GLB display changes cannot silently alter scientific geometry. |
| CT data selection | All four exact triples match the table above; archived primary data retain 146 provided/10 missing triples and negative-beta wood. CT steel/glass/gypsum/wood, 80/100/130 kVp and primary-family substitutions return unsupported statuses. |
| Scientific arithmetic | Pass all 20 reference forward benchmarks at documented arithmetic tolerances (reference examples: `1e-10` relative or `1e-12` absolute), boundary/monotonicity/inverse checks and finite log transmission at large thickness. Underflow is flagged, not reported as perfect shielding. |
| Shielding baseline | The same valid source/ROI/workload inputs produce `B=1` for a genuinely unshielded path; re-enabling a 2 mm lead path at 120 kVp produces `B=0.001239990179488295`. Other credited barriers remain accounted for; toggling never destroys geometry or material settings. |
| Reference example and source scaling | The reference's DEMO_ONLY 120-kVp, 2-mm-lead example reproduces physical `0.005511067464392423 mGy/week` and occupied `0.001377766866098106 mGy/week`. Do not turn its source, workload, occupancy or goal into project defaults. Valid inverse-square reference models divide K0 by four when distance doubles; direct weekly ROI fields receive no second scaling. |
| Workload, occupancy and verdicts | Doubling valid workload doubles physical/occupied kerma, not B. Changing occupancy affects only design weighting. Best/worst cases use declared inputs; missing contributions, missing goals, zero occupancy, incompatible units and unsupported spectra cannot yield a complete pass. |
| Finite shielding paths | Test no hit, source/ROI inside, intersections beyond ROI, finite-edge escape, tangent/ambiguous geometry, duplicate/overlapping solids and openings. Homogeneous contiguous layers use B(total); unsupported composite or joined geometry is not flattened or multiplied silently. |
| Immutable history and comparison | Save off/on and best/worst runs, then edit/reload/recalculate without changing old results. Matched comparison isolates shielding; other changed inputs are shown. Zero baselines suppress undefined ratios and exported records retain all inputs/statuses. |
| Platform persistence and interaction | Windows works after the original Dot/source data paths move; packaged resources and native data are portable. Actual hosted WebGL reload, filesystem sync, JSON download/reimport, numeric focus, modal/lock/history guards and late-job cancellation pass on the new build. |
| Regression and delivery | Existing 17 reference benchmarks and relevant adapter/workspace/authoring suites remain unchanged/passing. Fresh CT editor, Windows and compiled WebGL checks plus actual overlays/file interaction pass; exact tested build and refreshed web hashes are recorded. |

### Progress and evidence record

Planning entry, 2026-10-01: the supplied Dot file and desktop CT specification are available; the source asset hash and current CT/model/data boundaries were checked. This handoff now contains the requested feature designs, implementation stages and per-feature checklist. No Unity resource import, scientific implementation, build, screenshot, smoke test or deployment was performed for Request 6.

Implementation checkpoint, 2026-10-01: [CtShieldMath.cs](LinacRoomStudio/Assets/Scripts/CtShieldMath.cs), [CtCoefficientLibrary.cs](LinacRoomStudio/Assets/Scripts/CtCoefficientLibrary.cs), [CtShieldingData.cs](LinacRoomStudio/Assets/Scripts/CtShieldingData.cs), [CtShieldingCalculation.cs](LinacRoomStudio/Assets/Scripts/CtShieldingCalculation.cs) and [StudioCt.cs](LinacRoomStudio/Assets/Scripts/StudioCt.cs) implement the supported CT workflow. [Import-CtReference.ps1](LinacRoomStudio/Tools/Import-CtReference.ps1) imports marked structured reference data reproducibly. Dataset/resource JSON is separate from native source/point/result metadata. Result files use `RoomStudio.CT.Results` version 1 and preserve input/record hashes, quantities, intervals, per-contribution paths and limitations. Hashes detect accidental changes, not authenticity or clinical approval.

Current limits: 16 CT sources, 64 calculation points, 50 saved results, 512 KiB per frozen input snapshot and 16 MiB per result file. The UI offers direct unshielded weekly ROI fields and explicitly reviewed isotropic reference mGy/exam + exams/week models. Direct fields bind to ROI and scanner/protocol conditions; moving/changing them requires new field confirmation. Spatial/DLP providers can supply validated direct ROI inputs, but native map interpolation, DLP conversion, arbitrary source transport and multilayer solving are not implemented. Floor/ceiling/wall disabled-credit states are explicit; all-off differs from walls-off. Existing doors/joined/custom geometry retain rejection boundaries.

Initial checkpoint evidence, superseded by the follow-up at the top: `ROOM_STUDIO_CT_MATH_CHECKS_PASSED` (120), `ROOM_STUDIO_CT_DATA_CHECKS_PASSED` (59), Dot/editor/build markers, Windows assembly SHA-256 `a38e36e5fb1617a5eca464d8b8403097147686d333fa3b2eeae5918300842d76`, WebGL revision `b17645fec90c4312a2919ae5047b698f` and Wasm SHA-256 `fe55a524d62ea2452d0ea5465fe6b0c01e045730790b82e9a2ea90ed72d312f5`. Local delivery is now complete at the newer revision/hashes above; actual browser persistence and visual/touch acceptance remain `In progress`.

Historical runtime/browser increment, before the prohibition: `ROOM_STUDIO_CT_RUNTIME_CHECKS_PASSED`, `ROOM_STUDIO_CT_PLAYER_SMOKE_PASSED` and corrected `ROOM_STUDIO_CT_BROWSER_SMOKE_PASSED` were observed; the corrected browser returned no console errors. These covered marker/shader/collider, 3D distance, source ownership, focus/protection/history, scenario snapshots, internal native/result-file round trips and dependent deletion, not full interactive browser persistence acceptance. All 33 browser-worker checks also passed. The initial WebGL precision failure was resolved by explicit double conversion. Preserve this evidence but do not repeat these smoke workflows or capture/upload screenshots.

User restriction/status update, 2026-10-01: screenshot capture/upload and smoke-test execution are prohibited in every project and recorded in global persistent memory. Further implementation verification was stopped at the user's instruction; this update changes documentation/status only. Completed supported implementation is checked above; unfinished acceptance/delivery remains `In progress`. Prior evidence is historical, not authorization for prohibited actions.

Earlier implementation/delivery follow-up, 2026-10-01; R6.8/R6.9 implementation `Complete`, R6.10/R6.11 platform acceptance `In progress`: changed `StudioApp`, `StudioPrecision`, `StudioShortcuts`, `StudioCt`, `CtShieldMath`, `CtShieldingChecks`, `BrowserBridge` and `RoomStudio.jslib`. Permitted checks pass: 120 math/76 data assertions, viewport geometry, eight isolated bridge units, all editor gates and reference regressions. Windows/WebGL builds and 29-file delivery verification pass at that checkpoint's revision/hashes in the prior-output section above. Source fixes include incomplete-input guards, compact controls, active/full-3D measurements and storage acknowledgments/errors. No prohibited workflow or public deployment was performed at that checkpoint. Actual persistence/layout/control acceptance and external science review remain open.

Documentation reconciliation, 2026-10-01; R6.0-R6.9 supported implementation `Complete`, R6.10/R6.11 acceptance `In progress`: changed status documentation only. Existing [final Windows CT runtime](LinacRoomStudio/Logs/req5-req6-acceptance-windows-ct-final-2026-10-01.log) records `ROOM_STUDIO_CT_PLAYER_SMOKE_PASSED`; [final compiled WebGL runtime](LinacRoomStudio/work/req56-acceptance/webgl-feature-results.json) records `ROOM_STUDIO_CT_RUNTIME_CHECKS_PASSED` and `ROOM_STUDIO_WEB_FEATURE_CHECKS_PASSED`; the [browser-worker record](LinacRoomStudio/work/req56-acceptance/browser-workers.txt) reports all checks passed. These checks belong to the earlier scoped authorization; none was rerun for this update. Latest recorded revision, hashes and 29-file delivery receipt are linked in the current status reconciliation. Complete final-build browser reload/file exchange and comprehensive control/visual/touch acceptance remain open; scientific applicability review remains external.

For each future increment append: **date; R6 checklist IDs; status (In progress/Complete); changed owners; permitted check and result marker; evidence path; platform/build identity; unresolved limitations**. Record science/source review separately from software arithmetic tests. Do not reuse Request 5 or older runtime logs as Request 6 acceptance evidence, and do not run screenshot/smoke workflows.

Acceptance condition: after device scope is confirmed, Windows and WebGL users can place the supplied dot as a supported CT effective scatter point or independent ROI/patient marker, inspect the full 3D scatter-to-ROI distance in cm, calculate supported air-kerma scenarios with shielding off/on and declared workload/occupancy/goal inputs, and save/reload/export/compare immutable results without silent geometry, normalization or legacy-QA changes. Missing scientific inputs remain explicit and never become a pass. Mark Request 6 `Complete` only after its feature checklist and fresh platform evidence are recorded; a clinical/regulatory design conclusion additionally requires qualified physics review.

## Request 7: proportional scaling, direct placement, wall editing and room reset

Added: 2026-10-01. Status: `In progress` for platform acceptance; supported R7.1-R7.6 implementation and local delivery are `Complete`. The dated record below tracks each implementation/check increment. No screenshots were captured/shared and no player/browser smoke workflows ran. Permitted numerical/editor, compilation/build and file-integrity checks establish only their stated coverage; interactive acceptance remains separate. Request 5/6 acceptance and clinical-review boundaries are unchanged.

- [x] **R7.1 - Equipment scaling handle implementation:** a selectable corner bubble uses one uniform scale, preserves position/angle, rejects protected equipment and calculation markers, and commits/cancels through existing history. Editor numerical checks pass; visual/interactive acceptance remains unverified.
- [x] **R7.2 - Default click-to-place implementation:** clicking a saved preset arms room-click placement directly, with the explicit Place button retained. Independent identity/exact-position/preset-nonmutation editor checks pass; actual click/touch acceptance remains unverified.
- [x] **R7.3 - Door fitting implementation:** only oversized placement requests shrink to 90% of wall length; ordinary/equal-width requests are retained and still geometrically validated. Short-wall fitting, portable data, finite jambs, overlap and protection editor checks pass; actual click placement acceptance remains unverified.
- [x] **R7.4 - Shift angle snapping implementation:** drawing and endpoint gestures use the same live nearest-45-degree direction constraint, including captured release modifiers and free release. Editor direction/length/elevation/both-anchor checks pass; actual modifier interaction remains unverified.
- [x] **R7.5 - Keyboard scaling implementation:** plus/minus, keypad and Shift-plus use the configured dimensionless scale increment through guarded history; all selected members validate before mutation. Editor key-mapping/atomic-selection/bounds checks pass; actual key delivery remains unverified.
- [x] **R7.6 - Reset implementation:** Project > Reset confirms and clears all room contents/associated guide/generation/CT data in one undoable command, including protected contents. Room/shielding preferences, library and saved files are retained; data checks and all target compilation paths pass. Actual confirmation/history/late-callback interaction remains unverified.

- [x] **R7.7 - Combined verification/local delivery:** both final builds repeat the complete editor gate; all four source compilations pass; all 29 `web/` payload hashes match; exact output identities and static preview delivery are recorded. No screenshots, smoke workflows or public deployment; actual platform interaction remains a separate open gate.

### Step-by-step implementation record

2026-10-01; R7.1; `In progress`: confirmed rendering already uses one uniform `Item.scale`, bounded to 0.05-3, and wall handles use queued pointer events with atomic history. Added pure projected-drag scale math and equipment-only/protection checks in `PrecisionEditing.cs`, with focused fixtures in `PrecisionChecks.cs`. Next: compile, connect the corner bubble to both views, then run the numerical/editor checks. No build or interactive acceptance is claimed at this checkpoint.

2026-10-01; R7.1; `In progress`: Editor/Windows/WebGL and editor-check compilation passed with zero diagnostics using `work/req5/compile.ps1`. Added a bounded 24-pixel corner bubble using equipment selection bounds in both views, pointer-release commit and Escape/F1 cancellation through `StudioPrecision`, `StudioApp` and `StudioShortcuts`. Calculation markers remain excluded and camera changes are suppressed during scale drags. Numerical/editor execution is next; no screenshots, smoke tests or new delivered binaries are claimed.

2026-10-01; R7.1; implementation `Complete`, interactive acceptance `In progress`: `RoomStudio.PrecisionChecks.Run` passed `ROOM_STUDIO_EQUIPMENT_SCALE_CHECKS_PASSED` and the existing viewport/precision/clipboard markers in `Logs/req7-precision-step1-2026-10-01.log`. The initial marker fixture was corrected to use required version-1 CT metadata before the passing rerun. Checked R7.1 means supported implementation/numerical coverage only, not pointer/visual acceptance in a player.

2026-10-01; R7.2; `In progress`: component preset selection now arms the existing single-click placement path directly, including returning compact screens to the scene; the explicit Place button remains a fallback. Existing equipment palette click placement is retained. Added independent-ID, exact-position and preset-nonmutation fixtures to `ComponentChecks.Run`. Next: execute the focused editor component checks. No screenshots or smoke tests are authorized or performed.

2026-10-01; R7.2; implementation `Complete`, interactive acceptance `In progress`: `ComponentChecks.Run` passed `ROOM_STUDIO_COMPONENT_CHECKS_PASSED` in `Logs/req7-components-step2-2026-10-01.log`, including repeated independent placements for every default/custom fixture. This validates placement data/geometry and compilation, not actual pointer/touch delivery.

2026-10-01; R7.3; `In progress`: `DoorGeometry.Add` fits only widths greater than wall length to 90%, cloning the request and centering it within the available span. Short-wall jamb clearance is `min(0.1 m, 5% of length)` and minimum editable width is `min(0.5 m, 50% of length)`, permitting finite openings on supported 0.25 m walls. Ordinary requests/equal widths are not resized; ordinary geometric rejection, overlap, locks, room-resize guards and unsupported aperture QA/export boundaries remain. Added focused 0.25/0.5/0.8/2 m fixtures and portable roundtrips; checks are next.

2026-10-01; R7.3; implementation `Complete`, interactive acceptance `In progress`: `RoomStudio.DoorChecks.Run` passed `ROOM_STUDIO_SHORT_WALL_DOOR_CHECKS_PASSED` and `ROOM_STUDIO_DOOR_GEOMETRY_CHECKS_PASSED` in `Logs/req7-doors-step3-2026-10-01.log`. Existing aperture/scientific exclusions remain in force.

2026-10-01; R7.4; `In progress`: added one pure nearest-45-degree direction constraint in `PrecisionEditing`; drawing preview/click commit and endpoint drags/release use it through `StudioApp`/`StudioPrecision`. Queued events retain Shift state, and Shift-click can start an endpoint gesture instead of being consumed as multi-selection. While held, angle constraint takes priority over grid/wall targets, preserves planar pointer length/fixed anchor and shows its actual target; releasing restores ordinary assists. Focus/history/protection and existing R/Shift+R rotation remain unchanged. Focused numerical execution is next.

2026-10-01; R7.4; implementation `Complete`, interactive acceptance `In progress`: `RoomStudio.PrecisionChecks.Run` passed `ROOM_STUDIO_WALL_ANGLE_CHECKS_PASSED`, equipment scaling and existing precision/clipboard/layout checks in `Logs/req7-precision-step4-2026-10-01.log`. Both moving-end choices preserve the opposite anchor within 0.0001 m.

2026-10-01; R7.5; `In progress`: added plus/minus, keypad and Shift-plus mapping to guarded `StudioShortcuts` dispatch, using the saved dimensionless `scaleStep` (default 0.01). `PrecisionEditing.ScaleEquipment` validates every selected member before any mutation, clamps at 0.05-3 and preserves all positions/proportions; mixed, invalid or protected selections reject atomically. Text/modal/drawing/gesture guards and Ctrl/browser zoom combinations are retained. F1 help includes the new scale/angle bindings. Focused numerical key-mapping/selection execution is next.

2026-10-01; R7.5; implementation `Complete`, interactive acceptance `In progress`: `RoomStudio.PrecisionChecks.Run` passed `ROOM_STUDIO_EQUIPMENT_SCALE_SHORTCUT_CHECKS_PASSED` and all existing precision/equipment/angle/layout/clipboard checks in `Logs/req7-precision-step5-2026-10-01.log`. Text/modal dispatch is compiled but actual Windows/WebGL key delivery is not claimed.

2026-10-01; R7.6; `In progress`: `Design.ResetContents` builds and validates a detached empty design, clearing objects (including locked items/doors/points/components), regions, guide, generations/junctions, CT configuration/results and source-import links while preserving room dimensions, floor/ceiling settings and precision preferences. Imported shells below native minima expand to 3 x 3 x 2 m when their canonical link is removed; ordinary dimensions are unchanged. Added focused reset/portability/old-state preservation fixtures in `SelectionChecks.ResetChecks`. Next: run those checks, then connect a confirmed atomic UI reset with stale-work cancellation. Saved files and the reusable component library are outside the reset.

2026-10-01; R7.6; `In progress`: `SelectionChecks.ResetChecks` passed `ROOM_STUDIO_RESET_CHECKS_PASSED` in `Logs/req7-reset-step6-data-2026-10-01.log`. Added Project > Reset with a responsive confirmation, explicit protected/history scope and imported-minimum notice. Confirmation clears pending room imports/QA/CT imports, authoring/generation previews and transient selection/result state before one history commit; late task callbacks have no active mutation path. Escape/Cancel preserve the design. Reset blocks scene/UI/shortcut/camera input, and equipment scale gestures now also block arrow-key camera walking. Target compilation and final combined editor checks are next; actual UI/cancellation acceptance remains unverified.

2026-10-01; R7.6; implementation `Complete`, interactive acceptance `In progress`: all four `work/req5/compile.ps1` compilation paths passed with zero diagnostics after the reset UI/guard increment. R7.1-R7.6 supported implementation is now checked separately from acceptance. R7.7 delivery is `In progress`. A final role compatibility adjustment includes imported visual/calculation equipment kind `Source` in the same scaling contract; it does not change its model ID, source role, physics or calibrated coordinates. Focused precision checks precede combined editor/build gates.

2026-10-01; R7.7; `In progress`: final imported-Source scaling checks passed in `Logs/req7-precision-final-2026-10-01.log`; all touched code files have clean diagnostics. Reset confirmation uses a scrollable body with fixed action row. Initial Windows/WebGL builds passed the complete editor gate, including reset/door/equipment/angle/key checks and existing CT/authoring/component/native checks.

2026-10-01; R7.2/R7.7; `In progress`: final placement-route review found CT annotation picking ran before placement tools and could intercept their target click. Restricted annotation picking to Select mode in `StudioPrecision`, preserving annotation selection while placement goes directly to the existing tool. This adjacent control-flow correction requires fresh final compilation/builds; the preceding Windows output is an interim checkpoint. The existing local preview listens at 127.0.0.1:8097; no browser/player was opened.

2026-10-01; R7.2/R7.7; `In progress`: the placement-route guard passes all four compilation paths with zero diagnostics in `Logs/req7-compilation-final-2026-10-01.log`; focused VS Code diagnostics are also clean. Updated the root README and WebGL status without removing existing concurrent Req5/Req6 reconciliation. Final builds/hash receipt remain pending while the earlier in-flight WebGL build finishes. No interactive or screenshot evidence is implied.

2026-10-01; R7.7; local delivery `Complete`, platform acceptance `In progress`: rebuilt the final placement-corrected source sequentially with `BuildStudio.Build` and `BuildStudio.BuildWebGL`. Both complete editor gates and `ROOM_STUDIO_BUILD_SUCCESS` / `ROOM_STUDIO_WEBGL_BUILD_SUCCESS` pass in the final Request 7 logs. Revision `9c50d416fa804b609797d9af185b650a`, Windows/Wasm hashes and receipts are recorded above. `Refresh-WebPackage.ps1` reports `ROOM_STUDIO_WEB_PACKAGE_VERIFIED: 29 matching SHA-256 hashes`; current package manifest is refreshed, with deployment configuration/README preserved. Static requests pass with the final revision and correct MIME types; no JavaScript/player execution, screenshots, smoke workflows or public deployment were performed.

### Remaining platform acceptance

- [ ] **In progress:** equipment bubble visibility/proportional pointer scaling in 2D/3D, protection/groups, camera motion, release, Escape and undo/redo.
- [ ] **In progress:** actual preset select/room-click and touch placement, plus short-wall door placement/material controls on both targets.
- [ ] **In progress:** live Shift press/release while drawing/resizing, plus/minus/keypad delivery and text/modal/browser-reserved key guards.
- [ ] **In progress:** Reset confirmation/cancel, one-step undo/redo with topology/results/protection restored, and actual late-import/detection callback behavior.
- [ ] **In progress:** current-build browser persistence/reload/file exchange and complete desktop/mobile/touch control acceptance. Builds/hashes/editor evidence do not establish these outcomes. No screenshots, smoke workflows or clinical/facility approval are implied.
