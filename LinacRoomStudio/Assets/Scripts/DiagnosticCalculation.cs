using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
[Serializable] public sealed class DiagnosticIssue {
 public string code="",machineId="",roiId="",pointId="",barrierId="",inputKey="",observedValue="",expectedDomain="",explanation="",actionKind="",actionTarget="";
}
[Serializable] public sealed class DiagnosticReadiness {
 public string state="MissingMachine",inputFingerprint="",summary="";
 public bool canCalculatePhysical,canCompare,canSolveThickness;
 public List<DiagnosticIssue> issues=new List<DiagnosticIssue>();
 public DiagnosticIssue NextAction=>issues.FirstOrDefault();
}
[Serializable] public sealed class DiagnosticContribution {
 public string componentId="",kind="",spectrumId="",originRole="",fitId="";
 public CtVector originMeters,roiMeters;
 public double distanceMeters,unshieldedMgy,shieldedMgy,logUnshielded,logShielded,logB;
 public bool isZero,underflow;
 public List<CtPathSegment> segments=new List<CtPathSegment>();
}
[Serializable] public sealed class DiagnosticResult {
 public int version=1;
 public int nativeVersion=2,diagnosticVersion=1;
 public string id="",createdUtc="",label="",machineId="",roiId="",profileId="",profileVersion="",profileHash="",inputFingerprint="",inputsJson="",basis="",quantity="airKerma",unit="Gy",engineVersion="RoomStudio.Diagnostic.1";
 public string recordHash="",profileJson="",comparisonJson="";
 public DiagnosticNumber weeklyExposureCount=new DiagnosticNumber();
 public bool complete,isZero,underflow;
 public double shieldedAirKermaGy,unshieldedAirKermaGy,logShieldedGy;
 public List<DiagnosticContribution> contributions=new List<DiagnosticContribution>();
}
[Serializable] sealed class DiagnosticPhysicalInputs {
 public double units;
 public float width,depth,height,machineX,machineY,machineZ,machineAngle;
 public DiagnosticMachine machine;
 public string roiId="";
 public CtVector target,scatter,roi;
 public DiagnosticPoint targetPoint,scatterPoint,roiPoint;
 public Barrier floor,ceiling;
 public List<DiagnosticBarrierInput> barriers;
 public List<WallJunction> junctions;
}
[Serializable] sealed class DiagnosticBarrierInput {
 public string id="",kind="";
 public float x,y,z,angle,length,height;
 public bool unsupportedGeometry;
 public Barrier shielding;
 public ComponentShape component;
 public List<DoorOpening> doors;
}
public static class DiagnosticCalculation {
 static string Number(double value)=>value.ToString("G8",CultureInfo.InvariantCulture);
 public static double Distance(CtVector a,CtVector b)=>Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
 static Barrier PhysicalBarrier(Barrier b)=>b==null?null:new Barrier{material=b.material,thickness=b.thickness,shieldingEnabled=b.shieldingEnabled};
 static CtVector Metres(CtVector p,double units)=>new CtVector(p.x*units,p.y*units,p.z*units);
 public static string InputJson(Design design){
  var data=design.diagnosticCalculation;var machine=DiagnosticData.Machine(design,data?.activeMachineId);
  var item=design.items.Find(i=>i.id==machine?.machineId);
  Func<string,CtVector> position=id=>{var point=design.items.Find(i=>i.id==id);return point==null?null:DiagnosticData.Position(design,point);};
  Func<string,DiagnosticPoint> pointData=id=>{var point=design.items.Find(i=>i.id==id)?.diagnosticPoint;return point==null?null:new DiagnosticPoint{version=point.version,role=point.role,machineId=point.machineId,positionMeters=point.positionMeters};};
  var inputs=new DiagnosticPhysicalInputs{units=data?.metersPerUnityUnit??1,width=design.width,depth=design.depth,height=design.height,
   machine=machine==null?null:JsonUtility.FromJson<DiagnosticMachine>(JsonUtility.ToJson(machine)),machineX=item?.x??0,machineY=item?.y??0,machineZ=item?.z??0,machineAngle=item?.angle??0,
   roiId=data?.activeRoiId??"",target=position(machine?.targetPointId),scatter=position(machine?.scatterPointId),roi=position(data?.activeRoiId),
   targetPoint=pointData(machine?.targetPointId),scatterPoint=pointData(machine?.scatterPointId),roiPoint=pointData(data?.activeRoiId),
   floor=PhysicalBarrier(design.floor),ceiling=PhysicalBarrier(design.ceiling),barriers=design.items.Where(i=>i.kind=="Wall"||i.kind=="Component").Select(i=>new DiagnosticBarrierInput{id=i.id,kind=i.kind,x=i.x,y=i.y,z=i.z,angle=i.angle,length=i.length,height=i.height,unsupportedGeometry=i.generated?.unsupportedGeometry==true,shielding=PhysicalBarrier(i.shielding),component=i.component,doors=i.doors}).ToList(),junctions=design.wallJunctions};
  if(inputs.machine!=null)inputs.machine.weeklyExposureCount=new DiagnosticNumber();
  return JsonUtility.ToJson(inputs);
 }
 public static string Fingerprint(Design design)=>DiagnosticProfiles.Hash(InputJson(design));
 static DiagnosticReadiness Block(DiagnosticReadiness ready,string state,string code,string text,string action,string target="",string input="",string expected="",string observed=""){
  ready.state=state;ready.issues.Add(new DiagnosticIssue{code=code,explanation=text,actionKind=action,actionTarget=target,inputKey=input,expectedDomain=expected,observedValue=observed});
  return ready;
 }
 public static DiagnosticReadiness Readiness(Design design,DiagnosticProfile profile){
  var ready=Check(design,profile);
  var device=design.items.Find(i=>i.id==design.diagnosticCalculation?.activeMachineId);
  var first=ready.NextAction;
  if(device?.locked==true&&first!=null&&new[]{"Input","PlaceTarget","PlaceScatter","PlaceROI","ConfigureMachine","LoadProfile"}.Contains(first.actionKind)){
   first.code="ObjectLocked";first.explanation=device.name+" is locked. Select it in Object and unlock it before the required edit.";first.actionKind="UnlockObject";first.actionTarget=device.id;
  }
  foreach(var issue in ready.issues){issue.machineId=design.diagnosticCalculation?.activeMachineId??"";issue.roiId=design.diagnosticCalculation?.activeRoiId??"";if(issue.actionKind=="SelectBarrier"||issue.actionKind=="RoomBarrier")issue.barrierId=issue.actionTarget;if(issue.actionKind=="SelectPoint")issue.pointId=issue.actionTarget;}
  return ready;
 }
 static DiagnosticReadiness Check(Design design,DiagnosticProfile profile){
  var ready=new DiagnosticReadiness{inputFingerprint=Fingerprint(design)};
  var data=design.diagnosticCalculation;var item=design.items.Find(i=>i.id==data?.activeMachineId&&DiagnosticData.IsMachine(i));
  if(item==null)return Block(ready,"MissingMachine","MissingMachine",design.items.Any(DiagnosticData.IsMachine)?"Choose which machine supplies this calculation. Machines are never automatically summed.":"There is no active machine supplying radiation. Place diagnostic equipment first.","ChooseMachine");
  var family=DiagnosticData.Family(item);
  if(!DiagnosticData.IsXray(family))return Block(ready,"UnsupportedModel","NotDiagnosticXray",family==DiagnosticMachineType.Treatment?"Use the separate treatment calculation with MV/MeV inputs.":family==DiagnosticMachineType.NuclearMedicine?"Nuclear medicine requires radionuclide/activity models, not kVp.":"MRI and ultrasound do not use diagnostic X-ray shielding.","ChooseMachine");
  var machine=DiagnosticData.Machine(design,item.id);
  if(machine==null)return Block(ready,"MissingInput","MissingMachineConfiguration",item.name+" has no diagnostic input record. Configure this machine; no source output will be invented.","ConfigureMachine",item.id);
  foreach(var role in new[]{DiagnosticPointRole.Target,DiagnosticPointRole.Scatter,DiagnosticPointRole.ROI}){
   string id=role==DiagnosticPointRole.Target?machine.targetPointId:role==DiagnosticPointRole.Scatter?machine.scatterPointId:data.activeRoiId;
   var point=design.items.Find(i=>i.id==id&&DiagnosticData.IsPoint(i)&&i.diagnosticPoint.role==role);
   if(point==null)return Block(ready,"MissingPoint","MissingRequiredPoint",role==DiagnosticPointRole.Target?"The device source location is missing. Place Target at the physical tube/source reference.":role==DiagnosticPointRole.Scatter?"The patient position is missing. Place Scatter where the patient is irradiated.":"There is no measurement location. Place ROI where shielding is to be assessed.","Place"+role);
   if(point.diagnosticPoint.hasCoordinateDraft&&point.diagnosticPoint.coordinateDraft.Any(text=>!new DiagnosticNumber{text=text}.TryGet(out double value)||Math.Abs(value)>10000))
    return Block(ready,"MissingInput","InvalidNumericInput","Complete the finite coordinates of "+point.name+" in Object. Its previous position is not used for a new calculation.","SelectPoint",point.id);
   if(point.diagnosticPoint.machineId!=machine.machineId)return Block(ready,"MissingMachine","AmbiguousAssociation",point.name+" is not associated with "+item.name+". Choose its machine explicitly.","AssociatePoint",point.id);
  }
  if(!machine.tubeVoltageKvp.TryGet(out double kvp)||kvp<=0)return Block(ready,"MissingInput","InvalidNumericInput","Enter a finite positive tube voltage for "+item.name+". Voltage selects beam quality, not source strength.","Input","kvp","kvp","> 0 kVp",machine.tubeVoltageKvp.text);
  if(profile==null)return Block(ready,"UnsupportedModel","NoApplicableMachineProfile",item.name+" at "+Number(kvp)+" kVp has no matching installed calibrated source-output profile. Voltage alone cannot determine Gy; your points are retained.","LoadProfile");
  DiagnosticProfiles.Validate(profile);
  if(profile.machineType!=machine.machineType||profile.id!=machine.profileId||profile.revision!=machine.profileVersion)return Block(ready,"UnsupportedModel","NoApplicableMachineProfile","The selected profile does not match this machine and acquisition.","LoadProfile");
  if(profile.tubeVoltageKvp!=kvp)return Block(ready,"UnsupportedModel","UnsupportedSpectrum","This profile supports "+Number(profile.tubeVoltageKvp)+" kVp, not "+Number(kvp)+" kVp. No rounding or substitute coefficients have been applied.","Input","kvp","kvp",Number(profile.tubeVoltageKvp)+" kVp",Number(kvp));
  foreach(var requirement in profile.inputs){
   var input=machine.exposureInputs.Find(i=>i.key==requirement.key);
   if(input==null||!input.value.TryGet(out double value)||value<requirement.minimum||value>requirement.maximum)
    return Block(ready,"MissingInput","MissingRequiredInput","Enter "+requirement.label+" ("+requirement.unit+"). This profile needs that exposure to normalize its source output.","Input",requirement.key,requirement.key,Number(requirement.minimum)+" to "+Number(requirement.maximum),input?.value.text??"");
  }
  try{
   var ledger=Contributions(design,machine,profile);
   var barriers=ledger.SelectMany(c=>c.segments).Select(s=>s.barrierId).Distinct().ToList();
   string paths=barriers.Count==0?"No shielding on these supported paths.":string.Join("; ",ledger.SelectMany(row=>row.segments).Select(segment=>(design.items.Find(i=>i.id==segment.barrierId)?.name??segment.barrierId)+": "+Number(segment.pathThicknessMm)+" mm "+segment.material+" path").Distinct());
   var roi=design.items.Find(i=>i.id==data.activeRoiId);
   ready.summary="Ready for "+item.name+" -> "+roi.name+". "+Number(kvp)+" kVp; "+profile.exposureDefinition+". "+ledger.Count+" validated component/sample paths. "+paths+" Calculate combines the profile's unshielded field and matched transmission in Gy/"+profile.basis+".";
   ready.state="Ready";ready.canCalculatePhysical=true;
   ready.canCompare=Comparison(design,profile,out _,out _);
  }catch(CtCalculationException error){
   var barrier=design.items.FirstOrDefault(i=>(i.kind=="Wall"||i.kind=="Component")&&error.Message.Contains(i.id));
   string roomBarrier=error.Message.Contains("floor")?"floor":error.Message.Contains("ceiling")?"ceiling":"";
   if(barrier==null&&roomBarrier!="")return Block(ready,"InvalidGeometry",error.Status,error.Message,"RoomBarrier",roomBarrier);
   return Block(ready,"InvalidGeometry",error.Status,error.Message,barrier!=null?"SelectBarrier":"SelectPoint",barrier?.id??(string.IsNullOrEmpty(error.ActionTarget)?data.activeRoiId:error.ActionTarget),expected:error.ExpectedDomain,observed:error.ObservedValue);
  }
  return ready;
 }
 static void Fail(string code,string text,string target="",string observed="",string expected=""){throw new CtCalculationException(code,text,target,observed,expected);}
 static void ValidateBarriers(Design design){
  foreach(var pair in design.items.Where(i=>i.kind=="Wall"||i.kind=="Component").Select(i=>new KeyValuePair<string,Barrier>(i.id,i.shielding)).Concat(new[]{new KeyValuePair<string,Barrier>("floor",design.floor),new KeyValuePair<string,Barrier>("ceiling",design.ceiling)})){
   var b=pair.Value;
   if(b==null||!CtShieldMath.IsFinite(b.thickness)||b.thickness<0||string.IsNullOrWhiteSpace(b.material))Fail("InvalidGeometry","Set a material and finite nonnegative thickness on "+pair.Key+".");
   if(b.diagnosticThicknessEdited&&(b.diagnosticThickness==null||!b.diagnosticThickness.TryGet(out double thickness)||thickness<.01||thickness>3000))Fail("InvalidNumericInput","Enter a valid physical thickness on "+pair.Key+". The previous thickness is not used while this input is incomplete.");
  }
 }
 static List<DiagnosticContribution> Contributions(Design design,DiagnosticMachine machine,DiagnosticProfile profile){
  ValidateBarriers(design);
  var data=design.diagnosticCalculation;double units=data.metersPerUnityUnit;
  var target=DiagnosticData.Position(design,design.items.Find(i=>i.id==machine.targetPointId));
  var scatter=DiagnosticData.Position(design,design.items.Find(i=>i.id==machine.scatterPointId));
  var roi=DiagnosticData.Position(design,design.items.Find(i=>i.id==data.activeRoiId));
  var device=design.items.Find(i=>i.id==machine.machineId);
  double angle=device.angle*Math.PI/180,c=Math.Cos(angle),s=Math.Sin(angle);
  var ledger=new List<DiagnosticContribution>();
  foreach(var component in profile.components){
   double exposure=1;
   if(component.normalization=="PerInput")machine.exposureInputs.Find(i=>i.key==component.exposureKey).value.TryGet(out exposure);
   double patientLog=0;
   if(component.patientInverseSquare){
    double distance=Distance(target,scatter)*units;
    if(distance<component.minimumPatientDistanceMeters||distance>component.maximumPatientDistanceMeters)Fail("SourceModelOutsideValidityDomain","Target -> Scatter is "+Number(distance)+" m; "+component.id+" requires "+Number(component.minimumPatientDistanceMeters)+" to "+Number(component.maximumPatientDistanceMeters)+" m. Move Scatter; no distance clamp was applied.",machine.scatterPointId,Number(distance)+" m",Number(component.minimumPatientDistanceMeters)+" to "+Number(component.maximumPatientDistanceMeters)+" m");
    patientLog=2*(Math.Log(component.referencePatientDistanceMeters)-Math.Log(distance));
   }
   foreach(var sample in component.samples){
    var anchor=sample.origin==DiagnosticPointRole.Target?target:scatter;var offset=sample.offsetMeters;
    var origin=new CtVector(anchor.x+(c*offset.x+s*offset.z)/units,anchor.y+offset.y/units,anchor.z+(-s*offset.x+c*offset.z)/units);
    double distance=Distance(origin,roi)*units;
    if(distance<component.minimumDistanceMeters||distance>component.maximumDistanceMeters)Fail("SourceModelOutsideValidityDomain",component.id+" -> ROI is "+Number(distance)+" m; valid range is "+Number(component.minimumDistanceMeters)+" to "+Number(component.maximumDistanceMeters)+" m. Move ROI; no distance clamp was applied.");
    var row=new DiagnosticContribution{componentId=component.id,kind=component.kind,spectrumId=component.spectrumId,originRole=sample.origin.ToString(),originMeters=Metres(origin,units),roiMeters=Metres(roi,units),distanceMeters=distance,
     segments=CtShieldingCalculation.Path(design,origin,roi,units)};
    double thickness;
    try{thickness=CtShieldingCalculation.TotalHomogeneousPath(row.segments);}
    catch(CtCalculationException error){throw new CtCalculationException(error.Status,component.id+": "+error.Message+" Affected barriers: "+string.Join(", ",row.segments.Select(segment=>segment.barrierId)));}
    if(row.segments.Count>0){
     var fit=component.fits.Find(f=>f.material==row.segments[0].material);
     if(fit==null)Fail("UnsupportedMaterialSpectrumCombination",component.id+" crosses "+row.segments[0].barrierId+" ("+row.segments[0].material+") at "+Number(profile.tubeVoltageKvp)+" kVp, without a matching attenuation model.");
     row.fitId=fit.id;row.logB=CtShieldMath.LogTransmission(fit.Archer,thickness);
     foreach(var segment in row.segments){segment.fitId=fit.id;segment.alpha=fit.alpha;segment.beta=fit.beta;segment.gamma=fit.gamma;}
    }
    row.isZero=exposure==0||component.referenceMgy==0;
    if(!row.isZero){
     row.logUnshielded=Math.Log(component.referenceMgy)+Math.Log(exposure)+Math.Log(sample.weight)+2*(Math.Log(component.referenceDistanceMeters)-Math.Log(distance))+patientLog;
     row.logShielded=row.logUnshielded+row.logB;row.unshieldedMgy=Math.Exp(row.logUnshielded);row.shieldedMgy=Math.Exp(row.logShielded);row.underflow=row.shieldedMgy==0;
     if(!CtShieldMath.IsFinite(row.logShielded)||!CtShieldMath.IsFinite(row.unshieldedMgy)||!CtShieldMath.IsFinite(row.shieldedMgy))Fail("InvalidNumericInput","Nonfinite output from "+component.id+". Reduce the exposure or obtain a model with a valid numeric domain.");
    }
    ledger.Add(row);
   }
  }
  return ledger;
 }
 public static DiagnosticResult Calculate(Design design,DiagnosticProfile profile){
  var readiness=Readiness(design,profile);
  if(!readiness.canCalculatePhysical)throw new CtCalculationException(readiness.NextAction.code,readiness.NextAction.explanation);
  var data=design.diagnosticCalculation;var machine=DiagnosticData.Machine(design,data.activeMachineId);
  var result=new DiagnosticResult{id=Guid.NewGuid().ToString(),createdUtc=DateTime.UtcNow.ToString("O"),machineId=machine.machineId,roiId=data.activeRoiId,profileId=profile.id,profileVersion=profile.revision,profileHash=machine.profileHash,basis=profile.basis,inputFingerprint=readiness.inputFingerprint,inputsJson=InputJson(design),
   contributions=Contributions(design,machine,profile),complete=true,profileJson=DiagnosticProfiles.SnapshotJson(profile),comparisonJson=JsonUtility.ToJson(data.comparison),weeklyExposureCount=new DiagnosticNumber{text=machine.weeklyExposureCount.text}};
  result.label=design.items.Find(i=>i.id==machine.machineId).name+" / "+design.items.Find(i=>i.id==data.activeRoiId).name+" / "+result.createdUtc;
  result.isZero=result.contributions.All(c=>c.isZero);
  if(!result.isZero){
   var rows=result.contributions.Where(c=>!c.isZero).ToArray();
   double log=rows[0].logShielded,unshielded=rows[0].logUnshielded;
   foreach(var row in rows.Skip(1)){log=CtShieldMath.LogAdd(log,row.logShielded);unshielded=CtShieldMath.LogAdd(unshielded,row.logUnshielded);}
   result.logShieldedGy=log-Math.Log(1000);result.shieldedAirKermaGy=Math.Exp(result.logShieldedGy);result.unshieldedAirKermaGy=Math.Exp(unshielded-Math.Log(1000));result.underflow=result.shieldedAirKermaGy==0;
   if(!CtShieldMath.IsFinite(result.shieldedAirKermaGy)||!CtShieldMath.IsFinite(result.unshieldedAirKermaGy))Fail("InvalidNumericInput","Combined air kerma exceeds the numeric domain.");
  }
  if(Fingerprint(design)!=readiness.inputFingerprint)throw new Exception("Inputs changed during calculation. Recalculate.");
  result.recordHash=RecordHash(result);
  ValidateResult(result);
  return result;
 }
 public static string RecordHash(DiagnosticResult result){
  var copy=JsonUtility.FromJson<DiagnosticResult>(JsonUtility.ToJson(result));copy.recordHash="";
  return DiagnosticProfiles.Hash(JsonUtility.ToJson(copy));
 }
 public static void ValidateResult(DiagnosticResult result){
  if(result==null||result.version!=1||!result.complete||result.quantity!="airKerma"||result.unit!="Gy"||!new[]{"exposure","exam","min","week"}.Contains(result.basis)||string.IsNullOrWhiteSpace(result.id)||result.contributions==null||result.contributions.Count==0)
   throw new Exception("Invalid diagnostic result record.");
  if(!CtShieldMath.IsFinite(result.shieldedAirKermaGy)||result.shieldedAirKermaGy<0||!CtShieldMath.IsFinite(result.unshieldedAirKermaGy)||result.unshieldedAirKermaGy<0||!CtShieldMath.IsFinite(result.logShieldedGy))
   throw new Exception("Nonfinite diagnostic result.");
  if(result.recordHash!=RecordHash(result)||result.inputFingerprint!=DiagnosticProfiles.Hash(result.inputsJson)||result.profileHash!=DiagnosticProfiles.Hash(result.profileJson))
   throw new Exception("Diagnostic result integrity check failed. Saved history cannot establish a current calculation.");
 }
 public static bool Comparison(Design design,DiagnosticProfile profile,out double factor,out string reason){
  factor=1;reason="";var comparison=design.diagnosticCalculation.comparison;
  if(!comparison.requested){reason="Comparison not requested.";return false;}
  if(comparison.weekly){
   var machine=DiagnosticData.Machine(design,design.diagnosticCalculation.activeMachineId);
   if(profile.basis!="exam"&&profile.basis!="exposure"){reason="Weekly conversion requires a per-exam/exposure profile.";return false;}
   if(!machine.weeklyExposureCount.TryGet(out double count)||count<0||count>1e12){reason="Enter a weekly exposure count between 0 and 1e12.";return false;}factor=count;
  }
  if(comparison.occupied){
   if(!comparison.occupancy.TryGet(out double occupancy)||occupancy<0||occupancy>1){reason="Enter occupancy between 0 and 1 for this comparison only.";return false;}
   if(occupancy==0){reason="Not evaluated: zero occupancy does not establish a shielding requirement.";return false;}factor*=occupancy;
  }
  if(!comparison.limitGy.TryGet(out double limit)||limit<0){reason="Enter a finite nonnegative limit in Gy on the same basis.";return false;}
  return true;
 }
}
}
