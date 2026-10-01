using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
// Same numerical fixtures run in the editor, Mono player and IL2CPP/WebGL player.
public static class PrecisionChecks {
 static void Require(bool value,string message){if(!value)throw new Exception("Precision regression: "+message);}
 static void Near(float a,float b,string message){Require(Math.Abs(a-b)<=.0001f,message+" ("+a+" vs "+b+")");}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Require(rejected,message);}
 public static void Run(){
    LayoutChecks();
  EquipmentScaleChecks();
  WallAngleChecks();
  EquipmentScaleShortcutChecks();
  var p=new EditingPreferences();PrecisionEditing.Validate(p);
  Require(!p.gridSnap&&!p.wallSnap&&p.gridVisible,"independent new-design defaults");Near(p.moveStep,.01f,"move default");Near(p.resizeStep,.01f,"resize default");
  foreach(float bad in new[]{0,-1,float.NaN,float.PositiveInfinity}){var invalid=new EditingPreferences{moveStep=bad};Reject(()=>PrecisionEditing.Validate(invalid),"invalid increment accepted");}
  foreach(string invalid in new[]{"","-","1.","1e","1e-","NaN","Infinity","-1"})Require(!PrecisionEditing.TryNumber(invalid,0,10,out _),"incomplete/invalid entry: "+invalid);
  Require(PrecisionEditing.TryNumber("1.237",0,10,out float exact),"exact input");Near(exact,1.237f,"exact fractional input");
  Near(PrecisionEditing.Step(2.37f,.01f,1),2.38f,"fractional nudge");Near(PrecisionEditing.Step(-2.37f,.01f,-1),-2.38f,"negative nudge");
  float moved=2.37f;for(int n=0;n<100;n++)moved=PrecisionEditing.Step(moved,.01f,1);Near(moved,3.37f,"100 nudges total 1 m");
  Near(PrecisionEditing.Step(150,.1f,1),150.1f,"millimetre thickness increment");Near(PrecisionEditing.Step(1,.01f,1),1.01f,"dimensionless scale increment");
  Near(PrecisionEditing.Grid(.25f,.1f),.3f,"positive grid half");Near(PrecisionEditing.Grid(-.25f,.1f),-.3f,"negative grid half");
  var wall=Design.Wall("Test",2.37f,-1.237f,2.37f,37,5);var items=new List<Item>{wall};
  foreach(string anchor in new[]{"Start","Center","End"}){
   var copy=JsonUtility.FromJson<Item>(JsonUtility.ToJson(wall));var start=PrecisionEditing.Endpoint(copy,false);var end=PrecisionEditing.Endpoint(copy,true);
   PrecisionEditing.ResizeLength(copy,2.38f,anchor);Near(copy.length,2.38f,"fractional length");
   if(anchor=="Start")Near(Vector3.Distance(start,PrecisionEditing.Endpoint(copy,false)),0,"start anchor drift");
   if(anchor=="End")Near(Vector3.Distance(end,PrecisionEditing.Endpoint(copy,true)),0,"end anchor drift");
   if(anchor=="Center"){Near(copy.x,wall.x,"center X drift");Near(copy.z,wall.z,"center Z drift");}
  }
  var fixedEnd=PrecisionEditing.Endpoint(wall,false);PrecisionEditing.ResizeEndpoint(wall,fixedEnd,fixedEnd+new Vector3(1.237f,0,2.38f),true);
  Near(Vector3.Distance(fixedEnd,PrecisionEditing.Endpoint(wall,false)),0,"pointer fixed end");
  string before=JsonUtility.ToJson(wall);Reject(()=>PrecisionEditing.ResizeEndpoint(wall,fixedEnd,fixedEnd,true),"degenerate resize");Require(before==JsonUtility.ToJson(wall),"rejected resize mutation");
  wall.locked=true;before=JsonUtility.ToJson(wall);Reject(()=>PrecisionEditing.ResizeLength(wall,3,"Start"),"locked resize");Require(before==JsonUtility.ToJson(wall),"locked resize mutation");
  var freePoint=new Vector3(1.237f,0,-2.371f);Require(PrecisionEditing.Snap(freePoint,p,items).point==freePoint,"free pointer quantization");
  wall=Design.Wall("Target",0,0,2,0,5);items=new List<Item>{wall};var endpoint=new Vector3(1,0,0);var close=endpoint+Vector3.right*.015f;
  p.wallSnap=true;var snap=PrecisionEditing.Snap(close,p,items);Require(snap.IsWall&&snap.point==endpoint,"endpoint snap with grid off");
  p.gridSnap=true;snap=PrecisionEditing.Snap(close,p,items);Require(snap.IsWall&&snap.point==endpoint,"wall target priority over grid");
  snap=PrecisionEditing.Snap(new Vector3(0,0,.082f),p,items);Require(snap.kind=="Wall edge","physical edge target");Near(snap.point.z,.075f,"edge thickness conversion mm/m");
  p.gridSnap=false;snap=PrecisionEditing.Snap(endpoint+Vector3.right*.03f,p,items);Require(!snap.IsWall,"outside tolerance snapped");
  snap=PrecisionEditing.Snap(close,p,items,new HashSet<string>{wall.id});Require(!snap.IsWall,"self-snap");
  p.wallSnap=false;p.gridSnap=true;snap=PrecisionEditing.Snap(freePoint,p,items);Near(snap.point.x,1.2f,"grid X");Near(snap.point.z,-2.4f,"grid negative Z");
  var pivot=new Vector3(2.37f,0,-2.37f);var raw=new Vector3(.127f,0,-.127f);var anchors=new List<Vector3>{pivot};
  var delta=PrecisionEditing.DragDelta(raw,pivot,anchors,p,items,null,out snap);Near((pivot+delta).x,2.5f,"absolute world drag grid");
  p.gridSnap=false;Require(PrecisionEditing.DragDelta(raw,pivot,anchors,p,items,null,out snap)==raw,"free drag rounding");
  var d=Design.Example();var a=d.items[5];var b=d.items[6];a.x=2.37f;b.x=-2.37f;
  var spacing=new Vector3(b.x-a.x,b.y-a.y,b.z-a.z);SelectionEditing.Group(d,new[]{a.id,b.id});
  for(int n=0;n<100;n++)SelectionEditing.Move(d,new[]{a,b},Vector3.right*.01f);
  Near(a.x,3.37f,"100 rigid nudges");Near(Vector3.Distance(spacing,new Vector3(b.x-a.x,b.y-a.y,b.z-a.z)),0,"group spacing drift");
  a.locked=true;before=JsonUtility.ToJson(d);Reject(()=>SelectionEditing.Move(d,new[]{a,b},Vector3.right*.01f),"protected group movement");Require(before==JsonUtility.ToJson(d),"partial group mutation");
  SelectionEditing.SetLock(d,new[]{b.id},false);Require(!a.locked&&!b.locked,"checkbox unlock expands a group");
  SelectionEditing.SetLock(d,new[]{a.id},true);Require(a.locked&&b.locked,"checkbox lock expands a group");
  before=JsonUtility.ToJson(d);
  Reject(()=>SelectionEditing.Rotate(new[]{a,b},15),"locked group rotation");
  Reject(()=>SelectionEditing.Ungroup(d,new[]{a.id}),"locked group ungroup");
  Reject(()=>SelectionEditing.Remove(d,new[]{b.id}),"locked group deletion");
  Require(before==JsonUtility.ToJson(d),"locked group edit mutation");
  var lockedCopy=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));
  Require(lockedCopy.items.Find(i=>i.id==a.id).locked&&lockedCopy.items.Find(i=>i.id==b.id).locked,"checkbox lock native persistence");
  d.editing=new EditingPreferences{moveStep=.001f,resizeStep=.017f,gridSpacing=.125f,gridSnap=true,wallSnap=false,gridVisible=false,lengthAnchor="End",thicknessStepMm=.1f};
  a.x=1.237f;d.floorPlan=new FloorPlanData{kind="Vector",widthMeters=4,heightMeters=4,x=1.237f,segments=new List<FloorPlanSegment>{new FloorPlanSegment{start=new Vector2Data(-1,0),end=new Vector2Data(1,0)}}};
  string json=JsonUtility.ToJson(d);var restored=JsonUtility.FromJson<Design>(json);Design.Validate(restored);
  Require(JsonUtility.ToJson(d.editing)==JsonUtility.ToJson(restored.editing),"preferences persistence");Near(restored.items[5].x,1.237f,"exact item round-trip");Near(restored.floorPlan.x,1.237f,"exact guide round-trip");
  json=json.Replace("\"editing\":"+JsonUtility.ToJson(d.editing)+",","");restored=JsonUtility.FromJson<Design>(json);Design.Migrate(restored);Design.Validate(restored);
  var legacy=restored.editing??new EditingPreferences();Near(legacy.moveStep,.01f,"old design move default");Require(!legacy.gridSnap&&!legacy.wallSnap,"old design snap default");
  restored.editing=null;Design.Validate(restored);
  restored.version=1;restored.schemaVersion=1;restored.floor.thickness/=1000;restored.ceiling.thickness/=1000;foreach(var item in restored.items)item.shielding.thickness/=1000;
  Design.Migrate(restored);Design.Validate(restored);Near(restored.items[0].shielding.thickness,150,"v1 migration");
  SceneClipboardChecks.Run();
  Debug.Log("ROOM_STUDIO_PRECISION_CHECKS_PASSED: increments, 100 nudges, exact/free input, anchors, grid signs, wall edges/priority, rigid groups, protection, native/v1/v2/null defaults");
 }
 static void EquipmentScaleChecks(){
  var pivot=new Vector2(100,100);var start=new Vector2(140,130);
  Near(PrecisionEditing.EquipmentDragScale(.75f,pivot,start,start),.75f,"scale handle click");
  Near(PrecisionEditing.EquipmentDragScale(.75f,pivot,start,new Vector2(180,160)),1.5f,"proportional outward drag");
  Near(PrecisionEditing.EquipmentDragScale(.75f,pivot,start,new Vector2(120,115)),.375f,"proportional inward drag");
  Near(PrecisionEditing.EquipmentDragScale(.75f,pivot,start,new Vector2(110,170)),.75f,"sideways scale drag");
  Near(PrecisionEditing.EquipmentDragScale(.75f,pivot,start,pivot),.05f,"minimum scale bound");
  Near(PrecisionEditing.EquipmentDragScale(.75f,pivot,start,new Vector2(1000,1000)),3,"maximum scale bound");
  Reject(()=>PrecisionEditing.EquipmentDragScale(.75f,pivot,pivot,start),"zero scale radius");
  Reject(()=>PrecisionEditing.EquipmentDragScale(.75f,pivot,start,new Vector2(float.NaN,0)),"nonfinite scale pointer");
  foreach(string kind in new[]{"LINAC","Model","Desk","Source"}){
   var item=new Item{kind=kind,model=kind=="Model"||kind=="Source"?"CT":"",x=2.37f,y=.4f,z=-1.237f,angle=37,scale=.75f};
   PrecisionEditing.SetEquipmentScale(item,1.5f);Near(item.scale,1.5f,"equipment uniform scale");Near(item.x,2.37f,"scale pivot X");Near(item.y,.4f,"scale pivot Y");Near(item.z,-1.237f,"scale pivot Z");Near(item.angle,37,"scale orientation");
   var restored=JsonUtility.FromJson<Item>(JsonUtility.ToJson(item));Near(restored.scale,1.5f,"equipment scale persistence");
   item.locked=true;string before=JsonUtility.ToJson(item);Reject(()=>PrecisionEditing.SetEquipmentScale(item,2),"protected equipment scaling");Require(before==JsonUtility.ToJson(item),"protected scale mutation");
   item.locked=false;before=JsonUtility.ToJson(item);Reject(()=>PrecisionEditing.SetEquipmentScale(item,0),"invalid equipment scale");Require(before==JsonUtility.ToJson(item),"invalid scale mutation");
  }
  Reject(()=>PrecisionEditing.SetEquipmentScale(Design.Wall("Wall",0,0,2,0,4),1),"wall equipment scaling");
  Reject(()=>PrecisionEditing.SetEquipmentScale(new Item{kind="Model",model="Dot",ctPoint=new CtPointData{version=1,role="ROI"}},1),"calculation marker equipment scaling");
  Debug.Log("ROOM_STUDIO_EQUIPMENT_SCALE_CHECKS_PASSED: projected drags, proportional scalar, pivot/orientation, bounds, protection, marker exclusion and native persistence");
 }
 static void WallAngleChecks(){
  var origin=new Vector3(2.37f,.4f,-1.237f);
  for(int direction=-4;direction<=4;direction++)foreach(float deviation in new[]{-22.4f,-7f,7f,22.4f}){
   float angle=(direction*45+deviation)*Mathf.Deg2Rad;var target=origin+new Vector3(Mathf.Cos(angle)*2.37f,0,Mathf.Sin(angle)*2.37f);
   var snapped=PrecisionEditing.ConstrainWallAngle(origin,target,true);var delta=snapped-origin;
   Near(Mathf.DeltaAngle(Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,direction*45),0,"45-degree wall direction");
   Near(delta.magnitude,2.37f,"angle snapping changed pointer length");Near(snapped.y,origin.y,"angle snapping changed elevation");
   Require(PrecisionEditing.ConstrainWallAngle(origin,target,false)==target,"released Shift retained angle constraint");
   foreach(bool movingEnd in new[]{false,true}){
    var wall=Design.Wall("Angle wall",0,0,2,0,3);wall.y=origin.y;
    PrecisionEditing.ResizeEndpoint(wall,origin,snapped,movingEnd);
    Near(Vector3.Distance(origin,PrecisionEditing.Endpoint(wall,!movingEnd)),0,"angle snapping moved the fixed wall endpoint");
   }
  }
  Require(PrecisionEditing.ConstrainWallAngle(origin,origin,true)==origin,"coincident angle target changed");
  Debug.Log("ROOM_STUDIO_WALL_ANGLE_CHECKS_PASSED: all 45-degree directions, positive/negative coordinates, live-direction inputs, free release, preserved lengths/heights and both fixed-end anchors");
 }
 static void EquipmentScaleShortcutChecks(){
  foreach(var key in new[]{KeyCode.Plus,KeyCode.KeypadPlus})Require(PrecisionEditing.EquipmentScaleDirection(key,false)==1,"increase scale key");
  Require(PrecisionEditing.EquipmentScaleDirection(KeyCode.Equals,true)==1,"Shift-plus keyboard mapping");
  foreach(var key in new[]{KeyCode.Minus,KeyCode.KeypadMinus})Require(PrecisionEditing.EquipmentScaleDirection(key,false)==-1,"decrease scale key");
  Require(PrecisionEditing.EquipmentScaleDirection(KeyCode.Equals,false)==0&&PrecisionEditing.EquipmentScaleDirection(KeyCode.Minus,true)==0,"unrelated equals/underscore key captured");
  var first=new Item{kind="Model",model="CT",scale=.75f,x=2.37f,z=-1.237f};var second=new Item{kind="Desk",scale=.33f,x=-3,z=4};var equipment=new List<Item>{first,second};
  PrecisionEditing.ScaleEquipment(equipment,.01f,1);Near(first.scale,.76f,"increase scalar");Near(second.scale,.34f,"multi-equipment increase");
  PrecisionEditing.ScaleEquipment(equipment,.01f,-1);Near(first.scale,.75f,"decrease scalar");Near(second.scale,.33f,"multi-equipment decrease");Near(first.x,2.37f,"keyboard scale position X");Near(first.z,-1.237f,"keyboard scale position Z");
  string unchanged=JsonUtility.ToJson(first);second.locked=true;
  Reject(()=>PrecisionEditing.ScaleEquipment(equipment,.01f,1),"protected selection scale");Require(JsonUtility.ToJson(first)==unchanged,"partial protected scale mutation");second.locked=false;
  Reject(()=>PrecisionEditing.ScaleEquipment(new List<Item>{first,Design.Wall("Wall",0,0,2,0,3)},.01f,1),"mixed equipment/wall scale");Require(JsonUtility.ToJson(first)==unchanged,"partial mixed selection mutation");
  second.scale=float.NaN;Reject(()=>PrecisionEditing.ScaleEquipment(equipment,.01f,1),"nonfinite member scale");Require(JsonUtility.ToJson(first)==unchanged,"partial invalid selection mutation");second.scale=3;
  first.scale=.05f;PrecisionEditing.ScaleEquipment(equipment,.01f,-1);Near(first.scale,.05f,"keyboard minimum scale");
  PrecisionEditing.ScaleEquipment(equipment,1,1);Near(second.scale,3,"keyboard maximum scale");
  Reject(()=>PrecisionEditing.ScaleEquipment(equipment,0,1),"zero scale increment");
  Debug.Log("ROOM_STUDIO_EQUIPMENT_SCALE_SHORTCUT_CHECKS_PASSED: plus/minus/keypad/Shift-plus mapping, decimal increments, uniform scalar, unchanged pivots, bounded and atomic multi-selection");
 }
 static void LayoutChecks(){
  foreach(var screen in new[]{new Vector2Int(320,568),new Vector2Int(390,844),new Vector2Int(667,375),new Vector2Int(768,1024),new Vector2Int(1024,768),new Vector2Int(1600,1000),new Vector2Int(2560,1440)}){
   var layout=StudioViewportLayout.Create(screen.x,screen.y);var scene=layout.Scene;
   Require(layout.compact==(screen.x<1000||screen.y<600),"compact layout breakpoint");
   Require(scene.width>=296&&scene.height>=255,"usable phone/tablet/desktop scene dimensions");
   foreach(var rectangle in new[]{scene,layout.LeftPanel,layout.RightPanel,layout.LeftContent,layout.RightContent})
    Require(rectangle.width>0&&rectangle.height>0&&rectangle.xMin>=0&&rectangle.yMin>=0&&rectangle.xMax<=layout.width&&rectangle.yMax<=layout.height,"panel or scene outside viewport");
   if(layout.compact){Near(layout.scale,1,"compact controls retain readable scale");Require(layout.RightContent.height==layout.LeftContent.height,"compact calculation controls must remain inside the scrollable panel");}
   else Require(layout.LeftPanel.xMax<scene.xMin&&scene.xMax<layout.RightPanel.xMin&&layout.RightContent.yMax<layout.Calculation.yMin,"desktop scene/panels/calculation area overlap");
   foreach(var position in new[]{scene.min,scene.max,scene.center,new Vector2(-10000,10000)}){
    var label=StudioViewportLayout.DistanceLabel(scene,position);
    Require(label.width>0&&label.xMin>=scene.xMin+6&&label.xMax<=scene.xMax-6&&label.yMin>=scene.yMin+86&&label.yMax<=scene.yMax-6,"CT distance label overlaps panels or scene heading");
   }
  }
  Reject(()=>StudioViewportLayout.Create(0,568),"zero viewport accepted");
  Debug.Log("ROOM_STUDIO_VIEWPORT_LAYOUT_CHECKS_PASSED: 320/390 phones, 667 landscape, 768 tablet, 1024/1600/2560 desktop; positive scene/panels, scrollable compact calculations, bounded CT cm labels");
 }
}
}
