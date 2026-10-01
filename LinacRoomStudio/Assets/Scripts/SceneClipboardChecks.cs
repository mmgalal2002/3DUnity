using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public static class SceneClipboardChecks {
 static void Need(bool condition,string message){if(!condition)throw new Exception("Scene clipboard regression: "+message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Need(rejected,message);}
 public static void Run(){
  var design=Design.Example();var wall=design.items[0];
  var shape=new Item{kind="Component",name="Custom shape",x=2,z=-1,height=3,component=new ComponentShape{type="Polygon",points=new List<Vector2Data>{new Vector2Data(-1,-1),new Vector2Data(1,-1),new Vector2Data(1,1),new Vector2Data(-1,1)}}};
  design.items.Add(shape);SelectionEditing.Group(design,new[]{wall.id,shape.id});
  wall.doors=new List<DoorOpening>{DoorGeometry.New(0,wall.height)};wall.doors[0].leadLiningMm=5;
  wall.locked=true;
  var payload=SceneClipboard.Copy(design,new[]{wall.id},wall.id);
  Need(payload.items.Count==2&&payload.items.Any(item=>item.id==shape.id),"group selection was not expanded");
  string captured=JsonUtility.ToJson(payload);float sourceX=wall.x;
  wall.doors[0].leadLiningMm=10;shape.component.points[0].x=-2;
  Need(JsonUtility.ToJson(payload)==captured,"copy changed after its source was edited");
  int originalCount=design.items.Count;
  var first=SceneClipboard.Paste(design,payload,1);
  var second=SceneClipboard.Paste(design,payload,2);
  Need(design.items.Count==originalCount+4,"paste count");
  Need(first.items.Count==2&&second.items.Count==2&&first.primaryId==first.items[0].id,"pasted group/primary selection");
  var wallCopy=first.items.Find(item=>item.kind=="Wall");
  var shapeCopy=first.items.Find(item=>item.kind=="Component");
  var secondWall=second.items.Find(item=>item.kind=="Wall");
  Need(wallCopy!=null&&shapeCopy!=null&&secondWall!=null,"group members missing");
  Need(!wallCopy.locked&&!shapeCopy.locked&&wall.locked,"copied lock state");
  Need(wallCopy.id!=wall.id&&shapeCopy.id!=shape.id&&wallCopy.id!=secondWall.id,"fresh object IDs");
  Need(wallCopy.groupId==shapeCopy.groupId&&wallCopy.groupId!=wall.groupId&&wallCopy.groupId!=secondWall.groupId,"fresh independent group IDs");
  Need(Mathf.Abs(wallCopy.x-sourceX-SceneClipboard.PasteOffsetMetres)<.0001f&&Mathf.Abs(secondWall.x-sourceX-2*SceneClipboard.PasteOffsetMetres)<.0001f,"repeated paste offset");
  Need(wallCopy.doors[0].id!=wall.doors[0].id&&secondWall.doors[0].id!=wallCopy.doors[0].id,"fresh door IDs");
  Need(wallCopy.doors[0].leadLiningMm==5&&shapeCopy.component.points[0].x==-1,"nested door/component clone");
  Need(JsonUtility.ToJson(payload)==captured,"paste changed clipboard contents");
  string native=JsonUtility.ToJson(design);var restored=JsonUtility.FromJson<Design>(native);Design.Validate(restored);
  Need(restored.items.Find(item=>item.id==wallCopy.id).doors[0].leadLiningMm==5,"native persistence");
    var linacSource=design.items.Find(item=>item.kind=="LINAC");var linac=SceneClipboard.Copy(design,new[]{linacSource.id},"");
    var firstLinacCopy=SceneClipboard.Paste(design,linac,1);var secondLinacCopy=SceneClipboard.Paste(design,linac,2);
    Need(firstLinacCopy.items.Count==1&&secondLinacCopy.items.Count==1&&firstLinacCopy.items[0].id!=linacSource.id&&firstLinacCopy.items[0].id!=secondLinacCopy.items[0].id,"repeated LINAC paste");
    native=JsonUtility.ToJson(design);restored=JsonUtility.FromJson<Design>(native);Design.Validate(restored);
    Need(restored.items.Count(item=>item.kind=="LINAC")==3,"multiple LINAC native persistence");
  var edge=Design.Example();edge.items[0].x=9999.8f;var edgeCopy=SceneClipboard.Copy(edge,new[]{edge.items[0].id},"");
  native=JsonUtility.ToJson(edge);Reject(()=>SceneClipboard.Paste(edge,edgeCopy,1),"out-of-range paste accepted");Need(JsonUtility.ToJson(edge)==native,"out-of-range paste mutation");
  while(edge.items.Count<250)edge.items.Add(new Item{kind="Desk"});
  native=JsonUtility.ToJson(edge);Reject(()=>SceneClipboard.Paste(edge,edgeCopy,1),"capacity overflow accepted");Need(JsonUtility.ToJson(edge)==native,"capacity rejection mutation");
  Reject(()=>SceneClipboard.Copy(design,Array.Empty<string>(),""),"empty copy accepted");
    Debug.Log("ROOM_STUDIO_SCENE_CLIPBOARD_CHECKS_PASSED: grouped locked copies, fresh IDs, doors/components, repeated offsets, multiple LINAC paste, native persistence and atomic range/capacity rejections");
 }
}
}
