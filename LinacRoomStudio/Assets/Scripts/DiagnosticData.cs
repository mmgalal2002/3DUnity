using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public enum DiagnosticMachineType {
 Unconfigured, GeneralRadiography, Fluoroscopy, Mammography, DentalIntraoral,
 DentalPanoramic, DentalCephalometric, DentalCbct, ConventionalCt,
 Mri, Ultrasound, NuclearMedicine, Treatment
}
public enum DiagnosticPointRole { Target, Scatter, ROI }
[Serializable] public sealed class DiagnosticNumber {
 public string text="";
 public bool TryGet(out double value)=>double.TryParse(text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value)&&CtShieldMath.IsFinite(value);
 public void Set(double value){text=value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);}
}
[Serializable] public sealed class DiagnosticInput {
 public string key="";
 public DiagnosticNumber value=new DiagnosticNumber();
}
[Serializable] public sealed class DiagnosticMachine {
 public string machineId="",profileId="",profileVersion="",profileHash="",targetPointId="",scatterPointId="";
 public DiagnosticMachineType machineType;
 public DiagnosticNumber tubeVoltageKvp=new DiagnosticNumber();
 public List<DiagnosticInput> exposureInputs=new List<DiagnosticInput>();
 public DiagnosticNumber weeklyExposureCount=new DiagnosticNumber();
}
[Serializable] public sealed class DiagnosticPoint {
 public int version;
 public DiagnosticPointRole role;
 public string machineId="";
 public CtVector positionMeters=new CtVector();
 public bool hasCoordinateDraft;
 public string[] coordinateDraft=new string[0];
}
[Serializable] public sealed class DiagnosticProject {
 public int version;
 public double metersPerUnityUnit=1;
 public string activeMachineId="",activeRoiId="",quantity="airKerma",displayUnit="Gy";
 public List<DiagnosticMachine> machines=new List<DiagnosticMachine>();
 public List<DiagnosticResult> results=new List<DiagnosticResult>();
 public DiagnosticResult currentResult=null;
 public DiagnosticComparison comparison=new DiagnosticComparison();
 public bool legacyMapped;
}
[Serializable] public sealed class DiagnosticComparison {
 public bool requested,weekly,occupied;
 public DiagnosticNumber limitGy=new DiagnosticNumber(),occupancy=new DiagnosticNumber();
}
public static class DiagnosticData {
 public static bool IsPoint(Item item)=>item?.diagnosticPoint!=null&&item.diagnosticPoint.version==1;
 public static DiagnosticMachineType Family(Item item){
  if(item==null)return DiagnosticMachineType.Unconfigured;
  if(item.machineType!=DiagnosticMachineType.Unconfigured)return item.machineType;
  if(item.kind=="LINAC")return DiagnosticMachineType.Treatment;
  return DiagnosticMachineType.Unconfigured;
 }
 public static void MigrateMachineTypes(Design design){
  foreach(var item in design.items.Where(i=>i.machineType==DiagnosticMachineType.Unconfigured&&(i.kind=="Model"||i.kind=="Source"||i.kind=="LINAC"))){
   var entry=EquipmentPalette.Entries.FirstOrDefault(e=>e.model==item.model);
   item.machineType=item.kind=="LINAC"?DiagnosticMachineType.Treatment:entry?.machineType??DiagnosticMachineType.Unconfigured;
  }
 }
 public static bool IsMachine(Item item)=>Family(item)!=DiagnosticMachineType.Unconfigured;
 public static bool IsXray(DiagnosticMachineType type)=>type>=DiagnosticMachineType.GeneralRadiography&&type<=DiagnosticMachineType.ConventionalCt;
 public static DiagnosticProject Ensure(Design design){
  if(design.diagnosticCalculation==null||design.diagnosticCalculation.version==0)design.diagnosticCalculation=new DiagnosticProject{version=1};
  if(design.diagnosticCalculation.version!=1)throw new Exception("Unsupported diagnostic calculation version.");
  return design.diagnosticCalculation;
 }
 public static DiagnosticMachine Machine(Design design,string id)=>design.diagnosticCalculation?.machines.Find(m=>m.machineId==id);
 public static DiagnosticMachine Configure(Design design,Item item){
  if(!IsMachine(item)||!design.items.Contains(item))throw new Exception("Choose equipment in this room.");
  var existing=Machine(design,item.id);if(existing!=null)return existing;
  if(item.locked)throw new Exception(item.name+" is locked. Select it in Object to unlock before configuration.");
  var data=Ensure(design);
  if(data.machines.Count>=CtShieldingData.MaxSources)throw new Exception("Maximum 16 configured machines.");
  var machine=new DiagnosticMachine{machineId=item.id,machineType=Family(item)};
  data.machines.Add(machine);return machine;
 }
 public static void Select(Design design,Item item){
  if(item==null)return;
  if(IsPoint(item)){
   var data=Ensure(design);data.activeMachineId=item.diagnosticPoint.machineId;
   if(item.diagnosticPoint.role==DiagnosticPointRole.ROI)data.activeRoiId=item.id;
  }else if(IsMachine(item))Ensure(design).activeMachineId=item.id;
 }
 public static CtVector Position(Design design,Item item){
  var point=item.diagnosticPoint;if(!IsPoint(item))return new CtVector(item.x,item.y,item.z);
  double units=design.diagnosticCalculation?.metersPerUnityUnit??1;
  var p=point.positionMeters;
  if(point.role!=DiagnosticPointRole.Target)return new CtVector(p.x/units,p.y/units,p.z/units);
  var owner=design.items.Find(i=>i.id==point.machineId);
  if(owner==null)throw new Exception("Target owner is missing: "+item.id);
  double angle=owner.angle*Math.PI/180,c=Math.Cos(angle),s=Math.Sin(angle);
  return new CtVector(owner.x+(c*p.x+s*p.z)/units,owner.y+p.y/units,owner.z+(-s*p.x+c*p.z)/units);
 }
 public static void SetPosition(Design design,Item item,CtVector world,bool preserveDraft=false){
  if(item.locked)throw new Exception(item.name+" is locked.");
  CheckVector(world);
  double units=Ensure(design).metersPerUnityUnit;
  var p=new CtVector(world.x*units,world.y*units,world.z*units);
  if(item.diagnosticPoint.role==DiagnosticPointRole.Target){
   var owner=design.items.Find(i=>i.id==item.diagnosticPoint.machineId);
   if(owner==null||owner.locked)throw new Exception("Select and unlock the Target's machine before editing its source anchor.");
   double angle=owner.angle*Math.PI/180,c=Math.Cos(angle),s=Math.Sin(angle),x=world.x-owner.x,z=world.z-owner.z;
   p=new CtVector((c*x-s*z)*units,(world.y-owner.y)*units,(s*x+c*z)*units);
  }
  item.diagnosticPoint.positionMeters=p;Project(item,world);
  if(!preserveDraft){item.diagnosticPoint.hasCoordinateDraft=false;item.diagnosticPoint.coordinateDraft=new string[0];}
 }
 static void Project(Item item,CtVector world){item.x=(float)world.x;item.y=(float)world.y;item.z=(float)world.z;}
 public static void Synchronize(Design design){
  foreach(var item in design.items.Where(IsPoint)){
   var position=Position(design,item);
   // Float edits are imported only when their projection changed; exact stored doubles survive saves.
   if(item.diagnosticPoint.role!=DiagnosticPointRole.Target&&((float)position.x!=item.x||(float)position.y!=item.y||(float)position.z!=item.z))
    SetPosition(design,item,new CtVector(item.x,item.y,item.z));
   else Project(item,position);
  }
 }
 public static Item Place(Design design,DiagnosticPointRole role,string machineId,CtVector world){
  var machine=string.IsNullOrEmpty(machineId)?null:Machine(design,machineId);
  if(!string.IsNullOrEmpty(machineId)&&machine==null)throw new Exception("Configure the selected machine before placing its points.");
  if(role==DiagnosticPointRole.Target&&machine==null)throw new Exception("Choose a machine before placing its Target.");
  string existingId=role==DiagnosticPointRole.Target?machine?.targetPointId:role==DiagnosticPointRole.Scatter?machine?.scatterPointId:"";
  var existing=design.items.Find(i=>i.id==existingId);
  if(existing!=null){SetPosition(design,existing,world);return existing;}
  if(design.items.Count>=250||design.items.Count(IsPoint)>=64)throw new Exception("Calculation point/object limit reached.");
  if(machine!=null&&design.items.Find(i=>i.id==machineId).locked)throw new Exception("Unlock the machine before associating a new point.");
  var item=new Item{kind="Model",model="Dot",name=role==DiagnosticPointRole.Scatter?"Scatter (patient)":role.ToString(),scale=.15f,
   diagnosticPoint=new DiagnosticPoint{version=1,role=role,machineId=machineId??""}};
  SetPosition(design,item,world);
  design.items.Add(item);
  if(role==DiagnosticPointRole.Target)machine.targetPointId=item.id;
  else if(role==DiagnosticPointRole.Scatter&&machine!=null)machine.scatterPointId=item.id;
  else if(role==DiagnosticPointRole.ROI)Ensure(design).activeRoiId=item.id;
  return item;
 }
 public static void Associate(Design design,Item point,DiagnosticMachine machine){
  if(!IsPoint(point)||point.diagnosticPoint.role==DiagnosticPointRole.Target)throw new Exception("Select an independent Scatter or ROI.");
  if(point.locked||design.items.Find(i=>i.id==machine.machineId).locked)throw new Exception("Unlock the point and machine before associating them.");
  if(point.diagnosticPoint.role==DiagnosticPointRole.Scatter){
   if(!string.IsNullOrEmpty(machine.scatterPointId)&&machine.scatterPointId!=point.id)throw new Exception("This machine already has a Scatter point.");
   var previous=Machine(design,point.diagnosticPoint.machineId);if(previous?.scatterPointId==point.id)previous.scatterPointId="";
   machine.scatterPointId=point.id;
  }
  point.diagnosticPoint.machineId=machine.machineId;Select(design,point);
 }
 public static void RequireTransform(Design design,IEnumerable<Item> items){
  if(design==null)return;
  var selected=items.ToList();
  foreach(var point in selected.Where(i=>IsPoint(i)&&i.diagnosticPoint.role==DiagnosticPointRole.Target)){
   var owner=design.items.Find(i=>i.id==point.diagnosticPoint.machineId);
   if(owner.locked)throw new Exception("Unlock the machine before moving its Target.");
   if(selected.Contains(owner))throw new Exception("Select the machine without its Target; the Target follows its owner automatically.");
  }
  if(design.items.Any(i=>IsPoint(i)&&i.diagnosticPoint.role==DiagnosticPointRole.Target&&i.locked&&selected.Any(m=>m.id==i.diagnosticPoint.machineId)))
   throw new Exception("Unlock the owned Target before moving its machine.");
 }
 public static void ConfigureCopiedTargets(Design design,IEnumerable<Item> items){
  foreach(var point in items.Where(i=>IsPoint(i)&&i.diagnosticPoint.role==DiagnosticPointRole.Target)){
   var machine=Configure(design,design.items.Find(i=>i.id==point.diagnosticPoint.machineId));machine.targetPointId=point.id;
  }
 }
 public static void MigrateLegacy(Design design){
  if(!CtShieldingData.Active(design.ct)||design.diagnosticCalculation?.legacyMapped==true)return;
  var candidate=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  var data=Ensure(candidate);
  if(data.machines.Count==0&&!candidate.items.Any(IsPoint)){
   foreach(var source in candidate.ct.sources){
    var device=candidate.items.Find(i=>i.id==source.deviceId);
    if(device==null||device.locked||Family(device)!=DiagnosticMachineType.ConventionalCt)continue;
    var machine=Configure(candidate,device);
    if(source.kvp>0)machine.tubeVoltageKvp.Set(source.kvp);
    var scatter=candidate.items.Find(i=>i.id==source.scatterPointId);
    if(scatter!=null)Place(candidate,DiagnosticPointRole.Scatter,device.id,CtShieldingData.Position(candidate,scatter));
   }
   foreach(var point in candidate.items.Where(i=>CtShieldingData.IsRoi(i)&&i.ctPoint.role=="ROI").ToArray()){
    var associations=point.ctPoint.roi.contributions.Select(c=>candidate.ct.sources.Find(s=>s.id==c.sourceId)?.deviceId).Where(id=>Machine(candidate,id)!=null).Distinct().ToArray();
    if(associations.Length==1)Place(candidate,DiagnosticPointRole.ROI,associations[0],CtShieldingData.Position(candidate,point));
   }
   data.activeMachineId="";data.activeRoiId="";
  }
  data.legacyMapped=true;Validate(candidate);
  design.diagnosticCalculation=data;
  design.items.AddRange(candidate.items.Where(i=>IsPoint(i)&&!design.items.Any(old=>old.id==i.id)));
 }
 public static HashSet<string> RemovalIds(Design design,HashSet<string> ids){
  foreach(var point in design.items.Where(i=>IsPoint(i)&&i.diagnosticPoint.role==DiagnosticPointRole.Target&&ids.Contains(i.diagnosticPoint.machineId)).ToArray())ids.Add(point.id);
  SelectionEditing.RequireUnlocked(design.items.Where(i=>ids.Contains(i.id)));
  if(design.items.Any(i=>IsPoint(i)&&i.locked&&ids.Contains(i.diagnosticPoint.machineId)))throw new Exception("Unlock associated points before removing their machine.");
  return ids;
 }
 public static void RemoveReferences(Design design,HashSet<string> ids){
  var data=design.diagnosticCalculation;if(data==null)return;
  data.machines.RemoveAll(m=>ids.Contains(m.machineId));
  foreach(var machine in data.machines){if(ids.Contains(machine.targetPointId))machine.targetPointId="";if(ids.Contains(machine.scatterPointId))machine.scatterPointId="";}
  foreach(var point in design.items.Where(IsPoint))if(ids.Contains(point.diagnosticPoint.machineId))point.diagnosticPoint.machineId="";
  if(ids.Contains(data.activeMachineId))data.activeMachineId="";if(ids.Contains(data.activeRoiId))data.activeRoiId="";
 }
 public static void Validate(Design design){
  var data=design.diagnosticCalculation;
  if(design.items.Any(i=>!Enum.IsDefined(typeof(DiagnosticMachineType),i.machineType)))throw new Exception("Unknown machine calculation family.");
  if(design.items.Any(i=>i.diagnosticPoint!=null&&i.diagnosticPoint.version!=0&&i.diagnosticPoint.version!=1))throw new Exception("Unsupported diagnostic point version.");
  if(data==null||data.version==0){if(design.items.Any(IsPoint))throw new Exception("Calculation markers require diagnostic metadata.");return;}
  if(data.version!=1||!CtShieldMath.IsFinite(data.metersPerUnityUnit)||data.metersPerUnityUnit<=0||data.quantity!="airKerma"||data.displayUnit!="Gy"||data.machines==null||data.results==null||data.comparison==null)
   throw new Exception("Invalid diagnostic calculation metadata or units.");
  if(data.machines.Count>16||data.results.Count>50||design.items.Count(IsPoint)>64)throw new Exception("Diagnostic record limit exceeded.");
  var resultIds=new HashSet<string>();foreach(var result in data.results){DiagnosticCalculation.ValidateResult(result);if(!resultIds.Add(result.id))throw new Exception("Duplicate diagnostic result ID.");}
  if(data.currentResult?.complete==true)DiagnosticCalculation.ValidateResult(data.currentResult);
  var ids=new HashSet<string>();
  foreach(var machine in data.machines){
   var item=design.items.Find(i=>i.id==machine.machineId);
   if(!ids.Add(machine.machineId)||!IsMachine(item)||Family(item)!=machine.machineType||machine.tubeVoltageKvp==null||machine.exposureInputs==null||machine.weeklyExposureCount==null)
    throw new Exception("Duplicate, missing or mismatched diagnostic machine.");
   var keys=new HashSet<string>();foreach(var input in machine.exposureInputs)if(input==null||string.IsNullOrEmpty(input.key)||!keys.Add(input.key)||input.value==null)throw new Exception("Invalid diagnostic exposure input.");
   ValidateLink(design,machine,machine.targetPointId,DiagnosticPointRole.Target);
   ValidateLink(design,machine,machine.scatterPointId,DiagnosticPointRole.Scatter);
  }
  foreach(var item in design.items.Where(IsPoint)){
   var point=item.diagnosticPoint;
   if(item.kind!="Model"||item.model!="Dot"||CtShieldingData.IsPoint(item)||point.version!=1||!Enum.IsDefined(typeof(DiagnosticPointRole),point.role))
    throw new Exception("Invalid diagnostic point role/version.");
   if(point.role==DiagnosticPointRole.Target&&string.IsNullOrEmpty(point.machineId)||!string.IsNullOrEmpty(point.machineId)&&!ids.Contains(point.machineId))throw new Exception("Missing calculation point owner.");
   CheckVector(point.positionMeters);
   if(point.hasCoordinateDraft&&(point.coordinateDraft==null||point.coordinateDraft.Length!=3))throw new Exception("Invalid point coordinate draft.");
   var machine=Machine(design,point.machineId);
   if(point.role==DiagnosticPointRole.Target&&machine.targetPointId!=item.id||point.role==DiagnosticPointRole.Scatter&&machine!=null&&machine.scatterPointId!=item.id)throw new Exception("Inconsistent diagnostic point association.");
  }
  if(!string.IsNullOrEmpty(data.activeMachineId)&&!design.items.Any(i=>i.id==data.activeMachineId&&IsMachine(i)))throw new Exception("Active machine is missing.");
  if(!string.IsNullOrEmpty(data.activeRoiId)&&!design.items.Any(i=>i.id==data.activeRoiId&&IsPoint(i)&&i.diagnosticPoint.role==DiagnosticPointRole.ROI))throw new Exception("Active ROI is missing.");
 }
 static void ValidateLink(Design design,DiagnosticMachine machine,string id,DiagnosticPointRole role){
  if(string.IsNullOrEmpty(id))return;
  var point=design.items.Find(i=>i.id==id)?.diagnosticPoint;
  if(point==null||point.role!=role||point.machineId!=machine.machineId)throw new Exception("Invalid "+role+" association.");
 }
 static void CheckVector(CtVector p){if(p==null||!CtShieldMath.IsFinite(p.x)||!CtShieldMath.IsFinite(p.y)||!CtShieldMath.IsFinite(p.z))throw new Exception("Point coordinates must be finite.");}
}
}
