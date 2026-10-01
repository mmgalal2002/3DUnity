using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 static void DoorNeed(bool pass,string message){if(!pass)throw new Exception("Door runtime check: "+message);}
 void CheckDoorRender(Item wall,DoorOpening door,bool expectLintel,bool expectLead){
  var root=objects[wall.id].transform;
  bool panel=false,lead=false,marker=false,lintel=false;
  foreach(Transform part in root){
   if(part.name=="Wall solid"){
    float low=part.localPosition.x-part.localScale.x/2,high=part.localPosition.x+part.localScale.x/2;
    float bottom=part.localPosition.y-part.localScale.y/2;
    bool crossesDoor=low<door.center+door.width/2-.0001f&&high>door.center-door.width/2+.0001f;
    if(crossesDoor){DoorNeed(bottom>=door.height-.0001f,"wall solid seals the opening");lintel=true;}
   }
   if(part.name.StartsWith(door.name+" · "+door.panel.material+" panel"))panel=part.GetComponent<Collider>()!=null;
   if(part.name.Contains("lead lining"))lead=part.GetComponent<Collider>()!=null;
   if(part.name=="Door opening plan marker")marker=true;
  }
  DoorNeed(panel,"door panel not rendered");DoorNeed(lead==expectLead,"lead lining rendering does not match entered thickness");
  DoorNeed(lintel==expectLintel,"lintel differs from full/cutaway view");
  DoorNeed(marker==top,"top-plan marker missing or shown in 3D");
 }
 void DoorSmokeChecks(){
  string previous=JsonUtility.ToJson(design),oldTool=tool,oldActive=activeDoorId;
  bool oldTop=top,oldCutaway=cutaway;
  try{
   DoorChecks.Run();
   var wall=design.items.First(i=>i.kind=="Wall");Choose(wall.id);top=true;cutaway=true;tool="Door";
   var point=new Vector3(wall.x,wall.y,wall.z)+Quaternion.Euler(0,wall.angle,0)*Vector3.right*1.25f;
   string beforePlacement=JsonUtility.ToJson(design);PlaceDoorAtPoint(point);
   // The full suite can fill the bounded 80-entry history; its index then stays at 79.
   DoorNeed(tool=="Select"&&historyIndex>0&&history[historyIndex-1]==beforePlacement&&history[historyIndex]==JsonUtility.ToJson(design),"click placement did not commit one history step: "+status);
   DoorNeed(wall.doors!=null&&wall.doors.Count==1&&Mathf.Abs(wall.doors[0].center-1.25f)<.0001f,"click did not set local offset");
   string doorId=wall.doors[0].id;CheckDoorRender(wall,wall.doors[0],false,false);
   Undo(-1);DoorNeed(design.items.First(i=>i.id==wall.id).doors==null||design.items.First(i=>i.id==wall.id).doors.Count==0,"undo retained door");
   Undo(1);wall=design.items.First(i=>i.id==wall.id);DoorNeed(wall.doors.Count==1&&wall.doors[0].id==doorId,"redo lost door");
   var lined=JsonUtility.FromJson<DoorOpening>(JsonUtility.ToJson(wall.doors[0]));lined.leadLiningMm=2;DoorGeometry.Update(wall,doorId,lined);Commit();Rebuild();
   CheckDoorRender(wall,wall.doors[0],false,true);
   cutaway=false;Rebuild();CheckDoorRender(wall,wall.doors[0],true,true);
   top=false;Rebuild();CheckDoorRender(wall,wall.doors[0],true,true);
   top=true;cutaway=true;Rebuild();
   string saved=JsonUtility.ToJson(design,true);LoadJson(saved,"Door-smoke");
   wall=design.items.First(i=>i.id==wall.id);DoorNeed(wall.doors.Count==1&&wall.doors[0].panel.material=="Steel"&&wall.doors[0].leadLiningMm==2,"native load lost door shielding");
   wall.locked=true;string protectedJson=JsonUtility.ToJson(wall);
   try{SelectionEditing.Move(design,new[]{wall},Vector3.right);throw new Exception("protected wall moved");}catch(Exception e){if(e.Message=="protected wall moved")throw;}
   try{PrecisionEditing.ResizeLength(wall,wall.length-1,"Center");throw new Exception("protected wall resized");}catch(Exception e){if(e.Message=="protected wall resized")throw;}
   tool="Door";PlaceDoorAtPoint(point);
   DoorNeed(JsonUtility.ToJson(wall)==protectedJson&&wall.doors.Count==1,"protected click changed door");
   try{DoorGeometry.Remove(wall,doorId);throw new Exception("protected door removed");}catch(Exception e){if(e.Message=="protected door removed")throw;}
   DoorNeed(JsonUtility.ToJson(wall)==protectedJson,"protected removal changed wall");
   Debug.Log("ROOM_STUDIO_DOOR_RUNTIME_PASSED: click, physical opening, cutaway, material and lead layer, native load, undo/redo, protection");
  }finally{
   design=JsonUtility.FromJson<Design>(previous);ClearSelection();tool=oldTool;activeDoorId=oldActive;top=oldTop;cutaway=oldCutaway;Commit();Rebuild();
  }
 }
}
}
