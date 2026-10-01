using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public sealed class JoinedWallSelection : MonoBehaviour { public string[] wallIds; }

public partial class StudioApp {
 WallConnectionPlan wallConnectionPreview;
 string wallConnectionSelection="";
 float wallConnectionTolerance=WallConnections.DefaultTolerance;
 bool wallConnectionCrossCategory;

 bool IsJoinedWall(string id)=>design.wallJunctions!=null&&design.wallJunctions.Any(j=>j.arms!=null&&j.arms.Any(a=>a.wallId==id));

 void BuildJoinedWallMeshes(){
  if(design.wallJunctions==null||design.wallJunctions.Count==0)return;
  var rendered=new HashSet<string>();
  foreach(var junction in design.wallJunctions)foreach(var arm in junction.arms){
   if(!rendered.Add(arm.wallId))continue;
   var cluster=WallConnections.ConnectedCluster(design,arm.wallId);
   if(cluster.Count<2)continue;
   foreach(var member in cluster)rendered.Add(member.id);
   var visible=cluster.Select(i=>JsonUtility.FromJson<Item>(JsonUtility.ToJson(i))).ToList();
   if(cutaway)foreach(var wall in visible)wall.height=Mathf.Min(.35f,wall.height);
  var mesh=WallConnections.BuildJoinedMesh(visible,true);componentMeshes.Add(mesh);
   var go=new GameObject("Joined physical walls");go.transform.SetParent(world,false);
   go.AddComponent<MeshFilter>().sharedMesh=mesh;
  go.AddComponent<MeshRenderer>().sharedMaterials=cluster.Select(wall=>{
   if(selection.Contains(wall.id))return selectedMat;
   if(WallGenerationData.IsEmpty(wall.generated))return wallMat;
   var material=Material(wall.generated.displayColor);appearanceMaterials.Add(material);return material;
  }).ToArray();
   go.AddComponent<MeshCollider>().sharedMesh=mesh;
   go.AddComponent<JoinedWallSelection>().wallIds=cluster.Select(i=>i.id).ToArray();
   if(cluster.All(i=>Mathf.Abs(i.Opacity-cluster[0].Opacity)<.0001f))ApplyOpacity(go,cluster[0].Opacity);
  }
 }

 Bounds JoinedWallBounds(Item wall){
  float h=cutaway?Mathf.Min(.35f,wall.height):wall.height;
  var axis=Quaternion.Euler(0,wall.angle,0);
  var first=new Vector3(-wall.length/2,0,-wall.shielding.thickness/2000f);
  var bounds=new Bounds(new Vector3(wall.x,wall.y,wall.z)+axis*first,Vector3.zero);
  foreach(float x in new[]{-wall.length/2,wall.length/2})foreach(float y in new[]{0,h})foreach(float z in new[]{-wall.shielding.thickness/2000f,wall.shielding.thickness/2000f})
   bounds.Encapsulate(new Vector3(wall.x,wall.y,wall.z)+axis*new Vector3(x,y,z));
  return bounds;
 }

 void PropagateObjectPanelEdit(Design before){
  if(before==null)return;
  var changed=before.items.Where(old=>IsJoinedWall(old.id)&&design.items.Any(item=>item.id==old.id&&JsonUtility.ToJson(item)!=JsonUtility.ToJson(old))).ToList();
  if(changed.Count==0)return;
  if(changed.Count!=1||selection.Count!=1||!selection.Contains(changed[0].id))throw new Exception("Edit one connected wall at a time, or detach its junctions first.");
  design=WallConnections.PropagateEdit(before,design,changed[0].id);
 }

