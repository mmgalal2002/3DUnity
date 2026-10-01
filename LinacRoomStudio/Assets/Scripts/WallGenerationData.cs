using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RoomStudio {
// Optional version-2 editor data. These records are not shielding geometry by
// themselves: only a linked Wall item contributes a barrier to the room.
[Serializable] public class GeneratedWallRef {
 public string batchId="",pathId="",ruleId="",categoryName="";
 public Color displayColor=Color.magenta;
 public Vector2Data sourceAPixel=new Vector2Data(),sourceBPixel=new Vector2Data();
 public bool manuallyEdited,unsupportedGeometry;
}

[Serializable] public class GeneratedWallPath {
 public string id="",ruleId="",itemId="",categoryName="",material="";
 public string detectionPathId="";
 public Vector2Data detectionAPixel=null,detectionBPixel=null;
 public Color displayColor=Color.magenta;
 // Source endpoints never change. Corrected pixel/world endpoints may be edited.
 public Vector2Data sourceAPixel=new Vector2Data(),sourceBPixel=new Vector2Data();
 public Vector2Data aPixel=new Vector2Data(),bPixel=new Vector2Data();
 public Vector3 aWorld,bWorld;
 public int sourceComponent,areaPixels;
 public float lengthPixels,height,baseElevation,thicknessMm,densityKgM3;
 public bool manuallyEdited,deletedOverride,unsupportedGeometry;
}

[Serializable] public class WallGenerationOptions {
 public int version;
 public bool autoConnect=true,allowCrossCategory;
 public float tolerance=WallConnections.DefaultTolerance;
 public static WallGenerationOptions Read(WallGenerationOptions value){
  if(value==null||value.version==0)return new WallGenerationOptions{version=1};
  if(value.version!=1)throw new Exception("Unsupported wall-generation connection settings.");
  PrecisionEditing.Range(value.tolerance,.000001f,1,"generation connection tolerance");
  return JsonUtility.FromJson<WallGenerationOptions>(JsonUtility.ToJson(value));
 }
}

[Serializable] public class WallGenerationBatch {
 public int version=1;
 public string id="",sourceFingerprint="",snapshotSignature="";
 public int pixelWidth,pixelHeight;
 public WallGenerationOptions connectionOptions=null;
 // A frozen image, authoring rules, calibration and guide transform allow a
 // committed batch to survive replacement or removal of the active guide.
 public FloorPlanData sourceSnapshot=null;
 public List<GeneratedWallPath> paths=new List<GeneratedWallPath>();
}

public static class WallGenerationData {
 public const int MaxBatches=32,MaxStoredPaths=2000;
 static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
 public static bool IsEmpty(GeneratedWallRef value)=>value==null||(string.IsNullOrEmpty(value.batchId)&&string.IsNullOrEmpty(value.pathId)&&string.IsNullOrEmpty(value.ruleId));
 static void Id(string value,string name){if(string.IsNullOrWhiteSpace(value)||value.Length>128)throw new Exception("Invalid generated-wall "+name+".");}
 static void Text(string value,string name){if(string.IsNullOrWhiteSpace(value)||value.Length>100)throw new Exception("Invalid generated-wall "+name+".");}
 static void Range(float value,float min,float max,string name){if(float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max)throw new Exception("Invalid generated-wall "+name+".");}
 static void Pixel(Vector2Data point,int width,int height,string name,bool source){
  if(point==null)throw new Exception("Missing generated-wall "+name+".");
  // A corrected endpoint can extend beyond the original image, but remains
  // finite and bounded. Original extraction endpoints must remain in-bounds.
  Range(point.x,source?0:-10000,source?width:10000,name+" X");
  Range(point.y,source?0:-10000,source?height:10000,name+" Y");
 }
 static void World(Vector3 point,string name){Range(point.x,-10000,10000,name+" X");Range(point.y,-100,100,name+" elevation");Range(point.z,-10000,10000,name+" Z");}
 static void ColorValue(Color value){Range(value.r,0,1,"display red");Range(value.g,0,1,"display green");Range(value.b,0,1,"display blue");Range(value.a,0,1,"display alpha");}
 static bool Sha256(string value)=>value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
 static string F(float value)=>value.ToString("R",Invariant);
 public static string SnapshotSignature(FloorPlanData source){
  if(source==null||source.kind!="Image")throw new Exception("A generated-wall source must be an image guide.");
  // Visibility, opacity and source path are intentionally excluded: they do
  // not alter extracted geometry. Coordinates/rules/calibration are included.
  string payload=PlanAuthoring.Fingerprint(source)+"|"+source.pixelWidth+"|"+source.pixelHeight+"|"+F(source.x)+"|"+F(source.z)+"|"+F(source.rotation)+"|"+F(source.widthMeters)+"|"+F(source.heightMeters)+"|"+JsonUtility.ToJson(source.authoring);
  using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant();
 }
 public static WallGenerationBatch FindBatch(Design design,string batchId)=>design?.generationBatches?.FirstOrDefault(batch=>batch!=null&&batch.id==batchId);
 public static GeneratedWallPath FindPath(Design design,string batchId,string pathId)=>FindBatch(design,batchId)?.paths?.FirstOrDefault(path=>path!=null&&path.id==pathId);
 static bool Different(float a,float b)=>Math.Abs(a-b)>.0001f;
 public static bool WasEdited(Item wall,GeneratedWallPath path){
  var start=PrecisionEditing.Endpoint(wall,false);var end=PrecisionEditing.Endpoint(wall,true);
  return Vector3.Distance(start,path.aWorld)>.0001f||Vector3.Distance(end,path.bWorld)>.0001f||
   Different(wall.y,path.baseElevation)||Different(wall.height,path.height)||
   wall.shielding.material!=path.material||Different(wall.shielding.thickness,path.thicknessMm)||
   Different(wall.shielding.density,path.densityKgM3)||wall.name!=path.categoryName;
 }
 // Call after an ordinary property/gesture edit and before committing history.
 // Existing manual edits are monotonic; undo restores the earlier JSON snapshot.
 public static int MarkManualEdits(Design design){
  var changed=new List<Tuple<GeneratedWallRef,GeneratedWallPath>>();
  foreach(var item in design.items){
   if(IsEmpty(item.generated))continue;
   var path=FindPath(design,item.generated.batchId,item.generated.pathId);
   if(path==null||path.deletedOverride||path.itemId!=item.id)throw new Exception("Generated-wall path reference is broken.");
   if(WasEdited(item,path)&&(!path.manuallyEdited||!item.generated.manuallyEdited))changed.Add(Tuple.Create(item.generated,path));
  }
  foreach(var pair in changed){pair.Item1.manuallyEdited=true;pair.Item2.manuallyEdited=true;}
  return changed.Count;
 }
 // Call before removing selected walls. The tombstone prevents regeneration
 // from resurrecting a deliberate deletion, while leaving other batches alone.
 public static int MarkDeletedPaths(Design design,IEnumerable<Item> deleted){
  if(deleted==null)return 0;
  var selected=deleted.Where(item=>item!=null).Select(item=>item.id).ToList();
  var expanded=SelectionEditing.Expand(design,selected);
  var targets=design.items.Where(item=>expanded.Contains(item.id)).ToList();
  SelectionEditing.RequireUnlocked(targets);
  if(design.wallJunctions!=null&&design.wallJunctions.Any(junction=>junction?.arms!=null&&junction.arms.Any(arm=>arm!=null&&expanded.Contains(arm.wallId))))throw new Exception("Disconnect joined wall edges before deleting their walls.");
  var paths=new HashSet<GeneratedWallPath>();
  foreach(var item in targets){
   if(IsEmpty(item.generated))continue;
   var path=FindPath(design,item.generated.batchId,item.generated.pathId);
   if(path==null||path.deletedOverride||path.itemId!=item.id)throw new Exception("Generated-wall path reference is broken.");
   paths.Add(path);
  }
  foreach(var path in paths){path.itemId="";path.deletedOverride=true;}
  return paths.Count;
 }
 public static void Validate(Design design){
  if(design.generationBatches==null||design.generationBatches.Count==0){
   if(design.items.Any(item=>!IsEmpty(item.generated)))throw new Exception("A generated wall has no generation batch.");
   return;
  }
  if(design.generationBatches.Count>MaxBatches)throw new Exception("Too many wall-generation batches.");
  var batchIds=new HashSet<string>();var itemIds=new HashSet<string>();int storedPaths=0;
  foreach(var batch in design.generationBatches){
   if(batch==null||batch.version!=1)throw new Exception("Unsupported wall-generation batch.");
    WallGenerationOptions.Read(batch.connectionOptions);
   Id(batch.id,"batch ID");if(!batchIds.Add(batch.id))throw new Exception("Duplicate wall-generation batch ID.");
   if(!Sha256(batch.sourceFingerprint)||!Sha256(batch.snapshotSignature))throw new Exception("Invalid wall-generation source signature.");
   if(batch.pixelWidth<1||batch.pixelHeight<1||batch.pixelWidth>FloorPlanCodec.MaxDimension||batch.pixelHeight>FloorPlanCodec.MaxDimension||(long)batch.pixelWidth*batch.pixelHeight>FloorPlanCodec.MaxPixels)throw new Exception("Invalid wall-generation source dimensions.");
   if(batch.paths==null||batch.paths.Count==0)throw new Exception("A wall-generation batch needs stored paths.");
   storedPaths+=batch.paths.Count;if(storedPaths>MaxStoredPaths)throw new Exception("Too many stored generated-wall paths.");
   if(!Design.IsEmptyFloorPlan(batch.sourceSnapshot)){
    if(batch.sourceSnapshot.kind!="Image")throw new Exception("Wall-generation source snapshot must be an image.");
    FloorPlanCodec.Validate(batch.sourceSnapshot);
    if(batch.sourceSnapshot.pixelWidth!=batch.pixelWidth||batch.sourceSnapshot.pixelHeight!=batch.pixelHeight||PlanAuthoring.Fingerprint(batch.sourceSnapshot)!=batch.sourceFingerprint||SnapshotSignature(batch.sourceSnapshot)!=batch.snapshotSignature)throw new Exception("Wall-generation source snapshot does not match its identity.");
   }
   var pathIds=new HashSet<string>();
   foreach(var path in batch.paths){
    if(path==null)throw new Exception("Missing generated-wall path.");
    Id(path.id,"path ID");Id(path.ruleId,"rule ID");Text(path.categoryName,"category name");
    if(!pathIds.Add(path.id))throw new Exception("Duplicate generated-wall path ID.");
    if(!PlanAuthoring.Materials.Contains(path.material))throw new Exception("Generated wall needs an explicit physical material.");
    ColorValue(path.displayColor);Pixel(path.sourceAPixel,batch.pixelWidth,batch.pixelHeight,"source A",true);Pixel(path.sourceBPixel,batch.pixelWidth,batch.pixelHeight,"source B",true);
    if(!string.IsNullOrEmpty(path.detectionPathId)){
     Id(path.detectionPathId,"original detection ID");Pixel(path.detectionAPixel,batch.pixelWidth,batch.pixelHeight,"detected A",true);Pixel(path.detectionBPixel,batch.pixelWidth,batch.pixelHeight,"detected B",true);
    }
    Pixel(path.aPixel,batch.pixelWidth,batch.pixelHeight,"corrected A",false);Pixel(path.bPixel,batch.pixelWidth,batch.pixelHeight,"corrected B",false);
    World(path.aWorld,"world A");World(path.bWorld,"world B");
    Range(path.lengthPixels,0,12000,"source length");Range(path.height,.001f,100,"height");Range(path.baseElevation,-100,100,"base elevation");Range(path.thicknessMm,.01f,3000,"thickness");Range(path.densityKgM3,100,25000,"density");
    if(path.sourceComponent<0||path.areaPixels<0||path.areaPixels>PlanAuthoring.MaxProcessingPixels)throw new Exception("Invalid generated-wall source component.");
    if(path.deletedOverride){if(!string.IsNullOrEmpty(path.itemId))throw new Exception("A deleted generated path still refers to a wall.");}
    else {Id(path.itemId,"item ID");if(!itemIds.Add(path.itemId))throw new Exception("Two generated paths refer to the same wall.");}
   }
  }
  foreach(var item in design.items){
   var reference=item.generated;if(IsEmpty(reference))continue;
   if(item.kind!="Wall")throw new Exception("Only walls may have generated-wall provenance.");
   Id(reference.batchId,"reference batch ID");Id(reference.pathId,"reference path ID");Id(reference.ruleId,"reference rule ID");Text(reference.categoryName,"reference category name");ColorValue(reference.displayColor);
   var batch=FindBatch(design,reference.batchId);var path=FindPath(design,reference.batchId,reference.pathId);
   if(batch==null||path==null||path.deletedOverride||path.itemId!=item.id||path.ruleId!=reference.ruleId)throw new Exception("Generated-wall item reference is broken.");
   Pixel(reference.sourceAPixel,batch.pixelWidth,batch.pixelHeight,"item source A",true);Pixel(reference.sourceBPixel,batch.pixelWidth,batch.pixelHeight,"item source B",true);
   if(reference.categoryName!=path.categoryName||reference.displayColor!=path.displayColor||reference.manuallyEdited!=path.manuallyEdited||reference.unsupportedGeometry!=path.unsupportedGeometry)throw new Exception("Generated-wall item and path metadata disagree.");
  }
  foreach(var id in itemIds){var item=design.items.FirstOrDefault(value=>value.id==id);if(item==null||IsEmpty(item.generated))throw new Exception("A generated path has no linked wall item.");}
 }
}
}
