# Diagnostic radiology shielding: Target, Scatter, ROI and one Calculation tab

Version: 2.0. Revised: 2026-10-01. Thickness: **millimetres**. Geometry: **metres**. Radiation quantity: **air kerma**.

This is the implementation specification for the redesigned workflow, not a claim that the existing Windows/WebGL builds already implement it. The filename is retained for existing links. The numerical reference from version 1.0 remains below.

## 1. Product scope and authoritative point roles

The normal workflow must be:

```text
Place a diagnostic machine
    -> position Target
    -> position Scatter (patient)
    -> position ROI
    -> enter tube voltage and only the necessary numeric exposure inputs
    -> Calculate
    -> read one shielded air-kerma value at the ROI
```

Replace the separate **Beam** and **CT** tabs with one **Calculation** tab. Its model and necessary controls follow the active machine type. Do not make the user assemble a source model, fill out citations, or approve a checklist to reach the calculation.

There is one current case, with one numeric field for each required physical input. There are no Best/Nominal/Worst selectors, scenario-name fields, case factors, or multi-case calculation buttons.

### 1.1 Diagnostic machine coverage

Cover all diagnostic **X-ray** machine families through machine-specific calculation profiles:

| Machine family | Required calculation distinction |
|---|---|
| General radiography, including portable X-ray | Matched primary, patient-scatter and housing-leakage models. |
| Fluoroscopy, C-arm, angiography and CathLab | Matched beam quality, exposure/rate normalization and directional scatter. CathLab is not CT merely because of its palette group. |
| Mammography and breast tomosynthesis | Matched anode/filter spectrum and acquisition geometry; kVp alone does not identify the beam quality. |
| Dental intraoral X-ray | Dental exposure and scatter geometry, not an automatic CT model. |
| Dental panoramic/OPG and cephalometric X-ray | Validated moving-source/acquisition model for the actual mode. |
| Dental cone-beam CT (CBCT) | A CBCT-specific profile; neither conventional CT nor radiographic coefficients are automatic substitutes. |
| Conventional CT | Matched scanner/protocol secondary-field and shielding models, including rotating-source behavior where required. |

Machine type is typed calculation metadata, not a display name, GLB filename or palette category. A visual model becomes calculable only when a matching source profile is available.

MRI and ultrasound do not use this X-ray shielding calculation. Nuclear medicine/PET/SPECT require radionuclide/activity models, not a kVp input. Explain this when such equipment is selected; never give it a diagnostic X-ray result.

Retained treatment/LINAC equipment also uses **Calculation**, but keeps its existing, separate treatment engine and correct MV/MeV controls. Never pass diagnostic kVp into a LINAC energy field or use LINAC defaults for diagnostic machines.

### 1.2 Point roles: these are not interchangeable

| UI name | Physical meaning | Calculation responsibility |
|---|---|---|
| **Target / Device point** | The X-ray tube focal spot or the profile's declared device-source reference. | Establishes source position/orientation and primary/leakage geometry. |
| **Scatter / Patient point** | The patient location at which the scatter model is evaluated. This is the patient's point of calculation. | Establishes patient irradiation and the origin/reference of patient-scattered radiation. |
| **ROI / Measurement point** | The location at which shielding is being assessed. | Receives the shielded air-kerma result after the applicable paths cross walls and other barriers. |

The patient is **Scatter**, not a second ROI. Do not calculate patient dose or label the ROI result as patient dose.

- `Target -> Scatter` describes patient irradiation when the selected model needs it.
- `Scatter -> ROI` describes patient-scatter propagation and shielding.
- `Target -> ROI` is used for leakage and any primary component that actually reaches the ROI, as defined by the machine profile.
- CT, OPG and other moving sources may need weighted positions/angles internally. The Target marker is their declared reference; one arbitrary centre ray is not a substitute for a validated distributed-source model.
- A profile that already supplies a field at the ROI must not receive another distance, scatter or workload multiplier.

Target belongs to its machine and is stored in physical device-local coordinates. Scatter and ROI are independent world-space points unless a profile explicitly requires a different binding. Moving a machine moves its Target, not an independently placed patient or ROI. Changing any relevant point invalidates the result.

Use distinct markers, labels and placement commands for all three roles. Marker size, equipment display scale and point colliders must not alter physical distances or become shielding.

### 1.3 What makes a numeric result possible

```text
shielded air kerma at ROI
    = sum of validated unshielded contributions at ROI
      after each contribution's applicable shielding transmission
```

**kVp selects beam quality; it does not establish source output.** The same voltage can produce different air kerma with different exposure, filtration and machines. Never invent a Gy value from voltage and distance alone.

The machine profile supplies the calibrated source output, normalization, component coverage, validity domain and matching attenuation models. Load these as structured catalogue data, not user-written text. Show only a genuinely missing numeric exposure parameter, or a specific action to obtain a matching profile.

The retained CT attenuation data has four fits: lead and concrete at 120/140 kVp. Section 16 also retains primary radiographic/mammographic data. These are useful backend datasets, **not evidence that every diagnostic source or spectrum is already supported**.

Unsupported machine/energy/material combinations remain visibly unsupported until matching validated data is installed. Broad UI coverage must not mean silently reusing the wrong coefficients.

### 1.4 Backend reference labels and source record

The following labels apply to technical reference sections, not to fields the user must complete:

| Label | Meaning |
|---|---|
| `SOURCE` | Data retained from the supplied Appendix A. |
| `DERIVED` | Identified algebra or mathematics, not an additional quoted equation. |
| `ENGINEERING` | Application, data, geometry or validation requirements. |
| `EXTERNAL_REQUIRED` | Data that must come from a matching machine/model provider. |
| `DEMO_ONLY` | Arithmetic fixtures, never clinical defaults. |

The retained reference is NCRP Report 147, Appendix A, printed pages 116-124, supplied as nine PDF pages. Reported PDF SHA-256: `168bd82cfe61ff318c73e81becbdb381b04b0998af9a355246bf31158f801e81`. PDF page 1 corresponds to printed page 116. Source locations stay in dataset metadata and exports; there is no citation text box in the workflow.

Arithmetic verification is not clinical design approval. Machine-profile approval and facility review remain external responsibilities, not user-facing "reviewed" checkboxes that manufacture model validity.

## 2. Unified Calculation workflow and live guidance

`ENGINEERING`: this section defines the user-facing behavior. The equations and provider contracts below are backend requirements, not a form to expose wholesale.

### 2.1 One tab, one visible workflow

The right-panel tabs are **Room | Object | Calculation**. Remove **Beam** and **CT**, including alternate compact-screen routes to the old panels.

Calculation contains, in this order:

1. **Active machine**: its existing scene label and machine family; a selector only when needed.
2. **What to do next**: a live explanation and one primary action.
3. **Target | Scatter (patient) | ROI**: three placement/select controls with present/missing states.
4. **Inputs**: one tube-voltage field and only the exposure fields needed by this machine profile.
5. **Calculate**: one action for the current inputs.
6. **Air kerma at ROI**: one prominent number with its unit and exposure/time basis.
7. Optional collapsed **Details**, **Weekly total**, **Compare with a limit**, and saved-result history.

Do not put a second calculation button or a competing reference-QA workflow beneath every tab. Existing treatment/reference calculation is routed through the active machine's branch of Calculation.

Opening Calculation immediately explains why a numeric result is or is not available. The explanation must be visible without hovering, opening help, pressing Calculate, or scrolling past inputs.

### 2.2 Placement and machine switching

1. Place or select CT, Dental or another diagnostic machine.
2. The application resolves its typed machine family and available profile automatically.
3. Select **Place Target**, then choose the source location on the machine.
4. Select **Place Scatter (patient)**, then choose the patient location.
5. Select **Place ROI**, then choose the location at which shielding is measured.
6. Enter **Tube voltage (kVp)** and any exposure value required by the resolved profile.
7. Set material/thickness on actual walls in Object if they are not already defined.
8. Select **Calculate** and read the shielded result at the ROI.

A profile may provide a known physical Target anchor, visibly placed on the equipment. Never substitute an arbitrary mesh origin. Point-first placement is supported: preserve the point and guide the user to its missing machine/association.

In 2D, the placement height must come from a declared profile anchor or an explicitly entered numeric height. Do not silently put every point at floor level. Dragging and 3D placement are the normal position controls; exact coordinates remain available in Object, not as three compulsory text boxes per point.

Selecting a machine or its associated Target/Scatter makes that machine active. Selecting an ROI retains its explicit machine association. Selecting a wall does not switch sources. If an association is ambiguous, offer the available machines instead of silently choosing the first source.

Switching machine family changes the calculation provider and relevant fields. Keep each machine's own numeric inputs; never reuse another machine's energy, workload, reference output or units. CT/CBCT/CathLab/Dental classification must not depend on the old palette grouping.

Multiple placed machines are not automatically summed. This workflow calculates the explicitly active machine at the selected ROI; that profile's nonoverlapping physical components are summed internally. Copying equipment must not silently duplicate an active exposure.

### 2.3 Minimal numeric input contract

| Input | When visible | Rule |
|---|---|---|
| **Tube voltage (kVp)** | Diagnostic X-ray machine selected | One numeric field. kVp is peak tube potential, not MV, keV or "KVM". |
| **Exposure (mAs)** | Profile output is calibrated per mAs | One actual-mAs value under that profile's definition. |
| **Scan DLP (mGy cm)** | A validated CT/CBCT DLP-to-room-field provider requires it | One numeric value; DLP is not itself room air kerma. |
| **Beam-on time (min)** | A validated fluoroscopy/rate provider requires duration | One numeric value; no duplicate time or workload multiplier. |
| Other physical exposure parameter | The provider cannot calculate without it | Exactly one field per necessary parameter, with units and a specific explanation. |
| **Exposures/exams per week** | User requests a weekly total from a per-event result | One count, using the profile's event definition. |
| **Material / Thickness (mm)** | Editing a barrier in Object | Material selector and one physical-thickness field; calculated path length is read-only. |
| **Limit** | User requests a numerical comparison or required thickness | One number with the same quantity and basis as the result. |
| **Occupancy** | An explicitly requested occupied-person comparison needs it | One factor; never changes the physical air-kerma result. |

Source-output calibration, reference distance, scanner/protocol identity, filtration, angular model, validity domain, component coverage and dataset identity come from the structured profile. Do not ask the user to type them into narrative fields.

If a calibrated profile fully specifies one exposure, show its exposure definition read-only and require no additional loading input. If it does not, show the exact missing numeric parameter. Never insert a demo output, exposure or workload to keep the form short.

The default result is per profile-defined exposure/exam, or a clearly labelled rate when the provider supplies a rate. A weekly workload, occupancy and design goal are **not prerequisites** for this physical result. A requested weekly result does require a compatible workload.

Show only the provider's applicable normalization fields, not every row in the table. Do not multiply per-exam output by mAs as well unless that provider's declared law requires it.

An empty field is missing; explicit numeric zero is a value where physically allowed. Use ordinary empty numeric inputs, not a "has value" checkbox beside each input. Reject invalid/nonfinite input with a local message; never coerce it to zero or retain an undisclosed old value.

Use one editable field per parameter. No paired slider/input duplicates, duplicated kVp controls in Object, or simultaneous 120/140 buttons beside another energy field. Offer supported values as guidance, without silently rounding to one of them.

### 2.4 One current case, not three scenarios

- Remove Best/Nominal/Worst controls and their factors from the active input model.
- Remove scenario-name text, "Calculate + save scenario", "Save best / nominal / worst", and the default comparison-A/comparison-B interface.
- Calculate the single entered exposure and actual installed barriers. Do not apply a hidden uncertainty/case multiplier.
- Unshielded and shielded values may appear together in Details as quantities from the same calculation, not separate WallsOff/AllOff cases.
- Save snapshots with an automatic machine/ROI/timestamp label. History is optional and must not obstruct getting the current value.
- Different physical components, angular samples and spectra are contributions to one result, not additional user cases.

No citation, validity-domain, purpose, component-description, occupancy-convention or goal-authority text field belongs in the normal Calculation workflow.

Remove manual "reviewed", "confirmed", "anchor accepted", "components confirmed" and similar gates. Replace them with automatic checks of the structured profile and current geometry. A user checkbox must never turn missing physics data into a valid model.

Do not allow an unsupported or incomplete contribution to disappear from the total. A missing result is not zero. Required-thickness solving remains optional and needs a compatible numerical limit and a supported barrier model.

### 2.5 Live readiness state and the natural next step

Run readiness checks when Calculation opens and whenever selection, an input, a point, a profile or a relevant barrier changes. Do not wait for a failed calculation to discover a missing prerequisite.

The visible guidance card must answer:

1. **What is the current state?** For example, "No result yet: the patient position is missing."
2. **Why does this prevent the number?** Explain the missing physical dependency in ordinary language.
3. **What exactly happens next?** Name the action, scene object or numeric field.
4. **What is already available?** Summarize the actual machine, voltage, placed points and relevant barriers, without an approval checklist.

Use actual object names, input values, units and supported choices. Do not display only "MissingRequiredInput", "Review assumptions", "Complete setup" or a generic recommendation paragraph.

| Current condition | Required explanation | Primary action |
|---|---|---|
| No active machine | "There is no machine supplying radiation. Place a CT, Dental or another supported X-ray machine first." | **Place machine** opens the equipment picker. |
| Several possible machines | "This ROI is not associated with a machine. Choose which machine supplies this calculation." | **Choose machine** lists existing equipment. |
| Target missing | "The device source location is missing. Place Target at the tube/source reference so its geometry can be established." | **Place Target** arms the correct marker. |
| Scatter missing | "The patient location is missing. Place Scatter where the patient is, so patient scatter and its distance to the ROI can be calculated." | **Place Scatter (patient)**. |
| ROI missing | "There is no measurement location. Place ROI where you want the shielded air kerma reported." | **Place ROI**. |
| kVp missing/invalid | "Enter a valid tube voltage for {machine}. Voltage selects its available source and attenuation models." | **Enter kVp** focuses that field. |
| No applicable source profile | "{machine} at {kVp} kVp has no calibrated source-output model. Voltage alone cannot determine Gy; your placed points are retained." | **Load matching machine profile** opens the platform's file picker. |
| Unsupported spectrum | "No model is available at {kVp} kVp. This installed profile supports {actual supported values}." | **Edit kVp**; also offer **Load matching profile**. |
| Exposure value missing | "This profile gives output per {unit}. Enter {parameter} to determine how much radiation this exposure produces." | Focus the named numeric field. |
| Barrier material missing | "The path to {ROI} crosses {wall}, but its shielding material is not set." | **Set material on {wall}** selects it and opens that control. |
| Barrier thickness missing/invalid | "{wall} has no valid physical thickness. The ray path cannot determine attenuation without it." | **Set thickness on {wall}**. |
| Unsupported barrier fit/stack | "{component} crosses {materials} at {kVp} kVp, but no applicable transmission model exists for this path." | Select the affected barrier; offer a matching model, not a guessed substitute. |
| Geometry outside model domain | "The {named point/path} is {actual distance/condition}; this profile is valid for {domain}. No distance clamp has been applied." | **Move {point}** selects it; offer another applicable profile. |
| Object locked | "{object} is locked, so the required position/material edit is protected." | **Select {object} to unlock**; never silently unlock it. |
| Inputs changed since calculation | "{changed input} changed from {old} to {new}. The saved number does not describe the current setup." | **Recalculate** after current readiness passes. |
| Requested comparison only is incomplete | "The physical result is available. Enter a compatible limit to compare it or solve thickness." | Focus **Limit**, without blocking physical calculation. |

When multiple prerequisites are missing, show the earliest actionable dependency as the primary next step. Other blockers can be listed briefly in dependency order. Re-evaluate after every action; advance to the next step automatically.

Placement actions focus the viewport, show the point role and placement instruction, and work in compact layouts. Successful placement returns to Calculation. Escape cancels placement without discarding existing points or values.

