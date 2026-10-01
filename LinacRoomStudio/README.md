# LINAC Room Studio

The source project targets Unity 6000.6.0f1. The September 29, 2026 Windows and WebGL builds include **Linac HD**, **Dental OPG**, CathLab under CT, and the supplied Toilet, Basin, and Chair models under **Room items**. Press **V** to switch between 2D and 3D; **Ctrl+V** still pastes copied objects. They also include short numbered object labels and bounded text inputs. See [WebGL status and instructions](../WEBGL.md).

The current build removes the left palette's Linac HD/Dental OPG role note, shortens the left toggle to **Snap to wall**, and widens both side panels by 50 px. In 3D, holding the arrows smoothly moves the camera's center forward/backward and sideways relative to its heading; in 2D, they keep nudging selected objects. Unity editor checks passed during both builds, and Windows runtime checks passed the camera, shortcut and general workflow checks. Browser interaction was not repeated; see [the handoff](../PROJECT_HANDOFF.md) for build identity and evidence.

## Run
Launch `../Windows/LinacRoomStudio.exe` with its accompanying data folder and DLLs. To develop, open this project in Unity and use **Room Studio > Prepare scene**, then Play. **Room Studio > Build Windows app** rebuilds the sibling Windows folder; **Room Studio > Test Windows app** runs the automated integration test.

## Multi-object editing

Shift/Ctrl-click or drag a box from empty space to select multiple walls and equipment. **Multi-select** in the Object tab provides click-to-add/remove without a modifier key. Drag any selected object to move the selection. **R** rotates it 15 degrees around its center; numeric center and rotation controls support exact values.

**Lock together** (Ctrl+G) saves a group; selecting any member selects all members. **Unlock group** (Ctrl+Shift+G) restores individual editing. **Opacity** changes the appearance of all selected items and does not affect QA. Groups and opacity persist in native saved designs and participate in undo/redo. All numeric fields pair a slider with an exact input. Use the scene list to select fully transparent objects.

The **Lock in place (no movement or editing)** checkbox in Object protects the selected object or group. Locked objects remain selectable and can be unlocked with the same checkbox or Ctrl+L. A locked wall also prevents changes to its door openings. Lock state persists in native saves and supports undo/redo.

**Ctrl+C** copies selected scene objects and their grouped members; **Ctrl+V** pastes unlocked copies with fresh object, group, and door IDs. Each repeated paste shifts another 0.5 m along X and Z. Object also has **Copy / Ctrl+C** and **Paste / Ctrl+V** buttons for browsers that reserve shortcuts. LINACs can be placed and pasted repeatedly like other equipment. The first LINAC remains the calculation and beam-visual source; additional units are layout instances.

## Door openings

Select a wall, switch to the 2D plan, then choose **Click wall to cut / place door** in Object and click the desired point on that wall. The wall is cut at that point and a door panel, frame, and optional lead lining fill the opening. Select the wall to edit the door offset, width, height, panel material, panel thickness, density, and lead lining in millimetres, or remove the door to restore the wall. A wall can hold several non-overlapping doors. Moving or rotating the wall carries its doors with it. Native design saves preserve the openings and their settings.

The current reference engine and canonical ProShield format cannot represent an opening or door. Reference QA and canonical export reject a design with door openings; they do not treat the cut wall as a solid barrier. Door material and lead values are editor inputs, not an assessment that a design provides adequate shielding.

For a rigid vertical move of a selection containing walls, first turn off **Link walls to room size** in Room. Linked room resizing otherwise keeps its existing behavior.

## Import and export a ProShield room
1. Scroll the left panel to **ProShield JSON**.
2. Paste the full path to a canonical room JSON or a workspace containing `room`, then click **Import source JSON**.
3. Edit room geometry, shielding, source placement, workstations or occupied regions. Import validation errors leave the active design intact.
4. **Save design** retains the original source data alongside the editable projection. **Load design** restores both.
5. **Export ProShield JSON** writes `<design-name>-proshield.json` in the saved-files directory. Use **Open saved files** to find it.

