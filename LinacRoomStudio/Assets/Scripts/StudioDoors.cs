using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 string activeDoorId="";

 void BuildWall(Item wall,Transform root){
  float shownHeight=cutaway?Mathf.Min(.35f,wall.height):wall.height;
  float depth=wall.shielding.thickness/1000f;
  var wallMaterial=selection.Contains(wall.id)?selectedMat:wallMat;
    if(!selection.Contains(wall.id)&&!WallGenerationData.IsEmpty(wall.generated)&&!IsJoinedWall(wall.id)){
     wallMaterial=Material(wall.generated.displayColor);appearanceMaterials.Add(wallMaterial);
    }
  if(!IsJoinedWall(wall.id))foreach(var solid in DoorGeometry.Solids(wall,shownHeight))
   Cube("Wall solid",new Vector3(solid.center,solid.bottom+solid.height/2,0),new Vector3(solid.width,solid.height,depth),wallMaterial,root);
  if(wall.doors!=null)foreach(var door in wall.doors){
   float panelHeight=Mathf.Min(shownHeight,door.height);
   float panelDepth=door.panel.thickness/1000f;
   var panelMaterial=Material(Color.white);ApplyShieldAppearance(panelMaterial,door.panel);appearanceMaterials.Add(panelMaterial);
   Cube(door.name+" · "+door.panel.material+" panel",new Vector3(door.center,panelHeight/2,0),new Vector3(Mathf.Max(.01f,door.width-.04f),panelHeight,panelDepth),panelMaterial,root);
   if(door.leadLiningMm>0){
    var leadMaterial=Material(Color.white);ApplyShieldAppearance(leadMaterial,new Barrier{material="Lead",thickness=door.leadLiningMm,density=11340});appearanceMaterials.Add(leadMaterial);
    float lining=door.leadLiningMm/1000f;
    Cube(door.name+" · "+door.leadLiningMm+" mm lead lining",new Vector3(door.center,panelHeight/2,panelDepth/2+lining/2),new Vector3(Mathf.Max(.01f,door.width-.04f),panelHeight,lining),leadMaterial,root);
   }
   // A narrow steel frame makes the cut distinguishable from a painted stripe in plan view.
   var frameMaterial=Material(new Color(.34f,.46f,.56f));appearanceMaterials.Add(frameMaterial);
   const float jamb=.025f;
   foreach(float side in new[]{-1f,1f})Cube("Door jamb",new Vector3(door.center+side*(door.width/2-jamb/2),panelHeight/2,0),new Vector3(jamb,panelHeight,Mathf.Max(depth,panelDepth)),frameMaterial,root);
   Cube("Door frame head",new Vector3(door.center,Mathf.Max(.01f,panelHeight-jamb/2),0),new Vector3(door.width,jamb,Mathf.Max(depth,panelDepth)),frameMaterial,root);
   if(top){
    var marker=Line("Door opening plan marker",new Color(1,.72f,.18f),.07f,root);
    marker.SetPosition(0,root.TransformPoint(new Vector3(door.center-door.width/2,shownHeight+.06f,0)));
    marker.SetPosition(1,root.TransformPoint(new Vector3(door.center+door.width/2,shownHeight+.06f,0)));
   }
  }
  if(selection.Contains(wall.id)){
   var outline=Line("Selection",new Color(.2f,1,.85f),.045f,root);outline.loop=true;outline.positionCount=4;
   for(int k=0;k<4;k++)outline.SetPosition(k,root.TransformPoint(new Vector3((k<2?-1:1)*wall.length/2,shownHeight+.02f,(k==0||k==3?-1:1)*Mathf.Max(depth/2,.06f))));
  }
 }

 void PlaceDoorAtPoint(Vector3 raw){
  try{
   if(!top)throw new Exception("Switch to 2D plan to place a door.");
   var wall=Current;if(selection.Count!=1||wall==null||wall.kind!="Wall")throw new Exception("Select one wall before placing a door.");
   float center=DoorGeometry.LocalCenter(wall,raw);
   DoorGeometry.Add(wall,DoorGeometry.New(center,wall.height));
  var placed=wall.doors.Last();activeDoorId=placed.id;tool="Select";Commit();Rebuild();status=placed.width<1?"Door fitted to short wall: "+F(placed.width)+" m wide.":"Door cut and placed. Edit its material and lead lining in Object.";
  }catch(Exception e){status=e.Message;}
 }
 void DoorUI(Item wall){
  Section("Door openings");
  GUILayout.Label("A door cuts a physical opening through this wall. Choose its panel material and user-entered lead lining thickness; no shielding rating is inferred.",small);
  bool enabled=GUI.enabled;GUI.enabled=enabled&&top&&!wall.locked&&(wall.doors==null||wall.doors.Count<DoorGeometry.MaxDoorsPerWall);
  if(Btn(tool=="Door"?"Click this wall in the plan...":"Click wall to cut / place door")){tool="Door";wallStart=null;GUI.changed=false;}
  GUI.enabled=enabled;
  if(!top)GUILayout.Label("Switch to 2D plan for click placement.",small);
  if(wall.doors==null||wall.doors.Count==0){GUILayout.Label("No door in this wall yet.",small);return;}
  if(!wall.doors.Any(x=>x.id==activeDoorId))activeDoorId=wall.doors[0].id;
  foreach(var door in wall.doors){
   if(Btn(door.name+"  ·  "+F(door.center)+" m",door.id==activeDoorId)){activeDoorId=door.id;GUI.changed=false;}
  }
  int index=wall.doors.FindIndex(x=>x.id==activeDoorId);if(index<0)return;
  var original=wall.doors[index];var draft=JsonUtility.FromJson<DoorOpening>(JsonUtility.ToJson(original));
  bool before=GUI.changed;GUI.changed=false;
  draft.name=TextInput("door:"+draft.id,draft.name);
  draft.center=Number("Door offset",draft.center,-wall.length/2,wall.length/2,"m");
  draft.width=Number("Door width",draft.width,DoorGeometry.MinimumWidth(wall.length),4,"m");
  draft.height=Number("Door height",draft.height,.5f,wall.height,"m");
  Section("Door panel material");
  string[] materials={"Steel","Lead","Wood","Concrete","Gypsum","PlateGlass"};
  for(int row=0;row<2;row++){GUILayout.BeginHorizontal();for(int col=0;col<3;col++){
  var material=materials[row*3+col];if(Btn(material=="Gypsum"?"Gypsum":CtCoefficientLibrary.MaterialLabel(material),draft.panel.material==material)){draft.panel.material=material;draft.panel.density=DoorDensity(material);draft.panel.ctApplicabilityReviewed=false;GUI.changed=true;}
  }GUILayout.EndHorizontal();}
  draft.panel.thickness=Number("Panel thickness",draft.panel.thickness,1,3000,"mm");
  draft.panel.density=Number("Panel density",draft.panel.density,100,25000,"kg/m3");
  draft.leadLiningMm=Number("Lead lining",draft.leadLiningMm,0,100,"mm");
  GUILayout.BeginHorizontal();foreach(float level in new[]{0f,1f,2f,3f,5f,10f})if(GUILayout.Button(level.ToString("0")+" mm")){draft.leadLiningMm=level;GUI.changed=true;}GUILayout.EndHorizontal();
  bool changed=GUI.changed;GUI.changed=before||changed;
  if(changed)try{DoorGeometry.Update(wall,original.id,draft);}catch(Exception e){GUI.changed=before;status=e.Message;}
  if(Btn("Remove selected door")){
   try{DoorGeometry.Remove(wall,original.id);activeDoorId="";GUI.changed=true;}catch(Exception e){status=e.Message;GUI.changed=before;}
  }
  GUILayout.Label("Door openings are excluded from reference QA and canonical ProShield export until the reference calculation supports apertures. Native Save design preserves them.",small);
 }
 static float DoorDensity(string material){
  switch(material){case "Lead":return 11340;case "Steel":return 7850;case "Wood":return 700;case "Glass":return 2500;case "Gypsum":return 800;default:return 2350;}
 }
}
}