Actions from results, a selected point's Object panel and Calculation must use the same navigation helper. They must select the right machine/ROI, open the right control and preserve unsaved numeric input. No action should send the user to the old CT or Beam tab.

Return structured readiness data, not just an exception string:

```text
state: MissingMachine | MissingPoint | MissingInput | UnsupportedModel
       | InvalidGeometry | Ready | Calculating | CurrentResult | StaleResult | Failed
issues[]: code, machineId, roiId, pointId/barrierId/inputKey as applicable,
          observedValue, expectedDomain, explanation, actionKind, actionTarget
nextAction: the first executable prerequisite action
canCalculatePhysical: true only for a complete, applicable current model
canCompare/canSolveThickness: separate from physical readiness
inputFingerprint: machine/profile, physical inputs, geometry and result basis
```

Build explanations from this data so the tab, inline field errors and result view agree. Do not maintain separate stale copies of the validation rules.

### 2.6 Explain the ready state and the resulting Gy number

When ready, the card must describe the actual calculation, not say only "All checks passed":

```text
Ready for CT 01 -> ROI 01.
Tube voltage: 120 kVp. Exposure basis: one exam from the selected profile.
Target and patient positions are set; Scatter -> ROI is 3.00 m.
The applicable path crosses Wall 02 with 2.00 mm of lead.
Calculate uses the profile's unshielded field and the matched wall transmission.
Press Calculate to obtain shielded air kerma at ROI 01 in Gy/exam.
```

This is a wording example, not a default profile or a prescribed geometry. For distributed sources, explain the actual component/sample paths instead of implying that all radiation follows this one ray.

The main result label is **Air kerma at ROI**, with an explicit basis such as **Gy/exposure**, **Gy/exam**, **Gy/min** or **Gy/week**. "Gy" alone is insufficient when exposure or time normalization matters. It is not effective dose, patient dose or a facility-approval verdict.

Use scientific notation and, when helpful, a read-only mGy/microGy equivalent. Convert mGy to Gy by dividing by 1000; changing display units must not change the calculation.

Details explains where the number came from using actual values: profile exposure definition, unshielded ROI field, relevant component origins/distances, material/path thickness, transmission and shielded total. Metadata and references are read-only; none is a text-input prerequisite.

### 2.7 Missing, stale, failed and valid-zero results

- Before a valid calculation, display **No result yet** and `--`, never `0 Gy`.
- While calculating, show progress and prevent duplicate runs; keep the current guide visible.
- On a changed physical input, immediately mark the result **Out of date** and remove it from the current-value slot. Historical snapshots stay explicitly historical.
- If any required contribution is missing or unsupported, do not show a partial sum as the complete ROI result. Explain the affected component and next action.
- A failed provider, parse, file import or calculation must show its specific failure. Preserve the current design; never return a success-shaped empty result.
- A genuinely zero entered exposure/workload may produce a valid zero with that reason stated. Unknown exposure is not zero.
- Numerical underflow is not physical zero. Retain log-domain values and display an explicit below-range indication.
- A genuinely unobstructed supported path has `B=1`. Say **No shielding on this path** and report its unshielded value; an undefined wall is not an unobstructed path.
- Occupancy or a limit missing from an optional comparison does not erase a complete physical result. An incomplete physical model must never receive a Pass verdict.
- An asynchronous result is accepted only if its machine/ROI and input fingerprint still match. Otherwise retain it as historical or discard it with a visible stale status.

The same behavior is required on Windows, WebGL, compact screens and results reopened from saved designs.

## 3. Physical meaning, variables, and units

### 3.1 Terms in the source

- **Air kerma K:** the quantity used by A.1. The equations below do not calculate effective dose, organ dose, or patient dose.
- **Transmission B:** the dimensionless fraction of broad-beam air kerma remaining behind a barrier. `B=1` means no attenuation; `B=0.01` means 1 percent transmission.
- **Broad beam:** the supplied fits include the measurement/calculation geometry's scattered contribution. They are not microscopic monoenergetic absorption coefficients or a photon-by-photon transport simulation.
- **Primary radiation:** the original useful beam.
- **Scattered radiation:** radiation redirected by interactions, including the patient at Scatter in a matched diagnostic source model.
- **Leakage radiation:** radiation transmitted through the tube housing outside the useful beam; its source intensity is not given here.
- **Secondary radiation:** the category used for the CT fits in Figures A.2/A.3. The figure label does not specify a scanner-specific scatter/leakage normalization. That normalization must come from the source model.
- **kVp:** peak tube potential. It is a spectrum descriptor, not a single photon energy in keV.
- **HVL:** half-value layer. The source's high-attenuation HVL is different from the first thickness that halves an initially unshielded beam.

### 3.2 Canonical symbols

| Symbol / identifier | Meaning | Canonical unit / condition |
|---|---|---|
| `m` / `material` | Shielding material identifier, not mass | Enum/string |
| `x` / `pathThicknessMm` | Shielding thickness used in a transmission fit | mm, finite, >=0 |
| `t_n` / `normalThicknessMm` | Physical thickness normal to a slab | mm, finite, >=0 |
| `alpha` | First fitting coefficient | mm^-1, >0 |
| `beta` | Second fitting coefficient | mm^-1; can be negative in Table A.1 wood rows |
| `gamma` | Third fitting coefficient | Dimensionless, >0 |
| `r` | `beta/alpha` | Dimensionless; all supplied fits have `r>-1` |
| `B` | Shielded/unshielded air-kerma ratio | Dimensionless, 0<B<=1 for finite model thickness |
| `logB` | Natural logarithm of B | <=0; retain for tiny transmissions |
| `K0(q)` | Unshielded air kerma at ROI q | mGy per declared exposure or interval |
| `Kx(q)` | Shielded air kerma at ROI q | Same unit and interval as K0 |
| `t` | Target / Device point | Metres in the project frame |
| `p` | Scatter / Patient point | Metres in the project frame |
| `q` | ROI / Measurement point | Metres in the project frame |
| `s`, `s_i` | Origin/reference of the current contribution or contribution i | Target, Scatter or profile-defined sample, not an interchangeable point |
| `d` | Contribution-source-to-ROI distance | Metres, >0 when that provider uses inverse-square |
| `d_ref` | Calibration distance | Metres, >0 |
| `A_ref` | Unshielded source air kerma at d_ref | e.g. mGy/exam, with explicit normalization |
| `N` | Number of exams of a specified protocol | exams/week, if weekly mode |
| `T` | Occupancy factor under the selected design convention | Dimensionless, 0<=T<=1 |
| `P` | Design goal | mGy/week in the air-kerma workflow; external input |
| `rho` | Material density if a separate model uses it | Explicit unit; no numeric default supplied here |
| `theta` | Angle between ray and slab normal | Radians internally |
| `c` | `abs(dot(unitRay,unitNormal))` | Dimensionless, 0..1 |

### 3.3 Required conversions and exclusions

`ENGINEERING`

```text
millimetres = metres * 1000
metres = millimetres / 1000
millimetres = centimetres * 10
1 Gy = 1000 mGy = 1,000,000 microGy
1 mGy = 1000 microGy
1 hour = 3600 seconds
```

Use the same interval on both sides of A.1. Do not compare a per-exam result with a per-week goal. Convert rates by an explicitly supplied exposure duration or workload. Do not confuse mA with mAs; a current becomes mAs only after integration over exposure time. Effective mAs and actual tube mAs are not interchangeable without the source model's definition.

There is no automatic `mGy -> mSv` conversion in this specification. Photon radiation weighting alone does not make air kerma equal to effective dose or ambient dose equivalent. If a supplied map is in another dose quantity, retain that quantity and require a compatible validated attenuation/goal model.

Use `double` for scientific arithmetic. Convert Unity `Vector3` coordinates to a double-precision geometry representation where the required geometric tolerance exceeds float precision. Keep display rounding out of calculations.

## 4. All transmission equations from the attachment

### 4.1 A.1: transmission definition

`SOURCE`: PDF p1, report p116.

\[
B(x,m)=\frac{K(x)}{K(0)}. \tag{A.1}
\]

`K(x)` and `K(0)` refer to the same occupied location, source condition, and exposure basis, with and without the barrier. The ratio is not a new source-strength law.

`DERIVED`:

\[
K(x)=K(0)B(x,m).
\]

If `K(0)=0`, the forward result is zero for a valid model, but the measured ratio `K(x)/K(0)` is undefined. Do not estimate B by dividing zero by zero.

### 4.2 A.2: Archer broad-beam transmission

`SOURCE`: PDF p2, report p117.

\[
B(x)=\left[\left(1+\frac{\beta}{\alpha}\right)
e^{\alpha\gamma x}-\frac{\beta}{\alpha}\right]^{-1/\gamma}. \tag{A.2}
\]

Plain expression:

```text
r = beta / alpha
B = pow((1 + r) * exp(alpha * gamma * thicknessMm) - r, -1 / gamma)
```

The outer exponent applies to the **entire square bracket**. The exponential exponent is `alpha*gamma*x`; it is positive inside the bracket. The overall negative power produces attenuation. The coefficients already carry the beam/material information; do not multiply another attenuation factor into this expression without a separate model.

Use the stable version in Section 11 in code; the plain expression can overflow at large thickness.

### 4.3 A.3: required thickness for one fit

`SOURCE`: PDF p2, report p117.

\[
x(B)=\frac{1}{\alpha\gamma}
\ln\left[\frac{B^{-\gamma}+\beta/\alpha}{1+\beta/\alpha}\right]. \tag{A.3}
\]

Plain expression:

```text
xMm = log((pow(B, -gamma) + beta / alpha) / (1 + beta / alpha))
      / (alpha * gamma)
```

`ln` is natural logarithm. This solves a single material/spectrum transmission curve. It does not invert a weighted sum of different spectra by averaging their coefficients.

Boundary handling:

| Condition | Meaning / action |
|---|---|
| `B=1` | `x=0`. |
| `0<B<1` | Compute finite positive thickness. |
| `B=0` | No finite thickness produces exact zero in this model; return explicit no-finite-solution status. |
| `B<0`, NaN, infinity | Invalid input. |
| Required transmission `B_required>1` | The unshielded design contribution already meets that goal; required added shielding for this scalar criterion is zero. Do not feed B>1 to the primitive inverse. |

### 4.4 High-attenuation HVL

`SOURCE`: PDF p2, report p117; visualized in Figure A.1, PDF p7/report p122.

\[
x_{1/2,\infty}=\frac{\ln 2}{\alpha}.
\]

The source states that at sufficiently great thickness the transmission approaches an exponential with constant HVL. For a kVp workload distribution, it describes using the high-attenuation HVL for the highest operating potential as a conservative assumption.

This statement concerns the **asymptotic HVL**. It is not permission to replace every protocol with the nearest available CT kVp fit or to use a constant HVL from zero thickness.

### 4.5 Related exact derivations

`DERIVED` from A.2/A.3; these are not additional source-numbered equations.

With `r=beta/alpha`:

\[
B(x)\sim(1+r)^{-1/\gamma}e^{-\alpha x}
\quad\text{as }x\to\infty.
\]

The prefactor is generally not 1. Therefore `exp(-alpha*x)` is not the exact broad-beam curve over its full range.

\[
\mathrm{TVL}_\infty=\frac{\ln 10}{\alpha},\qquad
\mathrm{HVL}_{first}=x(B=0.5),\qquad
\mathrm{TVL}_{first}=x(B=0.1).
\]

The thickness of an additional half-value layer after existing thickness `x0` is:

\[
\Delta x_{1/2}(x_0)=x\!\left(\frac{B(x_0)}2\right)-x_0.
\]

The local logarithmic attenuation coefficient is:

\[
\mu_{eff}(x)=-\frac{d\ln B}{dx}
=\frac{\alpha(1+r)}{1+r-r e^{-\alpha\gamma x}}.
\]

Thus `mu_eff(0)=alpha+beta`, `mu_eff(infinity)=alpha`, and `dB/dx<0` for the supplied positive-alpha, positive-gamma fits with `alpha+beta>0`. These identities are useful for validation and sensitivity calculations.

Other useful derived outputs:

\[
\text{percent transmitted}=100B,\quad
\text{percent attenuated}=100(1-B),\quad
\text{attenuation factor}=1/B,\quad
\text{decimal reductions}=-\log_{10}B.
\]

## 5. Radiation qualifications that must accompany the equations

### 5.1 General primary and scattered radiation

`SOURCE`: PDF p1/report p116.

The appendix assumes, to first approximation, that scattered radiation has the attenuation of the corresponding primary beam for photons generated at **less than 150 kVp**, because their spectra are treated as approximately similar. Preserve the strict `<150 kVp` wording of this statement. It is not a universal scattered-spectrum equivalence for every modality or filtration.

The cited primary-transmission measurements cover medical imaging equipment operating at 50-150 kVp in lead, steel, plate glass, gypsum wallboard, lead acrylic, and wood. Concrete data are attributed separately to Legare et al. (1978), and 25-35 kVp mammographic data to Simpkin (1987a).

Although lead acrylic is mentioned in the prose, **the attached Table A.1 does not provide lead-acrylic coefficient rows**. Do not create them or substitute plate glass.

### 5.2 CT secondary radiation is a separate fit family

`SOURCE`: PDF p2/report p117.

The appendix explicitly says CT secondary radiation is more penetrating than radiation from radiographic units at the same potential because CT uses additional primary-beam filtration. The CT curves were refitted from Simpkin (1991) Monte Carlo results for beams hardened to simulate spectra typical of CT scanners.

Consequences for the application:

- `CT_SECONDARY` must be a different beam-family identifier from `PRIMARY_RADIOGRAPHIC`.
- A matching kVp alone is insufficient to choose a coefficient row.
- The four CT rows below are generic historical model fits, not individual scanner calibrations or modern protocol-specific measurements.
- The appendix does not validate them for tin-filtered CT, dual-energy mixed spectra, arbitrary bow-tie filters, cone-beam scanners, or every contemporary scanner. Such use requires external spectrum/applicability evidence.
- Split distinct spectra into distinct contributions where a valid fit exists. Do not invent one averaged-kVp protocol.

### 5.3 Leakage model

`SOURCE`: PDF p1/report p116.

The appendix assumes simple exponential transmission for leakage, reasoning that the tube housing removes all but the most penetrating x rays. At a given tube potential leakage is described as more penetrating than primary/scattered radiation. Its HVL is assumed equal to the primary beam HVL at great depth.

`DERIVED` mathematical form of that assumption:

\[
B_L(x)=2^{-x/\mathrm{HVL}_{L}}
=\exp\left(-\frac{\ln2}{\mathrm{HVL}_{L}}x\right).
\]

If an externally accepted matched primary fit is selected and the appendix's leakage assumption is adopted:

\[
\mathrm{HVL}_{L}=\frac{\ln2}{\alpha_{primary}},\qquad
B_L(x)=e^{-\alpha_{primary}x}.
\]

The source provides no leakage source intensity, CT-specific leakage fraction, or rule to identify the correct CT housing-hardened spectrum from an arbitrary radiographic alpha. Therefore leakage must be defined by the machine profile, not added by a user checkbox or automatically on top of total CT secondary radiation.

If the external unshielded source estimate includes total secondary radiation, do not add leakage again. If scatter and leakage are supplied separately, each needs its own consistent normalization, geometry, and transmission model.

## 6. Complete CT coefficient dataset

`SOURCE`: Figures A.2 and A.3, PDF pp8-9/report pp123-124. All values below were checked against the visible figure insets, not inferred from plotted curves.

| ID | Material | kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Source |
|---|---|---:|---:|---:|---:|---|
| `CT_PB_120` | lead | 120 | 2.246 | 5.73 | 0.547 | Fig. A.2, p123 |
| `CT_PB_140` | lead | 140 | 2.009 | 3.99 | 0.342 | Fig. A.2, p123 |
| `CT_CONCRETE_120` | concrete | 120 | 0.0383 | 0.0142 | 0.658 | Fig. A.3, p124 |
| `CT_CONCRETE_140` | concrete | 140 | 0.0336 | 0.0122 | 0.519 | Fig. A.3, p124 |

