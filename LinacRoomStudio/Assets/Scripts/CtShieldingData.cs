using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
[Serializable] public class CtPointData {
 public int version;
 public string role="",ownerId="";
 public double localX,localY,localZ;
 public bool hasWorldCoordinates;
 public double worldX,worldY,worldZ;
 public bool anchorConfirmed;
 public CtRoiData roi=null;
}
[Serializable] public class CtRoiData {
 public bool hasOccupancy,hasGoal,hasBestFactor,hasWorstFactor;
 public double occupancy,goalMgyPerWeek,bestFactor,worstFactor;
 public string purpose="",goalSource="",occupancyConvention="",goalConvention="Occupied";
 public List<CtRoiContribution> contributions=new List<CtRoiContribution>();
}
[Serializable] public class CtRoiContribution {
 public string sourceId="",directSourceFingerprint="";
 public bool hasDirectKerma,hasKermaPosition;
 public double directMgyPerWeek,kermaX,kermaY,kermaZ;
}
[Serializable] public class CtSourceData {
 public string id=Guid.NewGuid().ToString(),deviceId="",scatterPointId="",scannerId="",protocolId="";
 public string beamFamily="CT_SECONDARY",mode="DirectWeekly",citation="",validityDomain="",includedComponents="";
 public string radiationQuantity="airKerma",kermaUnit="mGy",interval="week";
 public int kvp;
 public bool applicabilityReviewed,pathApproximationAccepted,componentsConfirmed,includesRoomBarriers,isotropicReferenceAccepted;
 public bool hasReferenceKerma,hasReferenceDistance,hasWorkload,hasMinimumDistance,hasMaximumDistance;
 public double referenceMgyPerExam,referenceDistanceMeters,examsPerWeek,minimumDistanceMeters,maximumDistanceMeters;
}
[Serializable] public class CtProjectData {
 public int version;
 public double metersPerUnityUnit=1;
 public string scenarioName="Nominal",selectedCase="Nominal",shieldingMode="Current",selectedRoiId="",selectedSourceId="";
 public List<CtSourceData> sources=new List<CtSourceData>();
 public List<CtResultSnapshot> results=new List<CtResultSnapshot>();
}
[Serializable] public class CtPathSegment {
 public string barrierId="",material="",fitId="";
 public double entryMeters,exitMeters,pathThicknessMm,alpha,beta,gamma;
 public CtVector entry=new CtVector(),exit=new CtVector();
}
[Serializable] public class CtVector {
 public double x,y,z;
 public CtVector(){}
 public CtVector(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
 public Vector3 UnityVector=>new Vector3((float)x,(float)y,(float)z);
}
[Serializable] public class CtContributionResult {
 public string sourceId="",protocolId="",status="MissingRequiredInput",message="";
 public bool hasResult,isZero,underflow;
 public double distanceMeters,unshieldedMgyPerWeek,logUnshielded,shieldedMgyPerWeek,logShielded,logB,transmission;
 public List<CtPathSegment> segments=new List<CtPathSegment>();
 public List<string> flags=new List<string>();
}
[Serializable] public class CtRoiResult {
 public string roiId="",roiName="",status="MissingRequiredInput",verdict="Not evaluated";
 public CtVector position=new CtVector();
 public bool complete,hasPhysical,hasOccupied,hasGoal,hasVerdict,passed,isZero,underflow;
 public double physicalMgyPerWeek,occupiedMgyPerWeek,unshieldedMgyPerWeek,logPhysical,logOccupied,occupancy,goalMgyPerWeek,utilization,margin;
 public List<CtContributionResult> contributions=new List<CtContributionResult>();
 public List<string> errors=new List<string>(),flags=new List<string>();
}
[Serializable] public class CtResultSnapshot {
 public int version=1;
 public string id=Guid.NewGuid().ToString(),createdUtc="",name="",scenarioCase="Nominal",shieldingMode="Current";
 public string projectName="",engineVersion="RoomStudio.CT.1",datasetId="NCRP147_APPENDIX_A_CT_SECONDARY",datasetHash="";
 public string specificationHash="",reportedPdfHash="",recordHash="";
 public string quantity="airKerma",unit="mGy",interval="week",inputFingerprint="",inputsJson="";
 public double geometryToleranceMeters=CtShieldingCalculation.GeometryToleranceMeters;
 public List<CtRoiResult> rows=new List<CtRoiResult>();
}
[Serializable] public class CtResultFile {
 public string format="RoomStudio.CT.Results";
 public int version=1;
 public List<CtResultSnapshot> results=new List<CtResultSnapshot>();
}

public static class CtShieldingData {
 public const int MaxSources=16,MaxPoints=64,MaxResults=50,MaxResultFileBytes=16*1024*1024;
 public static bool Active(CtProjectData data)=>data!=null&&data.version==1;
 public static bool IsScanner(Item item)=>item!=null&&item.model=="CT"&&(item.kind=="Model"||item.kind=="Source");
 public static bool IsPoint(Item item)=>item!=null&&item.kind=="Model"&&item.model=="Dot"&&item.ctPoint!=null&&item.ctPoint.version==1;
 public static bool IsRoi(Item item)=>IsPoint(item)&&(item.ctPoint.role=="ROI"||item.ctPoint.role=="Patient");
 public static CtProjectData Ensure(Design design){
  if(!Active(design.ct))design.ct=new CtProjectData{version=1};
  return design.ct;
 }
 public static CtSourceData ConfigureSource(Design design,Item scanner){
  if(design==null||!IsScanner(scanner)||!design.items.Contains(scanner))throw new Exception("Select a CT scanner placed in this design.");
  if(scanner.locked)throw new Exception("Unlock the CT scanner before adding its scatter configuration.");
  if(design.ct!=null&&design.ct.version!=0&&!Active(design.ct))throw new Exception("Unsupported CT project metadata version.");
  var existing=Active(design.ct)?design.ct.sources.Find(source=>source.deviceId==scanner.id):null;
  if(existing!=null){design.ct.selectedSourceId=existing.id;return existing;}
  if(Active(design.ct)&&design.ct.sources.Count>=MaxSources||design.items.Count>=250||design.items.Count(IsPoint)>=MaxPoints)throw new Exception("CT source/point or project object limits reached.");
  var scatter=CreatePoint("Scatter",new CtVector(scanner.x,scanner.y,scanner.z),scanner.id);
  var source=new CtSourceData{deviceId=scanner.id,scatterPointId=scatter.id};
  var data=Ensure(design);design.items.Add(scatter);data.sources.Add(source);data.selectedSourceId=source.id;
  foreach(var roi in design.items.Where(item=>IsRoi(item)&&!item.locked))roi.ctPoint.roi.contributions.Add(new CtRoiContribution{sourceId=source.id});
  return source;
 }
 public static void SetTubePotential(Design design,CtSourceData source,int kvp){
  if(source==null||!Active(design.ct)||!design.ct.sources.Contains(source))throw new Exception("Select a configured CT source.");
  if(kvp!=120&&kvp!=140)throw new Exception("UnsupportedSpectrum: CT coefficients are supplied only for 120 and 140 kVp.");
  var scanner=design.items.Find(item=>item.id==source.deviceId);var scatter=design.items.Find(item=>item.id==source.scatterPointId);
  if(scanner==null||scatter==null||scanner.locked||scatter.locked)throw new Exception("Unlock the CT scanner and scatter point before changing tube potential.");
  if(source.kvp==kvp)return;
  source.kvp=kvp;source.applicabilityReviewed=false;
 }
 public static CtVector Position(Design design,Item item){
  if(DiagnosticData.IsPoint(item))return DiagnosticData.Position(design,item);
  if(!IsPoint(item)||string.IsNullOrEmpty(item.ctPoint.ownerId))return WorldPosition(item);
  var owner=design.items.Find(candidate=>candidate.id==item.ctPoint.ownerId);
  if(owner==null)throw new Exception("Calculation point owner is missing: "+item.id);
  double radians=owner.angle*Math.PI/180,cosine=Math.Cos(radians),sine=Math.Sin(radians);
  return new CtVector(owner.x+cosine*item.ctPoint.localX+sine*item.ctPoint.localZ,owner.y+item.ctPoint.localY,owner.z-sine*item.ctPoint.localX+cosine*item.ctPoint.localZ);
 }
 public static CtVector WorldPosition(Item item){var point=item.ctPoint;if(IsRoi(item)&&point.hasWorldCoordinates&&(float)point.worldX==item.x&&(float)point.worldY==item.y&&(float)point.worldZ==item.z)return new CtVector(point.worldX,point.worldY,point.worldZ);return new CtVector(item.x,item.y,item.z);}
 public static void SetPointPosition(Design design,Item item,CtVector point){
  if(item.locked)throw new Exception("Unlock the calculation point before editing it.");
  if(IsPoint(item)&&!string.IsNullOrEmpty(item.ctPoint.ownerId)){
   var owner=design.items.Find(candidate=>candidate.id==item.ctPoint.ownerId);
   if(owner==null||owner.locked)throw new Exception("Unlock the owning CT device before editing its scatter point.");
   double radians=owner.angle*Math.PI/180,cosine=Math.Cos(radians),sine=Math.Sin(radians),deltaX=point.x-owner.x,deltaZ=point.z-owner.z;
   item.ctPoint.localX=cosine*deltaX-sine*deltaZ;item.ctPoint.localY=point.y-owner.y;item.ctPoint.localZ=sine*deltaX+cosine*deltaZ;
   item.ctPoint.anchorConfirmed=false;
  }
  item.x=(float)point.x;item.y=(float)point.y;item.z=(float)point.z;
  if(IsRoi(item)){item.ctPoint.hasWorldCoordinates=true;item.ctPoint.worldX=point.x;item.ctPoint.worldY=point.y;item.ctPoint.worldZ=point.z;}
 }
 public static Item CreatePoint(string role,CtVector position,string ownerId=""){
  if(role!="Scatter"&&role!="ROI"&&role!="Patient")throw new Exception("Unknown calculation point role.");
  return new Item{kind="Model",model="Dot",name=role+" point",x=(float)position.x,y=(float)position.y,z=(float)position.z,scale=.15f,
   ctPoint=new CtPointData{version=1,role=role,ownerId=ownerId,hasWorldCoordinates=role!="Scatter",worldX=position.x,worldY=position.y,worldZ=position.z,roi=role=="Scatter"?null:new CtRoiData()}};
 }
 public static void ConfirmDirectPosition(Design design,Item roi,CtRoiContribution contribution){var position=Position(design,roi);contribution.hasKermaPosition=true;contribution.kermaX=position.x;contribution.kermaY=position.y;contribution.kermaZ=position.z;contribution.directSourceFingerprint=CtShieldingCalculation.DirectSourceFingerprint(design,contribution.sourceId);}
 public static void SynchronizeAnchors(Design design){foreach(var point in design.items.Where(item=>IsPoint(item)&&item.ctPoint.role=="Scatter")){var position=Position(design,point);point.x=(float)position.x;point.y=(float)position.y;point.z=(float)position.z;}}
 public static void RequireTransform(Design design,IEnumerable<Item> items){
  var targets=items.ToList();if(targets.Any(item=>IsPoint(item)&&item.ctPoint.role=="Scatter"))throw new Exception("Edit an attached scatter point through CT anchor controls, not a scene transform.");
  if(design!=null){var owners=new HashSet<string>(targets.Select(item=>item.id));if(design.items.Any(item=>IsPoint(item)&&owners.Contains(item.ctPoint.ownerId)&&item.locked))throw new Exception("A protected scatter point prevents moving its CT scanner.");}
 }
 public static HashSet<string> RemovalIds(Design design,IEnumerable<string> ids){
  var removed=new HashSet<string>(ids);foreach(var point in design.items.Where(IsPoint))if(removed.Contains(point.ctPoint.ownerId))removed.Add(point.id);
  SelectionEditing.RequireUnlocked(design.items.Where(item=>removed.Contains(item.id)));return removed;
 }
 public static void RemoveReferences(Design design,HashSet<string> removed){
  if(!Active(design.ct))return;
  var sourceIds=new HashSet<string>(design.ct.sources.Where(source=>removed.Contains(source.deviceId)||removed.Contains(source.scatterPointId)).Select(source=>source.id));
  design.ct.sources.RemoveAll(source=>sourceIds.Contains(source.id));
  foreach(var roi in design.items.Where(IsRoi))roi.ctPoint.roi.contributions.RemoveAll(contribution=>sourceIds.Contains(contribution.sourceId));
 }
 static void Range(double value,double minimum,double maximum,string name){if(!CtShieldMath.IsFinite(value)||value<minimum||value>maximum)throw new Exception("Invalid CT "+name+".");}
 static void Text(string value,int maximum,string name){if(value!=null&&value.Length>maximum)throw new Exception("CT "+name+" is too long.");}
 public static void Validate(Design design){
  if(design.ct!=null&&design.ct.version!=0&&design.ct.version!=1)throw new Exception("Unsupported CT project metadata version.");
  var points=design.items.Where(IsPoint).ToList();
  if(points.Count>MaxPoints)throw new Exception("Maximum 64 calculation points.");
  foreach(var item in design.items){
   if(item.ctPoint!=null&&item.ctPoint.version!=0&&!IsPoint(item))throw new Exception("Invalid calculation-point metadata: "+item.id);
   if(item.model=="Dot"&&!IsPoint(item)&&!DiagnosticData.IsPoint(item))throw new Exception("A Dot marker needs an explicit calculation role.");
  }
  if(!Active(design.ct)){if(points.Count>0)throw new Exception("Calculation points need CT project metadata.");return;}
  if(design.ct.metersPerUnityUnit!=1)throw new Exception("This Room Studio geometry uses exactly 1 metre per Unity unit.");
  if(!new[]{"Nominal","Best","Worst"}.Contains(design.ct.selectedCase)||!new[]{"Current","WallsOff","AllOff"}.Contains(design.ct.shieldingMode))throw new Exception("Unknown CT scenario mode.");
  Text(design.ct.scenarioName,100,"scenario name");
  if(design.ct.sources==null||design.ct.sources.Count>MaxSources||design.ct.results==null||design.ct.results.Count>MaxResults)throw new Exception("CT source/history limits exceeded.");
  var sourceIds=new HashSet<string>(StringComparer.Ordinal);
  foreach(var source in design.ct.sources){
   if(source==null||string.IsNullOrEmpty(source.id)||!sourceIds.Add(source.id)||source.id.Length>128)throw new Exception("Invalid/duplicate CT source ID.");
   var owner=design.items.Find(item=>item.id==source.deviceId);var scatter=points.Find(item=>item.id==source.scatterPointId);
   if(!IsScanner(owner)||scatter==null||scatter.ctPoint.role!="Scatter"||scatter.ctPoint.ownerId!=owner.id)throw new Exception("A CT source needs its own CT scanner and attached scatter point.");
   if(source.mode!="DirectWeekly"&&source.mode!="ReferenceExams")throw new Exception("Unsupported CT source normalization mode.");
  if(source.radiationQuantity!="airKerma"||source.kermaUnit!="mGy"||source.interval!="week")throw new Exception("Invalid CT source quantity, unit or time basis.");
   Text(source.scannerId,100,"scanner ID");Text(source.protocolId,100,"protocol ID");Text(source.citation,1000,"source citation");Text(source.validityDomain,1000,"source validity domain");Text(source.includedComponents,1000,"component coverage");Text(source.beamFamily,100,"beam family");
   if(source.kvp<0||source.kvp>1000)throw new Exception("Invalid CT tube potential.");
   if(source.hasReferenceKerma)Range(source.referenceMgyPerExam,0,1e12,"reference kerma");
   if(source.hasReferenceDistance)Range(source.referenceDistanceMeters,1e-8,1e8,"reference distance");
   if(source.hasWorkload)Range(source.examsPerWeek,0,1e12,"exam workload");
   if(source.hasMinimumDistance)Range(source.minimumDistanceMeters,1e-8,1e8,"minimum source distance");
   if(source.hasMaximumDistance){Range(source.maximumDistanceMeters,1e-8,1e8,"maximum source distance");if(source.hasMinimumDistance&&source.maximumDistanceMeters<source.minimumDistanceMeters)throw new Exception("Invalid CT source distance domain.");}
  }
  foreach(var point in points){
   var metadata=point.ctPoint;
   if(metadata.role!="Scatter"&&metadata.role!="ROI"&&metadata.role!="Patient")throw new Exception("Unknown CT point role.");
   Range(metadata.localX,-10000,10000,"anchor X");Range(metadata.localY,-100,100,"anchor Y");Range(metadata.localZ,-10000,10000,"anchor Z");
  if(metadata.hasWorldCoordinates){Range(metadata.worldX,-10000,10000,"world X");Range(metadata.worldY,-100,100,"world Y");Range(metadata.worldZ,-10000,10000,"world Z");}
   if(metadata.role=="Scatter"){
    var owner=design.items.Find(item=>item.id==metadata.ownerId);if(!IsScanner(owner))throw new Exception("Scatter point requires a CT scanner owner.");
   }else{
    if(!string.IsNullOrEmpty(metadata.ownerId)||metadata.roi==null)throw new Exception("ROI/patient evaluation points must be independent world points.");
    var roi=metadata.roi;Text(roi.purpose,200,"ROI purpose");Text(roi.goalSource,1000,"goal citation");Text(roi.occupancyConvention,1000,"occupancy convention");
    if(roi.goalConvention!="Occupied"&&roi.goalConvention!="FieldLimit")throw new Exception("Unknown CT goal convention.");
    if(roi.hasOccupancy)Range(roi.occupancy,0,1,"occupancy");if(roi.hasGoal)Range(roi.goalMgyPerWeek,0,1e12,"goal");
    if(roi.hasBestFactor)Range(roi.bestFactor,0,1e6,"best-case workload factor");if(roi.hasWorstFactor)Range(roi.worstFactor,0,1e6,"worst-case workload factor");
    if(roi.hasBestFactor&&roi.bestFactor>1||roi.hasWorstFactor&&roi.worstFactor<1)throw new Exception("Best-case factor must be <=1 and worst-case factor >=1 relative to nominal.");
    if(roi.contributions==null||roi.contributions.Count>MaxSources)throw new Exception("CT ROI contribution limits exceeded.");
    var included=new HashSet<string>(StringComparer.Ordinal);
    foreach(var contribution in roi.contributions){if(contribution==null||!sourceIds.Contains(contribution.sourceId)||!included.Add(contribution.sourceId))throw new Exception("Missing/duplicate CT ROI source reference.");if(contribution.hasDirectKerma)Range(contribution.directMgyPerWeek,0,1e12,"direct ROI kerma");if(contribution.hasKermaPosition){Range(contribution.kermaX,-10000,10000,"direct field X");Range(contribution.kermaY,-100,100,"direct field Y");Range(contribution.kermaZ,-10000,10000,"direct field Z");}}
   }
  }
  var resultIds=new HashSet<string>(StringComparer.Ordinal);
  foreach(var result in design.ct.results){ValidateResult(result);if(!resultIds.Add(result.id))throw new Exception("Duplicate CT result ID.");}
 }
 public static void ValidateResult(CtResultSnapshot result){
  if(result==null||result.version!=1||string.IsNullOrEmpty(result.id)||result.id.Length>128||result.quantity!="airKerma"||result.unit!="mGy"||result.interval!="week"||result.rows==null||result.rows.Count>MaxPoints)throw new Exception("Unsupported CT result record.");
  Text(result.inputsJson,512*1024,"result inputs");Text(result.name,100,"result name");Text(result.inputFingerprint,64,"result fingerprint");
  foreach(var row in result.rows){
   if(row==null||row.contributions==null||row.contributions.Count>MaxSources||row.errors==null||row.flags==null)throw new Exception("Invalid CT result row.");
   if(row.hasPhysical){Range(row.physicalMgyPerWeek,0,1e100,"saved physical kerma");Range(row.unshieldedMgyPerWeek,0,1e100,"saved unshielded kerma");Range(row.logPhysical,-1e100,1e100,"saved log kerma");}
   if(row.hasOccupied){Range(row.occupiedMgyPerWeek,0,1e100,"saved occupied kerma");Range(row.logOccupied,-1e100,1e100,"saved occupied log kerma");}
  if(row.hasOccupied)Range(row.occupancy,0,1,"saved occupancy");if(row.hasGoal)Range(row.goalMgyPerWeek,0,1e12,"saved goal");
   if(row.hasVerdict&&(!row.complete||!row.hasPhysical||!row.hasGoal))throw new Exception("Incomplete CT result cannot have a verdict.");
   foreach(var contribution in row.contributions){if(contribution==null||contribution.segments==null||contribution.segments.Count>252)throw new Exception("Invalid CT result contribution.");if(contribution.hasResult){Range(contribution.unshieldedMgyPerWeek,0,1e100,"saved contribution kerma");Range(contribution.logB,-1e100,0,"saved log transmission");Range(contribution.transmission,0,1,"saved transmission");Range(contribution.logShielded,-1e100,1e100,"saved shielded log kerma");}}
  }
  if(string.IsNullOrEmpty(result.recordHash)||result.recordHash!=CtShieldingCalculation.RecordHash(result))throw new Exception("CT result integrity hash mismatch; historical inputs/results were changed.");
 }
}
}