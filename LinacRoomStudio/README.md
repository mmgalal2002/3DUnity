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

1. Place the supported **CT** scanner from Equipment, then use **Place ROI point** or **Place patient point**. Placing a point opens **CT** settings (including the Properties pane on compact screens). Point-first placement is also supported.
2. If no source exists, use **Configure \<scanner label\>** in CT or the point's Object properties. If no CT scanner exists, **Place CT scanner** arms its placement. A point alone does not define a radiation source. Configuring creates one owned scatter marker and links existing editable ROI/patient points; reopening does not duplicate the source or workload. Protected points remain unchanged and must be unlocked before explicitly including a new source.
3. Choose **120 kVp** or **140 kVp** at the top of **CT source / energy**. These controls also appear near the top of a linked point's Object properties, with the matched lead/concrete coefficient tables from Figures A.2/A.3. This is the shared source protocol's tube potential, not a monoenergetic photon value; different source contributions keep their own spectra. No kVp is chosen automatically.
4. Complete **Scanner / protocol** identity, source citation, validity domain and nonoverlapping component coverage. Supply the provider's **Effective scatter anchor** and confirm the calibration/applicability assumptions only when reviewed. Energy selects attenuation coefficients; it does not supply unshielded source strength.
5. Select exactly one source normalization:
   - **ROI weekly field:** enter validated **Unshielded ROI kerma** in mGy/week for each included evaluation point, after configuring its scanner/protocol/anchor, and use **Confirm field at current ROI** when reconfirmation is required. No second workload or inverse-square factor is applied.
   - **Reference / exam:** supply reference mGy/exam, reference distance, exams/week, minimum/maximum valid distances and an explicitly reviewed isotropic model. The supplied workload and reference geometry determine the unshielded ROI field.
6. Enter declared best/worst factors if those scenarios are requested. Physical kerma can be calculated without a design goal, but an evaluated Pass/Fail requires a compatible goal and its authority; an occupied-person criterion also requires occupancy and its exposure convention. For credited barriers, review the installed material and effective-source path approximation.
7. Use **Calculate + save scenario**. Without a source, the calculation panel instead offers **Configure CT source / energy** and does not save a meaningless empty-source run. An existing results dialog has the same setup action above its scroll area and **Edit inputs for \<point name\>** for surviving points. These close the dialog and return to current inputs without changing historical snapshots.

Changing kVp requires renewed scanner/spectrum applicability review and invalidates direct POI kerma calibrated for the old source conditions. Absolute calculations still need validated unshielded source data, workload where applicable, and valid shielding paths. The primary-radiation archive is not a CT fallback: other CT kVp/material combinations remain unsupported rather than receiving invented coefficients.

Focused editor checks pass 120 math and 121 integration assertions, including point-first source setup, real input-to-kerma normalization, protected-point nonmutation, native persistence and atomic source/point/object limits. Earlier compilation/math evidence did not establish discoverability when no source existed. Interactive acceptance remains **In progress**; no screenshots or smoke tests were performed for this correction. See [current build identity and remaining acceptance](../PROJECT_HANDOFF.md).

### Redesigned diagnostic workflow specification

The [diagnostic-radiology specification, version 2.0](../CT_Point_of_Interest_Shielding_Unity_Specification.md) supersedes the CT-only product requirements for future work: one machine-aware **Calculation** tab, distinct **Target / Scatter (patient) / ROI** roles, one current numeric case and actionable missing-input guidance. It covers diagnostic X-ray families through matching source/material profiles. This is a specification update only; the current builds and the CT controls described above have not been redesigned by this documentation change.