Unedited source values retain double precision, optional settings, source components, maze configuration, wrapper metadata and unknown fields. Missing supported optional fields are repaired with a reported summary. Edited supported fields are merged into the preserved source document. Detailed source/beam/maze settings are preserved but do not yet have dedicated controls.

**Calculate reference QA** stays below the properties panel. Results open in a popup with **Recalculate**, **Export QA JSON**, and **Close**. Desktop uses the bundled Node runtime; WebGL uses the same engine in a browser worker. This is an interim reference bridge; the pure C# engine migration remains unfinished.

## Current checkpoint
In the Room panel, **Link walls to room size** links wall positions and lengths to room width/depth and their tops to the ceiling. Editing a linked wall height changes the shared ceiling. Disable the toggle for independent walls. Equipment and occupied regions retain their positions. Each design currently has one shared room/ceiling.

Visual source-room import/export is complete for the supported editor projection. The built app passed import, native save/load, exact JSON round-trip, and reference QA integration. Next: arbitrary source settings, matching beam cones, diagnostic configurations and explicit maze controls/report traces.

See [MIGRATION_STATUS.md](MIGRATION_STATUS.md) for verification and limitations. Thirteen supplied models are included: the LINAC visual uses the supplied Versa HD model under the **Linac HD** palette name, and the Planmeca Viso visual appears under **Dental OPG** in CT. CathLab is in CT; Toilet, Basin, and Chair are in Room items. Additional models placed from the visual-equipment palette do not become calculation sources. Bundled Node licensing is in `Assets/StreamingAssets/ProShield/NODE-LICENSE.txt`.

The CT, CyberKnife, and MRI palette entries use the latest supplied patient-free GLBs. The orange beam illustration starts below the Versa HD `Beam_Window` and follows its moving aperture. The gantry-angle control rotates the treatment head and its source and imaging arms around the configured isocentre; the gantry rings, bore, LINAC root, and reference QA geometry stay fixed.

## CT point-of-interest energy and materials

The core workflow from the version-2 [diagnostic specification](../CT_Point_of_Interest_Shielding_Unity_Specification.md) is implemented as a separate, versioned diagnostic workflow in source and the rebuilt Windows/WebGL players. The right panel has **Room | Object | Calculation**, including compact layouts. The retained treatment branch uses its own engine and MV inputs. Remaining specification coverage is listed below.

1. Place/select a machine and open **Calculation**. Typed family metadata distinguishes CathLab/fluoroscopy, dental modes, mammography, CT and treatment equipment independently of palette groups.
2. Place **Target** at the physical device source reference, **Scatter (patient)** at the patient and **ROI** at the measurement location. Enter an explicit height for 2D placement, or choose a 3D surface. Escape cancels without changing existing points.
3. Select **Tube voltage (kVp)** from the shared diagnostic dropdown; there is no numeric energy input. At the user's request, all diagnostic X-ray families, including OPG and CBCT, now offer the same CT-reference choices: **120 / 140 kVp**. Installed approved model energies extend the common list for every diagnostic family. Source calibration, spectrum, normalization and component coverage still require a matching machine-specific model.
4. Enter only the profile's required exposure inputs. Empty/invalid entries remain distinct from explicit zero. Edit actual wall material/thickness in **Object**.
5. Follow the live guidance and select **Calculate** when ready. The current result is **Air kerma at ROI** in Gy with its exposure/time basis. Details contains the component ledger; weekly totals and numerical comparisons are optional. Changed physical inputs remove stale numbers from the current slot.

Target uses double-precision device-local metres and ignores equipment display scale. Scatter/ROI use independent world coordinates. Machine selection is explicit; multiple machines are not automatically summed. Copying a machine remaps its Target but does not copy or activate exposure inputs. Native version-2 JSON retains the separately versioned diagnostic block, invalid numeric drafts and hashed result snapshots. Canonical export explicitly rejects unsupported diagnostic records instead of discarding them.

### Profiles and current implementation boundary

