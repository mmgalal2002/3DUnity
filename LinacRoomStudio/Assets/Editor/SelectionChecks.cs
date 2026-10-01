using System;
using System.Linq;
using RoomStudio;
using UnityEngine;

public static class SelectionChecks {
 static void Require(bool condition,string message){if(!condition)throw new Exception("Selection regression: "+message);}
 public static void Run(){
  LockChecks();
    ResetChecks();
  var d=Design.Example();var a=d.items[5];var b=d.items[6];var untouched=d.items[0];float untouchedX=untouched.x;
  SelectionEditing.Group(d,new[]{a.id,b.id});Require(a.groupId==b.groupId&&!string.IsNullOrEmpty(a.groupId),"group membership");
  var expanded=SelectionEditing.Expand(d,new[]{b.id});Require(expanded.SetEquals(new[]{a.id,b.id}),"select member expands whole group");
  var items=d.items.Where(i=>expanded.Contains(i.id)).ToList();var delta=new Vector3(b.x-a.x,b.y-a.y,b.z-a.z);var center=SelectionEditing.Center(items);
  SelectionEditing.Move(d,items,new Vector3(2,1,-3));Require(Vector3.Distance(new Vector3(b.x-a.x,b.y-a.y,b.z-a.z),delta)<.0001f,"move preserves offsets");Require(Vector3.Distance(SelectionEditing.Center(items),center+new Vector3(2,1,-3))<.0001f,"group movement");Require(untouched.x==untouchedX,"unselected object changed");
  center=SelectionEditing.Center(items);float spacing=delta.magnitude;SelectionEditing.Rotate(items,90);Require(Vector3.Distance(SelectionEditing.Center(items),center)<.0001f,"rotation pivot");Require(Mathf.Abs(new Vector3(b.x-a.x,b.y-a.y,b.z-a.z).magnitude-spacing)<.0001f,"rotation preserves spacing");Require(a.angle==90&&b.angle==90,"member rotations");
  a.Opacity=.25f;b.Opacity=0;string json=JsonUtility.ToJson(d);var restored=JsonUtility.FromJson<Design>(json);Design.Validate(restored);Require(restored.items[5].Opacity==.25f&&restored.items[6].Opacity==0,"opacity roundtrip including zero");Require(restored.items[5].groupId==a.groupId,"group roundtrip");
  var old=JsonUtility.FromJson<Item>("{\"id\":\"legacy\",\"kind\":\"Wall\"}");Require(old.Opacity==1&&string.IsNullOrEmpty(old.groupId),"old object defaults");
  string before=JsonUtility.ToJson(d);bool rejected=false;try{SelectionEditing.Move(d,items,new Vector3(20000,0,0));}catch{rejected=true;}Require(rejected&&before==JsonUtility.ToJson(d),"atomic bounds rejection");
  rejected=false;try{SelectionEditing.Move(d,new[]{d.items[0],a},Vector3.up);}catch{rejected=true;}Require(rejected&&before==JsonUtility.ToJson(d),"linked wall vertical guard");
  SelectionEditing.Group(d,new[]{a.id,untouched.id});Require(SelectionEditing.Expand(d,new[]{a.id}).Count==3,"merging groups includes all members");
  SelectionEditing.Ungroup(d,new[]{b.id});Require(d.items.All(i=>string.IsNullOrEmpty(i.groupId)),"ungroup clears all members");
  SelectionEditing.Group(d,new[]{a.id,b.id});SelectionEditing.Remove(d,new[]{b.id});Require(d.items.Count==5&&!d.items.Contains(a)&&!d.items.Contains(b),"group deletion");
  restored.items[5].transparency=float.NaN;rejected=false;try{Design.Validate(restored);}catch{rejected=true;}Require(rejected,"invalid opacity rejected");
  Debug.Log("ROOM_STUDIO_SELECTION_CHECKS_PASSED: grouping, expansion, movement, rotation, persistence, legacy defaults, atomic bounds, linked walls, ungrouping, deletion and opacity validation");
 }
 public static void ResetChecks(){
  var design=CtShieldingChecks.DemoFixture();design.ct.results.Add(CtShieldingCalculation.Calculate(design));
  design.items.Add(new Item{kind="Component",component=new ComponentShape(),height=3});design.items.Add(new Item{kind="Desk",scale=.33f});
  design.regions.Add(new RegionItem{points=new System.Collections.Generic.List<Vector2Data>{new Vector2Data(-1,-1),new Vector2Data(1,-1),new Vector2Data(1,1)}});
  design.floorPlan=new FloorPlanData{kind="Vector",widthMeters=4,heightMeters=4,segments=new System.Collections.Generic.List<FloorPlanSegment>{new FloorPlanSegment{start=new Vector2Data(-1,0),end=new Vector2Data(1,0)}}};
  var wall=design.items.Find(item=>item.kind=="Wall");DoorGeometry.Add(wall,DoorGeometry.New(0,wall.height));
  foreach(var item in design.items)item.locked=true;
  design.editing=new EditingPreferences{gridSnap=true,gridSpacing=.125f,moveStep=.017f};Design.Validate(design);
  string original=JsonUtility.ToJson(design);var reset=Design.ResetContents(design);Design.Validate(reset);
  Require(JsonUtility.ToJson(design)==original,"reset mutated the original room/history state");
  Require(reset.items.Count==0&&reset.regions.Count==0&&reset.generationBatches.Count==0&&reset.wallJunctions.Count==0,"reset retained scene contents/topology");
  Require(Design.IsEmptyFloorPlan(reset.floorPlan)&&!CtShieldingData.Active(reset.ct),"reset retained guide or calculation state");
  Require(reset.width==design.width&&reset.depth==design.depth&&reset.height==design.height,"reset changed ordinary room dimensions");
  Require(JsonUtility.ToJson(reset.floor)==JsonUtility.ToJson(design.floor)&&JsonUtility.ToJson(reset.ceiling)==JsonUtility.ToJson(design.ceiling),"reset changed floor/ceiling settings");
  Require(JsonUtility.ToJson(reset.editing)==JsonUtility.ToJson(design.editing),"reset changed precision preferences");
  var restored=JsonUtility.FromJson<Design>(JsonUtility.ToJson(reset));Design.Validate(restored);Require(restored.items.Count==0&&restored.regions.Count==0&&!CtShieldingData.Active(restored.ct),"empty room native persistence");
  var undone=JsonUtility.FromJson<Design>(original);Design.Validate(undone);Require(undone.items.Count==design.items.Count&&undone.ct.results.Count==1&&undone.items.All(item=>item.locked),"reset history snapshot lost protected objects/results");
  design.sourceJson="{}";design.sourceProjectionJson="{}";design.importSummary="Imported fixture";design.width=1;design.depth=2;design.height=1;Design.Validate(design);
  reset=Design.ResetContents(design);Require(reset.sourceJson==""&&reset.sourceProjectionJson==""&&reset.importSummary=="","reset retained old source import links");
  Require(reset.width==3&&reset.depth==3&&reset.height==2,"small imported room did not use native minimum dimensions");
  design.width=float.NaN;string invalid=JsonUtility.ToJson(design);bool rejected=false;try{Design.ResetContents(design);}catch{rejected=true;}
  Require(rejected&&JsonUtility.ToJson(design)==invalid,"invalid reset candidate changed the active design");
  Debug.Log("ROOM_STUDIO_RESET_CHECKS_PASSED: detached atomic reset, protected contents, doors/shapes/points/regions/guide/history removal, preserved room/settings/preferences, native persistence, source links and imported minimum bounds");
 }
 static void RejectUnchanged(Design d,Action action,string message){
  string before=JsonUtility.ToJson(d);bool rejected=false;try{action();}catch{rejected=true;}
  Require(rejected&&before==JsonUtility.ToJson(d),message);
 }
 static void LockChecks(){
  var d=Design.Example();var a=d.items[5];var b=d.items[6];var ids=new[]{a.id,b.id};
  a.locked=true;
  RejectUnchanged(d,()=>SelectionEditing.Move(d,new[]{b,a},Vector3.right),"mixed selection atomic move protection");
  RejectUnchanged(d,()=>SelectionEditing.Rotate(new[]{b,a},15),"mixed selection atomic rotation protection");
  RejectUnchanged(d,()=>SelectionEditing.Remove(d,ids),"mixed selection atomic deletion protection");
  RejectUnchanged(d,()=>SelectionEditing.Group(d,ids),"protected group edits");
  Require(SelectionEditing.ToggleLock(d,ids)&&a.locked&&b.locked,"mixed selection locks all");
  Require(!SelectionEditing.ToggleLock(d,ids)&&!a.locked&&!b.locked,"locked selection unlocks all");
  SelectionEditing.Group(d,ids);SelectionEditing.ToggleLock(d,new[]{b.id});
  Require(a.locked&&b.locked,"locking expands group");
  Require(SelectionEditing.Expand(d,new[]{b.id}).Count==2,"protected groups remain selectable");
  RejectUnchanged(d,()=>SelectionEditing.Remove(d,new[]{b.id}),"protected group deletion");
  RejectUnchanged(d,()=>SelectionEditing.Ungroup(d,new[]{b.id}),"protected group ungrouping");
  var copy=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));Design.Migrate(copy);Design.Validate(copy);
  Require(copy.version==2&&copy.schemaVersion==2&&copy.items[5].locked&&copy.items[6].locked,"native lock roundtrip without schema bump");
  string oldJson=JsonUtility.ToJson(Design.Example()).Replace(",\"locked\":false","");
  copy=JsonUtility.FromJson<Design>(oldJson);Design.Migrate(copy);Design.Validate(copy);Require(copy.items.All(i=>!i.locked),"old version-2 missing lock defaults");
  copy=JsonUtility.FromJson<Design>(oldJson);copy.version=1;copy.schemaVersion=1;copy.floor.thickness/=1000;copy.ceiling.thickness/=1000;foreach(var item in copy.items)item.shielding.thickness/=1000;
  Design.Migrate(copy);Design.Validate(copy);Require(copy.items.All(i=>!i.locked)&&copy.items[0].shielding.thickness==150,"legacy v1 migration and lock defaults");
  d.items[0].locked=true;
  RejectUnchanged(d,()=>Design.ResizeRoom(d,24,10,5),"protected linked wall width");
  RejectUnchanged(d,()=>Design.ResizeRoom(d,12,20,5),"protected linked wall depth");
  RejectUnchanged(d,()=>Design.ResizeRoom(d,12,10,7),"protected linked wall ceiling and other wall height");
  string wallJson=JsonUtility.ToJson(d.items[0]);d.linkWallsToRoom=false;Design.ResizeRoom(d,24,20,7);Require(wallJson==JsonUtility.ToJson(d.items[0]),"unlinked room leaves protected wall unchanged");
  d.linkWallsToRoom=true;RejectUnchanged(d,()=>Design.ResizeRoom(d,d.width,d.depth,d.height),"relink cannot change protected wall");
  SelectionEditing.ToggleLock(d,new[]{a.id});SelectionEditing.Move(d,new[]{a,b},Vector3.right);SelectionEditing.Remove(d,new[]{b.id});Require(d.items.Count==5,"unlock restores movement and deletion");
  Debug.Log("ROOM_STUDIO_LOCK_CHECKS_PASSED: mixed/group atomic protection, lock toggle, v1/v2 compatibility, save/load and linked room guards");
 }
}