Machine-readable version, using JSON numbers rather than strings:

```json
{
  "datasetId": "NCRP147_APPENDIX_A_CT_SECONDARY",
  "thicknessUnit": "mm",
  "coefficientInverseLengthUnit": "1/mm",
  "radiationQuantity": "airKerma",
  "beamFamily": "CT_SECONDARY",
  "interpolationEnabled": false,
  "extrapolationEnabled": false,
  "fits": [
    {"id":"CT_PB_120","material":"lead","kvp":120,"alpha":2.246,"beta":5.73,"gamma":0.547,"pdfPage":8,"reportPage":123,"figure":"A.2"},
    {"id":"CT_PB_140","material":"lead","kvp":140,"alpha":2.009,"beta":3.99,"gamma":0.342,"pdfPage":8,"reportPage":123,"figure":"A.2"},
    {"id":"CT_CONCRETE_120","material":"concrete","kvp":120,"alpha":0.0383,"beta":0.0142,"gamma":0.658,"pdfPage":9,"reportPage":124,"figure":"A.3"},
    {"id":"CT_CONCRETE_140","material":"concrete","kvp":140,"alpha":0.0336,"beta":0.0122,"gamma":0.519,"pdfPage":9,"reportPage":124,"figure":"A.3"}
  ]
}
```

### 6.1 Supported versus unsupported selection

Use exact supported spectrum/kVp identities. A 120-kVp CT beam with a lead barrier selects `CT_PB_120`, not the Table A.1 lead row at 120 kVp.

| Request | Behavior with this dataset alone |
|---|---|
| 120/140 kVp CT, lead/concrete | Matching fit exists; the machine/material profile must establish applicability automatically. |
| 80, 100, 110, 130, 150 kVp CT | No supplied CT row. Return unsupported spectrum. |
| CT, steel/glass/wood/gypsum/lead acrylic | No supplied CT row. Return unsupported material/spectrum combination. |
| Mixed CT spectra | Sum independently modelled contributions; require a supported fit for each. |
| Arbitrary kVp interpolation | Disabled by default; no CT interpolation rule supplied. |
| Density correction or special concrete mixture | No validated correction supplied; require external model. |

Do not assume the 140-kVp lead curve is a proven universal upper bound at all thicknesses or for all scanners. Any envelope is a separately validated backend model, not a Worst-case selector or a fallback for an unsupported entered voltage. The current result uses the actual selected spectrum.

### 6.2 What the three figures add

`SOURCE` plus exact reconstruction instructions:

- **Figure A.1**, report p122: x-axis kVp, displayed approximately 20-150; y-axis high-attenuation HVL in mm on a logarithmic scale, displayed 0.01-100 mm. Curves/points include lead, steel, plate glass, concrete, gypsum wallboard, and wood. The prose on p117 names five of those materials; the graph also shows steel. Every numerical point is reconstructed from the Table A.1 alpha values using `ln(2)/alpha`. No independent digitized dataset is needed. Some computed values, such as high-kVp wood HVLs, exceed the displayed upper axis limit; retain the values rather than clipping the data.
- **Figure A.2**, report p123: CT lead transmission versus lead thickness in mm; displayed x range 0-3 mm and logarithmic transmission range 1 to 10^-4. Recreate both curves with the two lead fits above.
- **Figure A.3**, report p124: CT concrete transmission versus concrete thickness in mm; displayed x range 0-300 mm and logarithmic transmission range 1 to 10^-5. Recreate both curves with the two concrete fits above. A computed point can fall below the displayed y-axis, for example 120 kVp at 300 mm.

Graph extents are display ranges, not explicitly stated validation bounds. Algebraic evaluation outside them is possible; it must be flagged as outside the plotted range, with model applicability unresolved unless separately supported. Do not call those extents certified maximum/minimum usable thicknesses.

### 6.3 Derived HVLs and TVLs for the CT rows

`DERIVED`, mm, rounded for display:

| Fit | First HVL from A.3 | High-attenuation HVL | High-attenuation TVL |
|---|---:|---:|---:|
| CT_PB_120 | 0.0993561061 | 0.3086140608 | 1.0251937191 |
| CT_PB_140 | 0.1248748507 | 0.3450209958 | 1.1461349393 |
| CT_CONCRETE_120 | 13.9585100035 | 18.0978376125 | 60.1197152218 |
| CT_CONCRETE_140 | 15.8177137995 | 20.6293803738 | 68.5293182439 |

These differences show why repeatedly applying the high-attenuation HVL from zero thickness does not reproduce A.2.

## 7. Machine-profile source-strength interface

`EXTERNAL_REQUIRED` and `ENGINEERING`: these are provider normalization rules, not extracted diagnostic source-output equations. A typed machine profile supplies them. Do not expose all their metadata as input boxes.

### 7.1 Provider output: unshielded air kerma already evaluated at the ROI

An external validated provider can return:

```text
K0(machineProfile, Target, Scatter, ROI, exposureInputs, basis) in mGy/basis
```

Then the calculation is simply:

\[
K_i(q)=K_{0,i}(q)B_i(x_i(q)).
\]

Do not add an inverse-square factor if the provider has already accounted for the POI distance. Do not multiply the weekly workload again if the provider already returns mGy/week.

This interface supports a measured field, a validated diagnostic scatter map, or an independently established source calculation, provided the field is unshielded with respect to the barriers being modelled. Per-exposure and rate outputs are valid bases; weekly normalization is not mandatory.

A scalar `K0(q)` value supplies intensity, not the incoming directions or which rays cross a finite barrier. To apply a geometry-dependent B, the provider must also supply a validated effective-source/path approximation or separate angular/path contributions. Multiplication by one B is justified only when that B represents the field reaching the point. For openings, finite panels or extended sources, a scalar map alone cannot establish shielding-path coverage; see Section 9.4.

### 7.2 Optional effective point-source normalization

When a separate source model explicitly supports inverse-square scaling, let `A_i(q_direction)` be air kerma per exam at a reference distance `d_ref,i`, for the same direction, protocol and reference geometry:

\[
K_{0,i}(q)=N_i A_i(\Omega_q)
\left(\frac{d_{ref,i}}{d_i(q)}\right)^2.
\]

If the calibration is isotropic, `A_i` is constant; isotropy must be an explicit assumption, not inferred from the appendix. If a normalized directional multiplier is used:

\[
A_i(\Omega)=A_{ref,i}f_i(\Omega),
\]

the reference direction and normalization of `f_i` must be specified. Do not multiply both a directional map and another directional factor for the same effect.

For patient scatter, the effective source is tied to Scatter or its profile-defined distribution. Primary/leakage uses Target or the device distribution. The origin is not automatically the gantry centre or Unity model pivot. CT and panoramic acquisitions can be extended/moving systems; a point approximation needs a declared applicability/minimum-distance domain. Do not extrapolate to `d=0` or clamp near distances to an arbitrary epsilon.

### 7.3 Explicit workload normalization modes

The profile selects exactly one normalization law per contribution. The user supplies only that law's necessary numeric exposure values; never display all laws as competing setup modes or apply them together.

| Source calibration | Workload input | Reference air kerma for the interval |
|---|---|---|
| `mGy/exam` at reference conditions | exams/week | `A_ref * N` |
| `mGy/scan` | scans/week | `A_ref * scanCount`; an exam can include several scans |
| `mGy/rotation` | rotations/week | `A_ref * rotationCount` |
| `mGy/mAs` | actual mAs/week under the calibration's definition | `A_ref * totalMas` |
| `mGy/week` | none | supplied weekly value |
| `mGy/hour` for an explicit operating condition | beam-on hours/week | `A_ref * beamOnHours` |
| DLP-based external scatter conversion | protocol DLP and exam counts | model-specific expression below |

For an **externally supplied and validated** DLP conversion coefficient `c_DLP`:

\[
Q_{DLP,i}=\sum_j N_{ij}\,DLP_{ij},\qquad
K_{ref,i}=c_{DLP,i} Q_{DLP,i}.
\]

If DLP is in `mGy*cm` and K is in mGy, `c_DLP` has units `cm^-1` and must also identify its reference distance, direction, phantom/protocol and included components. This is a dimensional interface, not an assertion of any numerical NCRP CT scatter coefficient. **No value for c_DLP is supplied by this appendix.**

CTDIvol and DLP are not unshielded room air kerma. Do not assign `K0=DLP`, do not use patient effective-dose conversion factors as room-scatter coefficients, and do not infer the missing conversion from the attenuation coefficients.

Protocol phases, repeated acquisitions, localizers, bolus tracking, and other workload components must be represented by the profile when included in the exposure definition. Their source strength and applicable beam family are model data. A localizer must not inherit a rotating CT-secondary fit without support. A missing relevant component makes the total incomplete.

### 7.4 Manufacturer or measured scatter maps

Each map must declare:

- Radiation quantity, units, normalization interval or exposure quantity.
- Scanner/protocol identity, kVp, filtration/phantom and acquisition conditions.
- Coordinate origin, axes, orientation, sample locations and map plane/volume.
- Whether tube-housing leakage, patient scatter, gantry attenuation, couch attenuation, and room barriers are already included.
- Valid spatial domain, interpolation rule, uncertainty and any prohibited extrapolation.
- Reference workload and the exact scaling law permitted by the supplier.
- An effective-source/path model or angular/path-resolved contributions suitable for the barriers being evaluated; a scalar spatial field alone does not provide that information.

Interpolate according to the map's supported method. A logarithmic interpolation is sometimes useful for positive fields but is an engineering choice requiring validation, not an appendix rule. Values outside the map's valid domain must be flagged. Do not impose inverse-square scaling on a spatial map that already gives the field at each POI.

## 8. From source contributions to a POI result

`DERIVED` and `ENGINEERING`.

### 8.1 Contribution summation

For independent nonoverlapping source/protocol/exposure contributions:

\[
K_{shielded}(q)=\sum_i K_{0,i}(q) B_i(x_i(q)),\qquad
K_{unshielded}(q)=\sum_i K_{0,i}(q).
\]

Do not multiply transmissions from alternative independent sources. Add their air kermas. If a source is discretized into positions/angles, its normalized workload weights must sum to the intended total, so the full workload is not assigned to every sample.

For a validated quadrature representation:

\[
K(q)=\sum_i\sum_j w_{ij}\,K^{full}_{0,ij}(q)B_{ij}(x_{ij}),
\qquad w_{ij}\ge0,\quad\sum_j w_{ij}=1.
\]

Here `K0_full` means the source field for the full protocol workload at a sample state, before multiplying its workload fraction. Alternatively use already-weighted `K0_ij` and omit `w_ij`. The representation must state which convention it uses.

### 8.2 Optional occupancy and design comparison

This comparison is separate from physical-result readiness. Its quantity, units and exposure convention come from a structured comparison definition; no narrative authority/convention field or review checkbox is required. The ordinary ROI value remains the physical field.

For a common POI occupancy factor under the selected air-kerma design convention:

\[
K_{occupied}(q)=T(q)K_{shielded}(q),\qquad
K_{occupied}(q)\le P(q).
\]

Equivalent, when `T>0`:

\[
K_{shielded}(q)\le\frac{P(q)}{T(q)}.
\]

If occupancy depends on when a source operates, use validated source-dependent factors:

\[
K_{occupied}(q)=\sum_i T_i(q)K_{0,i}(q)B_i(x_i(q)).
\]

A general occupancy factor is not automatically the fraction of calendar time; its correlation with exposure must match the design convention. No numeric occupancy defaults or design goals occur in the supplied nine pages.

If an external goal has already been adjusted for occupancy, do not apply that same adjustment again. Store whether a goal is an occupied-person criterion P or the already adjusted physical-field limit P/T, and use the corresponding comparison once.

Maintain two outputs:

- `physicalAirKerma`: radiation field at the point, independent of whether a person is there.
- `occupancyWeightedAirKerma`: the quantity used by this occupancy-based design comparison.

For `P>0`, define `utilization=occupancyWeightedAirKerma/P` and `margin=P-occupancyWeightedAirKerma`. A utilization <=1 satisfies this selected numeric criterion **only if the model and all required contributions are valid and complete**. It is not automatic regulatory approval.

For `T=0`, retain and display the physical field. Mark the design comparison as `NotEvaluatedZeroOccupancy` or an explicitly approved exclusion, rather than declaring the point intrinsically safe.

### 8.3 Required transmission and thickness, one spectrum

For `K0>0`, `T>0`, and `P>0`:

\[
B_{required}=\frac{P}{TK_0}.
\]

- If `B_required>=1`, the scalar criterion needs zero added thickness.
- If `0<B_required<1`, compute `x_required=A.3(B_required)`.
- If `K0=0`, this contribution requires no shielding, but absent/unknown sources must not be encoded as zero.
- A zero goal with a positive occupied source has no finite exact solution under these fits.
- A negative goal, negative workload, or nonfinite value is an error.

A.3 returns the **total required path thickness**, measured from the original unshielded spectrum. If a justified contiguous homogeneous barrier already provides `x_existing`, the additional path thickness is:

\[
\Delta x_{required}=\max(0,x_{required,total}-x_{existing}).
\]

For a valid full-slab incidence cosine `c`, the corresponding added normal thickness is `delta_t_normal=c*delta_x_required`. Do not treat the total returned by A.3 as automatically additional shielding, and do not apply this subtraction across different materials or unsupported separated layers.

If a fixed contribution outside the adjustable barrier also consumes the budget:

\[
P_{remaining}=P-K_{occupied,fixed}.
\]

Use `P_remaining` in the variable contribution's solve. If the fixed contribution alone exceeds P, changing this barrier cannot meet the goal. If it equals P and a positive variable contribution remains, no finite thickness can make the total exactly equal to P in this model.

### 8.4 Multiple spectra: solve the weighted sum

For a common adjustable normal thickness `t` and valid incidence cosines `c_i`:

\[
F(t)=K_{occupied,fixed}+\sum_i T_iK_{0,i}
B_i\!\left(x_{existing,i}+\frac{t}{c_i}\right)-P.
\]

The existing and added thicknesses may be combined here only when the homogeneous-path assumptions in Section 10 are justified. With no existing shielding set `x_existing=0`. Contributions that miss this adjustable barrier belong in the fixed term, not a thickness-dependent term.

For positive valid fits, F is nonincreasing. Use bracketed bisection:

For extreme ranges, implement the sign test by summing positive occupied contributions in log space with `LogTransmission` and `LogAdd`, then comparing `logK_total` with `log(P)`. The displayed F expression defines the mathematical residual; directly exponentiating tiny B values is not required and can otherwise create a false feasible result through underflow. Handle genuine zero terms explicitly and validate all weights before taking logarithms.

```text
if F(0) <= 0: return 0
if fixedOccupied >= P:
    return NoFiniteSolutionOrBudgetExhausted
low = 0
high = configuredInitialThickness
while F(high) > 0:
    grow high, capped at configured search limit
    if no bracket within limit: return NoBracketWithinConfiguredLimit
while high-low > requested thickness tolerance and iterations < maxIterations:
    mid = low + (high-low)/2
    if mid == low or mid == high: stop with NumericResolutionLimit
    if F(mid) <= 0: high = mid
    else: low = mid
    increment iterations
if iteration limit reached before tolerance: report IterationLimit
return high  // feasible side, followed by forward verification
```

Distinguish `no bracket within a chosen search limit` from `physically no finite solution`. Do not average kVp, alpha, beta, gamma, or first compute per-protocol thicknesses and average them. Do not allocate the whole goal independently to every simultaneous contribution and then sum their results.

