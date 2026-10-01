using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 EditingPreferences Precision=>design.editing??(design.editing=new EditingPreferences());
 bool showPrecisionSettings,resizingWall,showSnapTarget;
 SnapTarget pointerSnap;
 Vector3 dragPivot,resizeFixed,resizeMouseStart,resizePointerOffset;
 readonly List<Vector3> dragAnchors=new List<Vector3>();
 Item resizeOriginal;
 Design dragConnectedBefore,resizeConnectedBefore;
 bool resizeMovingEnd;
 bool scalingEquipment;
 Item scaleOriginal;
 Vector2 scalePivot,scaleMouseStart;
 Texture2D scaleBubble;
 struct ScenePointerEvent {public bool down,additive,blocked,shift;public Vector2 point;}
 readonly Queue<ScenePointerEvent> scenePointerEvents=new Queue<ScenePointerEvent>();
 void CaptureScenePointer(Event e){
  if(e.button!=0||(e.type!=EventType.MouseDown&&e.type!=EventType.MouseUp))return;
  // Input.mousePosition can already be at the release point when a quick drag is
  // sampled by Update. IMGUI retains each event's original position.
  scenePointerEvents.Enqueue(new ScenePointerEvent{down=e.type==EventType.MouseDown,additive=multiSelect||e.shift||e.control||e.command,blocked=CompactPanelOpen||showHelp||showFiles||showQa||showComponentEditor||showPlanAuthoring||showReset,shift=e.shift,point=e.mousePosition/uiScale});
 }
 void ProcessScenePointerEvents(){
  while(scenePointerEvents.Count>0){
  var e=scenePointerEvents.Dequeue();bool inView=View.Contains(e.point)&&!e.blocked&&!CompactPanelOpen&&!showHelp&&!showFiles&&!showQa&&!showComponentEditor&&!showPlanAuthoring&&!showReset;
   var ray=cam.ScreenPointToRay(new Vector3(e.point.x*uiScale,Screen.height-e.point.y*uiScale,0));
   bool onPlane=new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float enter);var raw=ray.GetPoint(enter);
   raw.x=Mathf.Clamp(raw.x,-10000,10000);raw.z=Mathf.Clamp(raw.z,-10000,10000);
  if(e.down){if(inView&&!e.additive&&TryBeginEquipmentScale(e.point))continue;if(inView&&onPlane)BeginScenePointer(e.point,raw,ray,e.additive,e.shift);}
   else{
   if(inView&&onPlane){if(dragging)UpdatePrecisionDrag(raw);if(resizingWall)UpdateWallResize(raw,e.shift);}
   if(inView&&scalingEquipment)UpdateEquipmentScale(e.point);
    if(marquee){marqueeEnd=new Vector2(Mathf.Clamp(e.point.x,View.xMin,View.xMax),Mathf.Clamp(e.point.y,View.yMin,View.yMax));FinishMarquee();marquee=false;}
   FinishDrag();FinishWallResize();FinishEquipmentScale();
   }
  }
 }
 void BeginScenePointer(Vector2 mouse,Vector3 raw,Ray ray,bool additive,bool angleSnap=false){
  GUI.FocusControl(null);if((!additive||angleSnap)&&TryBeginWallResize(mouse,raw))return;
  if(tool=="Select"&&TryPickCtAnnotation(mouse,out string ctPointId,out bool pointMarker)){Choose(ctPointId,additive,true);if(pointMarker&&CtShieldingData.IsRoi(Current))BeginPrecisionDrag(raw);return;}
  var p=PrecisionEditing.Snap(raw,Precision,design.items).point;
  if(tool=="Polygon")AddComponentPoint(p);
  else if(tool=="Component"){PlaceComponent(p);tool="Select";}
  else if(tool=="Door")PlaceDoorAtPoint(raw);
    else if(tool=="CT_ROI"||tool=="CT_Patient")PlaceCtRoi(p,tool=="CT_ROI"?"ROI":"Patient");
  else if(tool=="Wall"){
   if(!wallStart.HasValue)wallStart=p;
    else{var start=wallStart.Value;p=PrecisionEditing.ConstrainWallAngle(start,p,angleSnap);var delta=p-start;if(delta.magnitude>=.25f&&delta.magnitude<=60){Add(Design.Wall("Wall "+(design.items.Count+1),(p.x+start.x)/2,(p.z+start.z)/2,delta.magnitude,-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,design.height));wallStart=null;}else status="Wall length must be 0.25 to 60 metres.";}
  }else if(tool=="Desk"){Add(new Item{kind="Desk",name="Workstation "+(design.items.Count(i=>i.kind=="Desk")+1).ToString("00"),x=p.x,z=p.z,scale=.33f});tool="Select";}
  else if(tool=="LINAC"||tool=="Model"){PlaceEquipment(tool=="LINAC"?"Linac":design.selectedModel,p);tool="Select";}
  else{
   string pickedId=null;foreach(var hit in Physics.RaycastAll(ray).OrderBy(h=>h.distance)){
    var joined=hit.collider.GetComponentInParent<JoinedWallSelection>();
    if(joined!=null){pickedId=WallConnections.ClosestWall(design,joined.wallIds,hit.point)?.id;break;}
    var pick=hit.collider.GetComponentInParent<Selectable>();if(pick!=null){pickedId=pick.id;break;}
   }
   if(pickedId!=null){pendingToggleOff=additive&&selection.Contains(pickedId)?pickedId:"";Choose(pickedId,additive&&pendingToggleOff=="",true);if(selection.Contains(pickedId))BeginPrecisionDrag(raw);}
   else{marquee=true;marqueeStart=marqueeEnd=mouse;marqueeAdd=additive;}
  }
 }
 float NumberStep(string label,string unit){
  if(unit=="mm")return Precision.thicknessStepMm;
  if(unit=="m")return label.StartsWith("Position")||label.StartsWith("Center ")||label.StartsWith("Point ")||label.StartsWith("Vertex ")||label=="Guide X"||label=="Guide Z"||label=="Base height Y"||label.Contains("offset")?Precision.moveStep:Precision.resizeStep;
  if(label=="Model scale")return Precision.scaleStep;
  if(label=="Segments")return 1;
  return -1;
 }
 void SetPrecision(EditingPreferences next){
  PrecisionEditing.Validate(next);if(dirty)Commit();design.editing=next;
  // Preferences persist but are not transform undo entries. Keep the current baseline in sync.
  if(historyIndex>=0)history[historyIndex]=JsonUtility.ToJson(design);
 }
 void PrecisionUI(){
  bool changed=GUI.changed;GUI.changed=false;
  var next=JsonUtility.FromJson<EditingPreferences>(JsonUtility.ToJson(Precision));
  next.gridSnap=GUILayout.Toggle(next.gridSnap," Snap to grid · "+F(next.gridSpacing)+" m");
  next.wallSnap=GUILayout.Toggle(next.wallSnap," Snap to wall");
  next.gridVisible=GUILayout.Toggle(next.gridVisible," Show display grid (1 m)");
  if(Btn("Precision settings",showPrecisionSettings))showPrecisionSettings=!showPrecisionSettings;
  if(showPrecisionSettings){
   GUILayout.Label("Steps affect +/− and arrow nudges. Sliders and free drags remain continuous. Exact typed values bypass snapping.",small);
   next.moveStep=PrecisionStepUI("Move increment",next.moveStep);
   next.resizeStep=PrecisionStepUI("Resize increment",next.resizeStep);
   next.thicknessStepMm=Number("Thickness step",next.thicknessStepMm,.000001f,10000,"mm",-1);
   next.scaleStep=Number("Scale factor step",next.scaleStep,.000001f,1,"×",-1);
   next.gridSpacing=Number("Snap grid spacing",next.gridSpacing,.000001f,100,"m",-1);
   next.wallSnapTolerance=Number("Wall tolerance",next.wallSnapTolerance,.000001f,1,"m",-1);
   GUILayout.Label("Snap grid: world X/Z, origin 0/0; half cells round away from zero. Wall targets take priority. This assist does not connect wall geometry.",small);
  }
  if(JsonUtility.ToJson(next)!=JsonUtility.ToJson(Precision)){
   bool redraw=next.gridVisible!=Precision.gridVisible;SetPrecision(next);if(redraw)Rebuild();
  }
  GUI.changed=changed;
 }
 float PrecisionStepUI(string label,float value){
  value=Number(label,value,.000001f,100,"m",-1);GUILayout.BeginHorizontal();
  foreach(float preset in new[]{.001f,.01f,.1f,1f})if(GUILayout.Button(preset.ToString("G",CultureInfo.InvariantCulture))){value=preset;GUI.FocusControl(null);}
  GUILayout.EndHorizontal();return value;
 }
 void WallLengthUI(Item item){
  bool changed=GUI.changed;GUILayout.Label("Length anchor",small);GUILayout.BeginHorizontal();
  foreach(string anchor in new[]{"Start","Center","End"})if(Btn(anchor,Precision.lengthAnchor==anchor)){
   var next=JsonUtility.FromJson<EditingPreferences>(JsonUtility.ToJson(Precision));next.lengthAnchor=anchor;SetPrecision(next);
  }
  GUILayout.EndHorizontal();GUI.changed=changed;
  float length=Number("Length",item.length,.25f,60,"m");
  if(length!=item.length){
   float oldX=item.x,oldZ=item.z,oldLength=item.length;
   try{PrecisionEditing.ResizeLength(item,length,Precision.lengthAnchor);DoorGeometry.ValidateWall(item);}
   catch(Exception e){item.x=oldX;item.z=oldZ;item.length=oldLength;status=e.Message;GUI.changed=changed;}
  }
  GUILayout.Label("Start/end follow the wall's local length axis. In 2D, drag an endpoint handle to resize around the opposite fixed end.",small);
 }
 void NudgeSelection(Vector3 direction){
  if(selection.Count==0)return;
  Design before=null;
  try{if(dirty)Commit();if(SelectedItems.Any(i=>IsJoinedWall(i.id)))before=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
   SelectionEditing.Move(design,SelectedItems,direction*Precision.moveStep);
   PropagateObjectPanelEdit(before);numberBuffers.Clear();Commit();Rebuild();status="Moved selection by "+F(Precision.moveStep)+" m.";}
  catch(Exception e){if(before!=null){design=before;Rebuild();}status=e.Message;}
 }
 void NudgeUI(){
  GUILayout.Label("Nudge selection · "+F(Precision.moveStep)+" m",small);GUILayout.BeginHorizontal();
  if(Btn("X −"))NudgeSelection(Vector3.left);if(Btn("X +"))NudgeSelection(Vector3.right);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
  if(Btn("Z −"))NudgeSelection(Vector3.back);if(Btn("Z +"))NudgeSelection(Vector3.forward);GUILayout.EndHorizontal();
 }
 void BeginPrecisionDrag(Vector3 raw){
    try{CtShieldingData.RequireTransform(design,SelectedItems);}catch(Exception error){status=error.Message;return;}
  dragStart=raw;dragMoved=false;dragPositions.Clear();dragAnchors.Clear();
  dragConnectedBefore=SelectedItems.Any(i=>IsJoinedWall(i.id))?JsonUtility.FromJson<Design>(JsonUtility.ToJson(design)):null;
  foreach(var item in SelectedItems){
   var position=new Vector3(item.x,item.y,item.z);dragPositions[item.id]=position;
   if(item.kind=="Wall"){dragAnchors.Add(PrecisionEditing.Endpoint(item,false));dragAnchors.Add(PrecisionEditing.Endpoint(item,true));}else dragAnchors.Add(position);
  }
  dragPivot=new Vector3(Current.x,0,Current.z);dragging=true;
 }
 void UpdatePrecisionDrag(Vector3 raw){
  var rawDelta=raw-dragStart;rawDelta.y=0;
  if(!dragMoved&&rawDelta.sqrMagnitude<.0000000001f)return; // A click never snaps existing geometry.
  var items=SelectedItems;if(items.Any(i=>i.locked)){status="Unlock protected objects before moving this selection.";return;}if(items.Count==0)return;
  var delta=PrecisionEditing.DragDelta(rawDelta,dragPivot,dragAnchors,Precision,design.items,selection,out pointerSnap);showSnapTarget=pointerSnap.kind!="Free";
  foreach(var item in items){var next=dragPositions[item.id]+delta;if(Mathf.Abs(next.x)>10000||Mathf.Abs(next.z)>10000){status="Movement is outside the supported range.";return;}}
  if(dragConnectedBefore!=null){
   if(items.Count!=1){status="Drag one connected wall at a time, or detach its junctions first.";return;}
   try{
    var candidate=JsonUtility.FromJson<Design>(JsonUtility.ToJson(dragConnectedBefore));
    var wall=candidate.items.Find(i=>i.id==items[0].id);var next=dragPositions[wall.id]+delta;wall.x=next.x;wall.z=next.z;
    design=WallConnections.PropagateEdit(dragConnectedBefore,candidate,wall.id);
    dirty=dragMoved=true;Rebuild();UpdateBeam();
   }catch(Exception e){status=e.Message;}
   return;
  }
  foreach(var item in items){
   var next=dragPositions[item.id]+delta;if(next.x==item.x&&next.z==item.z)continue;
   item.x=next.x;item.z=next.z;dirty=dragMoved=true;
   var root=objects[item.id];Vector3 shift=new Vector3(item.x,item.y,item.z)-root.transform.position;root.transform.position+=shift;
   foreach(var line in root.GetComponentsInChildren<LineRenderer>())if(line.useWorldSpace)for(int k=0;k<line.positionCount;k++)line.SetPosition(k,line.GetPosition(k)+shift);
  }
  UpdateBeam();
 }
 Vector2 HandleScreenPoint(Item item,bool end){var p=cam.WorldToScreenPoint(PrecisionEditing.Endpoint(item,end));return new Vector2(p.x/uiScale,(Screen.height-p.y)/uiScale);}
 bool CanResizeWall=>top&&tool=="Select"&&selection.Count==1&&Current!=null&&Current.kind=="Wall"&&!Current.locked;
 bool TryBeginWallResize(Vector2 mouse,Vector3 raw){
  if(!CanResizeWall)return false;
  for(int end=0;end<2;end++)if(Vector2.Distance(mouse,HandleScreenPoint(Current,end==1))<=9){
   if(dirty)Commit();resizeOriginal=JsonUtility.FromJson<Item>(JsonUtility.ToJson(Current));resizeConnectedBefore=IsJoinedWall(Current.id)?JsonUtility.FromJson<Design>(JsonUtility.ToJson(design)):null;resizeMovingEnd=end==1;
   resizeFixed=PrecisionEditing.Endpoint(Current,!resizeMovingEnd);resizeMouseStart=raw;resizePointerOffset=PrecisionEditing.Endpoint(Current,resizeMovingEnd)-raw;resizingWall=true;return true;
  }
  return false;
 }
 void UpdateWallResize(Vector3 raw,bool angleSnap=false){
  var item=Current;if(item==null)return;
  if(!dirty&&(raw-resizeMouseStart).sqrMagnitude<.0000000001f)return;
  pointerSnap=PrecisionEditing.Snap(raw+resizePointerOffset,Precision,design.items,selection);showSnapTarget=pointerSnap.kind!="Free";
  var target=PrecisionEditing.ConstrainWallAngle(resizeFixed,pointerSnap.point,angleSnap);target.y=resizeFixed.y;
  if(angleSnap){pointerSnap=new SnapTarget{point=target,kind="45 degree angle"};showSnapTarget=true;}
  if(resizeConnectedBefore!=null){
   try{
    var candidate=JsonUtility.FromJson<Design>(JsonUtility.ToJson(resizeConnectedBefore));
    var wall=candidate.items.Find(i=>i.id==resizeOriginal.id);
    PrecisionEditing.ResizeEndpoint(wall,resizeFixed,target,resizeMovingEnd);DoorGeometry.ValidateWall(wall);
    design=WallConnections.PropagateEdit(resizeConnectedBefore,candidate,wall.id);dirty=true;Rebuild();
   }catch(Exception e){status=e.Message;}
   return;
  }
  float oldX=item.x,oldZ=item.z,oldAngle=item.angle,oldLength=item.length;
  try{PrecisionEditing.ResizeEndpoint(item,resizeFixed,target,resizeMovingEnd);DoorGeometry.ValidateWall(item);dirty=true;Rebuild();}
  catch(Exception e){item.x=oldX;item.z=oldZ;item.angle=oldAngle;item.length=oldLength;status=e.Message;}
 }
 void FinishWallResize(){if(!resizingWall)return;resizingWall=false;resizeOriginal=null;resizeConnectedBefore=null;if(dirty)Commit();}
 void CancelWallResize(){
  if(!resizingWall)return;var item=design.items.Find(i=>i.id==resizeOriginal.id);
  if(resizeConnectedBefore!=null){design=resizeConnectedBefore;resizeConnectedBefore=null;resizingWall=false;resizeOriginal=null;dirty=false;Rebuild();return;}
  if(item!=null){item.x=resizeOriginal.x;item.z=resizeOriginal.z;item.angle=resizeOriginal.angle;item.length=resizeOriginal.length;}
  resizingWall=false;resizeOriginal=null;dirty=false;Rebuild();
 }
 bool TryEquipmentScaleHandle(out Vector2 pivot,out Vector2 handle){
  pivot=handle=Vector2.zero;
  if(tool!="Select"||selection.Count!=1||Current==null||Current.locked||!PrecisionEditing.IsEquipment(Current)||!objects.TryGetValue(Current.id,out var root))return false;
  var collider=root.GetComponent<BoxCollider>();if(collider==null)return false;
  var origin=cam.WorldToScreenPoint(root.transform.position);if(origin.z<=0)return false;
  pivot=new Vector2(origin.x/uiScale,(Screen.height-origin.y)/uiScale);float best=144;
  var area=new Rect(View.x+14,View.y+14,View.width-28,View.height-28);bool found=false;
  for(int corner=0;corner<8;corner++){
   var local=collider.center+Vector3.Scale(collider.size*.5f,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
   var projected=cam.WorldToScreenPoint(root.transform.TransformPoint(local));if(projected.z<=0)continue;
   var point=new Vector2(projected.x/uiScale,(Screen.height-projected.y)/uiScale);float distance=(point-pivot).sqrMagnitude;
   if(area.Contains(point)&&distance>best){best=distance;handle=point;found=true;}
  }
  return found;
 }
 bool TryBeginEquipmentScale(Vector2 mouse){
  if(!TryEquipmentScaleHandle(out var pivot,out var handle)||Vector2.Distance(mouse,handle)>16)return false;
  if(dirty)Commit();GUI.FocusControl(null);scaleOriginal=JsonUtility.FromJson<Item>(JsonUtility.ToJson(Current));
  scalePivot=pivot;scaleMouseStart=mouse;scalingEquipment=true;return true;
 }
 void UpdateEquipmentScale(Vector2 mouse){
  var item=design.items.Find(candidate=>candidate.id==scaleOriginal.id);if(item==null)return;
  try{
   float value=PrecisionEditing.EquipmentDragScale(scaleOriginal.scale,scalePivot,scaleMouseStart,mouse);if(value==item.scale)return;
   PrecisionEditing.SetEquipmentScale(item,value);dirty=true;numberBuffers.Clear();Rebuild();status="Equipment scale: "+F(value)+".";
  }catch(Exception error){status=error.Message;}
 }
 void FinishEquipmentScale(){if(!scalingEquipment)return;scalingEquipment=false;scaleOriginal=null;if(dirty)Commit();}
 void CancelEquipmentScale(){
  if(!scalingEquipment)return;var item=design.items.Find(candidate=>candidate.id==scaleOriginal.id);if(item!=null)item.scale=scaleOriginal.scale;
  scalingEquipment=false;scaleOriginal=null;dirty=false;Rebuild();
 }
 void DrawEquipmentScaleHandle(){
  if(dragging||resizingWall||marquee||!TryEquipmentScaleHandle(out _,out var point))return;
  if(scaleBubble==null){
   scaleBubble=new Texture2D(24,24,TextureFormat.RGBA32,false);var pixels=new Color32[24*24];
   for(int row=0;row<24;row++)for(int column=0;column<24;column++){
    float distance=Vector2.Distance(new Vector2(column+.5f,row+.5f),new Vector2(12,12));
    pixels[row*24+column]=new Color32(255,255,255,(byte)(Mathf.Clamp01(12-distance)*255));
   }
   scaleBubble.SetPixels32(pixels);scaleBubble.Apply();
  }
  var rectangle=new Rect(point.x-12,point.y-12,24,24);var color=GUI.color;GUI.color=new Color(.2f,1,.85f);GUI.DrawTexture(rectangle,scaleBubble);GUI.color=color;
  GUI.Label(rectangle,new GUIContent("","Scale equipment"));
  if(rectangle.Contains(Event.current.mousePosition)||scalingEquipment)GUI.Label(new Rect(View.x+14,View.yMax-32,View.width-28,24),"Scale equipment",small);
 }
 void DrawPrecisionOverlay(){
  if(CompactPanelOpen||showHelp||showFiles||showQa||showComponentEditor||showPlanAuthoring||showReset)return;
  DrawEquipmentScaleHandle();
  if(CanResizeWall&&!dragging)for(int end=0;end<2;end++){
   var point=HandleScreenPoint(Current,end==1);if(!View.Contains(point))continue;
   var color=GUI.color;GUI.color=new Color(.2f,1,.85f);GUI.DrawTexture(new Rect(point.x-5,point.y-5,10,10),Texture2D.whiteTexture);GUI.color=color;
   GUI.Label(new Rect(point.x+8,point.y-18,55,22),end==0?"Start":"End",small);
  }
  if(showSnapTarget){
   var p=cam.WorldToScreenPoint(pointerSnap.point);var point=new Vector2(p.x/uiScale,(Screen.height-p.y)/uiScale);
   if(p.z<=0||!View.Contains(point))return;
   var color=GUI.color;GUI.color=Color.yellow;GUI.DrawTexture(new Rect(point.x-6,point.y-1,12,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(point.x-1,point.y-6,2,12),Texture2D.whiteTexture);GUI.color=color;
   GUI.Label(new Rect(View.x+14,View.yMax-32,View.width-28,24),pointerSnap.kind+" · X "+F(pointerSnap.point.x)+" / Z "+F(pointerSnap.point.z)+" m",small);
  }
 }
}
}
