using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {

// A junction is editor geometry, not a Lock together group. Endpoint references remain
// stable when an item is reordered; Interior is used for the uncut host of a T join.
[Serializable] public class WallJunctionArm {
 public string wallId="", endpoint="Start";
}
[Serializable] public class WallJunction {
 public string id=Guid.NewGuid().ToString(), kind="Corner";
 public float x,z;
 public List<WallJunctionArm> arms=new List<WallJunctionArm>();
}

public class WallConnectionPlan {
 public readonly List<Item> replacements=new List<Item>();
 public readonly List<Item> additions=new List<Item>();
 public readonly List<WallJunction> junctions=new List<WallJunction>();
 public readonly List<string> diagnostics=new List<string>();
 internal readonly Dictionary<string,string> originals=new Dictionary<string,string>();
 internal readonly List<WallSplit> splits=new List<WallSplit>();
 internal string junctionSnapshot="";
 public bool HasChanges=>junctions.Count>0;
}

internal class WallSplit {
 public string originalId,newId;
 public Vector2 point;
 public float fraction;
}

// All distances are world metres, independent of grid, zoom and wall-snap settings.
// Preview never mutates Design. A plan may be applied only to the wall snapshots from
// which it was calculated, then the caller commits a single undo-history transaction.
public static class WallConnections {
 public const float DefaultTolerance=.02f;
 const float GeometryEpsilon=.0001f;
 const float MinimumAngle=15f;
 const int MaxCandidates=2048;

 class Candidate {
  public Item a,b;
  public string aEnd,bEnd,kind;
  public Vector2 point;
  public float score;
  public bool IsX=>kind=="X";
 }

 static Vector2 XZ(Vector3 p)=>new Vector2(p.x,p.z);
 static Vector2 End(Item wall,string end)=>XZ(PrecisionEditing.Endpoint(wall,end=="End"));
 static Vector2 Direction(Item wall){var a=End(wall,"Start");var b=End(wall,"End");return (b-a).normalized;}
 static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
 static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 static string Pair(Item a,Item b)=>a.id+" / "+b.id;
 static Item Copy(Item item)=>JsonUtility.FromJson<Item>(JsonUtility.ToJson(item));
 static bool SamePlanarLayer(Item a,Item b)=>Mathf.Abs(a.y-b.y)<=GeometryEpsilon&&Mathf.Abs(a.height-b.height)<=GeometryEpsilon;
 static bool SamePhysicalMaterial(Item a,Item b)=>a.shielding!=null&&b.shielding!=null&&a.shielding.material==b.shielding.material&&Mathf.Abs(a.shielding.density-b.shielding.density)<=.01f;

 public static WallConnectionPlan Preview(Design design,IEnumerable<string> wallIds,float tolerance=DefaultTolerance,bool allowCrossCategory=false,Func<Vector2,Vector2,bool> crossesOpening=null){
  if(design==null||design.items==null)throw new Exception("No design is available for wall connection.");
  var selected=new HashSet<string>(wallIds??Enumerable.Empty<string>());
  if(selected.Count<2)throw new Exception("Select at least two walls to connect.");
  var walls=design.items.Where(i=>selected.Contains(i.id)).ToList();
  if(walls.Count!=selected.Count||walls.Any(i=>i.kind!="Wall"))throw new Exception("Connect wall edges needs only existing wall IDs.");
  var plan=PreviewWalls(walls,tolerance,allowCrossCategory,crossesOpening,design.wallJunctions);
  plan.junctionSnapshot=JunctionJson(design.wallJunctions);
  foreach(var wall in walls)plan.originals[wall.id]=JsonUtility.ToJson(wall);
  return plan;
 }

 // Also accepts temporary generated walls before they enter design.items. The same
 // conservative compatibility, ambiguity and footprint rules apply to both routes.
 public static WallConnectionPlan PreviewWalls(IList<Item> walls,float tolerance=DefaultTolerance,bool allowCrossCategory=false,Func<Vector2,Vector2,bool> crossesOpening=null,IEnumerable<WallJunction> existingJunctions=null){
  PrecisionEditing.Range(tolerance,.000001f,1,"connection tolerance");
  if(walls==null||walls.Count<2||walls.Count>250||walls.Any(w=>w==null||w.kind!="Wall"))throw new Exception("Connect 2–250 valid wall items.");
  if(walls.Select(w=>w.id).Distinct().Count()!=walls.Count)throw new Exception("Wall IDs must be unique before connection.");
  var plan=new WallConnectionPlan();var candidates=new List<Candidate>();
  var occupied=new HashSet<string>((existingJunctions??Enumerable.Empty<WallJunction>()).Where(j=>j?.arms!=null).SelectMany(j=>j.arms.Where(a=>a!=null).Select(a=>a.wallId)));
  foreach(var wall in walls)plan.originals[wall.id]=JsonUtility.ToJson(wall);
  for(int i=0;i<walls.Count;i++)for(int j=i+1;j<walls.Count;j++){
   var a=walls[i];var b=walls[j];
   if(occupied.Contains(a.id)||occupied.Contains(b.id)){plan.diagnostics.Add("Existing junction at "+Pair(a,b)+" must be detached before another automatic join.");continue;}
   if(a.doors!=null&&a.doors.Count>0||b.doors!=null&&b.doors.Count>0){plan.diagnostics.Add("Door-bearing walls "+Pair(a,b)+" were left unchanged.");continue;}
   if(!SamePlanarLayer(a,b)){
    if(OverlapsInPlan(a,b,tolerance))plan.diagnostics.Add("Different wall bases or heights at "+Pair(a,b)+" need a separately reviewed join.");
    continue;
   }
   if(!SamePhysicalMaterial(a,b)){
    if(OverlapsInPlan(a,b,tolerance))plan.diagnostics.Add("Different physical materials at "+Pair(a,b)+" need a separately reviewed join.");
    continue;
   }
   if(Mathf.Abs(a.Opacity-b.Opacity)>.0001f){plan.diagnostics.Add("Different wall opacities at "+Pair(a,b)+" cannot share one physical mesh.");continue;}
   if(!allowCrossCategory&&DifferentCategory(a,b)){
    if(OverlapsInPlan(a,b,tolerance))plan.diagnostics.Add("Different wall categories at "+Pair(a,b)+" need cross-category permission.");
    continue;
   }
   var da=Direction(a);var db=Direction(b);float dot=Mathf.Abs(Vector2.Dot(da,db));
   float angle=Mathf.Acos(Mathf.Clamp(dot,0,1))*Mathf.Rad2Deg;
   bool collinear=angle<MinimumAngle&&Mathf.Abs(Cross(da,End(b,"Start")-End(a,"Start")))<=Mathf.Min(.005f,tolerance*.25f);
   if(angle<MinimumAngle&&!collinear){
    if(OverlapsInPlan(a,b,tolerance))plan.diagnostics.Add("Near-parallel walls "+Pair(a,b)+" were not merged.");
    continue;
   }
   foreach(string ae in new[]{"Start","End"})foreach(string be in new[]{"Start","End"}){
    var pa=End(a,ae);var pb=End(b,be);float gap=Vector2.Distance(pa,pb);
    if(gap<=tolerance)AddCandidate(candidates,new Candidate{a=a,b=b,aEnd=ae,bEnd=be,kind=collinear?"Collinear":"Corner",point=(pa+pb)*.5f,score=gap},plan);
   }
   if(!collinear){
    AddEndpointToHost(candidates,plan,a,b,tolerance);
    AddEndpointToHost(candidates,plan,b,a,tolerance);
    var a0=End(a,"Start");var a1=End(a,"End");var b0=End(b,"Start");var b1=End(b,"End");
    if(TryLines(a0,a1,b0,b1,out float ta,out float tb,out var crossing)&&
       ta>.25f/a.length&&ta<1-.25f/a.length&&tb>.25f/b.length&&tb<1-.25f/b.length)
     AddCandidate(candidates,new Candidate{a=a,b=b,kind="X",point=crossing,score=0},plan);
   }
  }
  var ambiguous=new HashSet<string>();var sharedEndpoints=new HashSet<string>();var endpointCandidates=new Dictionary<string,List<Candidate>>();
  foreach(var c in candidates){
   foreach(string key in CandidateKeys(c)){
    if(!endpointCandidates.ContainsKey(key))endpointCandidates[key]=new List<Candidate>();
    endpointCandidates[key].Add(c);
   }
  }
  foreach(var pair in endpointCandidates)if(pair.Value.Count>1){
   if(SharedEndpointNode(pair.Value))sharedEndpoints.Add(pair.Key);
   else{ambiguous.Add(pair.Key);plan.diagnostics.Add("Ambiguous junction at "+pair.Key+"; no endpoint was moved.");}
  }
  var proposals=walls.ToDictionary(w=>w.id,Copy);
  var used=new Dictionary<string,Vector2>();var splitWalls=new HashSet<string>();var joinedWalls=new HashSet<string>();
  foreach(var c in candidates.OrderBy(x=>x.score).ThenBy(x=>x.a.id,StringComparer.Ordinal).ThenBy(x=>x.b.id,StringComparer.Ordinal)){
   var keys=CandidateKeys(c).ToList();if(keys.Any(k=>ambiguous.Contains(k)||used.ContainsKey(k)&&(!sharedEndpoints.Contains(k)||Vector2.Distance(used[k],c.point)>GeometryEpsilon)))continue;
   if(splitWalls.Contains(c.a.id)||splitWalls.Contains(c.b.id)||c.IsX&&(joinedWalls.Contains(c.a.id)||joinedWalls.Contains(c.b.id)))continue;
   if(crossesOpening!=null&&
      (c.IsX?crossesOpening(End(c.a,"Start"),End(c.a,"End"))||crossesOpening(End(c.b,"Start"),End(c.b,"End")):
       crossesOpening(End(c.a,c.aEnd),c.point)||c.kind!="T"&&crossesOpening(End(c.b,c.bEnd),c.point))){
    plan.diagnostics.Add("Confirmed opening blocks connection at "+Pair(c.a,c.b)+".");continue;
   }
   if(c.a.locked||c.b.locked){
    // A locked host of a T can anchor an editable branch. A pair of already
    // coincident locked endpoints may be recorded without changing either wall.
    if(c.IsX||c.kind=="T"&&c.a.locked||c.kind!="T"&&c.a.locked&&c.b.locked&&Vector2.Distance(End(c.a,c.aEnd),End(c.b,c.bEnd))>GeometryEpsilon){
     plan.diagnostics.Add("Locked wall at "+Pair(c.a,c.b)+" blocks this junction.");continue;
    }
   }
   if(!string.IsNullOrEmpty(c.a.groupId)||!string.IsNullOrEmpty(c.b.groupId)){
    plan.diagnostics.Add("Grouped walls at "+Pair(c.a,c.b)+" require explicit ungrouping before connection.");continue;
   }
   try{
    var trial=proposals.ToDictionary(x=>x.Key,x=>Copy(x.Value));
    var added=new List<Item>();var arms=new List<WallJunctionArm>();var splits=new List<WallSplit>();
    if(c.IsX){
     Split(trial,c.a.id,c.point,added,arms,splits);Split(trial,c.b.id,c.point,added,arms,splits);
     if(trial.Count+added.Count>250)throw new Exception("X split would exceed 250 walls.");
    }else if(c.kind=="T"){
     MoveEnd(trial[c.a.id],c.aEnd,c.point);
     arms.Add(new WallJunctionArm{wallId=c.a.id,endpoint=c.aEnd});
     arms.Add(new WallJunctionArm{wallId=c.b.id,endpoint="Interior"});
    }else{
     var target=c.point;
     if(c.a.locked)target=End(c.a,c.aEnd);
     if(c.b.locked)target=End(c.b,c.bEnd);
     if(!c.a.locked)MoveEnd(trial[c.a.id],c.aEnd,target);
     if(!c.b.locked)MoveEnd(trial[c.b.id],c.bEnd,target);
     arms.Add(new WallJunctionArm{wallId=c.a.id,endpoint=c.aEnd});
     arms.Add(new WallJunctionArm{wallId=c.b.id,endpoint=c.bEnd});
     c.point=target;
    }
    var junction=new WallJunction{kind=c.kind,x=c.point.x,z=c.point.y,arms=arms};
    var shared=c.kind=="Corner"||c.kind=="Collinear"?plan.junctions.FirstOrDefault(existing=>Vector2.Distance(new Vector2(existing.x,existing.z),c.point)<=GeometryEpsilon&&existing.arms.All(arm=>arm.endpoint!="Interior")&&existing.arms.Any(arm=>arms.Any(value=>value.wallId==arm.wallId&&value.endpoint==arm.endpoint))):null;
    if(shared!=null){
     var combined=shared.arms.Concat(arms).GroupBy(arm=>arm.wallId+":"+arm.endpoint).Select(group=>group.First()).ToList();
     if(combined.Count>4)throw new Exception("More than four branches require explicit junction review.");
     shared.arms=combined;shared.kind=combined.Count==4?"X":combined.Count==3?"T":shared.kind;
    }else plan.junctions.Add(junction);
    proposals=trial;plan.additions.AddRange(added);plan.splits.AddRange(splits);
    joinedWalls.Add(c.a.id);joinedWalls.Add(c.b.id);if(c.IsX){splitWalls.Add(c.a.id);splitWalls.Add(c.b.id);}
    foreach(var key in keys)used[key]=c.point;
   }catch(Exception e){plan.diagnostics.Add("Could not join "+Pair(c.a,c.b)+": "+e.Message);}
  }
  foreach(var original in walls)if(JsonUtility.ToJson(proposals[original.id])!=JsonUtility.ToJson(original))plan.replacements.Add(proposals[original.id]);
  return plan;
 }

 static bool SharedEndpointNode(List<Candidate> candidates){
  if(candidates.Any(candidate=>candidate.kind!="Corner"&&candidate.kind!="Collinear"))return false;
  var point=candidates[0].point;
  if(candidates.Any(candidate=>Vector2.Distance(candidate.point,point)>GeometryEpsilon))return false;
  var endpoints=candidates.SelectMany(candidate=>new[]{Tuple.Create(candidate.a,candidate.aEnd),Tuple.Create(candidate.b,candidate.bEnd)})
   .GroupBy(endpoint=>endpoint.Item1.id+":"+endpoint.Item2).Select(group=>group.First()).ToArray();
  if(endpoints.Length<3||endpoints.Length>4||endpoints.Select(endpoint=>endpoint.Item1.id).Distinct().Count()!=endpoints.Length)return false;
  if(endpoints.Any(endpoint=>Vector2.Distance(End(endpoint.Item1,endpoint.Item2),point)>GeometryEpsilon))return false;
  for(int first=0;first<endpoints.Length;first++)for(int second=first+1;second<endpoints.Length;second++){
    var firstEndpoint=endpoints[first];var secondEndpoint=endpoints[second];
    var rayA=Direction(firstEndpoint.Item1)*(firstEndpoint.Item2=="Start"?1:-1);var rayB=Direction(secondEndpoint.Item1)*(secondEndpoint.Item2=="Start"?1:-1);
   if(Vector2.Dot(rayA,rayB)>Mathf.Cos(MinimumAngle*Mathf.Deg2Rad))return false;
  }
  return true;
 }
 static bool DifferentCategory(Item a,Item b){
  // Hand-authored walls have no color-rule category. Two generated paths from
  // different rules require an explicit cross-category choice even when their
  // physical material happens to be the same.
  bool aGenerated=!WallGenerationData.IsEmpty(a.generated),bGenerated=!WallGenerationData.IsEmpty(b.generated);
  if(aGenerated!=bGenerated)return true;
  if(!aGenerated)return false;
  return a.generated.ruleId!=b.generated.ruleId||a.generated.categoryName!=b.generated.categoryName;
 }
 static void AddCandidate(List<Candidate> list,Candidate c,WallConnectionPlan plan){
  if(list.Count>=MaxCandidates){if(!plan.diagnostics.Contains("Connection candidate limit reached."))plan.diagnostics.Add("Connection candidate limit reached.");return;}
  list.Add(c);
 }
 static IEnumerable<string> CandidateKeys(Candidate c){
  if(c.IsX){yield return c.a.id+":Interior";yield return c.b.id+":Interior";}
  else if(c.kind=="T")yield return c.a.id+":"+c.aEnd;
  else{yield return c.a.id+":"+c.aEnd;yield return c.b.id+":"+c.bEnd;}
 }
 static bool OverlapsInPlan(Item a,Item b,float tolerance){
  var ca=new Vector2(a.x,a.z);var cb=new Vector2(b.x,b.z);
  return Vector2.Distance(ca,cb)<(a.length+b.length)*.5f+Mathf.Max(a.shielding.thickness,b.shielding.thickness)/1000f+tolerance;
 }
 static bool TryLines(Vector2 a,Vector2 b,Vector2 c,Vector2 d,out float ta,out float tb,out Vector2 point){
  var u=b-a;var v=d-c;float denom=Cross(u,v);ta=tb=0;point=Vector2.zero;
  if(Mathf.Abs(denom)<.000001f)return false;
  ta=Cross(c-a,v)/denom;tb=Cross(c-a,u)/denom;point=a+u*ta;return true;
 }
 static void AddEndpointToHost(List<Candidate> list,WallConnectionPlan plan,Item branch,Item host,float tolerance){
  var h0=End(host,"Start");var h1=End(host,"End");var p0=End(branch,"Start");var p1=End(branch,"End");
  if(!TryLines(p0,p1,h0,h1,out float tb,out float th,out var crossing))return;
  if(th<=.25f/host.length||th>=1-.25f/host.length)return;
  foreach(string endpoint in new[]{"Start","End"}){
   var p=End(branch,endpoint);float toCenter=Vector2.Distance(p,crossing);
   float hostHalf=host.shielding.thickness/2000f;
   if(toCenter<=hostHalf+tolerance&&toCenter<=Mathf.Max(.5f,hostHalf+tolerance)&&
      (endpoint=="Start"?tb<=.5f:tb>=.5f))
    AddCandidate(list,new Candidate{a=branch,b=host,aEnd=endpoint,kind="T",point=crossing,score=Mathf.Max(0,toCenter-hostHalf)},plan);
  }
 }
 static void MoveEnd(Item item,string endpoint,Vector2 target){
  var fixedEnd=PrecisionEditing.Endpoint(item,endpoint=="Start");
  PrecisionEditing.ResizeEndpoint(item,fixedEnd,new Vector3(target.x,item.y,target.y),endpoint=="End");
 }
 static void Split(Dictionary<string,Item> trial,string id,Vector2 point,List<Item> added,List<WallJunctionArm> arms,List<WallSplit> splits){
  var item=trial[id];if(item.locked)throw new Exception("A locked wall cannot be split.");
  var a=End(item,"Start");var b=End(item,"End");
  if(Vector2.Distance(a,point)<.25f||Vector2.Distance(b,point)<.25f)throw new Exception("Split would create a wall shorter than 0.25 m.");
  var second=Copy(item);second.id=Guid.NewGuid().ToString();second.name=WallGenerationData.IsEmpty(item.generated)?item.name+" (split)":item.name;
  if(!WallGenerationData.IsEmpty(second.generated))second.generated.pathId=Guid.NewGuid().ToString();
  MoveEnd(item,"End",point);MoveEnd(second,"Start",point);
  trial[id]=item;added.Add(second);
  splits.Add(new WallSplit{originalId=id,newId=second.id,point=point,fraction=Vector2.Distance(a,point)/Vector2.Distance(a,b)});
  arms.Add(new WallJunctionArm{wallId=id,endpoint="End"});
  arms.Add(new WallJunctionArm{wallId=second.id,endpoint="Start"});
 }
 static string JunctionJson(List<WallJunction> junctions)=>junctions==null?"":string.Join("|",junctions.Select(j=>JsonUtility.ToJson(j)).ToArray());
 public static void Apply(Design design,WallConnectionPlan plan,bool preserveAsManual=true){
  if(design==null||plan==null)throw new Exception("Missing wall connection preview.");
  if(!plan.HasChanges)throw new Exception("No compatible wall edges were found.");
  if(plan.junctionSnapshot!=JunctionJson(design.wallJunctions))throw new Exception("Wall junctions changed since preview; preview again.");
  foreach(var old in plan.originals){var current=design.items.Find(i=>i.id==old.Key);if(current==null||JsonUtility.ToJson(current)!=old.Value)throw new Exception("A wall changed since preview; preview again.");}
  var priorItems=design.items;var priorJunctions=design.wallJunctions;var priorBatches=design.generationBatches;
  var replacements=plan.replacements.ToDictionary(i=>i.id,i=>i);
  var next=new List<Item>(priorItems.Select(i=>Copy(replacements.ContainsKey(i.id)?replacements[i.id]:i)));
  next.AddRange(plan.additions.Select(Copy));
  var joins=priorJunctions==null?new List<WallJunction>():new List<WallJunction>(priorJunctions);
  joins.AddRange(plan.junctions);
  var batches=priorBatches==null?new List<WallGenerationBatch>():priorBatches.Select(b=>new WallGenerationBatch{
   version=b.version,id=b.id,sourceFingerprint=b.sourceFingerprint,snapshotSignature=b.snapshotSignature,pixelWidth=b.pixelWidth,pixelHeight=b.pixelHeight,
    sourceSnapshot=b.sourceSnapshot,connectionOptions=b.connectionOptions,paths=b.paths.Select(p=>JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(p))).ToList()
  }).ToList();
  foreach(var split in plan.splits){
   var oldItem=priorItems.Find(i=>i.id==split.originalId);
   if(oldItem==null||WallGenerationData.IsEmpty(oldItem.generated))continue;
   var newItem=next.Find(i=>i.id==split.newId);
   int batchIndex=batches.FindIndex(b=>b.id==oldItem.generated.batchId);
   if(batchIndex<0||newItem==null||WallGenerationData.IsEmpty(newItem.generated))throw new Exception("Generated split lost its source path.");
   var source=batches[batchIndex];
   var batch=new WallGenerationBatch{version=source.version,id=source.id,sourceFingerprint=source.sourceFingerprint,snapshotSignature=source.snapshotSignature,
    pixelWidth=source.pixelWidth,pixelHeight=source.pixelHeight,sourceSnapshot=source.sourceSnapshot,connectionOptions=source.connectionOptions,paths=new List<GeneratedWallPath>(source.paths)};
   int pathIndex=batch.paths.FindIndex(p=>p.id==oldItem.generated.pathId);
   if(pathIndex<0)throw new Exception("Generated split source path no longer exists.");
   var first=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(batch.paths[pathIndex]));
  if(string.IsNullOrEmpty(first.detectionPathId)){
   first.detectionPathId=first.id;first.detectionAPixel=new Vector2Data(first.sourceAPixel.x,first.sourceAPixel.y);first.detectionBPixel=new Vector2Data(first.sourceBPixel.x,first.sourceBPixel.y);
  }
   var second=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(first));
   var pixel=new Vector2Data(Mathf.Lerp(first.aPixel.x,first.bPixel.x,split.fraction),Mathf.Lerp(first.aPixel.y,first.bPixel.y,split.fraction));
   var originalEnd=PrecisionEditing.Endpoint(oldItem,true);var originalStart=PrecisionEditing.Endpoint(oldItem,false);
   first.aWorld=originalStart;first.bWorld=new Vector3(split.point.x,oldItem.y,split.point.y);first.bPixel=new Vector2Data(pixel.x,pixel.y);
   second.id=newItem.generated.pathId;second.itemId=newItem.id;second.aWorld=first.bWorld;second.bWorld=originalEnd;second.aPixel=new Vector2Data(pixel.x,pixel.y);
   first.sourceBPixel=new Vector2Data(pixel.x,pixel.y);second.sourceAPixel=new Vector2Data(pixel.x,pixel.y);
   float originalPixels=first.lengthPixels;first.lengthPixels=originalPixels*split.fraction;second.lengthPixels=originalPixels*(1-split.fraction);
   var firstItem=next.Find(i=>i.id==split.originalId);
   firstItem.generated.sourceBPixel=new Vector2Data(pixel.x,pixel.y);newItem.generated.sourceAPixel=new Vector2Data(pixel.x,pixel.y);
   batch.paths[pathIndex]=first;batch.paths.Add(second);batches[batchIndex]=batch;
  }
  foreach(var wall in next.Where(i=>!WallGenerationData.IsEmpty(i.generated))){
   var batch=batches.Find(b=>b.id==wall.generated.batchId);
   var path=batch?.paths.Find(p=>p.id==wall.generated.pathId);
   if(path==null)throw new Exception("Connected generated wall lost its path metadata.");
   if(!WallGenerationData.WasEdited(wall,path))continue;
   path.aWorld=PrecisionEditing.Endpoint(wall,false);path.bWorld=PrecisionEditing.Endpoint(wall,true);
   if(!Design.IsEmptyFloorPlan(batch.sourceSnapshot)){
    var a=PlanAuthoring.WorldToPixel(batch.sourceSnapshot,path.aWorld);
    var b=PlanAuthoring.WorldToPixel(batch.sourceSnapshot,path.bWorld);
    path.aPixel=new Vector2Data(a.x,a.y);path.bPixel=new Vector2Data(b.x,b.y);
   }
   if(preserveAsManual){path.manuallyEdited=true;wall.generated.manuallyEdited=true;}
  }
  design.items=next;design.wallJunctions=joins;design.generationBatches=batches;
  try{Design.Validate(design);}catch{design.items=priorItems;design.wallJunctions=priorJunctions;design.generationBatches=priorBatches;throw;}
 }
 // IDs may be selected wall IDs or explicit junction IDs. Removing a wall
 // always detaches its complete affected junction before Design.Validate runs.
 public static void Detach(Design design,IEnumerable<string> idsToDetach){
  if(design==null||design.wallJunctions==null)return;
  var ids=new HashSet<string>(idsToDetach??Enumerable.Empty<string>());
  if(ids.Count==0)return;
  design.wallJunctions=design.wallJunctions.Where(j=>!ids.Contains(j.id)&&!j.arms.Any(a=>ids.Contains(a.wallId))).ToList();
 }
 public static void Validate(Design design)=>ValidateStored(design);
 public static List<Item> ConnectedCluster(Design design,string wallId){
  if(design==null||design.items==null||string.IsNullOrEmpty(wallId))return new List<Item>();
  var found=new HashSet<string>{wallId};bool grew=true;
  while(grew){
   grew=false;
   if(design.wallJunctions==null)break;
   foreach(var junction in design.wallJunctions){
    if(junction?.arms==null||!junction.arms.Any(a=>a!=null&&found.Contains(a.wallId)))continue;
    foreach(var arm in junction.arms)if(arm!=null&&found.Add(arm.wallId))grew=true;
   }
  }
  return design.items.Where(i=>i.kind=="Wall"&&found.Contains(i.id)).ToList();
 }
 public static Mesh BuildJoinedMesh(IList<Item> connectedWalls,bool partitionByWall=false)=>WallUnionGeometry.Build(connectedWalls,partitionByWall);
 // The caller supplies its pre-edit design and current edited design. This
 // returns an independent revised design; on any protected or unsupported
 // change both inputs remain untouched. Endpoint edits propagate along the
 // junction graph while a T host can remain an unchanged interior anchor.
 public static Design PropagateEdit(Design before,Design edited,string editedWallId){
  if(before==null||edited==null||string.IsNullOrEmpty(editedWallId))throw new Exception("Missing connected wall edit state.");
  var original=before.items?.Find(i=>i.id==editedWallId);var changed=edited.items?.Find(i=>i.id==editedWallId);
  if(original==null||changed==null||original.kind!="Wall"||changed.kind!="Wall")throw new Exception("Connected edit needs the same wall before and after editing.");
  if(JunctionJson(before.wallJunctions)!=JunctionJson(edited.wallJunctions))throw new Exception("Connection topology changed while editing; preview again.");
  var result=JsonUtility.FromJson<Design>(JsonUtility.ToJson(edited));
  var junctions=result.wallJunctions;if(junctions==null||junctions.Count==0)return result;
  var affected=junctions.Where(j=>j.arms.Any(a=>a.wallId==editedWallId)).ToList();
  if(affected.Count==0)return result;
  if(original.locked||changed.locked||!string.IsNullOrEmpty(original.groupId)||!string.IsNullOrEmpty(changed.groupId))throw new Exception("Unlock or ungroup the connected wall before changing its geometry.");
  if(before.linkWallsToRoom&&(Mathf.Abs(original.y-changed.y)>.0001f||Mathf.Abs(original.height-changed.height)>.0001f))throw new Exception("Linked room height must be reviewed before changing connected wall elevation or height.");
  var beforeStart=End(original,"Start");var beforeEnd=End(original,"End");var afterStart=End(changed,"Start");var afterEnd=End(changed,"End");
  var root=result.items.Find(i=>i.id==editedWallId);
  if(root==null)throw new Exception("Edited wall disappeared during propagation.");
  var touched=new HashSet<string>{editedWallId};
  foreach(var junction in affected){
   var ownArm=junction.arms.Find(a=>a.wallId==editedWallId);
   Vector2 target;
   if(ownArm.endpoint=="Interior"){
    var originalPoint=new Vector2(junction.x,junction.z);var axis=beforeEnd-beforeStart;
    float fraction=Vector2.Dot(originalPoint-beforeStart,axis)/Mathf.Max(axis.sqrMagnitude,.000001f);
    target=Vector2.Lerp(afterStart,afterEnd,fraction);
   }else target=End(root,ownArm.endpoint);
   if(Vector2.Distance(target,new Vector2(junction.x,junction.z))<=GeometryEpsilon)continue;
   foreach(var arm in junction.arms){
    if(arm.wallId==editedWallId)continue;
    var neighbor=result.items.Find(i=>i.id==arm.wallId);
    if(neighbor==null)throw new Exception("Connected neighbor no longer exists.");
    if(arm.endpoint=="Interior"){
     if(!OnInterior(neighbor,target))throw new Exception("T host boundary cannot follow this edit; detach or reposition the host explicitly.");
     continue;
    }
    if(neighbor.locked||!string.IsNullOrEmpty(neighbor.groupId))throw new Exception("A connected neighbor is locked or grouped; the edit cannot propagate.");
    MoveEnd(neighbor,arm.endpoint,target);touched.Add(neighbor.id);
   }
   junction.x=target.x;junction.z=target.y;
  }
  ValidateStored(result);
  foreach(var id in touched){
   var cluster=ConnectedCluster(result,id);if(cluster.Count<2)continue;
   var basis=cluster[0];
   foreach(var member in cluster)if(!SamePlanarLayer(basis,member)||!SamePhysicalMaterial(basis,member)||Mathf.Abs(basis.Opacity-member.Opacity)>.0001f||member.doors!=null&&member.doors.Count>0)
    throw new Exception("Connected wall material, height, base or openings changed; detach before this edit.");
  }
  Design.Validate(result);return result;
 }
 static bool OnInterior(Item wall,Vector2 point){
  var a=End(wall,"Start");var b=End(wall,"End");var d=b-a;
  float t=Vector2.Dot(point-a,d)/Mathf.Max(d.sqrMagnitude,.000001f);
  return t>0&&t<1&&Vector2.Distance(a+d*t,point)<=.0002f;
 }
 public static Item ClosestWall(Design design,IEnumerable<string> clusterIds,Vector3 hitPoint){
  if(design==null||design.items==null)return null;
  var ids=new HashSet<string>(clusterIds??Enumerable.Empty<string>());
  Item best=null;float bestDistance=float.PositiveInfinity;var point=new Vector2(hitPoint.x,hitPoint.z);
  foreach(var wall in design.items){
   if(wall.kind!="Wall"||!ids.Contains(wall.id))continue;
   var a=End(wall,"Start");var b=End(wall,"End");var delta=b-a;
   float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/Mathf.Max(delta.sqrMagnitude,.000001f));
   float distance=(point-(a+delta*t)).sqrMagnitude;
   if(distance<bestDistance-1e-8f||Mathf.Abs(distance-bestDistance)<=1e-8f&&best!=null&&string.CompareOrdinal(wall.id,best.id)<0){best=wall;bestDistance=distance;}
  }
  return best;
 }
 public static void ValidateStored(Design design){
  if(design.wallJunctions==null)return;
  if(design.wallJunctions.Count>500)throw new Exception("Too many wall junctions.");
  var ids=new HashSet<string>();var wallById=design.items.Where(i=>i.kind=="Wall").ToDictionary(i=>i.id,i=>i);
  foreach(var junction in design.wallJunctions){
   if(junction==null||string.IsNullOrEmpty(junction.id)||!ids.Add(junction.id)||!Finite(junction.x)||!Finite(junction.z)||Mathf.Abs(junction.x)>10000||Mathf.Abs(junction.z)>10000||
      Array.IndexOf(new[]{"Collinear","Corner","T","X"},junction.kind)<0||junction.arms==null||junction.arms.Count<2||junction.arms.Count>4)
    throw new Exception("Invalid wall junction record.");
   var armIds=new HashSet<string>();
   foreach(var arm in junction.arms){
    if(arm==null||!wallById.TryGetValue(arm.wallId??"",out var wall)||Array.IndexOf(new[]{"Start","End","Interior"},arm.endpoint)<0||!armIds.Add(arm.wallId+":"+arm.endpoint))throw new Exception("Invalid wall junction arm.");
    var point=new Vector2(junction.x,junction.z);
    if(arm.endpoint!="Interior"){
     if(Vector2.Distance(End(wall,arm.endpoint),point)>.0002f)throw new Exception("Connected wall endpoint moved without updating its junction.");
    }else{
     var a=End(wall,"Start");var b=End(wall,"End");var d=b-a;float t=Vector2.Dot(point-a,d)/d.sqrMagnitude;
     if(t<=0||t>=1||Vector2.Distance(a+d*t,point)>.0002f)throw new Exception("Connected host wall no longer passes through its junction.");
    }
   }
  }
 }
}
}