Require finite positive initial thickness, search limit and stopping tolerance, `initialThickness<=searchLimit`, a positive maximum iteration count, and a growth factor greater than one. Cap growth before it can overflow; if the upper bound cannot increase, stop with an explicit bracket failure. Numeric resolution/iteration stops may preserve a feasible upper bound, but must report that the requested convergence tolerance was not established. The `fixedOccupied>=P` branch is evaluated only after the zero-thickness check, so an already feasible case is returned first.

For one homogeneous material protecting several POIs, compute the required normal thickness per POI and choose the maximum if one uniform barrier thickness is required. Account for fixed contributions and geometry for each point. Optimizing several walls/materials together is a separate constrained problem; this scalar solver does not solve it automatically.

## 9. Unity geometry and the point of interest

`ENGINEERING`: the appendix has no room geometry algorithm. The following specifies what a deterministic implementation must do.

### 9.1 Coordinate and distance contract

- Declare `metersPerUnityUnit`. Recommended project convention: `1.0`, explicitly stored rather than assumed.
- Resolve Target, Scatter, ROI and provider sample origins into the same world frame. Record any coordinate transform applied to a supplied scatter map.
- Use `d_i = length(q-s_i)*metersPerUnityUnit` only for a provider that requires this distance. Target-to-Scatter and Scatter-to-ROI are different distances.
- Use source-to-POI distance for inverse-square scaling, not source-to-wall plus an arbitrary offset. An occupancy point behind a wall is a distinct point from the wall surface.
- Define POI height, adjacent occupied location, floor/ceiling elevation and standoff explicitly. No universal POI height or standoff is supplied by Appendix A.
- Model finite walls, floors, ceilings, doors, windows, joints and penetrations that affect the path. A visible mesh is not evidence of shielding continuity.

### 9.2 Infinite slab formula

For a unit source-to-point direction `u` and a unit slab normal `n`:

\[
c=|u\cdot n|=|\cos\theta|,\qquad
x_{path}=\frac{t_n}{c}.
\]

At normal incidence `c=1`, path thickness equals normal thickness. At 60 degrees to the normal, `c=0.5`, so the infinite-slab path is twice the normal thickness.

This is a **geometric path-length approximation**. The appendix does not separately validate the angular response of its broad-beam fits. Applying A.2 to this oblique path must be identified as that approximation and reviewed for applicability.

Near `c=0`, do not invent a huge path by clamping the denominator. A finite ray may miss the slab, run beside it, or exit an edge. Use actual finite-solid intersections. A tangent or coplanar ambiguous path should be flagged until its geometry is resolved.

### 9.3 Finite homogeneous barrier intersection

Let `L=length(q-s)` in metres and `u=(q-s)/L`. Parameterize:

\[
r(\ell)=s+\ell u,\qquad 0\le\ell\le L.
\]

Intersect that segment with a closed shielding solid. Clip each entry/exit interval to `[0,L]`:

\[
x_{path,mm}=1000\sum_k \max(0,\ell_{exit,k}-\ell_{entry,k}).
\]

The sum is a geometrical length; whether disconnected intervals can be treated as one attenuation slab is the separate physical-model question in Section 10. Distances measured in Unity units must first be multiplied by `metersPerUnityUnit`.

Implementation requirements:

1. Associate each shielding volume with material, spectrum-model identity, thickness/solid definition, and source provenance.
2. Prefer analytic intersections for simple boxes/slabs; these are easier to validate than arbitrary meshes.
3. Sort intersection parameters along the finite source-to-POI segment.
4. Determine entry versus exit using robust solid containment/orientation logic; do not assume a single ray hit supplies wall thickness.
5. Correctly handle a source or POI inside a solid. A surface-hit API alone may omit an initial entry.
6. Use a documented geometric tolerance in metres. Merge coincident boundaries carefully; avoid counting both faces twice.
7. Detect overlapping shielding volumes. Merge redundant same-material geometry when it describes the same physical region; overlapping solids must not automatically double the material mass.
8. For overlapping different materials, require an explicit constructive-solid/material interpretation. Do not guess which one occupies the overlap.
9. Ignore intersections beyond the POI or behind the source.
10. Treat an opening as absence of the corresponding shielding along that path. An open door has a different geometry state from a closed door.
11. Reject open/nonmanifold/unresolved meshes when their interior cannot be established. A two-sided visual surface is not a solid wall.
12. Record entry/exit coordinates and accumulated thickness for audit/debug display.

The physics result should be computed by a deterministic geometry service. Unity collider/raycast APIs may help find candidates, but their exact version-specific behavior must be tested in the target Unity project; this document does not claim a single `RaycastAll` call solves closed-solid thickness extraction.

### 9.4 Missing shielding and weak paths

For a supported source model and a genuinely unobstructed path, `x=0` and `B=1`. A missing material definition is an error, not an unobstructed path. Check doors, service penetrations, lead-sheet seams and barrier edges with appropriate POIs; a single central-wall calculation cannot demonstrate continuity over an entire room.

Finite source size can expose both shielded and unshielded paths. Represent this with a validated source distribution and weighted contributions. One centre ray cannot establish that all secondary radiation follows that path.

## 10. Multiple layers, materials, existing shielding, and density

### 10.1 Contiguous layers of the same material and fit

`DERIVED`/`ENGINEERING`: if layers physically act as one contiguous homogeneous barrier under the same fit conditions:

\[
x_{total}=x_1+x_2+\cdots,\qquad B_{total}=B(x_{total}).
\]

In general:

\[
B(x_1+x_2)\ne B(x_1)B(x_2).
\]

The second expression restarts the original-spectrum broad-beam fit at each layer and loses beam-hardening behavior. For 120-kVp CT lead, `B(1 mm)^2` is about 0.000258, while `B(2 mm)` is about 0.001240; naive multiplication would substantially underpredict the transmitted field.

If an existing homogeneous path `x0` is known and a further contiguous same-material path `delta` is added, the incremental ratio is:

\[
\frac{K(x_0+\Delta x)}{K(x_0)}
=\frac{B(x_0+\Delta x)}{B(x_0)}.
\]

It is not generally `B(delta)`. If K0 refers to a field already shielded by existing material, it must not be treated as unshielded without this distinction.

### 10.2 Different materials or separated barriers

The attachment supplies homogeneous-material fits, not a spectral transport law for arbitrary stacks. Do not automatically calculate:

```text
B_total = B_lead(lead_mm) * B_concrete(concrete_mm)
```

Do not add millimetres of different materials. Material order, spectral hardening, buildup, and geometry can matter. Similarly, separated slabs with air gaps or different broad-beam collection geometry are not validated merely by summing thicknesses.

The provider must resolve the path using one of the following documented backend models, without adding another user-facing case selector:

- A separately validated multilayer/spectral model with its own provenance.
- A separately validated composite transmission dataset for that stack and geometry.
- A clearly labelled planning calculation crediting only one independently justified barrier, with the ignored layers and conditional nature reported; this is not an appendix-provided composite model or a guaranteed universal bound.
- `UnsupportedCompositeBarrier`, returning no complete design verdict.

A point may have contributions that cross different valid single-material barriers; those contributions can still be added. The unsupported case is an individual contribution requiring an unjustified composite transmission model.

### 10.3 Transmission-specific material equivalence

`DERIVED`: for a supported spectrum and two supported homogeneous materials, an equal-transmission thickness can be calculated:

\[
B_* = B_A(x_A),\qquad x_B = B_B^{-1}(B_*).
\]

For the two supplied CT materials this can compare lead and concrete at the same supplied kVp. Equivalence depends on spectrum and target transmission; it is not a fixed universal conversion factor and does not authorize arbitrary layer mixing.

### 10.4 Density and construction metadata

`SOURCE`: Table A.1 footnote a says the concrete fits assume **standard-weight concrete**. No numeric reference density, allowable density range, or density-scaling equation appears in the attachment. Figure A.3 identifies concrete but does not print a separate numeric density.

Store available material density, composition, product certification and minimum construction thickness, but leave the reference density unknown unless a documented external source supplies it. Do not silently insert a remembered concrete density or apply `x*rho/rho_ref` as an appendix equation. A validated mass-thickness correction is a separate optional model, not enabled here.

Nominal wall thickness, lead-equivalent product ratings and minimum installed shielding thickness are distinct. If a product supplies a certified equivalence, retain its beam quality and testing conditions. Do not equate lead acrylic, lead glass and pure lead by a common label.

After solving required normal thickness, select an available nominal thickness whose **guaranteed minimum installed thickness** meets the requirement under the adopted design assumptions. Rounding up to a stock increment is:

\[
t_{stock}=\Delta t\,\left\lceil t_{required}/\Delta t\right\rceil,
\]

only if uniform increments and no construction deduction are appropriate. If manufacturing tolerance subtracts `delta_t`, require `t_stock-delta_t>=t_required`. Forward-check the selected minimum thickness; do not round the computed result down.

## 11. Numerically stable calculation and C# reference core

### 11.1 Stable A.2 and A.3

`DERIVED`: set `r=beta/alpha`, `z=alpha*gamma*x`, and `y=-gamma*ln(B)`.

\[
\ln B=-\frac{z+\ln[1+r(1-e^{-z})]}{\gamma}.
\]

For small arguments use `expm1(v)=exp(v)-1` and `log1p(v)=ln(1+v)` with stable implementations:

```text
logB = -(z + log1p(r * (-expm1(-z)))) / gamma
```

For the inverse:

\[
x=\frac{\ln\left[1+\frac{e^y-1}{1+r}\right]}{\alpha\gamma}.
\]

For moderate y use `log1p(expm1(y)/(1+r))/(alpha*gamma)`. For large y use:

\[
x=\frac{y+\ln(1+r e^{-y})-\ln(1+r)}{\alpha\gamma}.
\]

These formulas also support the negative-beta wood rows in Table A.1 because all supplied fits satisfy `r>-1`. Do not enforce `beta>=0` across the general archive, though all four CT fits do have positive beta.

### 11.2 Copyable C# numerical core

`ENGINEERING`: pure .NET code, independent of Unity scene objects. This implements the transmission mathematics; coefficient selection, scene intersection, source validation and clinical design policy remain explicit services outside this class. The `ArcherFit` constructor is deliberately general so it can validate the archived primary fits too. The CT UI must still restrict selection to the CT dataset.

```csharp
using System;

public struct ArcherFit
{
    public readonly double AlphaPerMm;
    public readonly double BetaPerMm;
    public readonly double Gamma;

    public ArcherFit(double alphaPerMm, double betaPerMm, double gamma)
    {
        if (!CtShieldMath.IsFinite(alphaPerMm) || alphaPerMm <= 0.0 ||
            !CtShieldMath.IsFinite(betaPerMm) ||
            !CtShieldMath.IsFinite(gamma) || gamma <= 0.0 ||
            !CtShieldMath.IsFinite(alphaPerMm + betaPerMm) ||
            alphaPerMm + betaPerMm <= 0.0 ||
            !CtShieldMath.IsFinite(alphaPerMm * gamma) ||
            alphaPerMm * gamma <= 0.0)
            throw new ArgumentOutOfRangeException("Invalid Archer coefficients.");
        AlphaPerMm = alphaPerMm;
        BetaPerMm = betaPerMm;
        Gamma = gamma;
    }
}

public static class CtShieldMath
{
    public static bool IsFinite(double v)
    {
        return !double.IsNaN(v) && !double.IsInfinity(v);
    }

    private static void CheckFit(ArcherFit f)
    {
        // A struct may be default-initialized without its constructor.
        if (!IsFinite(f.AlphaPerMm) || f.AlphaPerMm <= 0.0 ||
            !IsFinite(f.BetaPerMm) || !IsFinite(f.Gamma) || f.Gamma <= 0.0 ||
            !IsFinite(f.AlphaPerMm + f.BetaPerMm) ||
            f.AlphaPerMm + f.BetaPerMm <= 0.0 ||
            !IsFinite(f.AlphaPerMm * f.Gamma) ||
            f.AlphaPerMm * f.Gamma <= 0.0)
            throw new ArgumentOutOfRangeException("Invalid Archer fit.");
    }

    private static double Log1p(double v)
    {
        if (!IsFinite(v) || v <= -1.0)
            throw new ArgumentOutOfRangeException("log1p domain.");
        if (Math.Abs(v) >= 1e-4) return Math.Log(1.0 + v);
        return v * (1.0 + v * (-0.5 + v * (1.0 / 3.0 +
               v * (-0.25 + v * 0.2))));
    }

    private static double Expm1(double v)
    {
        if (Math.Abs(v) >= 1e-5) return Math.Exp(v) - 1.0;
        return v * (1.0 + v * (0.5 + v * (1.0 / 6.0 +
               v * (1.0 / 24.0 + v / 120.0))));
    }

    public static double LogTransmission(ArcherFit f, double thicknessMm)
    {
        CheckFit(f);
        if (!IsFinite(thicknessMm) || thicknessMm < 0.0)
            throw new ArgumentOutOfRangeException("thicknessMm");
        if (thicknessMm == 0.0) return 0.0;
        double r = f.BetaPerMm / f.AlphaPerMm;
        double z = f.AlphaPerMm * f.Gamma * thicknessMm;
        if (!IsFinite(r) || !IsFinite(z))
            throw new OverflowException("Fit/thickness exceeds numeric range.");
        double logB = -(z + Log1p(r * (-Expm1(-z)))) / f.Gamma;
        if (!IsFinite(logB) || logB > 0.0)
            throw new ArithmeticException("Invalid computed log transmission.");
        return logB;
    }

    public static double Transmission(ArcherFit f, double thicknessMm)
    {
        // May numerically underflow to 0; preserve LogTransmission in result DTO.
        return Math.Exp(LogTransmission(f, thicknessMm));
    }

    public static double ThicknessFromLogTransmission(ArcherFit f, double logB)
    {
        CheckFit(f);
        if (!IsFinite(logB) || logB > 0.0)
            throw new ArgumentOutOfRangeException("Finite logB <= 0 required.");
        if (logB == 0.0) return 0.0;
        double r = f.BetaPerMm / f.AlphaPerMm;
        double y = -f.Gamma * logB;
        if (!IsFinite(r) || !IsFinite(y))
            throw new OverflowException("Fit/target exceeds numeric range.");
        double numerator = y <= 50.0
            ? Log1p(Expm1(y) / (1.0 + r))
            : y + Log1p(r * Math.Exp(-y)) - Log1p(r);
        double x = numerator / (f.AlphaPerMm * f.Gamma);
        if (!IsFinite(x) || x < 0.0)
            throw new ArithmeticException("Invalid computed thickness.");
        return x;
    }

    public static double ThicknessFromTransmission(ArcherFit f, double b)
    {
        if (!IsFinite(b) || b <= 0.0 || b > 1.0)
            throw new ArgumentOutOfRangeException("0 < transmission <= 1 required.");
        return ThicknessFromLogTransmission(f, Math.Log(b));
    }

    public static double RequiredPathThicknessMm(
        ArcherFit f, double unshieldedMgyPerWeek,
        double occupancy, double goalMgyPerWeek)
    {
        CheckFit(f);
        if (!IsFinite(unshieldedMgyPerWeek) || unshieldedMgyPerWeek < 0.0 ||
            !IsFinite(occupancy) || occupancy <= 0.0 || occupancy > 1.0 ||
            !IsFinite(goalMgyPerWeek) || goalMgyPerWeek <= 0.0)
            throw new ArgumentOutOfRangeException("Invalid design inputs.");
        if (unshieldedMgyPerWeek == 0.0) return 0.0;
        double logRequired = Math.Log(goalMgyPerWeek)
                           - Math.Log(occupancy)
                           - Math.Log(unshieldedMgyPerWeek);
        if (logRequired >= 0.0) return 0.0;
        return ThicknessFromLogTransmission(f, logRequired);
    }

    public static double LogAdd(double logA, double logB)
    {
        // Negative infinity represents a zero contribution, never missing data.
        if (double.IsNaN(logA) || double.IsNaN(logB) ||
            double.IsPositiveInfinity(logA) || double.IsPositiveInfinity(logB))
            throw new ArgumentOutOfRangeException("Invalid log contribution.");
        if (double.IsNegativeInfinity(logA)) return logB;
        if (double.IsNegativeInfinity(logB)) return logA;
        double hi = Math.Max(logA, logB);
        double lo = Math.Min(logA, logB);
        return hi + Log1p(Math.Exp(lo - hi));
    }
}
```

