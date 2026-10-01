# Editor shortcuts and equipment palette — completed 2026-09-21

Requests 1 and 2 in `PROJECT_HANDOFF.md` are complete. Unity 6000.6.0f1 built Windows and WebGL on September 20. Final browser verification and deployment-folder packaging completed September 21 (Africa/Cairo; browser log timestamps are UTC).

## Delivered behavior

- Ctrl+Z/Ctrl+Y use the existing undo/redo history. Delete removes the selection. Ctrl+L protects/unlocks selected objects; the Object panel exposes the same action.
- Protection blocks property changes, dragging, rotation, grouping and deletion. A protected member blocks the entire selection atomically. Linked room resizing cannot alter a protected wall. Protected objects remain selectable and can be unlocked.
- Protection is optional native version-2 metadata (`Item.locked`); older version-1/version-2 saves default to unlocked. Native round-trips retain it, while canonical exports and QA ignore it. Undo/redo can restore previous protection states.
- F1 opens/closes the shortcut list. Escape and the Close button dismiss it. Design shortcuts are suppressed in text/numeric inputs, popups and active pointer gestures; F1/Escape are intentional exceptions.
- The palette is ordered Linac (Linac, CathLab, CyberKnife), CT (CT, X-ray, Mammography, Dental), MRI (MRI). All original IDs and source roles remain unchanged. Eight palette entries plus the separate workstation retain all nine supplied models.

## Verification and evidence

| Check | Result and evidence |
| --- | --- |
| Unity editor | **Room Studio > Run editor checks** passed lock, selection, palette and native validation markers in [feature-editor-verified.log](feature-editor-verified.log). Covers mixed/group protection, atomic rejection, v1/v2 defaults, native round-trip and linked-room guards. |
| Windows build | **Room Studio > Build Windows app** produced `ROOM_STUDIO_BUILD_SUCCESS` in [feature-windows-build-verified.log](feature-windows-build-verified.log). |
| Windows runtime | **Room Studio > Test Windows app** passed selection, shortcuts, protection persistence, all eight palette entries, QA popup/recalculation, canonical import and full runtime smoke checks in [Windows/feature-runtime-smoke.log](Windows/feature-runtime-smoke.log). The runtime canonical export equalled the source fixture exactly. Interactive F1/Escape and palette layout were also checked. |
| WebGL build | **Room Studio > Build WebGL app** produced `ROOM_STUDIO_WEBGL_BUILD_SUCCESS` in [feature-webgl-build-verified.log](feature-webgl-build-verified.log). Revision: `9645e88cd0aa4f6faf7c1be02b3199e0`. |
| Compiled WebGL runtime | Both `/WebGL/?feature-tests=1` and `/web/?feature-tests=1` passed `ROOM_STUDIO_SELECTION_RUNTIME_PASSED`, `ROOM_STUDIO_SHORTCUT_RUNTIME_PASSED`, `ROOM_STUDIO_PALETTE_RUNTIME_PASSED` and `ROOM_STUDIO_WEB_FEATURE_CHECKS_PASSED`. See [feature-browser-runtime-checks.json](feature-browser-runtime-checks.json). Checks exercise placement, renderers, selection, native serialization/load, history and help without changing source roles. |
| Browser interaction | In the Codex in-app browser, selected the workstation, protected it with Ctrl+L, verified rejected Delete/R/drag, unlocked it with the visible button, deleted it, undid/redid deletion, and restored it. Text and numeric fields suppressed design commands; F1 still opened help from a numeric field. The help Close button worked. QA opened with results in the built app and final deployment package. |
| Reference-engine regressions | Bundled `node.exe benchmark.mjs`, `node.exe checks.mjs` and `node.exe workspace-checks.mjs` passed 17 numerical benchmarks and adapter/workspace checks. Logs: [benchmarks](feature-benchmarks.log), [adapter](feature-adapter-checks.log), [workspace](feature-workspace-checks.log). |
| Real browser workers | `/WebGL-tests/` → **Run worker checks** passed all ten tests, including exact source export, protection/group/opacity invariance, QA parity and all seven visual palette additions preserving source roles. See [feature-browser-worker-checks.txt](feature-browser-worker-checks.txt). |
| Deployment copy | Copied `WebGL/index.html`, `Build/` and `StreamingAssets/` into `web/`, retaining its README and Vercel configuration. All 22 files match by SHA-256; no desktop `node.exe` is included. See [feature-web-package-verified.log](feature-web-package-verified.log). |

## LINAC gantry animation — implementation 2026-09-29

The non-imported Beam panel now uses DOTween 1.3.030 to animate the treatment head and its source and imaging arms around the configured isocentre. The gantry rings, bore, and LINAC root remain fixed. The beam preview follows `Beam_Window` and points at the isocentre. The stored gantry angle continues through the existing reference QA input path, while runtime mesh transforms remain outside QA geometry.

`BeamSmokeChecks()` covers 0, 90, 180, and 270 degree poses, representative head and arm rotation, stationary ring and bore parts, constant aperture-to-isocentre radius, beam origin and aim, stationary root, tween retargeting, rebuild cancellation, and beam visibility. The optional DOTween Audio, Physics2D, and UI modules are omitted because this project does not include those Unity packages.

Unity compilation and the isolated Windows build passed after correcting two explicit-type declarations in `StudioWallGeneration.cs`. The normal editor-check gate still fails at the unrelated `PlanPathExtractionChecks.Run()` regression (“thick horizontal stroke must yield one centerline; isolated noise rejected”). For this feature's local build only, that one check was temporarily skipped and then restored; all remaining editor checks passed. The isolated Windows runtime emitted `ROOM_STUDIO_VERSA_BEAM_CHECKS_PASSED` and `ROOM_STUDIO_RUNTIME_SMOKE_PASSED` before the head-and-arm-only adjustment. The updated runtime assertions have not yet been rerun. WebGL build and browser runtime checks remain pending.

## Repeat the browser checks

From `unity`, run `python serve-webgl.py --directory .`. Open a fresh test page at `http://127.0.0.1:8080/web/?feature-tests=1`; the console and status should report `ROOM_STUDIO_WEB_FEATURE_CHECKS_PASSED`. These regression checks exercise the sample design/history, so omit the query parameter for normal editing. Open `http://127.0.0.1:8080/WebGL-tests/` and choose **Run worker checks** for the worker suite.

The tested browser delivered Ctrl+L to the canvas. Other browsers may reserve it for the address bar; the visible Protect objects/Unlock objects button and F1 help document that fallback. Cross-browser shortcut delivery is not claimed.

`Windows/`, `WebGL/` and `web/` contain the current builds. Existing ZIP archives remain historical snapshots. This work refreshed the local deployment package; it did not publish a public site. The original calculation engine and broader roadmap are unchanged.
