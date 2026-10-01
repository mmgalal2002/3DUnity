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
  var d=Fixture(out var machine,out var profile);DiagnosticProfiles.Validate(profile);Design.Validate(d);
  Reject(()=>DiagnosticProfiles.Import(JsonUtility.ToJson(profile)),"demo profile cannot enter production catalogue");
  Need(DiagnosticCalculation.Readiness(d,null).NextAction.code=="NoApplicableMachineProfile","kVp alone cannot determine Gy");
  Need(DiagnosticCalculation.Readiness(d,profile).canCalculatePhysical,"ready without occupancy, goal or workload");
  var result=DiagnosticCalculation.Calculate(d,profile);
  double expected=.12/9/1000*CtShieldMath.Transmission(new ArcherFit(2.246,5.73,.547),2);
  Near(result.shieldedAirKermaGy,expected,"forward 2mm lead ROI Gy/exam");
  Near(result.contributions.Single().segments.Single().pathThicknessMm,2,"physical path millimetres");
  Need(result.basis=="exam"&&result.complete&&!result.isZero,"physical result basis/completeness");
  DiagnosticCalculation.ValidateResult(result);d.diagnosticCalculation.results.Add(result);
  var saved=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));Design.Validate(saved);
  saved.diagnosticCalculation.results[0].shieldedAirKermaGy=42;Reject(()=>Design.Validate(saved),"tampered result rejected");
  string fingerprint=DiagnosticCalculation.Fingerprint(d);
  var device=d.items.Find(i=>i.id==machine.machineId);device.scale=2;Need(fingerprint==DiagnosticCalculation.Fingerprint(d),"display scale does not change physics");
  var target=d.items.Find(i=>i.id==machine.targetPointId);target.scale=3;Need(fingerprint==DiagnosticCalculation.Fingerprint(d),"marker scale does not change physics");
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
