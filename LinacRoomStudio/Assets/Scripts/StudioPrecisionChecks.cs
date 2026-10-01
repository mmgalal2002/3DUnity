using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 void PrecisionSmokeChecks(){
  PrecisionChecks.Run();string original=JsonUtility.ToJson(design);string oldTool=tool;tool="Select";
  try{
   var id=design.items[6].id;Choose(id);Current.x=2.37f;Commit();
   int count=history.Count,index=historyIndex;string geometry=JsonUtility.ToJson(Current);
   SetPrecision(new EditingPreferences{gridSnap=true,wallSnap=true,gridVisible=false,moveStep=.01f});Rebuild();
   if(count!=history.Count||index!=historyIndex||geometry!=JsonUtility.ToJson(Current))throw new Exception("Preference toggle changed transform/history");
   if(world.GetComponentsInChildren<LineRenderer>().Any(l=>l.name=="Grid"))throw new Exception("Grid visibility toggle failed");
   HandleShortcut(KeyCode.RightArrow);if(Mathf.Abs(Current.x-2.38f)>.0001f)throw new Exception("Exact nudge affected by snapping");
   Undo(-1);if(!selection.Contains(id)||Mathf.Abs(Current.x-2.37f)>.0001f||!Precision.gridSnap)throw new Exception("Nudge undo/selection/preferences failed");
   Undo(1);if(!selection.Contains(id)||Mathf.Abs(Current.x-2.38f)>.0001f)throw new Exception("Nudge redo/selection failed");
   string unchanged=JsonUtility.ToJson(design);int currentHistory=historyIndex;
   if(HandleShortcut(KeyCode.RightArrow,false,false,false,true))throw new Exception("Nudge captured numeric focus");
   showHelp=true;if(HandleShortcut(KeyCode.RightArrow))throw new Exception("Nudge captured modal");showHelp=false;
   tool="Wall";if(HandleShortcut(KeyCode.RightArrow))throw new Exception("Nudge captured drawing");tool="Select";
   if(unchanged!=JsonUtility.ToJson(design)||historyIndex!=currentHistory)throw new Exception("Guard mutation");
   SetPrecision(new EditingPreferences());Choose(id);BeginPrecisionDrag(Vector3.zero);var start=new Vector3(Current.x,Current.y,Current.z);
   UpdatePrecisionDrag(new Vector3(.1237f,0,-.2371f));UpdatePrecisionDrag(new Vector3(.2371f,0,-.1237f));
   if(Mathf.Abs(Current.x-start.x-.2371f)>.0001f)throw new Exception("Drag accumulated or quantized deltas");
   CancelInteraction();if(Mathf.Abs(Current.x-start.x)>.0001f||dragging||dirty)throw new Exception("Cancelled drag changed geometry");
   string wallId=design.items.First(i=>i.kind=="Wall").id;Choose(wallId);var before=JsonUtility.ToJson(Current);
   resizeOriginal=JsonUtility.FromJson<Item>(before);resizeMovingEnd=true;resizeFixed=PrecisionEditing.Endpoint(Current,false);resizeMouseStart=PrecisionEditing.Endpoint(Current,true);resizePointerOffset=Vector3.zero;resizingWall=true;
   int resizeHistory=historyIndex;UpdateWallResize(resizeMouseStart);if(dirty||resizeHistory!=historyIndex||before!=JsonUtility.ToJson(Current))throw new Exception("Stationary resize changed geometry/history");
   var moving=PrecisionEditing.Endpoint(Current,true)+new Vector3(.0137f,0,.0237f);UpdateWallResize(moving);
   if(Vector3.Distance(resizeFixed,PrecisionEditing.Endpoint(Current,false))>.0001f)throw new Exception("Resize handle anchor drift");
   CancelInteraction();if(before!=JsonUtility.ToJson(Current))throw new Exception("Cancelled resize changed wall");
   PrecisionEditing.ResizeLength(Current,Current.length+.01f,"Start");Commit();string resized=JsonUtility.ToJson(Current);Undo(-1);Choose(wallId);
   if(before!=JsonUtility.ToJson(Current))throw new Exception("Resize undo failed");Undo(1);Choose(wallId);if(resized!=JsonUtility.ToJson(Current))throw new Exception("Resize redo failed");
   // Press and release can arrive before one Update; retain the press position.
   top=true;var handle=HandleScreenPoint(Current,true);var anchor=PrecisionEditing.Endpoint(Current,false);float oldLength=Current.length;
   // Unity's Event.type is not available outside an OnGUI callback; enqueue the
   // same captured samples here. Actual IMGUI delivery is checked interactively.
   scenePointerEvents.Enqueue(new ScenePointerEvent{down=true,point=handle});
   scenePointerEvents.Enqueue(new ScenePointerEvent{down=false,point=handle+new Vector2(14,7)});
   if(scenePointerEvents.Count!=2||!CanResizeWall||showHelp||showFiles||showQa||showComponentEditor)throw new Exception("Queued drag setup: events="+scenePointerEvents.Count+", canResize="+CanResizeWall+", help="+showHelp+", files="+showFiles+", qa="+showQa+", component="+showComponentEditor);
   ProcessScenePointerEvents();
   if(Current==null||Current.length==oldLength||resizingWall||Vector3.Distance(anchor,PrecisionEditing.Endpoint(Current,false))>.0001f)throw new Exception("Queued endpoint drag failed: current="+(Current?.id??"none")+", length="+(Current?.length.ToString()??"none")+", old="+oldLength+", resizing="+resizingWall+", anchor drift="+(Current==null?-1:Vector3.Distance(anchor,PrecisionEditing.Endpoint(Current,false)))+", handle="+handle+", view="+View+", screen="+Screen.width+"x"+Screen.height+", camera="+cam.transform.position+", status="+status);
   SetPrecision(new EditingPreferences{gridSnap=true,moveStep=.001f});string save=JsonUtility.ToJson(design);LoadJson(save,"Precision-smoke");if(!Precision.gridSnap||Precision.moveStep!=.001f)throw new Exception("Preferences load failed");
   Debug.Log("ROOM_STUDIO_PRECISION_RUNTIME_PASSED: preferences without transform history, grid display, nudge/resize undo-redo, focus guards, free drag, cancellation and native load");
  }finally{showHelp=false;tool=oldTool;design=JsonUtility.FromJson<Design>(original);ClearSelection();Commit();Rebuild();}
 }
}
}