 void WallConnectionUI(){
  var selectedWalls=SelectedItems.Where(i=>i.kind=="Wall").ToArray();
  if(selectedWalls.Length<2&&wallConnectionPreview==null&&!SelectedItems.Any(i=>IsJoinedWall(i.id)))return;
  Section("Connect wall edges");
  bool oldChanged=GUI.changed;GUI.changed=false;
  float previousTolerance=wallConnectionTolerance;bool previousCrossCategory=wallConnectionCrossCategory;
  wallConnectionTolerance=Number("Connection tolerance",wallConnectionTolerance,.000001f,1,"m",-1);
  wallConnectionCrossCategory=GUILayout.Toggle(wallConnectionCrossCategory," Permit different categories");
  if(previousTolerance!=wallConnectionTolerance||previousCrossCategory!=wallConnectionCrossCategory){wallConnectionPreview=null;status="Connection settings changed. Preview the joins again.";}
  GUILayout.Label("Preview computes physical joins in metres. Grid and wall snapping remain independent.",small);
  GUI.changed=oldChanged;
  bool oldEnabled=GUI.enabled;
  GUI.enabled=oldEnabled&&selectedWalls.Length>=2&&selectedWalls.Length==SelectedItems.Count;
  if(Btn("Preview connect wall edges")){
   try{
    foreach(var wall in selectedWalls.Where(w=>!WallGenerationData.IsEmpty(w.generated))){
     var source=WallGenerationData.FindBatch(design,wall.generated.batchId)?.sourceSnapshot;
     if(source?.authoring?.rules!=null&&source.authoring.rules.Any(r=>r.enabled&&r.classification=="Opening"))
      throw new Exception("This generated source declares opening markers. Repreview generation before connecting its walls so openings cannot be bridged.");
    }
    wallConnectionPreview=WallConnections.Preview(design,selectedWalls.Select(i=>i.id),wallConnectionTolerance,wallConnectionCrossCategory);
    wallConnectionSelection=string.Join("|",selectedWalls.Select(i=>i.id).OrderBy(id=>id,StringComparer.Ordinal));
    status=wallConnectionPreview.HasChanges?"Inspect the proposed joins, then Apply or Cancel.":"No compatible joins found. Review diagnostics and tolerance.";
    Rebuild();
   }catch(Exception e){wallConnectionPreview=null;status=e.Message;}
   GUI.changed=false;
   GUIUtility.ExitGUI();
  }
  GUI.enabled=oldEnabled;
  if(wallConnectionPreview!=null){
   GUILayout.Label(wallConnectionPreview.junctions.Count+" proposed joins · "+wallConnectionPreview.replacements.Count+" adjusted walls · "+wallConnectionPreview.additions.Count+" split segments",small);
   foreach(var message in wallConnectionPreview.diagnostics.Take(8))GUILayout.Label(message,small);
   GUILayout.BeginHorizontal();
   if(Btn("Apply wall joins")){
    try{
     var chosen=string.Join("|",selectedWalls.Select(i=>i.id).OrderBy(id=>id,StringComparer.Ordinal));
     if(chosen!=wallConnectionSelection)throw new Exception("Selection changed; preview the connection again.");
     var next=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
     WallConnections.Apply(next,wallConnectionPreview);
     design=next;wallConnectionPreview=null;Commit();Rebuild();status="Wall edges connected in one undo step.";
    }catch(Exception e){status=e.Message;}
    GUI.changed=false;
    GUIUtility.ExitGUI();
   }
   if(Btn("Cancel connection")){wallConnectionPreview=null;Rebuild();GUI.changed=false;GUIUtility.ExitGUI();}
   GUILayout.EndHorizontal();
  }
  if(SelectedItems.Any(i=>IsJoinedWall(i.id))&&Btn("Detach selected wall joins")){
   try{var next=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));WallConnections.Detach(next,selection);Design.Validate(next);
    design=next;wallConnectionPreview=null;Commit();Rebuild();status="Selected wall junctions detached in one undo step.";
   }catch(Exception e){status=e.Message;}
   GUI.changed=false;
   GUIUtility.ExitGUI();
  }
 }

 void WallConnectionAfterRebuild(){
  if(wallConnectionPreview==null||world==null)return;
  var root=new GameObject("Wall connection preview (not committed)").transform;root.SetParent(world,false);
  var previewMaterial=Material(new Color(.17f,.86f,.54f,.45f));appearanceMaterials.Add(previewMaterial);
  foreach(var item in wallConnectionPreview.replacements.Concat(wallConnectionPreview.additions)){
   float shown=cutaway?Mathf.Min(.35f,item.height):item.height;
   var cube=Cube("Proposed "+item.name,new Vector3(item.x,item.y+shown/2,item.z),new Vector3(item.length,shown,item.shielding.thickness/1000f),previewMaterial,root);
   cube.transform.rotation=Quaternion.Euler(0,item.angle,0);Destroy(cube.GetComponent<Collider>());
  }
  foreach(var junction in wallConnectionPreview.junctions){
   var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Proposed junction";dot.transform.SetParent(root,false);
   dot.transform.position=new Vector3(junction.x,.42f,junction.z);dot.transform.localScale=Vector3.one*.16f;
   dot.GetComponent<Renderer>().sharedMaterial=previewMaterial;Destroy(dot.GetComponent<Collider>());
  }
 }

 void WallConnectionSceneSmokeChecks(){
  string original=JsonUtility.ToJson(design);
  try{
   design=Design.Example();design.linkWallsToRoom=false;ClearSelection();
   var a=Design.Wall("Join A",-1,0,2,0,3);var b=Design.Wall("Join B",1.015f,0,2,0,3);
   design.items.Add(a);design.items.Add(b);Commit();Rebuild();
   string before=JsonUtility.ToJson(design);
   var proposal=WallConnections.Preview(design,new[]{a.id,b.id});
   if(!proposal.HasChanges||JsonUtility.ToJson(design)!=before)throw new Exception("Existing-wall preview changed the room or missed a 0.015 m gap.");
   var next=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));WallConnections.Apply(next,proposal);
   design=next;Commit();Rebuild();Design.Validate(design);
   var joined=world.GetComponentsInChildren<JoinedWallSelection>();
   if(joined.Length!=1||joined[0].GetComponent<MeshCollider>()==null||joined[0].wallIds.Length!=2)throw new Exception("A physical joined mesh/collider was not built exactly once.");
   if(WallConnections.ClosestWall(design,joined[0].wallIds,new Vector3(-1,1,0)).id!=a.id)throw new Exception("Joined-wall picking lost individual wall identity.");
   Choose(a.id);float initial=design.wallJunctions[0].x;
   NudgeSelection(Vector3.right);
   Design.Validate(design);
   if(Mathf.Abs(design.wallJunctions[0].x-initial-Precision.moveStep)>.0001f)throw new Exception("Nudging a joined wall did not propagate to its neighbor.");
   Undo(-1);if(Mathf.Abs(design.wallJunctions[0].x-initial)>.0001f)throw new Exception("Joined-wall nudge undo failed.");
   Undo(1);if(Mathf.Abs(design.wallJunctions[0].x-initial-Precision.moveStep)>.0001f)throw new Exception("Joined-wall nudge redo failed.");
   WallConnections.Detach(design,new[]{a.id});Commit();Rebuild();
   if(design.wallJunctions.Count!=0||world.GetComponentsInChildren<JoinedWallSelection>().Length!=0)throw new Exception("Detach left physical junction geometry in the scene.");
   Debug.Log("ROOM_STUDIO_WALL_CONNECTION_RUNTIME_PASSED: existing selection preview, union mesh/collider, individual picking, propagated 0.01 m nudge, undo/redo and detach");
  }finally{wallConnectionPreview=null;design=JsonUtility.FromJson<Design>(original);ClearSelection();Commit();Rebuild();}
 }
}
}