CT coefficient construction:

```csharp
var lead120 = new ArcherFit(2.246, 5.73, 0.547);
var lead140 = new ArcherFit(2.009, 3.99, 0.342);
var concrete120 = new ArcherFit(0.0383, 0.0142, 0.658);
var concrete140 = new ArcherFit(0.0336, 0.0122, 0.519);
```

For positive K0, compute `logK = log(K0) + logB`; use `LogAdd` to sum extreme-range contributions. For zero K0, its log contribution is negative infinity only after validation has established it is truly zero. If `exp(logB)` or `exp(logK)` underflows, show a numeric-underflow/less-than-display-threshold indication; do not claim perfect shielding or exactly zero physical radiation.

The code throws for primitive-domain failures. The application should translate these exceptions into structured statuses before updating the UI. Zero occupancy and zero design goals are intentionally handled at the workflow level, not silently accepted by `RequiredPathThicknessMm`.

## 12. Unified diagnostic data and persistence contract

`ENGINEERING`: import structured model data into versioned DTOs or catalogue assets, not executable Markdown prose. Use invariant-culture stored numbers, unique IDs and strict validation. This section proposes the new records; it does not describe fields already implemented in the current binary.

### 12.1 Required model records

| Record | Required fields |
|---|---|
| Native design | Existing version-2 geometry, item IDs, walls, doors, locks, groups and preserved source document. |
| Diagnostic calculation | Its own version, coordinate conversion, active machine/ROI IDs, quantity and output basis. |
| Machine calculation | Scene item ID, typed family, profile ID/version, one kVp value, required numeric exposure inputs, Target/Scatter IDs. |
| Point | Stable ID, Target/Scatter/ROI role, machine association, coordinate space and double-precision physical position. |
| Machine profile | Supported family/spectra, exposure definition, source-output provider, normalization, components, transforms, validity domains and approval metadata. |
| Fit | ID, component/beam family, material, spectrum, coefficients/model, units, applicability and retained source metadata. |
| Barrier | Existing material and physical thickness/solid geometry, minimum installed specification and applicable model identity. |
| Contribution | Machine/profile/component ID, spectrum, effective origin/sample, ROI ID, K0 basis, path and transmission. |
| Comparison | Requested flag, numeric limit, quantity/unit/basis and occupancy only when that convention requires it. |
| Result | Physical air kerma, component ledger, completeness, readiness issues/actions, basis, input fingerprint and optional comparison/thickness. |

Profile metadata is read-only or supplied through validated profile import. It is not a collection of user-authored text boxes or manual confirmation flags.

### 12.2 Incomplete real-project template

The following is a JSON **fragment** illustrating the proposed optional `diagnosticCalculation` block in an existing version-2 native design. The referenced scene items/walls and mandatory native fields are omitted; it is not a standalone importable design or a calculable example. Null means missing, not zero.

```json
{
  "version": 2,
  "schemaVersion": 2,
  "diagnosticCalculation": {
    "version": 1,
    "metersPerUnityUnit": 1.0,
    "quantity": "airKerma",
    "displayUnit": "Gy",
    "basis": "ProfileExposure",
    "activeMachineId": "DEVICE_001",
    "activeRoiId": "ROI_001",
    "machines": [
      {
        "machineId": "DEVICE_001",
        "machineType": "DentalIntraoral",
        "profileId": null,
        "profileVersion": null,
        "tubeVoltageKvp": null,
        "exposureInputs": {},
        "weeklyExposureCount": null,
        "targetPointId": "TARGET_001",
        "scatterPointId": "SCATTER_001"
      }
    ],
    "points": [
      {"id":"TARGET_001","role":"Target","machineId":"DEVICE_001","coordinateSpace":"DeviceLocal","positionMeters":null},
      {"id":"SCATTER_001","role":"Scatter","machineId":"DEVICE_001","coordinateSpace":"World","positionMeters":null},
      {"id":"ROI_001","role":"ROI","machineId":"DEVICE_001","coordinateSpace":"World","positionMeters":null}
    ],
    "comparison": {
      "requested": false,
      "limitValue": null,
      "quantity": "airKerma",
      "unit": "Gy",
      "basis": "ProfileExposure",
      "occupancy": null
    },
    "currentResult": {
      "state": "MissingPoint",
      "inputFingerprint": null,
      "shieldedAirKermaGy": null,
      "canCompare": false,
      "canSolveThickness": false
    }
  }
}
```

Each position resolves to a finite `{x,y,z}` in metres when placed. Point IDs must resolve to actual scene markers, and machine IDs to actual equipment. Double-precision point records are authoritative; Unity float transforms are their display/edit projection.

If the serializer cannot preserve nullable numbers, use internal presence fields; never expose them as "has value" checkboxes or deserialize missing values to zero. Unknown families/roles, duplicate IDs, missing owners, unsupported versions and mismatched profiles must fail explicitly.

### 12.3 Provider-specific metadata, not additional form fields

These fields belong to the structured profile/provider. Only the necessary physical exposure values appear as numeric inputs in Section 2.

For `REFERENCE_POINT_KERMA`, require calibrated `referenceDistanceMeters`, `referenceKermaValue`, `referenceKermaUnit`, normalization, component origin, inverse-square applicability domain and directional function identity. Apply exposure/workload only when that law needs it.

For `SPATIAL_FIELD`, require map samples/provider, quantity/unit, normalization, coordinate transform, interpolation rule and valid spatial domain. Do not also request a point-source distance correction unless the map's specification explicitly calls for it.

For `EXTERNAL_DLP_MODEL`, require DLP units, normalization, conversion coefficient and units, reference geometry, applicability metadata and component coverage. Retain model provenance automatically. No numeric DLP coefficient is contained in this file.

Profile import must validate structure, units, supported machine identities, spectrum/material coverage, component nonoverlap and domains before activation. Merely loading a file or setting an `approved` boolean is not evidence of physical applicability. Activation is limited to independently approved catalogue entries/provider versions; approval is managed outside the room-editing UI. Reject invalid profiles without mutating the active design.

### 12.4 Suggested result status vocabulary

```text
ValidCalculation
ValidCalculationWithDeclaredApproximation
MissingRequiredInput
MissingRequiredPoint
NoApplicableMachineProfile
UnsupportedSpectrum
UnsupportedMaterialSpectrumCombination
UnsupportedCompositeBarrier
SourceModelOutsideValidityDomain
InvalidUnitsOrNormalization
InvalidGeometry
InvalidNumericInput
IncompleteContributions
NotEvaluatedMissingDesignGoal
NotEvaluatedZeroOccupancy
NoFiniteSolutionOrBudgetExhausted
NoBracketWithinConfiguredLimit
OutsidePlottedThicknessRange
NumericalUnderflow
```

These are backend calculation statuses, distinct from the user-facing readiness states in Section 2.5. Map every blocking status to its affected input/object and an actionable explanation.

Keep errors, validated model limitations and informational flags separate. Arithmetic can be computed for testing even when applicability is unresolved, but that is not a complete physical ROI result. `meetsSelectedNumericCriterion` is nullable and must never become `true` with missing or unsupported contributions.

### 12.5 Non-destructive migration and native persistence

Keep the existing native design root at version 2 and version the optional diagnostic block independently. Missing diagnostic metadata in an older design means unconfigured, not an invented source.

Migration must preserve room geometry, barrier units/materials, doors, floor-plan guides, groups, locks, source documents and existing scientific/historical data.

| Existing data | Required handling |
|---|---|
| Legacy `Design.ct` records | Preserve them and their source metadata. A migration adapter creates normalized records only when their meanings are established; do not run old and new providers together. |
| Legacy CT Scatter marker | Preserve its resolved physical world position and known association. It does not also become Target. |
| Legacy `Patient` evaluation point | Preserve its old role/results as legacy data. Do not silently turn a previous evaluation point into a scatter origin. Guide the user to choose/place the new Scatter and ROI explicitly. |
| Legacy ROI | Retain its coordinates and source association when unambiguous. Do not move it onto a wall or patient. |
| Missing Target | Use a declared physical profile anchor if available; otherwise request **Place Target**. Never reinterpret the patient/scatter point as the device source. |
| Best/Nominal/Worst factors and snapshots | Keep historical snapshots unchanged. The new active input has no factors or case selector. If a scaled legacy input cannot be mapped unambiguously to actual exposure, request one explicit numeric exposure value. |
| Legacy review flags and narrative inputs | Preserve as historical metadata, not new UI gates. Never turn an unapproved model into an approved profile by migrating a checkbox. |
| Legacy LINAC energy/workload | Retain for the treatment provider with its original units. Do not copy into diagnostic kVp/exposure. |
| Old Beam/CT navigation state | Open Calculation in the appropriate machine branch, including result and point-property setup actions. |

Migration is atomic and idempotent. Reopening a design, switching tabs or selecting a marker must not create another source, patient, workload or ROI. Unresolved associations stay visibly unresolved.

Undo/redo, copy/paste, deletion and protection use existing scene helpers. Remap copied machine/owned-marker IDs consistently; do not activate copied exposures automatically or leave dangling owners. Protected objects remain unchanged until explicitly unlocked.

Save/load must retain incomplete nullable numeric inputs, exact roles/positions, profile references and the current case without manufacturing zero values. Missing profile files after loading produce a specific **Load matching profile** action; loading must not silently switch models.

Native snapshots record all inputs and the applicable provider/dataset versions. Revalidate their fingerprint before treating them as current; imported snapshots never establish readiness for a different design.

Do not add these editor records to a canonical export format that cannot represent them. Preserve existing round-trip and explicit unsupported-export behavior. Calculation markers and floor-plan guides remain annotations, never shielding solids or source contributions.

## 13. Simulation and visualization behavior

`ENGINEERING`

- A heatmap should identify its quantity and unit: for example `unshielded mGy/week`, `shielded mGy/week`, `transmission`, or `occupancy-weighted mGy/week`.
- For physical radiation heatmaps, do not reduce the field by occupancy. A separate design-comparison overlay can show occupancy-weighted utilization.
- Logarithmic color scaling is useful, but any rendering floor such as `max(value,displayMinimum)` belongs only to the visualization. Preserve the actual/log calculation values in exported results.
- Show unsupported/missing calculations with a distinct state, not the zero-dose color.
- Cache immutable coefficient/profile lookups. Invalidate physical results when machine/profile, voltage, exposure, point position, material, geometry or door state changes. Occupancy/goal changes invalidate only dependent comparison/thickness outputs.
- Separate deterministic scientific calculation from frame-rate animation. A moving particle effect is illustrative unless a separately validated transport model drives it.
- A spatial grid has finite resolution. Check critical POIs and refine around openings/edges and large field gradients; a coarse grid does not prove a maximum was found.
- Keep geometry-ray overlays and contribution inspection available for debugging: selected fit, source, path, thickness, K0, B, Kx and occupancy weighting should be inspectable.
- Use the Target/Scatter/ROI role labels on rays and markers. A patient/scatter point must not receive an ROI result badge.
- Present current results and live next-step guidance within Calculation. Optional details/history may open a panel, but missing-input recovery must not depend on a modal dialog.
- Keep Windows/WebGL file-picking, compact layouts, point properties, help text and machine-selection routes consistent with the single Calculation tab.
- The appendix does not provide stochastic scattering angles, photon energy distributions, interaction cross sections, buildup transport kernels, room-scatter/skyshine models or time-resolved dose rates. Do not present an animated Monte Carlo claim using A.2 alone.

## 14. Worked numerical examples

All workloads, source strengths, occupancies and goals in this section are `DEMO_ONLY`. They are not values supplied by the appendix and must not become new-project defaults.

### 14.1 Forward calculation: 120-kVp CT with a 2-mm lead path

Inputs:

```text
reference source air kerma = 0.2 mGy/exam at 1 m
weekly exam count = 200 exams/week
effective source-to-POI distance = 3 m
inverse-square/direction model = explicitly assumed valid for this demo
lead path thickness = 2.0 mm
occupancy = 0.25
goal = 0.002 mGy/week (invented test value)
fit = CT_PB_120
```

\[
K_0=200(0.2)(1/3)^2=4.444444444444\ \mathrm{mGy/week}.
\]

\[
B(2)=0.001239990179488295.
\]

\[
K_{shielded}=0.005511067464392423\ \mathrm{mGy/week},
\]

\[
K_{occupied}=0.25K_{shielded}
=0.001377766866098106\ \mathrm{mGy/week}.
\]

This arithmetic meets the invented `0.002 mGy/week` criterion. The physical field is still `0.005511067464392423 mGy/week`; it is not the occupancy-weighted number.

For the same source and goal, `B_required=0.0018`; A.3 gives a total required **path** thickness of `1.845319208277487 mm`. Convert to normal thickness only with the relevant geometry model. The existing 2-mm path already exceeds this demo requirement, so the required added thickness is zero.

### 14.2 Inverse calculation: 1 percent transmission

`DERIVED` from the four source fits:

| Fit | Required path thickness for B=0.01 (mm) | Required path thickness for B=0.001 (mm) |
|---|---:|---:|
| CT_PB_120 | 1.170970658514653 | 2.090191206825494 |
| CT_PB_140 | 1.201337481742548 | 2.095803721322255 |
| CT_CONCRETE_120 | 108.4300860602584 | 168.0013128501408 |
| CT_CONCRETE_140 | 121.1723232772192 | 188.3995318562166 |

These are path thicknesses, not automatically normal wall thicknesses, and are not construction recommendations without the remaining design inputs.

### 14.3 Two protocol contributions

Assume the externally supplied unshielded POI contributions are `K0_120=3 mGy/week` and `K0_140=1 mGy/week`, both through a valid 2-mm lead path:

\[
K=3(0.001239990179488295)+1(0.001258504786528333)
=0.004978475324993218\ \mathrm{mGy/week}.
\]

Apply the POI's valid occupancy convention afterward. This is not the result of evaluating a lead curve at the average tube potential.

### 14.4 Oblique slab example

For a 1-mm normal lead slab and incidence at 60 degrees to the normal, an infinite-slab approximation gives a 2-mm path. Therefore the 120-kVp transmission is `0.001239990179488295` if that approximation is applicable. For a finite panel, confirm the ray exits the opposite slab face rather than a side edge before using this value.

## 15. Numerical verification dataset and acceptance rules

### 15.1 Forward benchmarks

`DERIVED` using A.2. Retain the full source coefficient precision; shown extra output digits are calculation precision, not physical measurement accuracy.

```csv
fit_id,path_mm,expected_transmission
CT_PB_120,0,1
CT_PB_120,0.5,0.07885850724852890
CT_PB_120,1,0.01606369426375395
CT_PB_120,2,0.001239990179488295
CT_PB_120,3,0.0001207782315903332
CT_PB_140,0,1
CT_PB_140,0.5,0.09659280317034600
CT_PB_140,1,0.01801306647431474
CT_PB_140,2,0.001258504786528333
CT_PB_140,3,0.0001275469390150275
CT_CONCRETE_120,0,1
CT_CONCRETE_120,50,0.1030057512057174
CT_CONCRETE_120,100,0.01390034709918690
CT_CONCRETE_120,200,0.0002926267010734886
CT_CONCRETE_120,300,0.000006337276497184778
CT_CONCRETE_140,0,1
CT_CONCRETE_140,50,0.1288248487488016
CT_CONCRETE_140,100,0.02096420076642798
CT_CONCRETE_140,200,0.0006748087984019304
CT_CONCRETE_140,300,0.00002313669196541927
```

### 15.2 Required calculation checks

