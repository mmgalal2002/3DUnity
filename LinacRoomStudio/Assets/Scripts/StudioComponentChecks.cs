using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 void ComponentSmokeChecks(){
  string original=JsonUtility.ToJson(design);var originalLibrary=componentLibrary;var originalStore=componentStore;string originalError=libraryError,originalId=componentId;
  string directory=Path.Combine(Application.temporaryCachePath,"ComponentRuntime-"+Guid.NewGuid().ToString("N"));
  try{
   componentLibrary=ComponentLibrary.Defaults();componentStore=new ComponentLibraryStore(directory);libraryError="";
   foreach(var preset in componentLibrary.components.ToArray()){
    componentId=preset.id;PlaceComponent(new Vector3(1,0,2));string id=Current.id;
    if(Current.kind=="Component"&&(objects[id].GetComponent<MeshCollider>()==null||objects[id].GetComponent<MeshFilter>().sharedMesh.vertexCount==0))throw new Exception("Component render/selection collider missing");
    LoadJson(JsonUtility.ToJson(design),"Component-smoke");Choose(id);if(Current==null||Current.name!=preset.name)throw new Exception("Component native save/load failed");
    HandleShortcut(KeyCode.L,true);string protectedJson=JsonUtility.ToJson(design);HandleShortcut(KeyCode.Delete);HandleShortcut(KeyCode.R);if(protectedJson!=JsonUtility.ToJson(design))throw new Exception("Protected component changed");HandleShortcut(KeyCode.L,true);Delete();
   }
   componentPoints.Add(new Vector3(-2,0,-2));componentPoints.Add(new Vector3(2,0,-2));componentPoints.Add(new Vector3(2,0,0));componentPoints.Add(new Vector3(0,0,0));componentPoints.Add(new Vector3(0,0,2));componentPoints.Add(new Vector3(-2,0,2));FinishComponentDrawing();
   string drawnId=Current.id;OpenComponentEditor(Current,"Saved custom component","",false);SaveComponentDraft();if(componentStore.Load().components.Count!=4)throw new Exception("Saving scene component did not persist preset");
   var saved=ActiveComponent;string originalPreset=JsonUtility.ToJson(saved);OpenComponentEditor(Current,"Edited scene component",drawnId,true);componentDraft.height=4;SaveComponentDraft();if(Current.height!=4)throw new Exception("Scene shape edit failed");Undo(-1);Choose(drawnId);if(Current.height!=3)throw new Exception("Shape edit undo failed");Undo(1);Choose(drawnId);if(Current.height!=4||JsonUtility.ToJson(saved)!=originalPreset)throw new Exception("Redo or independent preset failed");
   OpenComponentEditor(saved.item,"Renamed preset",saved.id,false);componentDraft.height=5;SaveComponentDraft();if(componentStore.Load().components.Find(p=>p.id==saved.id).item.height!=5||Current.height!=4)throw new Exception("Preset modification changed placed object");
   string exported=componentLibrary.Export(componentId);var parsed=ComponentLibrary.Parse(exported);if(parsed.components.Count!=1||parsed.components[0].name!="Renamed preset")throw new Exception("Component export failed");
   ImportComponents(exported);if(componentStore.Load().components.Count!=5)throw new Exception("Component JSON import failed");
   var next=ComponentLibrary.Clone(componentLibrary);next.components.RemoveAll(p=>p.id==componentId);CommitLibrary(next);if(componentStore.Load().components.Count!=4||Current.id!=drawnId)throw new Exception("Deleting preset affected scene instance");
   var loaded=componentStore.Load();componentLibrary=loaded;componentId=loaded.components.Last().id;PlaceComponent(new Vector3(4,0,3));if(Current.component.type!="Polygon"||Current.component.points.Count!=6)throw new Exception("Stored custom component reuse failed");
   Debug.Log("ROOM_STUDIO_COMPONENT_RUNTIME_PASSED: preset placement, mesh colliders, protection, native save/load, custom drawing, edit undo/redo, save/rename/delete/reuse and JSON export/import");
  }finally{
   componentLibrary=originalLibrary;componentStore=originalStore;libraryError=originalError;componentId=originalId;componentPoints.Clear();showComponentEditor=false;tool="Select";design=JsonUtility.FromJson<Design>(original);ClearSelection();Commit();Rebuild();if(Directory.Exists(directory))Directory.Delete(directory,true);
  }
 }
}
}
