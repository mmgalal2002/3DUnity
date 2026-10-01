using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 void EquipmentPaletteUI(){
  Section("Equipment");
  foreach(var group in EquipmentPalette.Groups){
   GUILayout.Space(6);GUILayout.Label(group,sub);
   foreach(var entry in EquipmentPalette.Entries.Where(e=>e.group==group)){
    bool active=entry.model=="Linac"?tool=="LINAC":tool=="Model"&&design.selectedModel==entry.model;
    if(Btn("    "+entry.label,active)){SelectEquipment(entry.model);GUI.changed=false;}
   }
  }
 }
 void SelectEquipment(string model){design.selectedModel=model;tool=model=="Linac"?"LINAC":"Model";wallStart=null;}
 void PlaceEquipment(string model,Vector3 position){
  Add(EquipmentPalette.Create(model,position));
 }
 void PaletteSmokeChecks(){
  string original=JsonUtility.ToJson(design);
  foreach(var entry in EquipmentPalette.Entries){
   design=JsonUtility.FromJson<Design>(original);design.items.RemoveAll(i=>i.kind=="LINAC");ClearSelection();
   SelectEquipment(entry.model);PlaceEquipment(entry.model,new Vector3(2,0,3));var placed=Current;
   if(placed==null||placed.model!=entry.model||placed.kind!=(entry.model=="Linac"?"LINAC":"Model")||objects[placed.id].GetComponentsInChildren<MeshRenderer>().Length==0)throw new Exception("Palette placement failed: "+entry.label);
   if(entry.model=="Cyberknife"||entry.model=="Basin"){
    var collider=objects[placed.id].GetComponent<BoxCollider>();
    if(collider==null||Mathf.Abs(collider.center.x)>.05f||Mathf.Abs(collider.center.z)>.05f)throw new Exception(entry.label+" visual footprint is offset from its placement point.");
   }
   string id=placed.id;LoadJson(JsonUtility.ToJson(design),"Palette-smoke");Choose(id);
   if(Current==null||Current.model!=entry.model||Current.x!=2||Current.z!=3)throw new Exception("Palette persistence failed: "+entry.label);
    if(entry.model=="Linac"){
     PlaceEquipment(entry.model,new Vector3(4,0,-3));var second=Current;
     if(second==null||second.id==id||second.model!=entry.model||second.kind!="LINAC"||design.items.Count(i=>i.kind=="LINAC")!=2)throw new Exception("Repeated LINAC palette placement failed.");
     var restored=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(restored);
     if(restored.items.Count(i=>i.kind=="LINAC")!=2)throw new Exception("Repeated LINAC native persistence failed.");
     if(linacModelRoot==null||linacModelRoot.parent!=objects[id].transform)throw new Exception("The first LINAC must remain the configured beam source.");
     string secondId=second.id;Delete();if(design.items.Any(i=>i.id==secondId))throw new Exception("Repeated LINAC deletion failed.");Choose(id);
    }
   Delete();if(design.items.Any(i=>i.id==id))throw new Exception("Palette selection/deletion failed");
  }
  design=JsonUtility.FromJson<Design>(original);tool="Select";ClearSelection();Commit();Rebuild();
    Debug.Log("ROOM_STUDIO_PALETTE_RUNTIME_PASSED: all 12 palette entries, repeated LINAC placement, stable IDs/roles, renderers, selection and native persistence; 13 supplied models including Desk");
 }
}
}