1. `B(0)=1` and `x(1)=0`.
2. For each supplied fit, transmission stays in `(0,1]` for ordinary finite nonnegative test thicknesses and decreases as thickness increases.
3. `x(B(x))` reproduces x; `B(x(B))` reproduces B over useful ranges. Include near-one B and tiny B values.
4. The high-thickness ratio `B(x+h)/B(x)` approaches 0.5 when `h=ln(2)/alpha`.
5. The initial logarithmic slope approaches `-(alpha+beta)`.
6. Negative-beta wood fits remain valid; do not reject them merely because beta is negative.
7. Millimetre/metre mistakes are caught by unit tests: 0.002 m of lead is 2 mm, not 0.002 mm or 2000 mm.
8. Under an explicitly valid inverse-square point model, doubling distance divides K0 by four. Do not perform this test on an arbitrary spatial map.
9. Doubling workload doubles K0 and Kx; it does not change B.
10. Occupancy changes only the design weighting and required thickness, not the physical radiation field.
11. A zero true workload produces zero kerma; a missing workload produces an error/incomplete result.
12. Unsupported kVp/material/beam family returns a structured unsupported result.
13. Do not silently use radiographic lead beta/gamma for the CT lead fit.
14. Same-material contiguous layers reproduce `B(sum thickness)`, not a product of independently restarted curves.
15. Mixed-material paths are rejected or delegated to an explicitly validated external model.
16. Tests include source inside a solid, POI inside a solid, no wall hit, tangency, finite-panel edge escape, a door opening, duplicate faces, overlapping solids and intersections past the POI.
17. Invalid NaN/infinite/negative values are rejected before arithmetic or visualization.
18. A source map already evaluated at a POI does not receive distance or workload scaling a second time.
19. Included scatter/leakage/workload components are not counted twice.
20. A mixture solver returns the feasible upper bracket and its forward evaluation satisfies the requested numeric tolerance.
21. Large thickness yields finite `logB`; an underflowed displayed B is flagged rather than called exact zero.
22. A missing source/model contribution prevents a complete design-pass verdict even when other calculated contributions meet the goal.

For the displayed numerical benchmarks, a relative tolerance of `1e-10` (or `1e-12` absolute where appropriate) is an example software arithmetic tolerance. For thickness round trips, use a documented absolute/relative tolerance appropriate to the scale. These tolerances do not express physical accuracy of the fitted data or construction tolerances.

### 15.3 Physical uncertainty and backend sensitivity tests

The attachment gives no coefficient covariance, confidence intervals or scanner-specific uncertainty bounds. Do not invent a percentage accuracy. Preserve supplied source/map uncertainty. Sensitivity checks for exposure, geometry, minimum thickness and spectrum belong to model validation and tests, not Best/Worst fields in the current UI.

For the optional point model, independently derived sensitivities include `K proportional to workload`, `K proportional to d^-2`, and `d ln K / dx = -mu_eff(x)`. Therefore a small thickness error can have a large relative effect on a heavily shielded result. These sensitivities explain input influence; they do not supply missing uncertainty magnitudes or make a complete uncertainty budget.

### 15.4 Unified workflow acceptance requirements

These checks are required for the future implementation, not reported as completed by this document revision. Use explicitly labelled test profiles; never install fixture outputs as real-machine defaults.

#### Tab, machine and point behavior

1. The right panel has Room, Object and Calculation, with no separate Beam/CT route on desktop or compact layouts.
2. Selecting CT, intraoral Dental, OPG, CBCT, CathLab, mammography and radiography selects their typed provider branches. A missing profile gives an actionable unavailable-model state, not CT or LINAC fallback arithmetic.
3. MRI/ultrasound and radionuclide machines explain why the X-ray/kVp workflow does not apply. Retained LINAC calculations keep their treatment units and engine.
4. Target, Scatter and ROI have distinct IDs, labels and placement actions. Only ROI receives the measured result.
5. The scatter provider uses the patient point, not the device pivot or an old Patient evaluation point. Primary/leakage uses the profile's device-source geometry.
6. Moving Target, Scatter or ROI invalidates every dependent result. Independent patient/ROI coordinates do not move merely because the device moves.
7. Multiple machines, ROI selection and wall selection preserve explicit associations. Opening a tab or copying a machine never duplicates exposure in the total.
8. Two-dimensional point placement has a declared height; equipment display scale and marker size do not rescale physical distances.

#### Input simplicity and automatic validation

9. Each physical input key has exactly one editable numeric field. No duplicate energy controls, paired slider fields or numeric-presence checkboxes appear.
10. The default diagnostic form has zero required narrative fields and zero manual review/confirmation gates.
11. Exactly one current case is calculated. Best/Worst factors, scenario names, bulk-case save and default A/B comparison controls are absent.
12. Each profile shows only its necessary exposure inputs. Complete per-event profiles calculate without weekly workload, occupancy or a limit.
13. Missing, invalid, nonfinite and explicit-zero inputs are distinguished. Unsupported kVp is not rounded, extrapolated or mapped to the nearest coefficient.
14. CT 120/140 kVp uses the matched CT-secondary fits. Dental/CBCT/CathLab does not inherit those fits merely through asset grouping; mammography requires the appropriate anode/filter identity.
15. A spatial field already normalized at the ROI receives neither a second inverse-square factor nor a second exposure count.
16. Missing material, unknown thickness, unsupported composites and unresolved component coverage prevent a complete physical result rather than producing zero or a partial total.

#### Natural next-step guidance

17. Open Calculation with each prerequisite independently absent: machine, Target, Scatter, ROI, kVp, profile, exposure value, material and thickness.
18. For every omission, the first visible card states the actual missing item, why it prevents the number and an executable next action. No generic "review inputs" placeholder satisfies this test.
19. Activate each next action and verify that it arms the correct placement tool or focuses the exact machine/object/numeric field. One primary action must reach the needed control without searching another tab.
20. With several omissions, actions follow prerequisite order and advance after each correction. Local readiness changes on the next UI update; no failed Calculate attempt is required.
21. Ready-state text identifies the actual machine, entered kVp, exposure basis and applicable shielding path/model. It explains how these inputs produce the ROI value.
22. Editing a linked point or recovering from results uses the same guide/action logic, including compact layouts and protected objects.

#### Numeric result and state correctness

23. Reproduce Section 14.1: physical `0.005511067464392423 mGy/week` displays as `5.511067464392423e-6 Gy/week`, not the occupancy-weighted value.
24. For that fixture, the per-exam physical value is `2.7555337321962115e-8 Gy/exam`; 200 exams/week gives the weekly value exactly once.
25. Before calculation, after a blocker, on provider failure and while stale, the current result is not a numeric zero. Historical values are explicitly distinguished.
26. Zero true exposure gives a valid zero; positive values that underflow retain log-domain information and are not labelled exact zero.
27. Changing voltage, loading, any relevant point, wall or door marks the current physical result stale. Changing only an optional limit/occupancy does not erase the physical field.
28. An asynchronous response for a previously active machine/input fingerprint cannot replace the current machine's result.
29. A missing optional limit/occupancy blocks only the requested comparison/thickness output. Missing physical contributions always block a design-pass verdict.
30. Details and the exported ledger reproduce the displayed physical number and its quantity/unit/basis, without requiring citation text.

#### Persistence and regression boundaries

31. New and incomplete native designs save/load without losing roles, precision, null/missing values, geometry or profile references.
32. Legacy Patient evaluation points and Best/Worst snapshots survive migration without changing their historical meaning or adding hidden active-case multipliers.
33. Repeated migration/setup is idempotent; undo/redo, locks, deletion and copy/paste preserve associations and reject dangling owners.
34. Profile/result import is atomic. Invalid files preserve the active design; cancellation is not reported as successful import.
35. Windows and WebGL use their proper file pickers, with visible errors and the same Calculation workflow.
36. Existing treatment/reference calculations, numerical benchmarks and native/canonical export boundaries retain their intended behavior.
37. Runtime documentation distinguishes UI-family coverage from actual installed validated source/spectrum/material coverage.

Record automated/editor checks plus Windows and browser interaction evidence before declaring UI delivery. Tests for one CT fixture alone do not establish all-diagnostic support.

## 16. Diagnostic primary coefficient archive: not a CT/CBCT fallback

`SOURCE`: PDF pp3-6/report pp118-121. The complete machine-readable data follows later in this section.

Table A.1 fits primary broad beams for lead, concrete, gypsum wallboard, steel, plate glass and wood. Thickness is in mm, alpha and beta are in mm^-1, gamma is dimensionless. The table is organized at 5-kVp intervals where data are supplied.

Source qualifications:

- The 25, 30 and 35 kVp rows correspond to molybdenum-anode mammographic beams. The p117 description also specifies molybdenum filtration.
- All other rows correspond to tungsten-anode beams; p117 describes primary three-phase aluminium-filtered tungsten-anode radiographic beams.
- The concrete fits assume standard-weight concrete, with no numeric reference density in this attachment.
- Concrete has supplied rows at 40 and 45 kVp. The other five materials do not have supplied rows at those two potentials. Missing cells are absent/null, never zero and never interpolated automatically.
- Wood beta becomes negative from 115 through 150 kVp. Preserve every minus sign. The wood gamma at 110 kVp is `3.309`, not `0.3309`.
- The table footnote attributes the data to Archer et al. (1994), Legare et al. (1978), and Simpkin (1987a), interpolated to 5-kVp intervals by Simpkin (1995).
- The general model is attributed in the prose to Archer et al. (1983). The appendix does not contain complete bibliographic titles/DOIs for these references; do not invent them.

The numeric archive contains **146 supplied material/kVp rows and 438 fitting coefficients**: 24 rows each for lead, gypsum, steel, plate glass and wood, and 26 for concrete. With the four CT rows, the attachment supplies 150 coefficient triples in total.

For Figure A.1 reconstruction, calculate `highAttenuationHvlMm=ln(2)/alpha` for each archived row. Keep unsupported material/kVp pairs absent. This exactly preserves the figure's generating information more accurately than reading approximate values off its log axis.

### 16.1 Lead

| kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Printed page |
|---:|---:|---:|---:|---:|
| 25 | 4.952e1 | 1.940e2 | 3.037e-1 | 118 |
| 30 | 3.880e1 | 1.780e2 | 3.473e-1 | 118 |
| 35 | 2.955e1 | 1.647e2 | 3.948e-1 | 118 |
| 40 | NOT_PROVIDED | NOT_PROVIDED | NOT_PROVIDED | 118 |
| 45 | NOT_PROVIDED | NOT_PROVIDED | NOT_PROVIDED | 118 |
| 50 | 8.801 | 2.728e1 | 2.957e-1 | 118 |
| 55 | 7.839 | 2.592e1 | 3.499e-1 | 118 |
| 60 | 6.951 | 2.489e1 | 4.198e-1 | 118 |
| 65 | 6.130 | 2.409e1 | 5.019e-1 | 118 |
| 70 | 5.369 | 2.349e1 | 5.881e-1 | 118 |
| 75 | 4.666 | 2.269e1 | 6.618e-1 | 118 |
| 80 | 4.040 | 2.169e1 | 7.187e-1 | 118 |
| 85 | 3.504 | 2.037e1 | 7.550e-1 | 118 |
| 90 | 3.067 | 1.883e1 | 7.726e-1 | 119 |
| 95 | 2.731 | 1.707e1 | 7.714e-1 | 119 |
| 100 | 2.500 | 1.528e1 | 7.557e-1 | 119 |
| 105 | 2.364 | 1.341e1 | 7.239e-1 | 119 |
| 110 | 2.296 | 1.170e1 | 6.827e-1 | 119 |
| 115 | 2.265 | 1.021e1 | 6.363e-1 | 119 |
| 120 | 2.246 | 8.950 | 5.873e-1 | 119 |
| 125 | 2.219 | 7.923 | 5.386e-1 | 119 |
| 130 | 2.170 | 7.094 | 4.909e-1 | 119 |
| 135 | 2.102 | 6.450 | 4.469e-1 | 119 |
| 140 | 2.009 | 5.916 | 4.018e-1 | 119 |
| 145 | 1.895 | 5.498 | 3.580e-1 | 119 |
| 150 | 1.757 | 5.177 | 3.156e-1 | 119 |

### 16.2 Concrete

| kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Printed page |
|---:|---:|---:|---:|---:|
| 25 | 3.904e-1 | 1.645 | 2.757e-1 | 118 |
| 30 | 3.173e-1 | 1.698 | 3.593e-1 | 118 |
| 35 | 2.528e-1 | 1.807 | 4.648e-1 | 118 |
| 40 | 1.297e-1 | 1.780e-1 | 2.189e-1 | 118 |
| 45 | 1.095e-1 | 1.741e-1 | 2.269e-1 | 118 |
| 50 | 9.032e-2 | 1.712e-1 | 2.324e-1 | 118 |
| 55 | 7.422e-2 | 1.697e-1 | 2.454e-1 | 118 |
| 60 | 6.251e-2 | 1.692e-1 | 2.733e-1 | 118 |
| 65 | 5.528e-2 | 1.696e-1 | 3.217e-1 | 118 |
| 70 | 5.087e-2 | 1.696e-1 | 3.847e-1 | 118 |
| 75 | 4.797e-2 | 1.663e-1 | 4.492e-1 | 118 |
| 80 | 4.583e-2 | 1.549e-1 | 4.926e-1 | 118 |
| 85 | 4.398e-2 | 1.348e-1 | 4.943e-1 | 118 |
| 90 | 4.228e-2 | 1.137e-1 | 4.690e-1 | 119 |
| 95 | 4.068e-2 | 9.705e-2 | 4.406e-1 | 119 |
| 100 | 3.925e-2 | 8.567e-2 | 4.273e-1 | 119 |
| 105 | 3.808e-2 | 7.862e-2 | 4.394e-1 | 119 |
| 110 | 3.715e-2 | 7.436e-2 | 4.752e-1 | 119 |
| 115 | 3.636e-2 | 7.201e-2 | 5.319e-1 | 119 |
| 120 | 3.566e-2 | 7.109e-2 | 6.073e-1 | 119 |
| 125 | 3.502e-2 | 7.113e-2 | 6.974e-1 | 119 |
| 130 | 3.445e-2 | 7.160e-2 | 7.969e-1 | 119 |
| 135 | 3.394e-2 | 7.263e-2 | 9.099e-1 | 119 |
| 140 | 3.345e-2 | 7.476e-2 | 1.047 | 119 |
| 145 | 3.296e-2 | 7.875e-2 | 1.224 | 119 |
| 150 | 3.243e-2 | 8.599e-2 | 1.467 | 119 |

### 16.3 Gypsum wallboard

| kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Printed page |
|---:|---:|---:|---:|---:|
| 25 | 1.576e-1 | 7.175e-1 | 3.048e-1 | 118 |
| 30 | 1.208e-1 | 7.043e-1 | 3.613e-1 | 118 |
| 35 | 8.878e-2 | 6.988e-1 | 4.245e-1 | 118 |
| 40 | NOT_PROVIDED | NOT_PROVIDED | NOT_PROVIDED | 118 |
| 45 | NOT_PROVIDED | NOT_PROVIDED | NOT_PROVIDED | 118 |
| 50 | 3.883e-2 | 8.730e-2 | 5.105e-1 | 118 |
| 55 | 3.419e-2 | 8.315e-2 | 5.606e-1 | 118 |
| 60 | 2.985e-2 | 7.961e-2 | 6.169e-1 | 118 |
| 65 | 2.609e-2 | 7.597e-2 | 6.756e-1 | 118 |
| 70 | 2.302e-2 | 7.163e-2 | 7.299e-1 | 118 |
| 75 | 2.066e-2 | 6.649e-2 | 7.750e-1 | 118 |
| 80 | 1.886e-2 | 6.093e-2 | 8.103e-1 | 118 |
| 85 | 1.746e-2 | 5.558e-2 | 8.392e-1 | 118 |
| 90 | 1.633e-2 | 5.039e-2 | 8.585e-1 | 119 |
| 95 | 1.543e-2 | 4.571e-2 | 8.763e-1 | 119 |
| 100 | 1.466e-2 | 4.171e-2 | 8.939e-1 | 119 |
| 105 | 1.397e-2 | 3.815e-2 | 9.080e-1 | 119 |
| 110 | 1.336e-2 | 3.521e-2 | 9.244e-1 | 119 |
| 115 | 1.283e-2 | 3.271e-2 | 9.423e-1 | 119 |
| 120 | 1.235e-2 | 3.047e-2 | 9.566e-1 | 119 |
| 125 | 1.192e-2 | 2.863e-2 | 9.684e-1 | 119 |
| 130 | 1.155e-2 | 2.702e-2 | 9.802e-1 | 119 |
| 135 | 1.122e-2 | 2.561e-2 | 9.901e-1 | 119 |
| 140 | 1.088e-2 | 2.436e-2 | 9.964e-1 | 119 |
| 145 | 1.056e-2 | 2.313e-2 | 9.987e-1 | 119 |
| 150 | 1.030e-2 | 2.198e-2 | 1.013 | 119 |

