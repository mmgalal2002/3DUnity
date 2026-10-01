using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 Vector2 helpScroll;
 bool showReset;
 Vector2 resetScroll;

 System.Collections.IEnumerator FeatureSmokeTest(){
  yield return null;
  try{SelectionSmokeChecks();ShortcutSmokeChecks();PaletteSmokeChecks();ComponentSmokeChecks();FloorPlanSmokeChecks();PrecisionSmokeChecks();PlanAuthoringSmokeChecks();WallConnectionSceneSmokeChecks();WallGenerationSmokeChecks();DoorSmokeChecks();BeamSmokeChecks();CtRuntimeChecks();status="Editor feature checks PASSED: shortcuts, protection, help, grouping, palette placements, components, floor-plan tracing, fractional editing, color masks, connected wall joins and generation, physical door openings, Linac HD beam placement and CT ROI scenarios.";Debug.Log("ROOM_STUDIO_WEB_FEATURE_CHECKS_PASSED");}
  catch(Exception e){status="Editor feature checks FAILED: "+e.Message;Debug.LogError(status+"\n"+e);}
 }
 string TextInput(string key,string value){
  const int maxLength=24;string original=value??"",visible=original.Length>maxLength?original.Substring(0,maxLength):original;
  GUI.SetNextControlName("edit:"+key);string edited=GUILayout.TextField(visible,maxLength,field,GUILayout.Width(220));CaptureCtControl(key,"input");
  return edited==visible?original:edited;
 }
 string PathInput(string key,string value){
  const int maxLength=260;string original=value??"",visible=original.Length>maxLength?original.Substring(original.Length-maxLength):original;
  GUI.SetNextControlName("edit:"+key);string edited=GUILayout.TextArea(visible,maxLength,textAreaField,GUILayout.Width(220),GUILayout.Height(64));
  return edited==visible?original:edited;
 }
 void HandleKeyboardEvent(Event e){
  cameraTextFocused=(GUI.GetNameOfFocusedControl()??"").StartsWith("edit:",StringComparison.Ordinal);
  if(e.type!=EventType.KeyDown)return;
  if(HandleShortcut(e.keyCode,e.control,e.shift,e.alt||e.command,cameraTextFocused)){if(e.keyCode==KeyCode.Escape)GUI.FocusControl(null);e.Use();GUIUtility.ExitGUI();}
 }
 // One dispatch path for actual IMGUI events and runtime regression checks.
 bool HandleShortcut(KeyCode key,bool control=false,bool shift=false,bool otherModifier=false,bool editing=false){
  if(key==KeyCode.F1&&!control&&!shift&&!otherModifier){ToggleHelp();return true;}
  if(key==KeyCode.Escape&&!control&&!shift&&!otherModifier){CancelInteraction();ClosePlanAuthoring();compactPanel="";showHelp=showFiles=showQa=showComponentEditor=showReset=false;return true;}
  if(editing||CompactPanelOpen||showHelp||showFiles||showQa||showComponentEditor||showPlanAuthoring||showReset||dragging||resizingWall||scalingEquipment||marquee||otherModifier)return false;
  if(control&&!shift&&key==KeyCode.V){PasteSceneSelection();return true;}
  if(!control&&!shift&&key==KeyCode.V){ToggleCameraView();return true;}
  if(!control&&!shift&&key==KeyCode.C){cutaway=!cutaway;Rebuild();status=cutaway?"Cutaway walls on.":"Cutaway walls off.";return true;}
  if(!top&&!control&&(key==KeyCode.LeftArrow||key==KeyCode.RightArrow||key==KeyCode.UpArrow||key==KeyCode.DownArrow))return true;
  if(tool=="Polygon"){if(!control&&!shift&&(key==KeyCode.Return||key==KeyCode.KeypadEnter)){try{FinishComponentDrawing();}catch(Exception e){status=e.Message;}return true;}return false;}
  if(key==KeyCode.R&&tool!="Select")return false;
  int scaleDirection=PrecisionEditing.EquipmentScaleDirection(key,shift);
  if(!control&&tool=="Select"&&scaleDirection!=0){ScaleSelectionShortcut(scaleDirection);return true;}
  if(control){
   if(!shift&&key==KeyCode.Z){Undo(-1);return true;}
   if(!shift&&key==KeyCode.Y){Undo(1);return true;}
   if(!shift&&key==KeyCode.C){CopySceneSelection();return true;}
   if(!shift&&key==KeyCode.L){ToggleSelectionLock();return true;}
   if(!shift&&key==KeyCode.S){Save();return true;}
   if(!shift&&key==KeyCode.A){SelectAll();return true;}
   if(key==KeyCode.G){GroupSelection(shift);return true;}
  }else if(!shift){
   if(top&&tool=="Select"){
    if(key==KeyCode.LeftArrow){NudgeSelection(Vector3.left);return true;}
    if(key==KeyCode.RightArrow){NudgeSelection(Vector3.right);return true;}
    if(key==KeyCode.UpArrow){NudgeSelection(Vector3.forward);return true;}
    if(key==KeyCode.DownArrow){NudgeSelection(Vector3.back);return true;}
   }
   if(key==KeyCode.Delete){Delete();return true;}
   if(key==KeyCode.R){RotateSelectionShortcut(15);return true;}
  }else if(key==KeyCode.R){
   RotateSelectionShortcut(-15);return true;
  }
  return false;
 }
 void CancelInteraction(){
  scenePointerEvents.Clear();CancelWallResize();CancelEquipmentScale();showSnapTarget=false;
  if(dragging&&dragMoved){
   if(dragConnectedBefore!=null)design=dragConnectedBefore;
   else foreach(var item in SelectedItems)if(dragPositions.TryGetValue(item.id,out var start)){item.x=start.x;item.y=start.y;item.z=start.z;}
   dirty=false;Rebuild();
  }
  dragConnectedBefore=null;
  componentPoints.Clear();
  dragging=dragMoved=marquee=false;pendingToggleOff="";wallStart=null;tool="Select";
 }
 void ToggleHelp(){
  // Cancel an uncommitted pointer gesture; opening help never creates a history entry.
  CancelInteraction();showHelp=!showHelp;showFiles=showQa=showReset=false;helpScroll=Vector2.zero;
 }
 void BeginRoomReset(){
  if(dirty)Commit();CancelInteraction();ClosePlanAuthoring();showHelp=showFiles=showQa=showComponentEditor=false;resetScroll=Vector2.zero;showReset=true;
 }
 void ResetRoom(){
  try{
   if(dirty)Commit();var next=Design.ResetContents(design);CancelInteraction();ClosePlanAuthoring();InvalidatePlanWork();
   fileTask=workspaceTask=qaTask=floorPlanTask=ctImportTask=null;
   qa=null;qaJson=qaInput=qaError=null;ctLatest=null;ctResultsPopup=false;ctCompareFirst=ctCompareSecond=ctImportFingerprint="";ctInputDrafts.Clear();
   design=next;ClearSelection();activeDoorId="";generationBatchId="";generationNewBatch=false;generationOptionsBatchKey="";generationOptionsEpoch=-1;
   previewCorrections.Clear();previewCorrectionIndex=-1;cameraWalkVelocity=Vector3.zero;showReset=false;compactPanel="";tab="Room";
   Commit();ClearGenerationPreview();status="Room reset. Undo restores its previous contents; saved files and presets are unchanged.";
  }catch(Exception error){status="Reset failed: "+error.Message;}
 }
 void ResetModal(){
  bool enabled=GUI.enabled,changed=GUI.changed;GUI.enabled=true;
  float width=Mathf.Min(520,W-24),height=Mathf.Min(300,H-80);var rect=new Rect((W-width)/2,(H-height)/2,width,height);GUI.DrawTexture(rect,card);
  GUILayout.BeginArea(new Rect(rect.x+20,rect.y+20,rect.width-40,rect.height-40));GUILayout.Label("Reset room?",sub);
  resetScroll=GUILayout.BeginScrollView(resetScroll);
  GUILayout.Label("Clear all placed contents, floor-plan data and calculation history, including protected objects?",body);
  GUILayout.Space(8);GUILayout.Label(design.items.Count+" objects / "+(design.regions?.Count??0)+" occupied regions",small);
  GUILayout.Label("Room and shielding settings, presets and saved files remain. Undo restores the cleared contents.",small);
  if(design.width<3||design.depth<3||design.height<2)GUILayout.Label("Native room size after reset: "+F(Mathf.Max(3,design.width))+" x "+F(Mathf.Max(3,design.depth))+" x "+F(Mathf.Max(2,design.height))+" m.",small);
  GUILayout.EndScrollView();GUILayout.BeginHorizontal();bool confirm=Btn("Reset room"),cancel=Btn("Cancel");GUILayout.EndHorizontal();GUILayout.EndArea();GUI.enabled=enabled;GUI.changed=changed;
  if(confirm){ResetRoom();GUIUtility.ExitGUI();}if(cancel){showReset=false;GUI.FocusControl(null);}
 }
 void ToggleSelectionLock(){
  if(selection.Count==0){status="Select objects to protect or unlock.";return;}
  if(dirty)Commit();
  bool value=SelectionEditing.ToggleLock(design,selection);numberBuffers.Clear();Commit();Rebuild();
  status=value?"Objects locked in place. Uncheck Lock in place or use Ctrl+L to edit again.":"Objects unlocked.";
 }
 const string HelpContent="V  — Toggle between 2D plan and 3D view\nCtrl+Z  — Undo design change\nCtrl+Y  — Redo design change\nDelete  — Delete selected objects\nCtrl+C  — Copy selected scene objects\nCtrl+V  — Paste unlocked copies\nCtrl+L  — Lock in place / unlock selected objects\nF1  — Open / close help\nEscape  — Close a popup or cancel movement / drawing\nCtrl+S  — Save design\nCtrl+A  — Select all objects\nCtrl+G  — Lock together as a group\nCtrl+Shift+G  — Unlock group\nR  — Rotate selection clockwise 15 degrees\nShift+R  — Rotate selection counter-clockwise 15 degrees\nArrow keys  — Nudge selected objects in 2D; hold to move the camera in 3D\n\nSELECTION AND NAVIGATION\nShift / Ctrl-click adds or removes objects. Drag empty space to box select. The Multi-select toggle works without a keyboard. Drag selected objects to move them together. Right drag pans in 2D or orbits in 3D; the mouse wheel zooms. In 3D, hold Up/Down to move forward/backward and Left/Right to move sideways along the camera heading.\nCopy expands selected groups, keeps their spacing, doors and component shapes, and leaves the design unchanged. Paste selects the new objects, moves each repeated copy another 0.5 m in X/Z, gives objects/groups/doors fresh IDs, and supports undo. A copied LINAC cannot be pasted while a LINAC already exists. The Object tab also has Copy and Paste buttons for browsers that reserve these keys.\n\nREUSABLE COMPONENTS\nThe Components tab manages presets and JSON import/export. Draw custom footprint adds corners; Enter or Finish footprint closes it, and Escape cancels. Edit component geometry changes a placed object. Save as predefined component makes an independent reusable preset. Custom shapes are saved in native designs and component JSON, but are not supported by reference QA or canonical ProShield export.\n\nLOCKING AND GROUPING\nLock in place prevents property edits, movement, rotation, grouping and deletion. Select a locked object and uncheck the box to edit it. A locked member blocks changes to the entire selection. Linked room resizing is blocked when it would change a locked wall. Undo / redo can restore prior lock states and geometry.\nLock together saves a group that moves as one. Lock state, groups and opacity persist in native designs; older designs start unlocked.\n\nPRECISION AND SNAPPING\nPrecision settings control move and resize increments (initially 0.01 m), thickness steps in mm, and dimensionless scale steps. +/− buttons use these increments; sliders remain continuous. Exact numeric entries bypass snapping. Snap to grid and Snap to wall endpoints / edges are independent and initially off. The display grid is 1 m and its visibility has no effect on snapping. The snap grid uses world X/Z at origin 0/0; half cells round away from zero. Wall targets take priority within the displayed tolerance, but snapping alone does not join physical wall footprints. Yellow markers show the active target.\nChoose Start / Center / End as the wall length anchor. In 2D, drag either endpoint handle to resize around the opposite end; Escape cancels. Nudges and gestures preserve group spacing and respect locked objects. Precision preferences persist in native designs and remain unchanged by transform undo/redo.\n\nFLOOR-PLAN TRACING\nOpen Floor plan and choose PNG/JPEG or RoomStudio.FloorPlan v1 JSON. Set metres per pixel (for example, 1000 pixels spanning 10 metres = 0.01 m/px), opacity, position and dimensions. Trace walls over guide starts wall drawing. Escape returns to selection. Turn off both snap assists for free tracing. Width and height can correct scan distortion; metres per pixel resets both dimensions to uniform scale.\nSave design embeds the guide and its display settings. Export guide JSON creates a reusable source file; canonical ProShield export retains the guide under room.floorPlan as editor metadata. The guide is hidden in 3D and never becomes shielding or a QA barrier.\n\nCALIBRATION AND COLOR MASKS\nAfter loading an image, open Calibrate / color rules. Confirm a uniform scale using two source-image points and a positive distance, or explicit m/px, mm/px or px/m. The top-left pixel center is (0.5,0.5); image Y runs downward and maps to decreasing world Z before guide rotation. Zoom and pan only change the preview.\nAdd rules and pick target colors from the original image. Choose RGB or circular-hue HSV tolerance, priority and minimum area. Lower priority numbers win; stable rule IDs break ties. Wall, Opening, Reference and Ignore categories stay separate. Physical material is an explicit choice, independent of target/display color. Preview color mask processes original pixels cooperatively (up to 4 MP and 32 rules); Cancel keeps the previous preview.\nApply settings saves calibration/rules in one undo step without creating objects. Close or Escape discards unapplied edits. Native saves and canonical room.floorPlan metadata retain settings; standalone guide v1 export keeps only the image and guide settings. Automatic wall-path generation, physical joins and regeneration are still pending.\n\nDOORS\nSelect one wall in Object, then use Click wall to cut / place door in the 2D plan. Edit its wall-local offset, width, height, physical panel material and user-entered lead lining in Object. The opening follows the wall and can be removed there. Lock in place prevents door edits. Save design retains doors; reference QA and canonical ProShield export reject rooms with openings until aperture calculations are supported.\n\nLINAC HD BEAM PREVIEW\nThe orange line starts just below the supplied Linac HD model's Beam_Window, follows the moving aperture, and aims at the configured isocentre. The gantry-angle control rotates the treatment head and its source and imaging arms around the configured isocentre; the gantry rings, bore and LINAC root stay fixed. Reference QA continues to use the stored gantry angle through its existing input path. This preview is not a dose calculation.\n\nKEYBOARD FOCUS\nDesign shortcuts pause in text/numeric inputs, popups and mouse gestures. Rotation also pauses while placing equipment, drawing walls or editing components. F1 and Escape stay available. Click the scene or a button to leave a text field. Browsers may reserve Ctrl+L: use the Lock in place checkbox in the Object tab.\n\nCOPY THIS HELP\nSelect text here and press Ctrl+C. In WebGL, Copy / select help opens browser-native selectable text. Copying or opening help never changes the design.";
 static readonly string UpdatedHelpContent=HelpContent
  .Replace("V  — Toggle between 2D plan and 3D view\nCtrl+Z", "V  — Toggle between 2D plan and 3D view\nC  — Toggle cutaway walls\nCtrl+Z")
  .Replace("Automatic wall-path generation, physical joins and regeneration are still pending.", "Generate connected walls previews source-pixel centerlines and physical joins before one-step Apply. Select existing walls in Object to preview Connect wall edges. Native saves retain source paths and regeneration diffs.")
  .Replace("A copied LINAC cannot be pasted while a LINAC already exists.", "LINACs can be pasted repeatedly like other equipment. The first LINAC remains the calculation and beam-visual source; additional units are layout instances.")
  .Replace("R  — Rotate selection clockwise 15 degrees", "+ / -  — Scale selected equipment (keypad supported)\nShift  — Constrain wall drawing / endpoint direction to 45 degrees\nR  — Rotate selection clockwise 15 degrees");
 void HelpModal(){
  bool enabled=GUI.enabled;bool changed=GUI.changed;GUI.enabled=true;
  float width=Mathf.Min(760,W-64),height=Mathf.Min(720,H-80);
  var rect=new Rect((W-width)/2,(H-height)/2,width,height);GUI.DrawTexture(rect,card);
  GUILayout.BeginArea(new Rect(rect.x+24,rect.y+20,rect.width-48,rect.height-40));
  GUILayout.Label("Help & keyboard shortcuts",title);
  GUILayout.Label("Select text and press Ctrl+C, or use Copy / select help.",small);
  helpScroll=GUILayout.BeginScrollView(helpScroll,GUILayout.Height(Mathf.Max(80,rect.height-160)));
  GUI.SetNextControlName("helpSelectableText");
    GUILayout.TextArea(UpdatedHelpContent,body,GUILayout.Height(body.CalcHeight(new GUIContent(UpdatedHelpContent),rect.width-76)+16));
  GUILayout.EndScrollView();GUILayout.Space(10);
  GUILayout.BeginHorizontal();
    if(Btn("Copy / select help"))BrowserBridge.ShowCopyableHelp(UpdatedHelpContent);
  if(Btn("Close help / F1 / Escape")){showHelp=false;GUI.FocusControl(null);}
  GUILayout.EndHorizontal();GUILayout.EndArea();GUI.enabled=enabled;GUI.changed=changed;
 }
 void RotateSelectionShortcut(float degrees){if(selection.Count==0)return;Design before=null;try{
  if(SelectedItems.Any(i=>i.locked)){status="Unlock protected objects before rotating this selection.";return;}
  if(SelectedItems.Any(i=>IsJoinedWall(i.id)))before=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  SelectionEditing.Rotate(SelectedItems,degrees,design);PropagateObjectPanelEdit(before);Commit();Rebuild();
  status=degrees<0?"Rotated selection counter-clockwise.":"Rotated selection clockwise.";
 }catch(Exception e){if(before!=null){design=before;Rebuild();}status=e.Message;}}
 void ScaleSelectionShortcut(int direction){
  try{
   if(dirty)Commit();PrecisionEditing.ScaleEquipment(SelectedItems,Precision.scaleStep,direction);
   numberBuffers.Clear();Commit();Rebuild();status=direction>0?"Equipment scale increased.":"Equipment scale decreased.";
  }catch(Exception error){status=error.Message;}
 }
 void ShortcutSmokeChecks(){
  string original=JsonUtility.ToJson(design);bool originalTop=top,originalCutaway=cutaway;var item=design.items[6];string id=item.id;Choose(id);
  HandleShortcut(KeyCode.L,true);if(!Current.locked)throw new Exception("Ctrl+L did not protect selection");
  string protectedJson=JsonUtility.ToJson(design);int protectedHistory=historyIndex;
  HandleShortcut(KeyCode.Delete);HandleShortcut(KeyCode.R);
  if(protectedJson!=JsonUtility.ToJson(design)||protectedHistory!=historyIndex||!selection.Contains(id))throw new Exception("Protected shortcut changed object/history/selection");
  HandleShortcut(KeyCode.Z,true);if(design.items.Find(i=>i.id==id).locked)throw new Exception("Ctrl+Z lock undo failed");
  HandleShortcut(KeyCode.Y,true);Choose(id);if(!Current.locked)throw new Exception("Ctrl+Y lock redo failed");
  LoadJson(JsonUtility.ToJson(design),"Shortcut-smoke");Choose(id);if(!Current.locked)throw new Exception("Lock native persistence failed");
  protectedJson=JsonUtility.ToJson(design);protectedHistory=historyIndex;
  foreach(var key in new[]{KeyCode.Z,KeyCode.Y,KeyCode.L,KeyCode.A,KeyCode.G,KeyCode.S})if(HandleShortcut(key,true,false,false,true))throw new Exception("Shortcut captured a text field: "+key);
  if(HandleShortcut(KeyCode.Delete,false,false,false,true)||HandleShortcut(KeyCode.R,false,false,false,true)||HandleShortcut(KeyCode.R,false,true,false,true)||HandleShortcut(KeyCode.V,false,false,false,true)||HandleShortcut(KeyCode.C,false,false,false,true)||HandleShortcut(KeyCode.UpArrow,false,false,false,true))throw new Exception("Editing text captured scene action");
  top=true;cutaway=false;string beforeView=JsonUtility.ToJson(design);int beforeViewHistory=historyIndex;
  if(!HandleShortcut(KeyCode.C)||!cutaway||beforeView!=JsonUtility.ToJson(design)||beforeViewHistory!=historyIndex)throw new Exception("C did not enable cutaway without editing the design");
  if(!HandleShortcut(KeyCode.C)||cutaway||beforeView!=JsonUtility.ToJson(design)||beforeViewHistory!=historyIndex)throw new Exception("C did not disable cutaway without editing the design");
  if(!HandleShortcut(KeyCode.C,true)||cutaway||beforeView!=JsonUtility.ToJson(design)||beforeViewHistory!=historyIndex)throw new Exception("Ctrl+C changed cutaway or design");
  if(!HandleShortcut(KeyCode.V)||top||cutaway||beforeView!=JsonUtility.ToJson(design)||beforeViewHistory!=historyIndex)throw new Exception("V did not switch to 3D without editing the design");
  Vector3 beforeFocus=focus;float beforeYaw=yaw;cameraWalkVelocity=Vector3.zero;
  if(!HandleShortcut(KeyCode.UpArrow)||beforeView!=JsonUtility.ToJson(design)||beforeViewHistory!=historyIndex)throw new Exception("3D arrow key edited the selected object");
  yaw=0;StepCameraWalk(0,1,.016f);float initialWalkSpeed=cameraWalkVelocity.magnitude;
  for(int frame=0;frame<19;frame++)StepCameraWalk(0,1,.016f);
  if(focus.z<=beforeFocus.z+.01f||Mathf.Abs(focus.x-beforeFocus.x)>.001f||cameraWalkVelocity.magnitude<=initialWalkSpeed||beforeView!=JsonUtility.ToJson(design))throw new Exception("3D camera walk did not accelerate forward without editing the design");
  focus=beforeFocus;yaw=beforeYaw;cameraWalkVelocity=Vector3.zero;
  if(!HandleShortcut(KeyCode.V)||!top||!cutaway||beforeView!=JsonUtility.ToJson(design)||beforeViewHistory!=historyIndex)throw new Exception("V did not switch back to 2D plan without editing the design");
  top=originalTop;cutaway=originalCutaway;Rebuild();
  string previousTool=tool;foreach(var mode in new[]{"Wall","Component","Polygon","Desk","LINAC","Model"}){tool=mode;if(HandleShortcut(KeyCode.R)||HandleShortcut(KeyCode.R,false,true))throw new Exception("Rotation captured scene interaction: "+mode);}tool=previousTool;
  HandleShortcut(KeyCode.F1);if(!showHelp)throw new Exception("F1 did not open help");
  if(HandleShortcut(KeyCode.L,true))throw new Exception("Help allowed design shortcut");
  HandleShortcut(KeyCode.F1);if(showHelp)throw new Exception("F1 did not close help");
  HandleShortcut(KeyCode.F1);HandleShortcut(KeyCode.Escape);if(showHelp)throw new Exception("Escape did not close help");
  if(protectedJson!=JsonUtility.ToJson(design)||protectedHistory!=historyIndex)throw new Exception("Help or text input mutated design");
  HandleShortcut(KeyCode.L,true);if(Current.locked)throw new Exception("Ctrl+L did not unlock");
  Choose(id);float beforeRotation=Current.angle;HandleShortcut(KeyCode.R);float clockwise=Current.angle;HandleShortcut(KeyCode.R,false,true);float reverse=Current.angle;if(Mathf.Abs(Mathf.DeltaAngle(beforeRotation,clockwise)-15)>.001f||Mathf.Abs(Mathf.DeltaAngle(clockwise,reverse)+15)>.001f)throw new Exception("Rotation direction shortcuts failed");
  HandleShortcut(KeyCode.Delete);if(design.items.Any(i=>i.id==id))throw new Exception("Delete shortcut failed");
  HandleShortcut(KeyCode.Z,true);if(!design.items.Any(i=>i.id==id))throw new Exception("Delete shortcut undo failed");
  HandleShortcut(KeyCode.Y,true);if(design.items.Any(i=>i.id==id))throw new Exception("Delete shortcut redo failed");
  design=JsonUtility.FromJson<Design>(original);ClearSelection();Commit();Rebuild();
  SceneClipboardRuntimeChecks();
  Debug.Log("ROOM_STUDIO_SHORTCUT_RUNTIME_PASSED: C cutaway toggle, V camera toggle, smooth 3D arrow movement, Ctrl+V paste, undo, redo, delete, protection, persistence, text focus and help lifecycle");
 }
}
}
