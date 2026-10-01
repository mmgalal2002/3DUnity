# LINAC Room Studio — WebGL

Extract the entire archive. With Python installed, run this command from the extracted folder:

```sh
python serve-webgl.py --directory .
```

Open http://127.0.0.1:8080/. Keep the server running while using the app.

For static web hosting, upload the complete folder. Serve `.mjs` and `.js` as `application/javascript`, `.wasm` as `application/wasm`. Open over HTTP/HTTPS rather than double-clicking index.html. Calculation runs locally in a browser worker.

Use **Link walls to room size** to resize walls with the floor and ceiling. Linked wall-height edits update the shared ceiling. Each design has one room/ceiling. Equipment and occupied regions retain their positions.

**Calculate reference QA** remains below the properties panel; results open in a popup with Recalculate, Export QA JSON and Close.

On smaller screens, use **Scene**, **Build** and **Properties** to switch between the scene and scrollable controls. Selecting a placement tool returns to the scene. Calculation controls are inside Properties on compact screens.

The **CT** properties tab handles supported CT scanner sources, scatter/ROI/patient points, air-kerma scenarios and saved comparisons. Required scanner calibration, source strength, occupancy conventions and design goals are supplied and reviewed externally. Only CT-secondary lead/concrete fits at exactly 120/140 kVp are supported; air kerma is not patient dose or clinical approval.

Placing an ROI/patient point opens CT settings. If no source is configured, **Configure \<scanner label\>** creates its source and attached scatter marker; **Place CT scanner** is offered if the room has none. Choose **120 kVp / 140 kVp** at the top of CT settings, then supply scanner/protocol provenance, reviewed anchor/component coverage and either a validated **ROI weekly field** or **Reference / exam** normalization. Energy alone cannot determine absolute kerma. Existing protected points are not automatically linked to a new source.

Point properties expose **CT source / energy settings** before position fields. Results dialogs offer **Configure CT source / energy** above the scroll area and **Edit inputs for \<point name\>** to return to current inputs. Historical results are retained unchanged. Without a source, the calculation panel offers setup instead of saving an empty-source scenario.

**Save design** downloads portable native JSON and waits for browser storage synchronization before reporting persistence success. A browser storage error is visible; retain the downloaded JSON before reloading. CT result JSON is limited to 16 MiB and 50 stored records. Full native project JSON preserves generation data and CT history; unsupported joined geometry or CT records cannot be silently flattened into canonical LINAC export.

Only the active ROI pair has a distance label; its line is highlighted while other measurements remain selectable. In plan view, **(3D)** identifies a full spatial distance when elevations differ. Measurements and Dot markers are annotations, not shielding solids.

Project source and future development notes: the repository's `unity/PROJECT_HANDOFF.md`.