### 16.4 Steel

| kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Printed page |
|---:|---:|---:|---:|---:|
| 25 | 9.364 | 4.125e1 | 3.202e-1 | 120 |
| 30 | 7.406 | 4.193e1 | 3.959e-1 | 120 |
| 35 | 5.716 | 4.341e1 | 4.857e-1 | 120 |
| 40 | ROW_OMITTED | ROW_OMITTED | ROW_OMITTED | 120 |
| 45 | ROW_OMITTED | ROW_OMITTED | ROW_OMITTED | 120 |
| 50 | 1.817 | 4.840 | 4.021e-1 | 120 |
| 55 | 1.493 | 4.515 | 4.293e-1 | 120 |
| 60 | 1.183 | 4.219 | 4.571e-1 | 120 |
| 65 | 9.172e-1 | 3.982 | 4.922e-1 | 120 |
| 70 | 7.149e-1 | 3.798 | 5.378e-1 | 120 |
| 75 | 5.793e-1 | 3.629 | 5.908e-1 | 120 |
| 80 | 4.921e-1 | 3.428 | 6.427e-1 | 120 |
| 85 | 4.355e-1 | 3.178 | 6.861e-1 | 120 |
| 90 | 3.971e-1 | 2.913 | 7.204e-1 | 120 |
| 95 | 3.681e-1 | 2.654 | 7.461e-1 | 120 |
| 100 | 3.415e-1 | 2.420 | 7.645e-1 | 120 |
| 105 | 3.135e-1 | 2.227 | 7.788e-1 | 120 |
| 110 | 2.849e-1 | 2.061 | 7.897e-1 | 121 |
| 115 | 2.579e-1 | 1.922 | 8.008e-1 | 121 |
| 120 | 2.336e-1 | 1.797 | 8.116e-1 | 121 |
| 125 | 2.130e-1 | 1.677 | 8.217e-1 | 121 |
| 130 | 1.969e-1 | 1.557 | 8.309e-1 | 121 |
| 135 | 1.838e-1 | 1.440 | 8.391e-1 | 121 |
| 140 | 1.724e-1 | 1.328 | 8.458e-1 | 121 |
| 145 | 1.616e-1 | 1.225 | 8.519e-1 | 121 |
| 150 | 1.501e-1 | 1.132 | 8.566e-1 | 121 |

### 16.5 Plate glass

| kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Printed page |
|---:|---:|---:|---:|---:|
| 25 | 3.804e-1 | 1.543 | 2.869e-1 | 120 |
| 30 | 3.061e-1 | 1.599 | 3.693e-1 | 120 |
| 35 | 2.396e-1 | 1.694 | 4.683e-1 | 120 |
| 40 | ROW_OMITTED | ROW_OMITTED | ROW_OMITTED | 120 |
| 45 | ROW_OMITTED | ROW_OMITTED | ROW_OMITTED | 120 |
| 50 | 9.721e-2 | 1.799e-1 | 4.912e-1 | 120 |
| 55 | 8.552e-2 | 1.661e-1 | 5.112e-1 | 120 |
| 60 | 7.452e-2 | 1.539e-1 | 5.304e-1 | 120 |
| 65 | 6.514e-2 | 1.443e-1 | 5.582e-1 | 120 |
| 70 | 5.791e-2 | 1.357e-1 | 5.967e-1 | 120 |
| 75 | 5.291e-2 | 1.280e-1 | 6.478e-1 | 120 |
| 80 | 4.955e-2 | 1.208e-1 | 7.097e-1 | 120 |
| 85 | 4.721e-2 | 1.140e-1 | 7.786e-1 | 120 |
| 90 | 4.550e-2 | 1.077e-1 | 8.522e-1 | 120 |
| 95 | 4.410e-2 | 1.013e-1 | 9.222e-1 | 120 |
| 100 | 4.278e-2 | 9.466e-2 | 9.791e-1 | 120 |
| 105 | 4.143e-2 | 8.751e-2 | 1.014 | 120 |
| 110 | 4.008e-2 | 8.047e-2 | 1.030 | 121 |
| 115 | 3.878e-2 | 7.394e-2 | 1.033 | 121 |
| 120 | 3.758e-2 | 6.808e-2 | 1.031 | 121 |
| 125 | 3.652e-2 | 6.304e-2 | 1.031 | 121 |
| 130 | 3.561e-2 | 5.874e-2 | 1.037 | 121 |
| 135 | 3.481e-2 | 5.519e-2 | 1.049 | 121 |
| 140 | 3.407e-2 | 5.145e-2 | 1.057 | 121 |
| 145 | 3.336e-2 | 4.795e-2 | 1.063 | 121 |
| 150 | 3.266e-2 | 4.491e-2 | 1.073 | 121 |

### 16.6 Wood

| kVp | alpha (mm^-1) | beta (mm^-1) | gamma | Printed page |
|---:|---:|---:|---:|---:|
| 25 | 2.230e-2 | 4.340e-2 | 1.937e-1 | 120 |
| 30 | 2.166e-2 | 3.966e-2 | 2.843e-1 | 120 |
| 35 | 1.901e-2 | 3.873e-2 | 3.732e-1 | 120 |
| 40 | ROW_OMITTED | ROW_OMITTED | ROW_OMITTED | 120 |
| 45 | ROW_OMITTED | ROW_OMITTED | ROW_OMITTED | 120 |
| 50 | 1.076e-2 | 1.862e-3 | 1.170 | 120 |
| 55 | 1.012e-2 | 1.404e-3 | 1.269 | 120 |
| 60 | 9.512e-3 | 9.672e-4 | 1.333 | 120 |
| 65 | 8.990e-3 | 6.470e-4 | 1.353 | 120 |
| 70 | 8.550e-3 | 5.390e-4 | 1.194 | 120 |
| 75 | 8.203e-3 | 6.421e-4 | 1.062 | 120 |
| 80 | 7.903e-3 | 8.640e-4 | 9.703e-1 | 120 |
| 85 | 7.686e-3 | 1.056e-3 | 1.015 | 120 |
| 90 | 7.511e-3 | 1.159e-3 | 1.081 | 120 |
| 95 | 7.345e-3 | 1.133e-3 | 1.116 | 120 |
| 100 | 7.230e-3 | 9.343e-4 | 1.309 | 120 |
| 105 | 7.050e-3 | 6.199e-4 | 1.365 | 120 |
| 110 | 6.921e-3 | 1.976e-4 | 3.309 | 121 |
| 115 | 6.864e-3 | -3.908e-4 | 6.469e-1 | 121 |
| 120 | 6.726e-3 | -8.308e-4 | 1.006 | 121 |
| 125 | 6.584e-3 | -1.214e-3 | 1.192 | 121 |
| 130 | 6.472e-3 | -1.539e-3 | 1.285 | 121 |
| 135 | 6.306e-3 | -1.731e-3 | 1.465 | 121 |
| 140 | 6.191e-3 | -1.849e-3 | 1.530 | 121 |
| 145 | 6.115e-3 | -1.869e-3 | 1.498 | 121 |
| 150 | 6.020e-3 | -1.752e-3 | 1.483 | 121 |

### 16.7 Complete importable primary-fit CSV

Dataset ID: `NCRP147_APPENDIX_A_TABLE_A1_PRIMARY`. For every row, beam family is `PRIMARY_MAMMOGRAPHIC` when anode is molybdenum, otherwise `PRIMARY_RADIOGRAPHIC`; `ct_secondary_applicable=false` for all rows. Units are mm for thickness, 1/mm for alpha/beta, and dimensionless for gamma.

`NOT_PROVIDED` in the human tables corresponds to `blank_triplet` in CSV; `ROW_OMITTED` corresponds to `row_omitted` in CSV. Empty numeric CSV fields mean null/missing, never zero. Only `provided` rows may construct an Archer fit. Parse scientific notation with invariant culture. The explicit missing rows retain the full material/kVp grid without inventing data.

