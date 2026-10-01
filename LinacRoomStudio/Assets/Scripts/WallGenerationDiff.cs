using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RoomStudio {
[Serializable] public class WallGenerationChange {
 public string kind="",pathId="",itemId="",reason="";
 public GeneratedWallPath candidate;
}
[Serializable] public class WallGenerationDiff {
 public string batchId="",designSignature="",sourceSignature="";
 public FloorPlanData sourceSnapshot;
 public List<WallGenerationChange> changes=new List<WallGenerationChange>();
 public int Count(string kind)=>changes.Count(change=>change.kind==kind);
 public bool HasConflicts=>changes.Any(change=>change.kind=="Conflict");
}

public static class WallGenerationDiffs {
 static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
 static T Clone<T>(T value)=>JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
 static string DesignSignature(Design design)=>Hash(JsonUtility.ToJson(design));
 static string NewItemId(string batchId,string pathId)=>"generated-"+Hash(batchId+"|"+pathId).Substring(0,32);
 static void RequireSource(FloorPlanData source){
  if(source==null||source.kind!="Image")throw new Exception("Choose an image source for wall generation.");
  FloorPlanCodec.Validate(source);
  if(!PlanAuthoring.CalibrationCurrent(source))throw new Exception("Confirm a uniform image calibration before generating walls.");
  if((long)source.pixelWidth*source.pixelHeight>PlanAuthoring.MaxProcessingPixels)throw new Exception("Wall generation supports at most 4 megapixels.");
 }
 static List<GeneratedWallPath> Candidates(IEnumerable<GeneratedWallPath> input){
  if(input==null)throw new Exception("Missing generated wall paths.");
  var result=input.Select(path=>path==null?null:Clone(path)).ToList();
  if(result.Count==0||result.Count>WallGenerationData.MaxStoredPaths)throw new Exception("Wall generation needs 1 to 2000 paths.");
  var ids=new HashSet<string>();foreach(var path in result){
   if(path==null||string.IsNullOrWhiteSpace(path.id)||path.id.Length>128||!ids.Add(path.id)||string.IsNullOrWhiteSpace(path.ruleId))throw new Exception("Invalid or duplicate extracted path ID.");
   if(!string.IsNullOrEmpty(path.itemId))throw new Exception("Candidate paths cannot contain committed item IDs.");
  }
  return result;
 }
 public static List<GeneratedWallPath> CreatePaths(Design design,FloorPlanData source,PlanPathResult result,out List<PlanPathIssue> rejections){
  RequireSource(source);if(result==null)throw new Exception("Extract image paths before proposing walls.");
  var paths=new List<GeneratedWallPath>();rejections=new List<PlanPathIssue>();
  foreach(var extracted in result.paths){
   var rule=source.authoring.rules.FirstOrDefault(value=>value.id==extracted.ruleId);
   string reason="";
   if(rule==null||!rule.enabled||rule.classification!="Wall")reason="Inactive wall rule.";
   else{
    PlanAuthoring.RequireWallRule(rule,design);
    var start=PlanAuthoring.PixelToWorld(source,new Vector2(extracted.aPixel.x,extracted.aPixel.y),rule.baseElevation);
    var end=PlanAuthoring.PixelToWorld(source,new Vector2(extracted.bPixel.x,extracted.bPixel.y),rule.baseElevation);
    float length=Vector3.Distance(start,end),clearance=rule.thicknessMm/1000f/source.authoring.calibration.metresPerPixel/2;
    if(length<.25f||length>60f)reason="World length outside 0.25-60 m.";
    else if(clearance>64)reason="Opening-check clearance exceeds 64 px.";
    else if(result.CrossesOpening(extracted.aPixel,extracted.bPixel,clearance))reason="Confirmed opening overlap.";
    else paths.Add(new GeneratedWallPath{
     id=extracted.id,ruleId=rule.id,categoryName=rule.name,displayColor=rule.display,material=rule.material,
     sourceAPixel=new Vector2Data(extracted.aPixel.x,extracted.aPixel.y),sourceBPixel=new Vector2Data(extracted.bPixel.x,extracted.bPixel.y),
     aPixel=new Vector2Data(extracted.aPixel.x,extracted.aPixel.y),bPixel=new Vector2Data(extracted.bPixel.x,extracted.bPixel.y),
    aWorld=start,bWorld=end,sourceComponent=Mathf.Max(0,result.components.FindIndex(component=>component.id==extracted.sourceComponent)),
     areaPixels=extracted.sourceAreaPixels,lengthPixels=extracted.lengthPixels,height=rule.height,baseElevation=rule.baseElevation,
     thicknessMm=rule.thicknessMm,densityKgM3=rule.densityKgM3
    });
   }
   if(reason!="")rejections.Add(new PlanPathIssue{code="RejectedWall",ruleId=extracted.ruleId,sourceComponent=extracted.sourceComponent,message=reason,count=1});
  }
  return paths;
 }
 static Item MakeWall(GeneratedWallPath path,string id){
  float dx=path.bWorld.x-path.aWorld.x,dz=path.bWorld.z-path.aWorld.z;
  float length=Mathf.Sqrt(dx*dx+dz*dz);
  if(length<.25f||length>10000||Mathf.Abs(path.aWorld.y-path.baseElevation)>.0001f||Mathf.Abs(path.bWorld.y-path.baseElevation)>.0001f)throw new Exception("A generated wall has invalid endpoints or elevation.");
  var wall=Design.Wall(path.categoryName,(path.aWorld.x+path.bWorld.x)*.5f,(path.aWorld.z+path.bWorld.z)*.5f,length,-Mathf.Atan2(dz,dx)*Mathf.Rad2Deg,path.height);
  wall.id=id;wall.y=path.baseElevation;wall.shielding=new Barrier{material=path.material,thickness=path.thicknessMm,density=path.densityKgM3};
  wall.generated=new GeneratedWallRef{batchId="",pathId=path.id,ruleId=path.ruleId,categoryName=path.categoryName,displayColor=path.displayColor,
   sourceAPixel=Clone(path.sourceAPixel),sourceBPixel=Clone(path.sourceBPixel),manuallyEdited=path.manuallyEdited,unsupportedGeometry=path.unsupportedGeometry};
  return wall;
 }
 static void UpdateWall(Item wall,GeneratedWallPath path,string batchId){
  var replacement=MakeWall(path,wall.id);
  replacement.shielding.shieldingEnabled=wall.shielding.shieldingEnabled;
  replacement.shielding.ctApplicabilityReviewed=wall.shielding.ctApplicabilityReviewed&&wall.shielding.material==replacement.shielding.material&&wall.shielding.density==replacement.shielding.density;
  wall.name=replacement.name;wall.x=replacement.x;wall.z=replacement.z;wall.y=replacement.y;wall.angle=replacement.angle;wall.length=replacement.length;wall.height=replacement.height;
  wall.shielding=replacement.shielding;wall.generated=replacement.generated;wall.generated.batchId=batchId;
 }
 static bool SamePixel(Vector2Data a,Vector2Data b)=>a!=null&&b!=null&&Mathf.Abs(a.x-b.x)<.0001f&&Mathf.Abs(a.y-b.y)<.0001f;
 static bool SameSource(GeneratedWallPath a,GeneratedWallPath b)=>a.ruleId==b.ruleId&&SamePixel(a.sourceAPixel,b.sourceAPixel)&&SamePixel(a.sourceBPixel,b.sourceBPixel);
 static bool SamePath(GeneratedWallPath a,GeneratedWallPath b)=>SameSource(a,b)&&a.categoryName==b.categoryName&&a.displayColor==b.displayColor&&a.material==b.material&&
  Vector3.Distance(a.aWorld,b.aWorld)<.0001f&&Vector3.Distance(a.bWorld,b.bWorld)<.0001f&&Mathf.Abs(a.height-b.height)<.0001f&&Mathf.Abs(a.baseElevation-b.baseElevation)<.0001f&&
  Mathf.Abs(a.thicknessMm-b.thicknessMm)<.0001f&&Mathf.Abs(a.densityKgM3-b.densityKgM3)<.0001f;
 static bool SameManualSource(GeneratedWallPath previous,GeneratedWallPath candidate,WallGenerationBatch batch,FloorPlanData source){
  if(!SameSource(previous,candidate)||batch.pixelWidth!=source.pixelWidth||batch.pixelHeight!=source.pixelHeight||
   previous.categoryName!=candidate.categoryName||previous.displayColor!=candidate.displayColor||previous.material!=candidate.material||
   Mathf.Abs(previous.height-candidate.height)>=.0001f||Mathf.Abs(previous.baseElevation-candidate.baseElevation)>=.0001f||
   Mathf.Abs(previous.thicknessMm-candidate.thicknessMm)>=.0001f||Mathf.Abs(previous.densityKgM3-candidate.densityKgM3)>=.0001f)return false;
  if(Design.IsEmptyFloorPlan(batch.sourceSnapshot))return SamePath(previous,candidate);
  foreach(var point in new[]{previous.sourceAPixel,previous.sourceBPixel}){
   var pixel=new Vector2(point.x,point.y);
   var before=PlanAuthoring.PixelToWorld(batch.sourceSnapshot,pixel,previous.baseElevation);
   var after=PlanAuthoring.PixelToWorld(source,pixel,candidate.baseElevation);
   if(Vector3.Distance(before,after)>=.0001f)return false;
  }
  return true;
 }
 static bool NearSource(GeneratedWallPath a,GeneratedWallPath b){
  if(a.ruleId!=b.ruleId)return false;
  float same=Vector2.Distance(new Vector2(a.sourceAPixel.x,a.sourceAPixel.y),new Vector2(b.sourceAPixel.x,b.sourceAPixel.y))+
   Vector2.Distance(new Vector2(a.sourceBPixel.x,a.sourceBPixel.y),new Vector2(b.sourceBPixel.x,b.sourceBPixel.y));
  float reversed=Vector2.Distance(new Vector2(a.sourceAPixel.x,a.sourceAPixel.y),new Vector2(b.sourceBPixel.x,b.sourceBPixel.y))+
   Vector2.Distance(new Vector2(a.sourceBPixel.x,a.sourceBPixel.y),new Vector2(b.sourceAPixel.x,b.sourceAPixel.y));
  return Math.Min(same,reversed)<=8;
 }
 static bool SameSplitDetection(GeneratedWallPath path,GeneratedWallPath candidate,WallGenerationBatch batch,FloorPlanData source){
  var detection=Clone(path);detection.sourceAPixel=Clone(path.detectionAPixel);detection.sourceBPixel=Clone(path.detectionBPixel);
  return SameManualSource(detection,candidate,batch,source);
 }
 static bool Joined(Design design,string itemId)=>design.wallJunctions!=null&&design.wallJunctions.Any(junction=>junction?.arms!=null&&junction.arms.Any(arm=>arm!=null&&arm.wallId==itemId));
 static bool Protected(Design design,Item wall)=>wall==null||wall.locked||!string.IsNullOrEmpty(wall.groupId)||(wall.doors!=null&&wall.doors.Count>0)||Joined(design,wall.id);
 static void Change(WallGenerationDiff diff,string kind,GeneratedWallPath path,GeneratedWallPath candidate,string reason="")=>diff.changes.Add(new WallGenerationChange{kind=kind,pathId=path?.id??candidate?.id??"",itemId=path?.itemId??"",candidate=candidate==null?null:Clone(candidate),reason=reason});

 // Pure staging operation. A generation batch and its items are committed to
 // the returned clone together; the caller records it as one undo operation.
 public static Design ApplyNewBatch(Design design,FloorPlanData source,IEnumerable<GeneratedWallPath> candidates){
  Design.Validate(design);RequireSource(source);var proposed=Candidates(candidates);
  string signature=WallGenerationData.SnapshotSignature(source);
  if(design.generationBatches!=null&&design.generationBatches.Any(batch=>batch.snapshotSignature==signature))throw new Exception("This source/settings already have a generation batch. Preview regeneration instead of adding duplicates.");
  var result=Clone(design);if(result.generationBatches==null)result.generationBatches=new List<WallGenerationBatch>();
  string batchId=Guid.NewGuid().ToString("N");var batch=new WallGenerationBatch{id=batchId,sourceFingerprint=PlanAuthoring.Fingerprint(source),snapshotSignature=signature,
   pixelWidth=source.pixelWidth,pixelHeight=source.pixelHeight,sourceSnapshot=FloorPlanCodec.Clone(source)};
  var known=new HashSet<string>(result.items.Select(item=>item.id));
  foreach(var path in proposed){
   if(!path.deletedOverride){string itemId=NewItemId(batchId,path.id);if(!known.Add(itemId))throw new Exception("Generated item identity collision.");
    path.itemId=itemId;var wall=MakeWall(path,itemId);wall.generated.batchId=batchId;result.items.Add(wall);
   }
   batch.paths.Add(path);
  }
  result.generationBatches.Add(batch);Design.Validate(result);return result;
 }

 public static WallGenerationDiff PreviewRegeneration(Design design,string batchId,FloorPlanData newSource,IEnumerable<GeneratedWallPath> candidates){
  Design.Validate(design);RequireSource(newSource);var proposed=Candidates(candidates);
  var batch=WallGenerationData.FindBatch(design,batchId);if(batch==null)throw new Exception("Choose a saved generation batch.");
  var diff=new WallGenerationDiff{batchId=batchId,designSignature=DesignSignature(design),sourceSignature=WallGenerationData.SnapshotSignature(newSource),sourceSnapshot=FloorPlanCodec.Clone(newSource)};
  var existing=batch.paths.ToDictionary(path=>path.id);var proposedIds=new HashSet<string>(proposed.Select(path=>path.id));
    var retainedSplitIds=new HashSet<string>();
  bool changedDimensions=batch.pixelWidth!=newSource.pixelWidth||batch.pixelHeight!=newSource.pixelHeight;
  if(changedDimensions)foreach(var old in batch.paths.Where(path=>path.deletedOverride||path.manuallyEdited))Change(diff,"Conflict",old,null,"Source dimensions changed while a manual or deleted-path override exists.");
  foreach(var candidate in proposed){
    var splitPaths=batch.paths.Where(path=>!string.IsNullOrEmpty(path.detectionPathId)&&path.detectionPathId==candidate.id).ToArray();
    if(splitPaths.Length>0){
     foreach(var splitPath in splitPaths){
      retainedSplitIds.Add(splitPath.id);
      bool unchanged=!candidate.deletedOverride&&SameSplitDetection(splitPath,candidate,batch,newSource);
      Change(diff,unchanged?"Unchanged":"Conflict",splitPath,null,unchanged?"Split wall identities and manual/deleted overrides retained.":"A changed detection affects split walls; review their shared junction before regenerating.");
     }
     continue;
    }
   if(candidate.deletedOverride){
    if(existing.TryGetValue(candidate.id,out var removed)&&removed.deletedOverride)Change(diff,"Unchanged",removed,null,"Deleted-path override retained.");
    else if(existing.TryGetValue(candidate.id,out var active)){
     var wall=design.items.FirstOrDefault(item=>item.id==active.itemId);
     if(Protected(design,wall))Change(diff,"Conflict",active,candidate,"A protected, grouped, opened or connected wall cannot be deleted.");
     else Change(diff,"DeleteOverride",active,candidate,"Explicit path deletion will be retained during regeneration.");
    }else Change(diff,"Add",null,candidate,"New deleted-path override retained.");
    continue;
   }
   if(existing.TryGetValue(candidate.id,out var old)){
    if(old.deletedOverride){Change(diff,"Unchanged",old,null,"Deleted-path override retained.");continue;}
    var wall=design.items.FirstOrDefault(item=>item.id==old.itemId);
    if(wall==null){Change(diff,"Conflict",old,candidate,"The committed wall is missing.");continue;}
    if(old.manuallyEdited||wall.generated?.manuallyEdited==true||WallGenerationData.WasEdited(wall,old)){
    Change(diff,SameManualSource(old,candidate,batch,newSource)?"Unchanged":"Conflict",old,null,"Manual wall edit retained; changed detection, properties or calibration need review.");continue;
    }
    if(SamePath(old,candidate)||Joined(design,wall.id)&&SameManualSource(old,candidate,batch,newSource)){Change(diff,"Unchanged",old,null);continue;}
    if(Protected(design,wall)){Change(diff,"Conflict",old,candidate,"A protected, grouped, opened or connected wall cannot be changed automatically.");continue;}
    Change(diff,"Update",old,candidate);continue;
   }
   var nearProtected=batch.paths.FirstOrDefault(old=>(old.deletedOverride||old.manuallyEdited||(!string.IsNullOrEmpty(old.itemId)&&design.items.Any(item=>item.id==old.itemId&&WallGenerationData.WasEdited(item,old))))&&NearSource(old,candidate));
   if(nearProtected!=null){Change(diff,"Conflict",nearProtected,candidate,"New detection overlaps a manual edit or deleted-path override.");continue;}
   Change(diff,"Add",null,candidate);
  }
  foreach(var old in batch.paths){
    if(proposedIds.Contains(old.id)||retainedSplitIds.Contains(old.id))continue;
   if(old.deletedOverride){Change(diff,"Unchanged",old,null,"Deleted-path override retained.");continue;}
   var wall=design.items.FirstOrDefault(item=>item.id==old.itemId);
   if(wall==null||old.manuallyEdited||wall.generated?.manuallyEdited==true||WallGenerationData.WasEdited(wall,old)||Protected(design,wall))Change(diff,"Conflict",old,null,"A missing detection affects a manually edited or protected wall.");
   else Change(diff,"Delete",old,null);
  }
  return diff;
 }

 public static Design ApplyRegeneration(Design design,WallGenerationDiff diff){
  if(diff==null||diff.HasConflicts||diff.sourceSnapshot==null||diff.designSignature!=DesignSignature(design)||diff.sourceSignature!=WallGenerationData.SnapshotSignature(diff.sourceSnapshot))throw new Exception("Regeneration preview is stale or has unresolved conflicts.");
  var result=Clone(design);WallGenerationData.MarkManualEdits(result);var batch=WallGenerationData.FindBatch(result,diff.batchId);if(batch==null)throw new Exception("Generation batch no longer exists.");
  var ids=new HashSet<string>(result.items.Select(item=>item.id));
  foreach(var change in diff.changes){
   if(change.kind=="Unchanged")continue;
   if(change.kind=="Delete"){
    var path=batch.paths.First(p=>p.id==change.pathId);result.items.RemoveAll(item=>item.id==path.itemId);batch.paths.Remove(path);continue;
   }
   if(change.kind=="DeleteOverride"){
    var path=batch.paths.First(p=>p.id==change.pathId);result.items.RemoveAll(item=>item.id==path.itemId);
    path.itemId="";path.deletedOverride=true;continue;
   }
   if(change.kind=="Update"){
    var path=batch.paths.First(p=>p.id==change.pathId);var wall=result.items.First(item=>item.id==path.itemId);
    var replacement=Clone(change.candidate);replacement.itemId=path.itemId;UpdateWall(wall,replacement,batch.id);
    batch.paths[batch.paths.IndexOf(path)]=replacement;continue;
   }
   if(change.kind=="Add"){
    var path=Clone(change.candidate);
    if(!path.deletedOverride){path.itemId=NewItemId(batch.id,path.id);if(!ids.Add(path.itemId))throw new Exception("Generated item identity collision.");
     var wall=MakeWall(path,path.itemId);wall.generated.batchId=batch.id;result.items.Add(wall);}
    batch.paths.Add(path);continue;
   }
   throw new Exception("Unknown regeneration preview action.");
  }
  batch.sourceSnapshot=FloorPlanCodec.Clone(diff.sourceSnapshot);batch.sourceFingerprint=PlanAuthoring.Fingerprint(diff.sourceSnapshot);
  batch.snapshotSignature=diff.sourceSignature;batch.pixelWidth=diff.sourceSnapshot.pixelWidth;batch.pixelHeight=diff.sourceSnapshot.pixelHeight;
  Design.Validate(result);return result;
 }
}
}
