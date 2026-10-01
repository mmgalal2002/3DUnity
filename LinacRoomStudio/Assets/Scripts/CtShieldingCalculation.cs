using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RoomStudio {
public sealed class CtCalculationException : Exception {
 public readonly string Status;
 public readonly string ActionTarget,ObservedValue,ExpectedDomain;
 public CtCalculationException(string status,string message,string actionTarget="",string observedValue="",string expectedDomain=""):base(message){Status=status;ActionTarget=actionTarget;ObservedValue=observedValue;ExpectedDomain=expectedDomain;}
}
[Serializable] sealed class CtCalculationInputs {
 public string projectName="";
 public float width,depth,height;
 public double metersPerUnityUnit;
 public Barrier floor,ceiling;
 public List<Item> items;
 public List<WallJunction> junctions;
 public List<CtSourceData> sources;
}
public static class CtShieldingCalculation {
 [Serializable] sealed class DirectSourceContext {public CtSourceData source;public CtVector devicePosition,scatterPosition;public float angle;}
 public const double GeometryToleranceMeters=1e-7;
 static void Fail(string status,string message){throw new CtCalculationException(status,message);}
 static double Length(CtVector first,CtVector second){return Math.Sqrt(Math.Pow(second.x-first.x,2)+Math.Pow(second.y-first.y,2)+Math.Pow(second.z-first.z,2));}
 public static double DistanceMeters(Design design,Item source,Item roi)=>Length(CtShieldingData.Position(design,source),CtShieldingData.Position(design,roi))*(CtShieldingData.Active(design.ct)?design.ct.metersPerUnityUnit:1);
 static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
 public static string RecordHash(CtResultSnapshot result){var copy=JsonUtility.FromJson<CtResultSnapshot>(JsonUtility.ToJson(result));copy.recordHash="";return Hash(JsonUtility.ToJson(copy));}
 public static string InputJson(Design design){
  var input=new CtCalculationInputs{projectName=design.name,width=design.width,depth=design.depth,height=design.height,metersPerUnityUnit=design.ct?.metersPerUnityUnit??1,
   floor=design.floor,ceiling=design.ceiling,items=design.items,junctions=design.wallJunctions,sources=design.ct?.sources??new List<CtSourceData>()};
  string json=JsonUtility.ToJson(input);if(json.Length>512*1024)throw new Exception("CT calculation input snapshot exceeds 512 KiB.");return json;
 }
 public static string InputFingerprint(Design design)=>Hash(InputJson(design));
 public static string DirectSourceFingerprint(Design design,string sourceId){
  var source=design.ct.sources.Find(candidate=>candidate.id==sourceId);var owner=design.items.Find(item=>item.id==source?.deviceId);var scatter=design.items.Find(item=>item.id==source?.scatterPointId);
  if(owner==null||scatter==null)throw new Exception("Missing CT source calibration context.");
  return Hash(JsonUtility.ToJson(new DirectSourceContext{source=source,devicePosition=new CtVector(owner.x,owner.y,owner.z),scatterPosition=CtShieldingData.Position(design,scatter),angle=owner.angle}));
 }
 static string ComparisonFingerprint(CtResultSnapshot snapshot){
  var input=JsonUtility.FromJson<CtCalculationInputs>(snapshot.inputsJson);if(input?.items==null||input.floor==null||input.ceiling==null)return "";
  input.floor.shieldingEnabled=input.ceiling.shieldingEnabled=true;
  foreach(var item in input.items)if(item.shielding!=null)item.shielding.shieldingEnabled=true;
  return Hash(JsonUtility.ToJson(input));
 }
 public static bool SameInputs(CtResultSnapshot first,CtResultSnapshot second){
  if(first==null||second==null||first.datasetHash!=second.datasetHash||first.quantity!=second.quantity||first.unit!=second.unit||first.interval!=second.interval)return false;
  try{string fingerprint=ComparisonFingerprint(first);return fingerprint.Length>0&&fingerprint==ComparisonFingerprint(second);}catch{return false;}
 }
 static bool ClipBox(CtVector source,CtVector roi,double centerX,double centerY,double centerZ,double angle,double halfX,double halfY,double halfZ,out double entry,out double exit,out bool ambiguous){
  double radians=angle*Math.PI/180,cosine=Math.Cos(radians),sine=Math.Sin(radians);
  double sourceX=source.x-centerX,sourceZ=source.z-centerZ,deltaX=roi.x-source.x,deltaZ=roi.z-source.z;
  double[] origins={cosine*sourceX-sine*sourceZ,source.y-centerY,sine*sourceX+cosine*sourceZ};
  double[] directions={cosine*deltaX-sine*deltaZ,roi.y-source.y,sine*deltaX+cosine*deltaZ};
  double[] halves={halfX,halfY,halfZ};entry=0;exit=1;ambiguous=false;
  for(int axis=0;axis<3;axis++){
   if(halves[axis]<=0)return false;
   if(Math.Abs(directions[axis])<GeometryToleranceMeters){if(Math.Abs(origins[axis])>halves[axis]+GeometryToleranceMeters)return false;if(Math.Abs(Math.Abs(origins[axis])-halves[axis])<=GeometryToleranceMeters)ambiguous=true;continue;}
   double first=(-halves[axis]-origins[axis])/directions[axis],second=(halves[axis]-origins[axis])/directions[axis];
   entry=Math.Max(entry,Math.Min(first,second));exit=Math.Min(exit,Math.Max(first,second));if(entry>exit)return false;
  }
  if(entry==exit)ambiguous=true;return exit>=entry;
 }
 static void AddBox(List<CtPathSegment> segments,CtVector source,CtVector roi,string id,Barrier barrier,double centerX,double centerY,double centerZ,double angle,double halfX,double halfY,double halfZ,bool unsupported){
  if(!ClipBox(source,roi,centerX,centerY,centerZ,angle,halfX,halfY,halfZ,out double entry,out double exit,out bool ambiguous))return;
  double distance=Length(source,roi);
  if(ambiguous||(exit-entry)*distance<=GeometryToleranceMeters)Fail("InvalidGeometry","Tangent/coplanar shielding path is unresolved at "+id+".");
  if(unsupported)Fail("InvalidGeometry","Unsupported opening, joined or custom shielding geometry: "+id+".");
  segments.Add(new CtPathSegment{barrierId=id,material=barrier.material,entryMeters=entry*distance,exitMeters=exit*distance,pathThicknessMm=(exit-entry)*distance*1000,
   entry=new CtVector(source.x+(roi.x-source.x)*entry,source.y+(roi.y-source.y)*entry,source.z+(roi.z-source.z)*entry),exit=new CtVector(source.x+(roi.x-source.x)*exit,source.y+(roi.y-source.y)*exit,source.z+(roi.z-source.z)*exit)});
 }
 static CtVector Metres(CtVector point,double units)=>new CtVector(point.x*units,point.y*units,point.z*units);
 public static List<CtPathSegment> Path(Design design,Item scatter,Item roi,string shieldingMode){
  return Path(design,CtShieldingData.Position(design,scatter),CtShieldingData.Position(design,roi),design.ct.metersPerUnityUnit,shieldingMode);
 }
 public static List<CtPathSegment> Path(Design design,CtVector sourcePosition,CtVector roiPosition,double units,string shieldingMode="Current"){
  var segments=new List<CtPathSegment>();if(shieldingMode=="AllOff")return segments;
  var source=Metres(sourcePosition,units);var target=Metres(roiPosition,units);
  if(shieldingMode!="WallsOff")foreach(var wall in design.items.Where(item=>item.kind=="Wall")){
   var barrier=wall.shielding;if(!barrier.shieldingEnabled||barrier.thickness==0)continue;
   bool joined=design.wallJunctions!=null&&design.wallJunctions.Any(junction=>junction.arms.Any(arm=>arm.wallId==wall.id));
  AddBox(segments,source,target,wall.id,barrier,wall.x*units,((double)wall.y+wall.height/2.0)*units,wall.z*units,wall.angle,wall.length*units/2,wall.height*units/2,barrier.thickness/2000.0,joined||wall.generated?.unsupportedGeometry==true||wall.doors?.Count>0);
  }
  foreach(var component in design.items.Where(item=>item.kind=="Component"&&item.shielding.shieldingEnabled))Fail("InvalidGeometry","Custom shielding footprint is not supported by CT path calculation: "+component.id+".");
  if(design.floor.shieldingEnabled&&design.floor.thickness>0)AddBox(segments,source,target,"floor",design.floor,0,-design.floor.thickness/2000.0,0,0,design.width*units/2,design.floor.thickness/2000.0,design.depth*units/2,false);
  if(design.ceiling.shieldingEnabled&&design.ceiling.thickness>0)AddBox(segments,source,target,"ceiling",design.ceiling,0,design.height*units+design.ceiling.thickness/2000.0,0,0,design.width*units/2,design.ceiling.thickness/2000.0,design.depth*units/2,false);
  segments.Sort((first,second)=>first.entryMeters.CompareTo(second.entryMeters));
  return segments;
 }
 static Barrier BarrierFor(Design design,string id)=>id=="floor"?design.floor:id=="ceiling"?design.ceiling:design.items.Find(item=>item.id==id)?.shielding;
 public static double TotalHomogeneousPath(List<CtPathSegment> segments){
  if(segments.Count==0)return 0;
  string material=segments[0].material;double entry=segments[0].entryMeters,exit=segments[0].exitMeters;
  foreach(var segment in segments.Skip(1)){
   if(segment.material!=material)Fail("UnsupportedCompositeBarrier","Different materials along one CT contribution need a separately validated composite model.");
   if(segment.entryMeters>exit+GeometryToleranceMeters)Fail("UnsupportedCompositeBarrier","Separated shielding slabs are not validated as one homogeneous CT barrier.");
   exit=Math.Max(exit,segment.exitMeters);
  }
  return Math.Max(0,exit-entry)*1000;
 }
 static CtContributionResult Contribution(Design design,Item roi,CtSourceData source,CtRoiContribution ledger,double factor,string shieldingMode){
  var result=new CtContributionResult{sourceId=source.id,protocolId=source.protocolId};
  try{
   if(source.kvp!=120&&source.kvp!=140||source.beamFamily!="CT_SECONDARY")Fail("UnsupportedSpectrum","A matching supported 120/140-kVp CT_SECONDARY spectrum is required.");
   if(string.IsNullOrWhiteSpace(source.scannerId)||string.IsNullOrWhiteSpace(source.protocolId)||string.IsNullOrWhiteSpace(source.citation)||string.IsNullOrWhiteSpace(source.validityDomain)||!source.applicabilityReviewed)Fail("MissingRequiredInput","Validated scanner/protocol source data, citation, domain and applicability review are required.");
   if(!source.componentsConfirmed||string.IsNullOrWhiteSpace(source.includedComponents))Fail("IncompleteContributions","Confirm nonoverlapping included scatter/leakage/workload components.");
   if(source.includesRoomBarriers)Fail("InvalidUnitsOrNormalization","Source values must be unshielded with respect to the barriers being modelled.");
   var scatter=design.items.Find(item=>item.id==source.scatterPointId);if(scatter==null||!scatter.ctPoint.anchorConfirmed)Fail("MissingRequiredInput","Confirm the provider's effective scatter anchor.");
   result.distanceMeters=DistanceMeters(design,scatter,roi);
  double logUnshielded;bool sourceZero;
   if(source.mode=="DirectWeekly"){
    if(ledger==null||!ledger.hasDirectKerma)Fail("MissingRequiredInput","Supply unshielded mGy/week evaluated at this ROI for "+source.protocolId+".");
    var currentPosition=CtShieldingData.Position(design,roi);
    if(!ledger.hasKermaPosition||Math.Abs(ledger.kermaX-currentPosition.x)>GeometryToleranceMeters||Math.Abs(ledger.kermaY-currentPosition.y)>GeometryToleranceMeters||Math.Abs(ledger.kermaZ-currentPosition.z)>GeometryToleranceMeters||ledger.directSourceFingerprint!=DirectSourceFingerprint(design,source.id))Fail("SourceModelOutsideValidityDomain","Direct ROI kerma belongs to different/unconfirmed ROI or scanner conditions; supply the current unshielded field.");
    sourceZero=ledger.directMgyPerWeek==0||factor==0;logUnshielded=sourceZero?0:Math.Log(ledger.directMgyPerWeek)+Math.Log(factor);
   }else{
    if(!source.isotropicReferenceAccepted)Fail("MissingRequiredInput","The scalar reference-point model requires an explicitly reviewed isotropic assumption.");
    if(!source.hasReferenceKerma||!source.hasReferenceDistance||!source.hasWorkload||!source.hasMinimumDistance||!source.hasMaximumDistance)Fail("MissingRequiredInput","Reference mGy/exam, distance, exams/week and valid distance limits are required.");
    if(result.distanceMeters<=0||result.distanceMeters<source.minimumDistanceMeters||result.distanceMeters>source.maximumDistanceMeters)Fail("SourceModelOutsideValidityDomain","The source-to-ROI distance is outside the supplied inverse-square applicability domain.");
    sourceZero=source.referenceMgyPerExam==0||source.examsPerWeek==0||factor==0;
    logUnshielded=sourceZero?0:Math.Log(source.referenceMgyPerExam)+Math.Log(source.examsPerWeek)+Math.Log(factor)+2*(Math.Log(source.referenceDistanceMeters)-Math.Log(result.distanceMeters));
   }
     double unshielded=sourceZero?0:Math.Exp(logUnshielded);if(!CtShieldMath.IsFinite(logUnshielded)||!CtShieldMath.IsFinite(unshielded))Fail("InvalidNumericInput","Nonfinite source normalization.");
     result.unshieldedMgyPerWeek=unshielded;result.logUnshielded=logUnshielded;result.segments=Path(design,scatter,roi,shieldingMode);
   double thickness=TotalHomogeneousPath(result.segments);result.logB=0;
   if(thickness>0){
    if(!source.pathApproximationAccepted)Fail("MissingRequiredInput","Accept/review the effective-source finite-path attenuation approximation.");
    if(!CtCoefficientLibrary.TryGet(source.beamFamily,result.segments[0].material,source.kvp,out var coefficient,out var unsupported))Fail(unsupported,"No CT fit for "+result.segments[0].material+" / "+source.kvp+" kVp.");
    foreach(var segment in result.segments){
     if(BarrierFor(design,segment.barrierId)?.ctApplicabilityReviewed!=true)Fail("MissingRequiredInput","Review installed material applicability for barrier "+segment.barrierId+"; no automatic density correction is provided.");
     segment.fitId=coefficient.Id;segment.alpha=coefficient.Fit.AlphaPerMm;segment.beta=coefficient.Fit.BetaPerMm;segment.gamma=coefficient.Fit.Gamma;
    }
    result.logB=CtShieldMath.LogTransmission(coefficient.Fit,thickness);result.flags.Add("Declared effective-source finite-path broad-beam approximation");
    if(thickness>CtCoefficientLibrary.PlottedMaximumMm(coefficient))result.flags.Add("OutsidePlottedThicknessRange");
   }
  result.transmission=Math.Exp(result.logB);result.isZero=sourceZero;result.logShielded=result.isZero?0:logUnshielded+result.logB;
   result.shieldedMgyPerWeek=result.isZero?0:Math.Exp(result.logShielded);result.underflow=!result.isZero&&(result.transmission==0||result.shieldedMgyPerWeek==0);
   if(result.underflow)result.flags.Add("NumericalUnderflow");
   result.hasResult=true;result.status=result.flags.Count>0?"ValidCalculationWithDeclaredApproximation":"ValidCalculation";
  }catch(CtCalculationException error){result.status=error.Status;result.message=error.Message;}
  catch(Exception error){result.status="InvalidNumericInput";result.message=error.Message;}
  return result;
 }
 static CtRoiResult EvaluateRoi(Design design,Item roi,string scenarioCase,string shieldingMode){
  var record=new CtRoiResult{roiId=roi.id,roiName=roi.name,position=CtShieldingData.Position(design,roi)};
  var data=roi.ctPoint.roi;double factor=1;
  if(scenarioCase=="Best"){if(!data.hasBestFactor){record.errors.Add("Missing best-case workload factor.");return record;}factor=data.bestFactor;}
  if(scenarioCase=="Worst"){if(!data.hasWorstFactor){record.errors.Add("Missing worst-case workload factor.");return record;}factor=data.worstFactor;}
  if(design.ct.sources.Count==0){record.errors.Add("No CT source model configured. Open CT source / energy settings, configure a placed CT scanner and supply validated source inputs.");return record;}
  double logPhysical=double.NegativeInfinity,unshielded=0;
  foreach(var source in design.ct.sources){
   var ledger=data.contributions.Find(contribution=>contribution.sourceId==source.id);
   if(ledger==null){record.errors.Add("IncompleteContributions: include source "+source.id+" explicitly.");continue;}
   var contribution=Contribution(design,roi,source,ledger,factor,shieldingMode);record.contributions.Add(contribution);
   if(!contribution.hasResult){record.errors.Add(contribution.status+": "+contribution.message);continue;}
   unshielded+=contribution.unshieldedMgyPerWeek;if(!contribution.isZero)logPhysical=CtShieldMath.LogAdd(logPhysical,contribution.logShielded);
   foreach(var flag in contribution.flags)if(!record.flags.Contains(flag))record.flags.Add(flag);
  }
  if(record.errors.Count>0){record.status=record.contributions.FirstOrDefault(contribution=>!contribution.hasResult)?.status??"IncompleteContributions";return record;}
  record.complete=record.hasPhysical=true;record.unshieldedMgyPerWeek=unshielded;record.isZero=double.IsNegativeInfinity(logPhysical);record.logPhysical=record.isZero?0:logPhysical;
  record.physicalMgyPerWeek=record.isZero?0:Math.Exp(logPhysical);record.underflow=!record.isZero&&record.physicalMgyPerWeek==0;
  if(data.hasOccupancy){record.occupancy=data.occupancy;record.hasOccupied=true;record.logOccupied=record.isZero||data.occupancy==0?0:logPhysical+Math.Log(data.occupancy);record.occupiedMgyPerWeek=record.isZero||data.occupancy==0?0:Math.Exp(record.logOccupied);}
  record.hasGoal=data.hasGoal;record.goalMgyPerWeek=data.goalMgyPerWeek;
  if(!data.hasGoal||string.IsNullOrWhiteSpace(data.goalSource)){record.status="NotEvaluatedMissingDesignGoal";return record;}
  if(data.goalConvention=="Occupied"&&(!data.hasOccupancy||string.IsNullOrWhiteSpace(data.occupancyConvention))){record.status="MissingRequiredInput";record.errors.Add("Occupancy and its exposure convention are required for this goal.");return record;}
  if(data.goalConvention=="Occupied"&&data.occupancy==0){record.status="NotEvaluatedZeroOccupancy";return record;}
  bool zero=record.isZero;double logCompared=data.goalConvention=="FieldLimit"?logPhysical:record.logOccupied;
  double compared=data.goalConvention=="FieldLimit"?record.physicalMgyPerWeek:record.occupiedMgyPerWeek;
  record.hasVerdict=true;record.passed=zero||data.goalMgyPerWeek>0&&logCompared<=Math.Log(data.goalMgyPerWeek);
  record.verdict=record.passed?"Pass":"Fail";record.margin=data.goalMgyPerWeek-compared;
  if(data.goalMgyPerWeek>0){double logUtilization=zero?double.NegativeInfinity:logCompared-Math.Log(data.goalMgyPerWeek);record.utilization=logUtilization>Math.Log(double.MaxValue)?double.MaxValue:Math.Exp(logUtilization);}
  record.status=record.flags.Count>0?"ValidCalculationWithDeclaredApproximation":"ValidCalculation";
  return record;
 }
 public static CtResultSnapshot Calculate(Design design,string scenarioCase=null,string shieldingMode=null){
  Design.Validate(design);if(!CtShieldingData.Active(design.ct))throw new Exception("Configure CT sources and ROI points first.");
  scenarioCase=scenarioCase??design.ct.selectedCase;shieldingMode=shieldingMode??design.ct.shieldingMode;
  if(!new[]{"Best","Nominal","Worst"}.Contains(scenarioCase)||!new[]{"Current","WallsOff","AllOff"}.Contains(shieldingMode))throw new Exception("Unknown CT calculation scenario.");
  string inputs=InputJson(design);
  var result=new CtResultSnapshot{name=design.ct.scenarioName,projectName=design.name,createdUtc=DateTime.UtcNow.ToString("O"),scenarioCase=scenarioCase,shieldingMode=shieldingMode,datasetHash=CtCoefficientLibrary.DatasetHash,specificationHash=CtCoefficientLibrary.SpecificationHash,reportedPdfHash=CtCoefficientLibrary.ReportedPdfHash,inputsJson=inputs,inputFingerprint=Hash(inputs)};
  foreach(var roi in design.items.Where(CtShieldingData.IsRoi))result.rows.Add(EvaluateRoi(design,roi,scenarioCase,shieldingMode));
  if(result.rows.Count==0)throw new Exception("Place at least one ROI/patient evaluation point.");
  result.recordHash=RecordHash(result);
  CtShieldingData.ValidateResult(result);return result;
 }
 public static void SaveResult(Design design,CtResultSnapshot result){
  CtShieldingData.ValidateResult(result);if(design.ct.results.Count>=CtShieldingData.MaxResults)throw new Exception("CT result history is full (50). Export or explicitly remove a result before adding another.");
  if(design.ct.results.Any(existing=>existing.id==result.id))throw new Exception("A result with this ID is already saved.");
  design.ct.results.Add(JsonUtility.FromJson<CtResultSnapshot>(JsonUtility.ToJson(result)));
 }
 public static string ExportResults(IEnumerable<CtResultSnapshot> results){
  var file=new CtResultFile{results=results.ToList()};if(file.results.Count>CtShieldingData.MaxResults)throw new Exception("Too many CT results.");foreach(var result in file.results)CtShieldingData.ValidateResult(result);
  string json=JsonUtility.ToJson(file,true);if(Encoding.UTF8.GetByteCount(json)>CtShieldingData.MaxResultFileBytes)throw new Exception("CT result file exceeds 16 MiB.");return json;
 }
 public static void ImportResults(Design design,string json){
  if(json==null||Encoding.UTF8.GetByteCount(json)>CtShieldingData.MaxResultFileBytes)throw new Exception("CT result file exceeds 16 MiB.");
  var header=JsonUtility.FromJson<CtResultHeader>(json);if(header==null||header.format!="RoomStudio.CT.Results"||header.version!=1)throw new Exception("Choose a RoomStudio.CT.Results version-1 JSON file.");
  var file=JsonUtility.FromJson<CtResultFile>(json);if(file.results==null||file.results.Count==0)throw new Exception("The CT result file contains no result records.");
  var existing=design.ct?.results??new List<CtResultSnapshot>();if(existing.Count+file.results.Count>CtShieldingData.MaxResults)throw new Exception("CT result history exceeds 50 records.");
  var ids=new HashSet<string>(existing.Select(result=>result.id));
  foreach(var result in file.results){CtShieldingData.ValidateResult(result);if(!ids.Add(result.id))throw new Exception("Duplicate CT result ID; existing history was not changed.");if(Hash(result.inputsJson)!=result.inputFingerprint)throw new Exception("CT result input fingerprint mismatch.");}
  CtShieldingData.Ensure(design).results.AddRange(file.results);
 }
 [Serializable] sealed class CtResultHeader {public string format;public int version;}
}
}