using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public static class DoorChecks {
 static void Need(bool pass,string message){if(!pass)throw new Exception("Door check: "+message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch(Exception){rejected=true;}Need(rejected,message);}
 public static void Run(){
    ShortWallChecks();
  var old=JsonUtility.FromJson<Design>(JsonUtility.ToJson(Design.Example()));Design.Validate(old);
  Need(old.items.All(i=>i.doors==null||i.doors.Count==0),"older designs gained a door");
  var d=Design.Example();var wall=d.items[0];var door=DoorGeometry.New(0,wall.height);
  DoorGeometry.Add(wall,door);Design.Validate(d);
  Need(wall.doors.Count==1&&door.width==1&&door.panel.material=="Steel"&&door.leadLiningMm==0,"default door");
  var full=DoorGeometry.Solids(wall,wall.height);var cut=DoorGeometry.Solids(wall,.35f);
  Need(full.Count==3&&cut.Count==2,"solid pier/lintel count");
  Need(full.Count(s=>s.bottom==0)==2&&full.Any(s=>Mathf.Abs(s.bottom-door.height)<.0001f),"real opening below lintel");
  Need(cut.All(s=>s.center+s.width/2<=-.5f+.0001f||s.center-s.width/2>=.5f-.0001f),"cutaway has wall across door");
  var state=JsonUtility.ToJson(wall);
  Reject(()=>DoorGeometry.Add(wall,DoorGeometry.New(.25f,wall.height)),"overlap accepted");
  Need(JsonUtility.ToJson(wall)==state,"failed add changed wall");
  var replacement=JsonUtility.FromJson<DoorOpening>(JsonUtility.ToJson(door));replacement.center=5;
  Reject(()=>DoorGeometry.Update(wall,door.id,replacement),"door beyond wall accepted");
  Need(JsonUtility.ToJson(wall)==state,"failed edit changed wall");
  replacement.center=1.25f;replacement.panel.material="Wood";replacement.panel.density=700;replacement.leadLiningMm=5;
  DoorGeometry.Update(wall,door.id,replacement);Need(wall.doors[0].center==1.25f&&wall.doors[0].leadLiningMm==5,"physical edit");
  var point=new Vector3(wall.x,wall.y,wall.z)+Quaternion.Euler(0,wall.angle,0)*new Vector3(1.25f,0,0);
  Need(Mathf.Abs(DoorGeometry.LocalCenter(wall,point)-1.25f)<.0001f,"click position / rotation");
  Reject(()=>DoorGeometry.LocalCenter(wall,point+Quaternion.Euler(0,wall.angle,0)*Vector3.forward),"off-wall click accepted");
  string json=JsonUtility.ToJson(d);var loaded=JsonUtility.FromJson<Design>(json);Design.Validate(loaded);
  var restored=loaded.items[0].doors[0];Need(restored.id==door.id&&restored.panel.material=="Wood"&&restored.panel.density==700&&restored.leadLiningMm==5,"native roundtrip");
  float beforeDepth=d.depth,beforeLength=wall.length;
  Reject(()=>Design.ResizeRoom(d,d.width,3,d.height),"linked resize clipped door");
  Need(d.depth==beforeDepth&&wall.length==beforeLength,"failed linked resize mutated design");
  wall.locked=true;state=JsonUtility.ToJson(wall);
  Reject(()=>DoorGeometry.Add(wall,DoorGeometry.New(-2,wall.height)),"protected wall accepted door");
  Reject(()=>DoorGeometry.Update(wall,door.id,door),"protected wall accepted edit");
  Reject(()=>DoorGeometry.Remove(wall,door.id),"protected wall removed door");
  Need(JsonUtility.ToJson(wall)==state,"protected operation changed wall");
  wall.locked=false;DoorGeometry.Remove(wall,door.id);Need(wall.doors.Count==0&&DoorGeometry.Solids(wall,wall.height).Count==1,"door removal restored solid wall");
  Debug.Log("ROOM_STUDIO_DOOR_GEOMETRY_CHECKS_PASSED");
 }
 static void ShortWallChecks(){
  foreach(float length in new[]{.25f,.5f,.8f,2f}){
   var wall=Design.Wall("Short wall",2.37f,-1.237f,length,37,3);var requested=DoorGeometry.New(length*.25f,wall.height);requested.width=length<1?1:3;
   string original=JsonUtility.ToJson(requested);DoorGeometry.Add(wall,requested);var placed=wall.doors.Single();
   Need(Mathf.Abs(placed.width-length*.9f)<.00001f,"oversized door was not fitted to 90 percent");
   Need(placed.center-placed.width/2>=-length/2-.00001f&&placed.center+placed.width/2<=length/2+.00001f,"fitted door lies outside its wall");
   Need(JsonUtility.ToJson(requested)==original,"fitting mutated the requested door record");
   DoorGeometry.ValidateWall(wall);Need(DoorGeometry.Solids(wall,.35f).Count==2,"fitted door lost its end jambs");
   var loaded=JsonUtility.FromJson<Item>(JsonUtility.ToJson(wall));DoorGeometry.ValidateWall(loaded);Need(loaded.doors[0].width==placed.width,"fitted door native roundtrip");
   wall.locked=true;string before=JsonUtility.ToJson(wall);Reject(()=>DoorGeometry.Add(wall,requested),"fitting changed a protected wall");Need(before==JsonUtility.ToJson(wall),"protected fitting mutation");
  }
  Need(DoorGeometry.PlacementWidth(1,1)==1,"equal-width request was silently fitted");
  Need(DoorGeometry.PlacementWidth(.95f,1)==.95f,"narrower request was silently fitted");
  var normal=Design.Wall("Normal wall",0,0,2,0,3);var door=DoorGeometry.New(.2f,3);door.width=1.7f;door.center=0;
  DoorGeometry.Add(normal,door);Need(normal.doors[0].width==1.7f,"valid requested width was not retained");
  string unchanged=JsonUtility.ToJson(normal);Reject(()=>DoorGeometry.Add(normal,DoorGeometry.New(0,3)),"fitting bypassed overlap validation");Need(unchanged==JsonUtility.ToJson(normal),"overlap rejection mutated wall");
  Debug.Log("ROOM_STUDIO_SHORT_WALL_DOOR_CHECKS_PASSED: 0.25/0.5/0.8/2 m walls, 90-percent fitting only for oversized requests, finite jambs, unchanged requests, locks, overlap and native persistence");
 }
}
}