The attenuation reference comes from [the supplied specification](../CT_Point_of_Interest_Shielding_Unity_Specification.md), not a user-selected JSON file. [Import-CtReference.ps1](Tools/Import-CtReference.ps1) generates the bundled structured datasets from its section 6 CT records and section 16 primary archive. The temporary shared UI starts with CT's 120/140-kVp values; it does not replace the distinct primary/CT attenuation datasets. The tungsten primary reference still has supplied rows at 40-150 kVp in 5-kVp steps, and the molybdenum mammographic reference at 25/30/35 kVp.

Dental/OPG/CBCT, fluoroscopy and the other diagnostic families now have the same selectable voltage list, even without a source profile. The UI visibly labels this **Shared CT-reference kVp choices (temporary)**. Selection never changes the machine family or substitutes CT calibration, scatter, spectrum or attenuation models. Unlisted older numeric entries stay preserved in native data/history but are not added to the dropdown, rounded, or silently replaced. Each machine keeps its own selected value; locks, result invalidation and Escape cancellation remain intact. MRI/ultrasound/radionuclide modes do not receive this dropdown, and treatment retains its MV controls.

**No clinically calibrated source profile is bundled.** The appendix provides attenuation fits, not machine output. OPG/CBCT voltage selection is now available, but a matching machine-specific source/acquisition model is still needed for an absolute Gy result. Sharing kVp choices does not establish physical equivalence between CT, CBCT and OPG.

[DiagnosticProfiles.cs](Assets/Scripts/DiagnosticProfiles.cs) defines the version-1 structured profile schema. Independent catalogue maintainers must validate the actual machine/acquisition/spectrum and then pin the exact UTF-8 JSON SHA-256, ID and revision in [trusted-profiles.json](Assets/Resources/DiagnosticProfiles/trusted-profiles.json). Approved assets can be packaged under `Resources/DiagnosticProfiles/Catalogue`, or imported through the Windows/WebGL file picker against those pinned entries. The room UI cannot approve a profile; `demoOnly` profiles cannot activate. Profile files absent after reopening remain explicitly unavailable.

The implemented provider is `ReferencePointKerma` with an independently validated isotropic angular law, finite distance domains, weighted physical source samples, optional declared Target-to-Scatter irradiation scaling, and exactly one `ProfileExposure` or `PerInput` normalization per component. Primary/leakage samples originate at Target; patient-scatter samples originate at Scatter. Profiles carry spectrum-specific Archer material fits and provenance; the existing CT and primary reference archives remain separate and are not automatic substitutes.

Spatial-field interpolation, dedicated external DLP providers, non-isotropic directional laws, validated moving-acquisition catalogue data and optional required-thickness UI are **not implemented**. Such providers are rejected rather than mapped to the reference model. Openings, joined/custom shielding geometry, separated slabs and mixed-material stacks remain explicitly unsupported by the reused conservative geometry engine. Supported finite homogeneous paths use exact segment intersections and the stable Archer/log-domain core.

### Legacy data and verification

Legacy CT source records, review flags, Patient evaluation points and scenario snapshots remain unchanged. An atomic, idempotent adapter copies only unambiguous known Scatter/ROI positions into the new block; it does not manufacture Target, trust a legacy review flag, or reinterpret Patient as Scatter. Ambiguous/protected cases require explicit new placement/association.

[DiagnosticChecks.cs](Assets/Editor/DiagnosticChecks.cs) passes 118 focused editor assertions for roles, numeric results, underflow, input invalidation, profiles, identical diagnostic voltage choices, OPG/CBCT selection, retained machine-specific model guards, native precision/integrity, copying, protection and migration. Both final Windows/WebGL builds pass `BuildStudio.RunChecks`, including existing 120 CT math and 121 CT data assertions. The prior unified delivery also passed reference-engine/workspace checks; all 29 current WebGL package payloads match by SHA-256. [Current build identities and evidence](../PROJECT_HANDOFF.md#october-1-shared-ct-reference-kvp-dropdown---current-delivery) distinguish this delivery from older ZIP archives.

Compilation/numerical checks are not clinical validation. Interactive Windows/WebGL acceptance and the remaining provider coverage in specification section 15.4 remain **In progress**; this implementation is not a claim of full specification acceptance.