```csv
material,kvp,anode,alpha_per_mm,beta_per_mm,gamma,source_state,pdf_page,report_page
lead,25,molybdenum,4.952e1,1.940e2,3.037e-1,provided,3,118
lead,30,molybdenum,3.880e1,1.780e2,3.473e-1,provided,3,118
lead,35,molybdenum,2.955e1,1.647e2,3.948e-1,provided,3,118
lead,40,tungsten,,,,blank_triplet,3,118
lead,45,tungsten,,,,blank_triplet,3,118
lead,50,tungsten,8.801,2.728e1,2.957e-1,provided,3,118
lead,55,tungsten,7.839,2.592e1,3.499e-1,provided,3,118
lead,60,tungsten,6.951,2.489e1,4.198e-1,provided,3,118
lead,65,tungsten,6.130,2.409e1,5.019e-1,provided,3,118
lead,70,tungsten,5.369,2.349e1,5.881e-1,provided,3,118
lead,75,tungsten,4.666,2.269e1,6.618e-1,provided,3,118
lead,80,tungsten,4.040,2.169e1,7.187e-1,provided,3,118
lead,85,tungsten,3.504,2.037e1,7.550e-1,provided,3,118
lead,90,tungsten,3.067,1.883e1,7.726e-1,provided,4,119
lead,95,tungsten,2.731,1.707e1,7.714e-1,provided,4,119
lead,100,tungsten,2.500,1.528e1,7.557e-1,provided,4,119
lead,105,tungsten,2.364,1.341e1,7.239e-1,provided,4,119
lead,110,tungsten,2.296,1.170e1,6.827e-1,provided,4,119
lead,115,tungsten,2.265,1.021e1,6.363e-1,provided,4,119
lead,120,tungsten,2.246,8.950,5.873e-1,provided,4,119
lead,125,tungsten,2.219,7.923,5.386e-1,provided,4,119
lead,130,tungsten,2.170,7.094,4.909e-1,provided,4,119
lead,135,tungsten,2.102,6.450,4.469e-1,provided,4,119
lead,140,tungsten,2.009,5.916,4.018e-1,provided,4,119
lead,145,tungsten,1.895,5.498,3.580e-1,provided,4,119
lead,150,tungsten,1.757,5.177,3.156e-1,provided,4,119
concrete,25,molybdenum,3.904e-1,1.645,2.757e-1,provided,3,118
concrete,30,molybdenum,3.173e-1,1.698,3.593e-1,provided,3,118
concrete,35,molybdenum,2.528e-1,1.807,4.648e-1,provided,3,118
concrete,40,tungsten,1.297e-1,1.780e-1,2.189e-1,provided,3,118
concrete,45,tungsten,1.095e-1,1.741e-1,2.269e-1,provided,3,118
concrete,50,tungsten,9.032e-2,1.712e-1,2.324e-1,provided,3,118
concrete,55,tungsten,7.422e-2,1.697e-1,2.454e-1,provided,3,118
concrete,60,tungsten,6.251e-2,1.692e-1,2.733e-1,provided,3,118
concrete,65,tungsten,5.528e-2,1.696e-1,3.217e-1,provided,3,118
concrete,70,tungsten,5.087e-2,1.696e-1,3.847e-1,provided,3,118
concrete,75,tungsten,4.797e-2,1.663e-1,4.492e-1,provided,3,118
concrete,80,tungsten,4.583e-2,1.549e-1,4.926e-1,provided,3,118
concrete,85,tungsten,4.398e-2,1.348e-1,4.943e-1,provided,3,118
concrete,90,tungsten,4.228e-2,1.137e-1,4.690e-1,provided,4,119
concrete,95,tungsten,4.068e-2,9.705e-2,4.406e-1,provided,4,119
concrete,100,tungsten,3.925e-2,8.567e-2,4.273e-1,provided,4,119
concrete,105,tungsten,3.808e-2,7.862e-2,4.394e-1,provided,4,119
concrete,110,tungsten,3.715e-2,7.436e-2,4.752e-1,provided,4,119
concrete,115,tungsten,3.636e-2,7.201e-2,5.319e-1,provided,4,119
concrete,120,tungsten,3.566e-2,7.109e-2,6.073e-1,provided,4,119
concrete,125,tungsten,3.502e-2,7.113e-2,6.974e-1,provided,4,119
concrete,130,tungsten,3.445e-2,7.160e-2,7.969e-1,provided,4,119
concrete,135,tungsten,3.394e-2,7.263e-2,9.099e-1,provided,4,119
concrete,140,tungsten,3.345e-2,7.476e-2,1.047,provided,4,119
concrete,145,tungsten,3.296e-2,7.875e-2,1.224,provided,4,119
concrete,150,tungsten,3.243e-2,8.599e-2,1.467,provided,4,119
gypsum_wallboard,25,molybdenum,1.576e-1,7.175e-1,3.048e-1,provided,3,118
gypsum_wallboard,30,molybdenum,1.208e-1,7.043e-1,3.613e-1,provided,3,118
gypsum_wallboard,35,molybdenum,8.878e-2,6.988e-1,4.245e-1,provided,3,118
gypsum_wallboard,40,tungsten,,,,blank_triplet,3,118
gypsum_wallboard,45,tungsten,,,,blank_triplet,3,118
gypsum_wallboard,50,tungsten,3.883e-2,8.730e-2,5.105e-1,provided,3,118
gypsum_wallboard,55,tungsten,3.419e-2,8.315e-2,5.606e-1,provided,3,118
gypsum_wallboard,60,tungsten,2.985e-2,7.961e-2,6.169e-1,provided,3,118
gypsum_wallboard,65,tungsten,2.609e-2,7.597e-2,6.756e-1,provided,3,118
gypsum_wallboard,70,tungsten,2.302e-2,7.163e-2,7.299e-1,provided,3,118
gypsum_wallboard,75,tungsten,2.066e-2,6.649e-2,7.750e-1,provided,3,118
gypsum_wallboard,80,tungsten,1.886e-2,6.093e-2,8.103e-1,provided,3,118
gypsum_wallboard,85,tungsten,1.746e-2,5.558e-2,8.392e-1,provided,3,118
gypsum_wallboard,90,tungsten,1.633e-2,5.039e-2,8.585e-1,provided,4,119
gypsum_wallboard,95,tungsten,1.543e-2,4.571e-2,8.763e-1,provided,4,119
gypsum_wallboard,100,tungsten,1.466e-2,4.171e-2,8.939e-1,provided,4,119
gypsum_wallboard,105,tungsten,1.397e-2,3.815e-2,9.080e-1,provided,4,119
gypsum_wallboard,110,tungsten,1.336e-2,3.521e-2,9.244e-1,provided,4,119
gypsum_wallboard,115,tungsten,1.283e-2,3.271e-2,9.423e-1,provided,4,119
gypsum_wallboard,120,tungsten,1.235e-2,3.047e-2,9.566e-1,provided,4,119
gypsum_wallboard,125,tungsten,1.192e-2,2.863e-2,9.684e-1,provided,4,119
gypsum_wallboard,130,tungsten,1.155e-2,2.702e-2,9.802e-1,provided,4,119
gypsum_wallboard,135,tungsten,1.122e-2,2.561e-2,9.901e-1,provided,4,119
gypsum_wallboard,140,tungsten,1.088e-2,2.436e-2,9.964e-1,provided,4,119
gypsum_wallboard,145,tungsten,1.056e-2,2.313e-2,9.987e-1,provided,4,119
gypsum_wallboard,150,tungsten,1.030e-2,2.198e-2,1.013,provided,4,119
steel,25,molybdenum,9.364,4.125e1,3.202e-1,provided,5,120
steel,30,molybdenum,7.406,4.193e1,3.959e-1,provided,5,120
steel,35,molybdenum,5.716,4.341e1,4.857e-1,provided,5,120
steel,40,tungsten,,,,row_omitted,5,120
steel,45,tungsten,,,,row_omitted,5,120
steel,50,tungsten,1.817,4.840,4.021e-1,provided,5,120
steel,55,tungsten,1.493,4.515,4.293e-1,provided,5,120
steel,60,tungsten,1.183,4.219,4.571e-1,provided,5,120
steel,65,tungsten,9.172e-1,3.982,4.922e-1,provided,5,120
steel,70,tungsten,7.149e-1,3.798,5.378e-1,provided,5,120
steel,75,tungsten,5.793e-1,3.629,5.908e-1,provided,5,120
steel,80,tungsten,4.921e-1,3.428,6.427e-1,provided,5,120
steel,85,tungsten,4.355e-1,3.178,6.861e-1,provided,5,120
steel,90,tungsten,3.971e-1,2.913,7.204e-1,provided,5,120
steel,95,tungsten,3.681e-1,2.654,7.461e-1,provided,5,120
steel,100,tungsten,3.415e-1,2.420,7.645e-1,provided,5,120
steel,105,tungsten,3.135e-1,2.227,7.788e-1,provided,5,120
steel,110,tungsten,2.849e-1,2.061,7.897e-1,provided,6,121
steel,115,tungsten,2.579e-1,1.922,8.008e-1,provided,6,121
steel,120,tungsten,2.336e-1,1.797,8.116e-1,provided,6,121
steel,125,tungsten,2.130e-1,1.677,8.217e-1,provided,6,121
steel,130,tungsten,1.969e-1,1.557,8.309e-1,provided,6,121
steel,135,tungsten,1.838e-1,1.440,8.391e-1,provided,6,121
steel,140,tungsten,1.724e-1,1.328,8.458e-1,provided,6,121
steel,145,tungsten,1.616e-1,1.225,8.519e-1,provided,6,121
steel,150,tungsten,1.501e-1,1.132,8.566e-1,provided,6,121
plate_glass,25,molybdenum,3.804e-1,1.543,2.869e-1,provided,5,120
plate_glass,30,molybdenum,3.061e-1,1.599,3.693e-1,provided,5,120
plate_glass,35,molybdenum,2.396e-1,1.694,4.683e-1,provided,5,120
plate_glass,40,tungsten,,,,row_omitted,5,120
plate_glass,45,tungsten,,,,row_omitted,5,120
plate_glass,50,tungsten,9.721e-2,1.799e-1,4.912e-1,provided,5,120
plate_glass,55,tungsten,8.552e-2,1.661e-1,5.112e-1,provided,5,120
plate_glass,60,tungsten,7.452e-2,1.539e-1,5.304e-1,provided,5,120
plate_glass,65,tungsten,6.514e-2,1.443e-1,5.582e-1,provided,5,120
plate_glass,70,tungsten,5.791e-2,1.357e-1,5.967e-1,provided,5,120
plate_glass,75,tungsten,5.291e-2,1.280e-1,6.478e-1,provided,5,120
plate_glass,80,tungsten,4.955e-2,1.208e-1,7.097e-1,provided,5,120
plate_glass,85,tungsten,4.721e-2,1.140e-1,7.786e-1,provided,5,120
plate_glass,90,tungsten,4.550e-2,1.077e-1,8.522e-1,provided,5,120
plate_glass,95,tungsten,4.410e-2,1.013e-1,9.222e-1,provided,5,120
plate_glass,100,tungsten,4.278e-2,9.466e-2,9.791e-1,provided,5,120
plate_glass,105,tungsten,4.143e-2,8.751e-2,1.014,provided,5,120
plate_glass,110,tungsten,4.008e-2,8.047e-2,1.030,provided,6,121
plate_glass,115,tungsten,3.878e-2,7.394e-2,1.033,provided,6,121
plate_glass,120,tungsten,3.758e-2,6.808e-2,1.031,provided,6,121
plate_glass,125,tungsten,3.652e-2,6.304e-2,1.031,provided,6,121
plate_glass,130,tungsten,3.561e-2,5.874e-2,1.037,provided,6,121
plate_glass,135,tungsten,3.481e-2,5.519e-2,1.049,provided,6,121
plate_glass,140,tungsten,3.407e-2,5.145e-2,1.057,provided,6,121
plate_glass,145,tungsten,3.336e-2,4.795e-2,1.063,provided,6,121
plate_glass,150,tungsten,3.266e-2,4.491e-2,1.073,provided,6,121
wood,25,molybdenum,2.230e-2,4.340e-2,1.937e-1,provided,5,120
wood,30,molybdenum,2.166e-2,3.966e-2,2.843e-1,provided,5,120
wood,35,molybdenum,1.901e-2,3.873e-2,3.732e-1,provided,5,120
wood,40,tungsten,,,,row_omitted,5,120
wood,45,tungsten,,,,row_omitted,5,120
wood,50,tungsten,1.076e-2,1.862e-3,1.170,provided,5,120
wood,55,tungsten,1.012e-2,1.404e-3,1.269,provided,5,120
wood,60,tungsten,9.512e-3,9.672e-4,1.333,provided,5,120
wood,65,tungsten,8.990e-3,6.470e-4,1.353,provided,5,120
wood,70,tungsten,8.550e-3,5.390e-4,1.194,provided,5,120
wood,75,tungsten,8.203e-3,6.421e-4,1.062,provided,5,120
wood,80,tungsten,7.903e-3,8.640e-4,9.703e-1,provided,5,120
wood,85,tungsten,7.686e-3,1.056e-3,1.015,provided,5,120
wood,90,tungsten,7.511e-3,1.159e-3,1.081,provided,5,120
wood,95,tungsten,7.345e-3,1.133e-3,1.116,provided,5,120
wood,100,tungsten,7.230e-3,9.343e-4,1.309,provided,5,120
wood,105,tungsten,7.050e-3,6.199e-4,1.365,provided,5,120
wood,110,tungsten,6.921e-3,1.976e-4,3.309,provided,6,121
wood,115,tungsten,6.864e-3,-3.908e-4,6.469e-1,provided,6,121
wood,120,tungsten,6.726e-3,-8.308e-4,1.006,provided,6,121
wood,125,tungsten,6.584e-3,-1.214e-3,1.192,provided,6,121
wood,130,tungsten,6.472e-3,-1.539e-3,1.285,provided,6,121
wood,135,tungsten,6.306e-3,-1.731e-3,1.465,provided,6,121
wood,140,tungsten,6.191e-3,-1.849e-3,1.530,provided,6,121
wood,145,tungsten,6.115e-3,-1.869e-3,1.498,provided,6,121
wood,150,tungsten,6.020e-3,-1.752e-3,1.483,provided,6,121
```

## 17. Missing-data handling without a paperwork form

The retained appendix does not provide diagnostic source outputs, patient scatter factors/maps, acquisition definitions, actual room geometry or design limits. The following are application responsibilities, not additional narrative questions for the user.

| Missing information | Where it must come from | Visible behavior |
|---|---|---|
| Machine/source-output calibration | Approved machine profile/provider. | Explain why kVp cannot give Gy; offer a matching-profile load action. |
| Exposure value required by the profile | One numeric exposure input. | Name the missing value and units; focus that field. |
| Weekly workload, only if requested | One event count or compatible loading value. | Keep the per-exposure/rate result; explain what is needed for the weekly total. |
| Component coverage and normalization | Structured profile ledger. | Block an incomplete total and identify the missing component/model. |
| Target, Scatter or ROI position | Explicit role-specific placement or declared physical anchor. | Offer the appropriate Place/Move action. |
| Source direction/map transform/domain | Machine profile/provider. | Identify the affected point or domain and offer a supported correction. |
| Matching spectrum/material attenuation | Component-aware coefficient/model catalogue. | Show actual unsupported kVp/material; never choose a nearest row. |
| Material/thickness/door geometry | Existing scene barrier controls. | Select the affected wall/door and its missing control. |
| Density/composition applicability | Material/product model metadata. | Request a matching material model, not an invented density correction. |
| Composite path model | Validated stack/transport provider. | Explain the unsupported stack and offer a matching model. |
| Optional numeric comparison limit | One compatible numeric Limit field. | Show physical kerma; leave comparison/thickness unavailable until supplied. |
| Optional occupancy convention | Structured comparison definition plus its factor if needed. | Preserve physical kerma; explain only the missing comparison dependency. |
| Model uncertainty or facility approval | Independent model/facility validation. | Do not invent accuracy intervals or declare regulatory approval. |

An available attenuation curve alone supports transmission arithmetic, not an absolute ROI air-kerma claim. Preserve the entered geometry and numeric values when data is missing; the guidance must make the next useful action obvious.

## 18. Implementation boundaries and delivery requirements

This is an engineering implementation list, **not a checklist the end user must approve**:

1. Add a typed diagnostic machine registry covering Section 1.1. Do not infer the engine from the palette group or reuse the current CT-only scanner predicate for Dental/CathLab.
2. Resolve source profiles by actual machine family, acquisition/spectrum and kVp. Import the retained CT fits and primary archive into distinct immutable datasets.
3. Implement one provider-selected normalization law per contribution, with numeric exposure requirements declared by the provider.
4. Route primary/leakage from Target and patient scatter from Scatter, or use the provider's validated distributed representation.
5. Reuse the stable numerical core and deterministic geometry helpers. Select transmission by component/beam family, spectrum and actual material.
6. Handle finite barriers, openings and supported composites explicitly. Never replace a failed intersection or unsupported stack with zero shielding/dose.
7. Sum nonoverlapping physical contributions into one complete ROI result. Keep optional occupancy comparison and required-thickness solving separate.
8. Create one Calculation panel/router for diagnostic and retained treatment branches. Remove CT/Beam tab buttons and their duplicate source/energy/case controls.
9. Implement role-specific placement, active-machine association and the minimal single-field numeric editor contract.
10. Share structured readiness checks and next-action navigation across the tab, Object point controls, results, compact layouts and error handling.
11. Invalidate results by dependency and reject stale asynchronous responses. Never display an obsolete number as current.
12. Add non-destructive versioned migration, native save/load, undo/redo and historical-result preservation.
13. Use existing Windows/WebGL file workflows for profile/result exchange; report cancelled, rejected and failed operations accurately.
14. Update relevant runtime checks, browser test hooks, help and documentation to the single tab and new role meanings.
15. Verify Section 15.4 before claiming that the redesigned application is delivered. A specification edit alone is not UI implementation.

Keep calculation code independent from rendering and UI strings. Provider availability, input validation, path calculation and result completeness must have one authoritative implementation.

Minimum automatically generated export per ROI:

```text
project/native/diagnostic versions and automatic snapshot identity
machine ID/family, profile ID/version, selected spectrum/kVp
Target, Scatter and ROI IDs/physical coordinates/coordinate spaces
numeric exposure inputs, event definition, quantity/unit/basis
component coverage, calibration identity and reference geometry
unshielded K0 per contribution
barrier IDs, entry/exit path segments, materials and physical/path thicknesses
actual transmission-model/fit IDs, coefficients where used, logB and B
shielded contribution values and complete physical total
optional occupancy/limit definition, comparison and required thickness
missing/unsupported components, model limitations and numerical flags
input fingerprint, dataset identities/hashes, software/solver versions
```

Export metadata is captured from the selected catalogue/provider and current inputs. Do not require the user to type a citation, scenario title or explanation to save the result.

## 19. Extraction completeness and scope record

- A.1, A.2 and A.3 are included with original equation labels and page locations.
- The high-attenuation HVL expression and its workload qualification are included.
- The leakage-exponential assumption is included, with its mathematical expression explicitly labelled as a derivation.
- All Table A.1 numeric rows, units, material names, unusual signs and footnotes are included.
- Both CT coefficient insets are included exactly, with separate dataset identities.
- All three figures are described and have exact reconstruction rules from the extracted coefficients.
- The named underlying studies are preserved to the bibliographic detail present in the appendix; missing bibliography is not fabricated.
- No DLP scatter coefficients, occupancy defaults, numeric design goals, concrete density, scanner corrections or regulatory rules have been attributed to this appendix.
- Unity scene geometry, workload bookkeeping, solver logic, code and example cases are additions for implementation and are labelled accordingly.

Version 2.0 specifies the unified diagnostic-radiology workflow and retains the appendix as one backend transmission-data source. The archived primary fits may be used for matching diagnostic beam qualities; the CT fits remain a separate family. Additional diagnostic source/scatter/acquisition profiles and unsupported spectrum/material models are external requirements.

The nine-page appendix does not contain all-diagnostic source-output models or the complete CT shielding methodology. A broad machine picker, a clean UI and passing arithmetic tests do not establish physical support for every listed machine.

## 20. Verification record

### 20.1 Retained version-1.0 numerical verification

The following is the original numerical verification record dated 2026-10-01. It is retained as historical evidence, not a claim that the PDF was rechecked or the redesigned diagnostic UI was tested during the version-2.0 rewrite:

- The equations and figure inset coefficients were checked against rendered PDF pages; all four Table A.1 pages were visually checked for values, column alignment, missing cells and signs.
- The full embedded primary CSV was checked against the extracted source records: 156 grid rows, 146 supplied triples, 10 explicitly missing triples, and eight negative-beta wood entries.
- The four embedded CT JSON records and the version-1.0 template parsed successfully. The CT records plus primary archive contain all 450 supplied coefficient values.
- The C# numerical core was extracted directly from this Markdown, compiled with the local .NET C# compiler, and executed successfully.
- **3,636 assertions passed**, covering all 150 supplied fits, 75-digit decimal reference calculations for log transmission, forward/inverse log-domain round trips, monotonicity, the 20 forward benchmarks, example arithmetic, log-domain addition, and invalid-input handling.
- The independent scientific review and implementation review were incorporated, including source-path requirements, total versus added thickness, occupancy adjustment conventions, and mixture-solver stopping/underflow rules.

That historical verification establishes extraction consistency and numerical-core behavior, not validation of scanner source profiles, the redesigned Unity workflow, a clinical room or constructed shielding. The new data contract and Section 15.4 acceptance requirements are implementation targets. This documentation revision does not rebuild Windows/WebGL or claim completion of all-diagnostic UI support.

### 20.2 Version-2.0 document verification

Completed on 2026-10-01: **58 assertions passed** for the revised document.

- Normalized-text SHA-256 fingerprints confirm that the CT-fit JSON, both C# reference blocks, the benchmark CSV and the complete primary CSV are unchanged from the pre-rewrite file.
- Both current JSON blocks parse. The new example preserves native root version 2, diagnostic metadata version 1, distinct Target/Scatter/ROI roles, coordinate spaces and null missing inputs/results, with no active case-factor or manual-review fields.
- The primary archive still has 156 grid rows, 146 provided triples and 10 missing triples, including eight negative-beta wood entries. Together with the four CT fits, it retains 450 coefficients.
- All 20 CT forward benchmarks were recomputed within `1e-10` relative tolerance. The weekly Gy conversion and per-exam normalization in Section 15.4 were checked.
- Main section numbering, numbered-heading uniqueness, section references, code-fence balance and updated specification links in the related runtime documentation passed.
- `git diff --check` passed for the documentation changes.

These are document/data-consistency checks only. No redesigned Unity UI, new diagnostic machine profile or Windows/WebGL application build was implemented or tested by this rewrite.
