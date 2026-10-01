using System;
using System.Linq;
using UnityEngine;
using RoomStudio;

public static class DiagnosticChecks {
 static int assertions;
 static void Need(bool value,string message){assertions++;if(!value)throw new Exception("Diagnostic check: "+message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch(Exception){rejected=true;}Need(rejected,message);}
 static void Near(double value,double expected,string message){Need(Math.Abs(value-expected)<=Math.Max(1e-14,Math.Abs(expected)*1e-10),message+"; actual "+value+", expected "+expected);}
 static DiagnosticProfile Profile(){
  return new DiagnosticProfile{id="DEMO_ONLY",revision="1",demoOnly=true,machineIdentity="Arithmetic fixture, not clinical",acquisition="Arithmetic fixture",machineType=DiagnosticMachineType.ConventionalCt,tubeVoltageKvp=120,spectrumId="CT_SECONDARY_120",exposureDefinition="one arithmetic exam",basis="exam",provenance="DEMO_ONLY",
   requiredComponents=new System.Collections.Generic.List<string>{"Secondary"},
   components=new System.Collections.Generic.List<DiagnosticComponent>{new DiagnosticComponent{id="secondary",kind="Secondary",spectrumId="CT_SECONDARY_120",referenceMgy=.12,referenceDistanceMeters=1,minimumDistanceMeters=.1,maximumDistanceMeters=100,angularModel="IsotropicValidated",validityDomain="DEMO_ONLY",provenance="DEMO_ONLY",
    samples=new System.Collections.Generic.List<DiagnosticSample>{new DiagnosticSample{origin=DiagnosticPointRole.Scatter,weight=1}},
    fits=new System.Collections.Generic.List<DiagnosticFit>{new DiagnosticFit{id="CT_PB_120",material="Lead",spectrumId="CT_SECONDARY_120",alpha=2.246,beta=5.73,gamma=.547,provenance="NCRP147 appendix A retained fixture"}}}}};
 }
 static Design Fixture(out DiagnosticMachine machine,out DiagnosticProfile profile){
  var d=new Design();d.floor.shieldingEnabled=d.ceiling.shieldingEnabled=false;
  var item=EquipmentPalette.Create("CT",Vector3.zero);d.items.Add(item);machine=DiagnosticData.Configure(d,item);
  d.diagnosticCalculation.activeMachineId=item.id;machine.tubeVoltageKvp.Set(120);
  DiagnosticData.Place(d,DiagnosticPointRole.Target,item.id,new CtVector(-1,1,0));
  DiagnosticData.Place(d,DiagnosticPointRole.Scatter,item.id,new CtVector(0,1,0));
  DiagnosticData.Place(d,DiagnosticPointRole.ROI,item.id,new CtVector(3,1,0));
  profile=Profile();machine.profileId=profile.id;machine.profileVersion=profile.revision;machine.profileHash=DiagnosticProfiles.Hash(JsonUtility.ToJson(profile));
  var wall=Design.Wall("Lead wall",1.5f,0,4,90,3);wall.shielding.material="Lead";wall.shielding.thickness=2;d.items.Add(wall);return d;
 }
 public static void Run(){
  assertions=0;
  var empty=Design.Example();Need(JsonUtility.ToJson(empty)==JsonUtility.ToJson(empty),"absent inline diagnostic DTOs serialize deterministically");
  var old=JsonUtility.FromJson<Design>(JsonUtility.ToJson(Design.Example()));Design.Validate(old);
  Need(!old.items.Any(DiagnosticData.IsPoint),"old native items must not become calculation markers");
  foreach(var entry in EquipmentPalette.Entries.Where(e=>e.group!="Room items"))Need(DiagnosticData.IsMachine(EquipmentPalette.Create(entry.model,Vector3.zero)),"typed palette machine "+entry.model);
  Need(DiagnosticData.Family(EquipmentPalette.Create("CathLab",Vector3.zero))==DiagnosticMachineType.Fluoroscopy,"CathLab not CT");
  Need(DiagnosticData.Family(EquipmentPalette.Create("Dental",Vector3.zero))==DiagnosticMachineType.DentalIntraoral,"Dental not CT");
  var visual=new Item{kind="Model",model="CT"};Need(!DiagnosticData.IsMachine(visual),"GLB identity alone is not calculation metadata");
  visual.machineType=DiagnosticMachineType.Fluoroscopy;Need(DiagnosticData.Family(visual)==DiagnosticMachineType.Fluoroscopy,"typed family takes precedence over visual identity");
  foreach(var invalid in new[]{"","NaN","Infinity","1e999","1,2","-","1e+"})Need(!new DiagnosticNumber{text=invalid}.TryGet(out _),"invalid/missing number "+invalid);
  Need(new DiagnosticNumber{text="0"}.TryGet(out double zero)&&zero==0,"zero is present");
  Need(CtCoefficientLibrary.SupportedTubeVoltages("CT_SECONDARY").SequenceEqual(new[]{120,140}),"CT dropdown has only exact supplied spectra");
  Need(CtCoefficientLibrary.SupportedTubeVoltages("PRIMARY_MAMMOGRAPHIC").SequenceEqual(new[]{25,30,35}),"molybdenum mammography reference choices");
  Need(CtCoefficientLibrary.SupportedTubeVoltages("PRIMARY_RADIOGRAPHIC").SequenceEqual(Enumerable.Range(0,23).Select(index=>40+5*index)),"exact provided tungsten reference energies without interpolation");
  Reject(()=>CtCoefficientLibrary.SupportedTubeVoltages("OPG"),"unknown acquisition is not a primary reference alias");
  Need(DiagnosticProfiles.SupportedTubeVoltages(DiagnosticMachineType.ConventionalCt).SequenceEqual(new[]{120.0,140.0}),"CT energy choices come from its reference, not palette neighbours");
  Need(DiagnosticProfiles.SupportedTubeVoltages(DiagnosticMachineType.Mammography).SequenceEqual(new[]{25.0,30.0,35.0}),"mammography does not receive tungsten/CT choices");
  Need(DiagnosticProfiles.SupportedTubeVoltages(DiagnosticMachineType.GeneralRadiography).SequenceEqual(Enumerable.Range(0,23).Select(index=>40.0+5*index)),"radiographic dropdown excludes missing/interpolated energies");
  foreach(var type in new[]{DiagnosticMachineType.DentalIntraoral,DiagnosticMachineType.DentalPanoramic,DiagnosticMachineType.DentalCephalometric,DiagnosticMachineType.DentalCbct,DiagnosticMachineType.Fluoroscopy,DiagnosticMachineType.Mri,DiagnosticMachineType.Treatment})
   Need(DiagnosticProfiles.SupportedTubeVoltages(type).Length==0,"no unsupported spectrum fallback for "+type);
  var opgDesign=new Design();var opgItem=EquipmentPalette.Create("PlanmecaViso",Vector3.zero);opgDesign.items.Add(opgItem);
  var opgMachine=DiagnosticData.Configure(opgDesign,opgItem);opgDesign.diagnosticCalculation.activeMachineId=opgItem.id;opgMachine.tubeVoltageKvp.Set(85);
  DiagnosticData.Place(opgDesign,DiagnosticPointRole.Target,opgItem.id,new CtVector(0,1,0));
  DiagnosticData.Place(opgDesign,DiagnosticPointRole.Scatter,opgItem.id,new CtVector(1,1,0));
  DiagnosticData.Place(opgDesign,DiagnosticPointRole.ROI,opgItem.id,new CtVector(3,1,0));
  Need(DiagnosticCalculation.Readiness(opgDesign,null).NextAction.code=="UnsupportedSpectrum"&&!DiagnosticCalculation.Readiness(opgDesign,null).canCalculatePhysical,"OPG 85 kVp does not become supported through a primary archive row");
  Reject(()=>DiagnosticProfiles.SelectTubeVoltage(opgDesign,opgMachine,85),"OPG cannot select an energy without a matching acquisition model");
  Need(opgMachine.tubeVoltageKvp.text=="85","unsupported legacy input remains historical data, never silently replaced");
  var d=Fixture(out var machine,out var profile);DiagnosticProfiles.Validate(profile);Design.Validate(d);
  string beforeEnergy=JsonUtility.ToJson(d);
  Need(!DiagnosticProfiles.SelectTubeVoltage(d,machine,120)&&beforeEnergy==JsonUtility.ToJson(d),"selecting current energy is a nonmutating no-op");
  Reject(()=>DiagnosticProfiles.SelectTubeVoltage(d,machine,130),"unsupported energy cannot be set outside dropdown");
  Need(beforeEnergy==JsonUtility.ToJson(d),"unsupported selection is atomic");
  d.items.Find(i=>i.id==machine.machineId).locked=true;beforeEnergy=JsonUtility.ToJson(d);
  Reject(()=>DiagnosticProfiles.SelectTubeVoltage(d,machine,140),"energy selection respects protected machine");
  Need(beforeEnergy==JsonUtility.ToJson(d),"protected energy input does not change");
  d.items.Find(i=>i.id==machine.machineId).locked=false;
  Need(DiagnosticProfiles.SelectTubeVoltage(d,machine,140)&&machine.tubeVoltageKvp.text=="140","supported dropdown choice is stored exactly");
  var energyCopy=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));Design.Validate(energyCopy);
  Need(DiagnosticData.Machine(energyCopy,machine.machineId).tubeVoltageKvp.text=="140","selected energy persists without rounding");
  DiagnosticProfiles.SelectTubeVoltage(d,machine,120);
  Reject(()=>DiagnosticProfiles.Import(JsonUtility.ToJson(profile)),"demo profile cannot enter production catalogue");
  var bad=Profile();bad.demoOnly=false;
  Reject(()=>DiagnosticProfiles.Import(JsonUtility.ToJson(bad)),"untrusted profile cannot activate by dropping demo flag");
  bad=Profile();bad.components[0].provider="SpatialField";Reject(()=>DiagnosticProfiles.Validate(bad),"unsupported field provider is not a point-source fallback");
  bad=Profile();bad.requiredComponents.Add("Leakage");Reject(()=>DiagnosticProfiles.Validate(bad),"overlapping secondary/leakage coverage rejected");
  bad=Profile();bad.components[0].samples[0].weight=.5;Reject(()=>DiagnosticProfiles.Validate(bad),"weighted samples must normalize");
  bad=Profile();bad.components[0].fits[0].spectrumId="other";Reject(()=>DiagnosticProfiles.Validate(bad),"spectrum mismatch cannot use a fit");
  Need(DiagnosticCalculation.Readiness(d,null).NextAction.code=="NoApplicableMachineProfile","kVp alone cannot determine Gy");
  machine.tubeVoltageKvp.text="";
  Need(DiagnosticCalculation.Readiness(d,null).NextAction.actionKind=="SelectEnergy","missing energy recovery opens selector, not numeric editor");
  machine.tubeVoltageKvp.Set(120);
  Need(DiagnosticCalculation.Readiness(d,profile).canCalculatePhysical,"ready without occupancy, goal or workload");
  var result=DiagnosticCalculation.Calculate(d,profile);
  double expected=.12/9/1000*CtShieldMath.Transmission(new ArcherFit(2.246,5.73,.547),2);
  Near(result.shieldedAirKermaGy,expected,"forward 2mm lead ROI Gy/exam");
  Near(result.contributions.Single().segments.Single().pathThicknessMm,2,"physical path millimetres");
  Need(result.basis=="exam"&&result.complete&&!result.isZero,"physical result basis/completeness");
  DiagnosticCalculation.ValidateResult(result);d.diagnosticCalculation.results.Add(result);
  d.diagnosticCalculation.currentResult=result;
  var saved=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));Design.Validate(saved);
  saved.diagnosticCalculation.results[0].shieldedAirKermaGy=42;Reject(()=>Design.Validate(saved),"tampered result rejected");
  string fingerprint=DiagnosticCalculation.Fingerprint(d);
  var device=d.items.Find(i=>i.id==machine.machineId);device.scale=2;Need(fingerprint==DiagnosticCalculation.Fingerprint(d),"display scale does not change physics");
  var target=d.items.Find(i=>i.id==machine.targetPointId);target.scale=3;Need(fingerprint==DiagnosticCalculation.Fingerprint(d),"marker scale does not change physics");
  Need(!PrecisionEditing.IsEquipment(target),"Target cannot receive equipment-scale handles");
  machine.weeklyExposureCount.Set(30);d.diagnosticCalculation.comparison.limitGy.Set(1);Need(fingerprint==DiagnosticCalculation.Fingerprint(d),"comparison does not stale physical result");
  device.x=2;DiagnosticData.Synchronize(d);
  Near(DiagnosticData.Position(d,target).x,1,"machine moves physical Target");
  Near(DiagnosticData.Position(d,d.items.Find(i=>i.id==machine.scatterPointId)).x,0,"patient remains independent");
  Need(fingerprint!=DiagnosticCalculation.Fingerprint(d),"machine movement invalidates");
  d=Fixture(out machine,out profile);var roi=d.items.Find(i=>i.id==d.diagnosticCalculation.activeRoiId);
  DiagnosticData.SetPosition(d,roi,new CtVector(3.123456789012,1,0));
  var restored=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));Design.Validate(restored);DiagnosticData.Synchronize(restored);
  Near(DiagnosticData.Position(restored,restored.items.Find(i=>i.id==roi.id)).x,3.123456789012,"exact double persistence");
  roi.diagnosticPoint.hasCoordinateDraft=true;roi.diagnosticPoint.coordinateDraft=new[]{"invalid","1","0"};
  Need(DiagnosticCalculation.Readiness(d,profile).NextAction.pointId==roi.id,"invalid coordinate draft navigates to its point");
  roi.diagnosticPoint.hasCoordinateDraft=false;
  machine.tubeVoltageKvp.text="1e";Need(DiagnosticCalculation.Readiness(d,profile).NextAction.code=="InvalidNumericInput","invalid draft cannot reuse old kVp");
  machine.tubeVoltageKvp.Set(130);Need(DiagnosticCalculation.Readiness(d,profile).NextAction.code=="UnsupportedSpectrum","no energy rounding");
  d.items.Find(i=>i.id==machine.machineId).locked=true;Need(DiagnosticCalculation.Readiness(d,profile).NextAction.actionKind=="UnlockObject","locked missing input has an executable unlock route");
  d.items.Find(i=>i.id==machine.machineId).locked=false;
  machine.tubeVoltageKvp.Set(120);d.items.Find(i=>i.kind=="Wall").shielding.material="Wood";
  Need(!DiagnosticCalculation.Readiness(d,profile).canCalculatePhysical,"unsupported material cannot disappear");
  d=Fixture(out machine,out profile);var wall=d.items.Find(i=>i.kind=="Wall");
  wall.shielding.diagnosticThicknessEdited=true;wall.shielding.diagnosticThickness.text="";
  Need(DiagnosticCalculation.Readiness(d,profile).NextAction.code=="InvalidNumericInput","blank barrier input is not unobstructed");
  wall.shielding.diagnosticThicknessEdited=false;wall.shielding.shieldingEnabled=false;
  result=DiagnosticCalculation.Calculate(d,profile);Near(result.shieldedAirKermaGy,.12/9/1000,"unobstructed B=1");
  wall.shielding.shieldingEnabled=true;
  var wall2=Design.Wall("Second slab",2,0,4,90,3);wall2.shielding.material="Lead";wall2.shielding.thickness=2;d.items.Add(wall2);
  Need(DiagnosticCalculation.Readiness(d,profile).NextAction.code=="UnsupportedCompositeBarrier","separated slabs rejected");
  d=Fixture(out machine,out profile);
  profile.inputs.Add(new DiagnosticExposureDefinition{key="mas",label="Exposure",unit="mAs",minimum=0,maximum=100000});
  profile.components[0].normalization="PerInput";profile.components[0].exposureKey="mas";
  machine.profileHash=DiagnosticProfiles.Hash(JsonUtility.ToJson(profile));
  machine.exposureInputs.Add(new DiagnosticInput{key="mas"});
  Need(!DiagnosticCalculation.Readiness(d,profile).canCalculatePhysical,"empty exposure not zero");
  machine.exposureInputs[0].value.Set(0);result=DiagnosticCalculation.Calculate(d,profile);Need(result.isZero&&result.shieldedAirKermaGy==0,"explicit zero exposure");
  machine.exposureInputs[0].value.Set(3);result=DiagnosticCalculation.Calculate(d,profile);Near(result.shieldedAirKermaGy,expected*3,"exposure applied exactly once");
  d.items.Find(i=>i.kind=="Wall").shielding.thickness=3000;result=DiagnosticCalculation.Calculate(d,profile);
  Need(result.underflow&&!result.isZero&&CtShieldMath.IsFinite(result.logShieldedGy),"underflow distinct from zero");
  d=Fixture(out machine,out profile);d.items.Find(i=>i.kind=="Wall").shielding.shieldingEnabled=false;
  profile.requiredComponents=new System.Collections.Generic.List<string>{"Scatter","Leakage"};
  profile.components[0].kind="Scatter";profile.components[0].id="patient";
  var leakage=JsonUtility.FromJson<DiagnosticComponent>(JsonUtility.ToJson(profile.components[0]));
  leakage.kind="Leakage";leakage.id="housing";leakage.samples[0].origin=DiagnosticPointRole.Target;profile.components.Add(leakage);
  machine.profileHash=DiagnosticProfiles.Hash(JsonUtility.ToJson(profile));DiagnosticProfiles.Validate(profile);
  result=DiagnosticCalculation.Calculate(d,profile);
  Near(result.shieldedAirKermaGy,(.12/9+.12/16)/1000,"patient and housing paths summed once at ROI");
  Need(result.contributions[0].originRole=="Scatter"&&result.contributions[1].originRole=="Target","component origins are not interchangeable");
  profile.components[0].patientInverseSquare=true;profile.components[0].referencePatientDistanceMeters=2;profile.components[0].minimumPatientDistanceMeters=.1;profile.components[0].maximumPatientDistanceMeters=10;
  machine.profileHash=DiagnosticProfiles.Hash(JsonUtility.ToJson(profile));
  result=DiagnosticCalculation.Calculate(d,profile);Near(result.shieldedAirKermaGy,(.12/9*4+.12/16)/1000,"patient irradiation normalization occurs only on scatter");
  d.diagnosticCalculation.comparison.requested=true;d.diagnosticCalculation.comparison.occupied=true;d.diagnosticCalculation.comparison.occupancy.Set(0);d.diagnosticCalculation.comparison.limitGy.Set(1);
  Need(!DiagnosticCalculation.Comparison(d,profile,out _,out _)&&DiagnosticCalculation.Readiness(d,profile).canCalculatePhysical,"zero occupancy cannot erase physical result or claim a shielding verdict");
  d=Fixture(out machine,out profile);var copy=SceneClipboard.Copy(d,new[]{machine.machineId},machine.machineId);
  var pasted=SceneClipboard.Paste(d,copy,1);Design.Validate(d);
  var copiedTarget=pasted.items.Single(DiagnosticData.IsPoint);var copiedMachine=DiagnosticData.Machine(d,copiedTarget.diagnosticPoint.machineId);
  Need(copiedMachine!=null&&copiedMachine.machineId!=machine.machineId&&copiedMachine.tubeVoltageKvp.text==""&&copiedMachine.profileId=="","copy remaps Target without active exposure");
  Need(d.diagnosticCalculation.activeMachineId==machine.machineId,"copy does not activate source");
  var scatter=d.items.Find(i=>i.id==machine.scatterPointId);scatter.locked=true;
  Reject(()=>SelectionEditing.Remove(d,new[]{machine.machineId}),"locked associated point protects deletion");
  scatter.locked=false;SelectionEditing.Remove(d,new[]{machine.machineId});Design.Validate(d);
  Need(d.items.Contains(scatter)&&scatter.diagnosticPoint.machineId=="","deleting machine retains independent patient without dangling owner");
  var legacy=CtShieldingChecks.DemoFixture();string history=JsonUtility.ToJson(legacy.ct);int patients=legacy.items.Count(i=>CtShieldingData.IsPoint(i)&&i.ctPoint.role=="Patient");
  Design.Migrate(legacy);Design.Validate(legacy);string first=JsonUtility.ToJson(legacy);Design.Migrate(legacy);
  Need(first==JsonUtility.ToJson(legacy),"migration idempotence");Need(history==JsonUtility.ToJson(legacy.ct),"legacy source/history not rewritten");
  Need(patients==legacy.items.Count(i=>CtShieldingData.IsPoint(i)&&i.ctPoint.role=="Patient"),"legacy patient evaluation roles preserved");
  Need(!legacy.items.Any(i=>DiagnosticData.IsPoint(i)&&i.diagnosticPoint.role==DiagnosticPointRole.Target),"no arbitrary Target migration");
  Debug.Log("ROOM_STUDIO_DIAGNOSTIC_CHECKS_PASSED: "+assertions+" assertions");
 }
}
