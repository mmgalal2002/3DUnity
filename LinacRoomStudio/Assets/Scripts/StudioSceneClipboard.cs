using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 SceneClipboardPayload sceneClipboard;
 int scenePasteCount;
 void CopySceneSelection(){
  try{
   sceneClipboard=SceneClipboard.Copy(design,selection,selected);scenePasteCount=0;
   status="Copied "+sceneClipboard.items.Count+" scene object"+(sceneClipboard.items.Count==1?"":"s")+". Paste creates unlocked copies.";
  }catch(Exception e){status=e.Message;}
 }
 void PasteSceneSelection(){
  try{
   if(sceneClipboard==null)throw new Exception("Copy a scene object before pasting.");
   var pasted=SceneClipboard.PreparePaste(design,sceneClipboard,scenePasteCount+1);
   if(dirty)Commit();
   design.items.AddRange(pasted.items);
   DiagnosticData.ConfigureCopiedTargets(design,pasted.items);
   scenePasteCount++;
   ClearSelection();selection.UnionWith(pasted.items.Select(item=>item.id));selected=pasted.primaryId;
   tab="Object";tool="Select";wallStart=null;
   Commit();Rebuild();
   status="Pasted "+pasted.items.Count+" unlocked scene object"+(pasted.items.Count==1?"":"s")+". Undo restores the previous design.";
  }catch(Exception e){status=e.Message;}
 }
 void ClipboardControls(){
  bool enabled=GUI.enabled,changed=GUI.changed;
  GUILayout.BeginHorizontal();
  GUI.enabled=enabled&&selection.Count>0;if(Btn("Copy / Ctrl+C"))CopySceneSelection();
  GUI.enabled=enabled&&sceneClipboard!=null;if(Btn("Paste / Ctrl+V"))PasteSceneSelection();
  GUI.enabled=enabled;GUILayout.EndHorizontal();GUI.changed=changed;
 }
 void SceneClipboardRuntimeChecks(){
  string original=JsonUtility.ToJson(design),originalName=fileName,originalTool=tool,originalSelected=selected;
  var originalSelection=selection.ToArray();var previousClipboard=sceneClipboard;int previousPasteCount=scenePasteCount;
  bool previousHelp=showHelp;
  try{
   var wall=design.items.First(item=>item.kind=="Wall");wall.locked=true;Commit();Choose(wall.id);
   string beforeCopy=JsonUtility.ToJson(design);int beforeHistory=historyIndex,initialCount=design.items.Count;
   if(!HandleShortcut(KeyCode.C,true)||sceneClipboard==null||sceneClipboard.items.Count!=1||beforeCopy!=JsonUtility.ToJson(design)||beforeHistory!=historyIndex)
    throw new Exception("Ctrl+C changed the design or failed to copy selection.");
   if(HandleShortcut(KeyCode.C,true,false,false,true)||HandleShortcut(KeyCode.V,true,false,false,true)||beforeCopy!=JsonUtility.ToJson(design))
    throw new Exception("Copy/paste captured an active text field.");
   showHelp=true;
   if(HandleShortcut(KeyCode.C,true)||HandleShortcut(KeyCode.V,true)||beforeCopy!=JsonUtility.ToJson(design))throw new Exception("Copy/paste captured Help.");
   showHelp=false;
   tool="Polygon";
   bool pasteViewTop=top;
   if(!HandleShortcut(KeyCode.V,true)||top!=pasteViewTop||design.items.Count!=initialCount+1||Current==null||Current.locked||Current.id==wall.id)
    throw new Exception("Ctrl+V did not create and select an unlocked copy.");
   string pastedId=Current.id;int pastedHistory=historyIndex;
   Undo(-1);if(design.items.Count!=initialCount||design.items.Any(item=>item.id==pastedId))throw new Exception("Paste undo failed.");
   Undo(1);if(design.items.Count!=initialCount+1||!design.items.Any(item=>item.id==pastedId)||historyIndex!=pastedHistory)throw new Exception("Paste redo failed.");
   LoadJson(JsonUtility.ToJson(design),"Clipboard-smoke");
   if(!design.items.Any(item=>item.id==pastedId))throw new Exception("Pasted object did not persist in a native save.");
    var linac=design.items.First(item=>item.kind=="LINAC");Choose(linac.id);
    if(!HandleShortcut(KeyCode.C,true))throw new Exception("Ctrl+C failed to copy the LINAC.");
    int linacCount=design.items.Count(item=>item.kind=="LINAC");
    if(!HandleShortcut(KeyCode.V,true)||design.items.Count(item=>item.kind=="LINAC")!=linacCount+1||Current==null||Current.id==linac.id||Current.kind!="LINAC")
     throw new Exception("Ctrl+V failed to create a second LINAC.");
    string pastedLinacId=Current.id;int pastedLinacHistory=historyIndex;
    Undo(-1);if(design.items.Count(item=>item.kind=="LINAC")!=linacCount||design.items.Any(item=>item.id==pastedLinacId))throw new Exception("LINAC paste undo failed.");
    Undo(1);if(design.items.Count(item=>item.kind=="LINAC")!=linacCount+1||!design.items.Any(item=>item.id==pastedLinacId)||historyIndex!=pastedLinacHistory)throw new Exception("LINAC paste redo failed.");
    LoadJson(JsonUtility.ToJson(design),"Clipboard-smoke");
    if(!design.items.Any(item=>item.id==pastedLinacId))throw new Exception("Pasted LINAC did not persist in a native save.");
    Debug.Log("ROOM_STUDIO_SCENE_CLIPBOARD_RUNTIME_PASSED: shortcuts, text/help focus, unlocked copies, undo/redo, native load and repeated LINAC paste");
  }
  finally{
   design=JsonUtility.FromJson<Design>(original);ClearSelection();selection.UnionWith(SelectionEditing.Expand(design,originalSelection));
   selected=selection.Contains(originalSelected)?originalSelected:selection.FirstOrDefault()??"";
   sceneClipboard=previousClipboard;scenePasteCount=previousPasteCount;tool=originalTool;fileName=originalName;showHelp=previousHelp;dirty=false;Commit();Rebuild();
  }
 }
}
}
