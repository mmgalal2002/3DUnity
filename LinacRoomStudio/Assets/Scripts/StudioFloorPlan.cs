using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 FloorPlanData floorPlan=>Design.IsEmptyFloorPlan(design?.floorPlan)?null:design.floorPlan;
 Texture2D floorPlanTexture;
 string floorPlanTextureKey="";
#if !UNITY_WEBGL || UNITY_EDITOR
 string floorPlanPath="";
#endif
 GameObject floorPlanGuideObject;
 Material floorPlanMaterial;
 Mesh floorPlanMesh;
 System.Threading.Tasks.Task<string> floorPlanTask;
 [Serializable] class BrowserFloorPlanFile {public string name,payload;}

 void FloorPlanUpdate() {
  PlanAuthoringUpdate();
  WallGenerationUpdate();
  if(floorPlanTask!=null&&floorPlanTask.IsCompleted) {
   try {
    var file=JsonUtility.FromJson<BrowserFloorPlanFile>(floorPlanTask.GetAwaiter().GetResult());
    ApplyFloorPlanPayload(file.payload,file.name);
   }catch(Exception e){status="Floor plan import: "+e.Message;}
   floorPlanTask=null;
  }
  if(floorPlanGuideObject!=null)floorPlanGuideObject.SetActive(top&&floorPlan!=null&&floorPlan.visible);
 }
 void ClearFloorPlanRenderer() {
  if(floorPlanGuideObject!=null){floorPlanGuideObject.SetActive(false);Destroy(floorPlanGuideObject);}
  if(floorPlanMesh!=null)Destroy(floorPlanMesh);
  if(floorPlanMaterial!=null)Destroy(floorPlanMaterial);
  floorPlanGuideObject=null;floorPlanMesh=null;floorPlanMaterial=null;
 }
 void ClearFloorPlanTexture() {
  ClearFloorPlanRenderer();
  if(floorPlanTexture!=null)Destroy(floorPlanTexture);
  floorPlanTexture=null;floorPlanTextureKey="";
 }
 void FloorPlanAfterRebuild() {
  ClearFloorPlanRenderer();var p=floorPlan;
  if(p==null){ClearFloorPlanTexture();return;}
  if(p.kind=="Image"&&(floorPlanTexture==null||floorPlanTextureKey!=p.imageBase64)) {
   // Decode once per source change, never on opacity/position/selection edits.
   var texture=FloorPlanCodec.DecodeImage(p);
   if(floorPlanTexture!=null)Destroy(floorPlanTexture);
   floorPlanTexture=texture;floorPlanTextureKey=p.imageBase64;
  } else if(p.kind=="Vector"&&floorPlanTexture!=null){Destroy(floorPlanTexture);floorPlanTexture=null;floorPlanTextureKey="";}
  floorPlanGuideObject=new GameObject("Floor plan guide (visual only)");
  floorPlanGuideObject.transform.SetParent(world,false);
  floorPlanGuideObject.transform.localPosition=new Vector3(p.x,.03f,p.z);
  floorPlanGuideObject.transform.localRotation=Quaternion.Euler(0,p.rotation,0);
  floorPlanMesh=new Mesh{name="Floor-plan visual mesh"};
  if(p.kind=="Image") {
   float w=p.widthMeters/2,h=p.heightMeters/2;
   floorPlanMesh.vertices=new[]{new Vector3(-w,0,-h),new Vector3(w,0,-h),new Vector3(w,0,h),new Vector3(-w,0,h)};
   floorPlanMesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
   floorPlanMesh.triangles=new[]{0,2,1,0,3,2};
  } else {
   var vertices=new List<Vector3>();var triangles=new List<int>();
   foreach(var s in p.segments) {
    var a=new Vector3(s.start.x,0,s.start.y);var b=new Vector3(s.end.x,0,s.end.y);
    var across=Vector3.Cross((b-a).normalized,Vector3.up)*.0125f;int n=vertices.Count;
    vertices.Add(a-across);vertices.Add(a+across);vertices.Add(b+across);vertices.Add(b-across);
    triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
   }
   floorPlanMesh.SetVertices(vertices);floorPlanMesh.SetTriangles(triangles,0);
  }
  floorPlanMesh.RecalculateBounds();
  var shader=Resources.Load<Shader>("FloorPlanGuide");
  if(shader==null)throw new Exception("Floor-plan guide shader is missing from the build.");
  floorPlanMaterial=new Material(shader){mainTexture=p.kind=="Image"?floorPlanTexture:Texture2D.whiteTexture,
   color=p.kind=="Image"?new Color(1,1,1,p.opacity):new Color(.95f,.75f,.25f,p.opacity)};
  floorPlanGuideObject.AddComponent<MeshFilter>().sharedMesh=floorPlanMesh;
  var renderer=floorPlanGuideObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=floorPlanMaterial;
  renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  floorPlanGuideObject.SetActive(top&&p.visible);
 }
 void FloorPlanUI() {
  bool previousChanged=GUI.changed;GUI.changed=false;
  Section("Floor plan guide");
  Label("Load a PNG/JPEG image or vector JSON, set its real-world size, then trace walls in 2D.");
#if UNITY_WEBGL && !UNITY_EDITOR
  bool enabled=GUI.enabled;GUI.enabled=enabled&&floorPlanTask==null;
  if(Btn(floorPlanTask==null?"Choose floor-plan file":"Reading floor plan..."))floorPlanTask=BrowserBridge.ChooseFile(true);
  GUI.enabled=enabled;
#else
  if(Btn("Choose floor-plan file")) {
   try{string path=FloorPlanFilePicker.Open();if(!string.IsNullOrEmpty(path)){floorPlanPath=path;LoadFloorPlanFile(path);}}
   catch(Exception e){status="Floor plan import: "+e.Message;}
   GUIUtility.ExitGUI();
  }
  // Wrap path text inside a bounded field so it never creates horizontal scroll.
  const int maxPathLength=260;string pathValue=floorPlanPath??"",visiblePath=pathValue.Length>maxPathLength?pathValue.Substring(pathValue.Length-maxPathLength):pathValue;
  GUI.SetNextControlName("edit:floorPlanPath");
  string editedPath=GUILayout.TextArea(visiblePath,maxPathLength,textAreaField,GUILayout.Width(216),GUILayout.Height(64));
  if(editedPath!=visiblePath)floorPlanPath=editedPath;
  if(Btn("Load from path")){try{LoadFloorPlanFile(floorPlanPath.Trim().Trim('"'));}catch(Exception e){status="Floor plan import: "+e.Message;}GUIUtility.ExitGUI();}
#endif
  GUILayout.Label("PNG/JPEG: 18 MB, 16 MP, 8192 px per side.\nRoomStudio.FloorPlan v1 JSON: 25 MB.",small);
  var p=floorPlan;
  if(p!=null) {
   Section("Guide settings");
   bool visible=GUILayout.Toggle(p.visible," Show guide in 2D");
   float opacity=Slider("Guide opacity",p.opacity,0,1);
   float x=Number("Guide X",p.x,-10000,10000,"m"),z=Number("Guide Z",p.z,-10000,10000,"m");
   float rotation=Number("Guide rotation",p.rotation,-3600,3600,"deg");
   float width=p.widthMeters,height=p.heightMeters;
   if(p.kind=="Image") {
    float minScale=Mathf.Max(.000001f,.001f/Mathf.Min(p.pixelWidth,p.pixelHeight));
    float maxScale=Mathf.Min(10,10000f/Mathf.Max(p.pixelWidth,p.pixelHeight));
    float scale=Number("Metres per pixel",p.metersPerPixel,minScale,maxScale,"m/px");
    if(scale!=p.metersPerPixel){width=p.pixelWidth*scale;height=p.pixelHeight*scale;}
    GUILayout.Label("Scale example: 1000 px across 10 m = 0.01 m/px.",small);
   }
   float widthInput=Number("Guide width",width,p.kind=="Image"?Mathf.Max(.001f,p.pixelWidth*.000001f):.001f,p.kind=="Image"?Mathf.Min(10000,p.pixelWidth*10):10000,"m");
   float heightInput=Number("Guide height",height,.001f,10000,"m");
   width=widthInput;height=heightInput;
   if(visible!=p.visible||opacity!=p.opacity||x!=p.x||z!=p.z||rotation!=p.rotation||width!=p.widthMeters||height!=p.heightMeters) {
    var next=FloorPlanCodec.Clone(p);next.visible=visible;next.opacity=opacity;next.x=x;next.z=z;next.rotation=rotation;
    try{FloorPlanCodec.Resize(next,width,height);FloorPlanCodec.Validate(next);design.floorPlan=next;dirty=true;FloorPlanAfterRebuild();}
    catch(Exception e){status=e.Message;}
   }
   GUILayout.Label(p.sourceName,small);
   if(p.kind=="Image"&&Btn("Calibrate / color rules"))OpenPlanAuthoring();
   if(p.kind=="Image")GenerationUI();
   if(!PlanAuthoring.IsEmpty(p.authoring))GUILayout.Label("Guide v1 export contains image/size only. Native saves and ProShield metadata retain calibration and color rules.",small);
   if(Btn("Trace walls over guide")){tool="Wall";wallStart=null;top=true;cutaway=true;Rebuild();status="Click the two endpoints of each wall. Escape finishes tracing.";}
   if(Btn("Fit guide")){top=true;focus=new Vector3(p.x,0,p.z);zoom=Mathf.Clamp(Mathf.Max(p.widthMeters,p.heightMeters)*1.8f,5,90);}
   if(Btn("Export guide JSON")){try{ExportFloorPlan();}catch(Exception e){status="Guide export: "+e.Message;}}
   if(Btn("Clear guide")){ReplaceFloorPlan(null,"Floor-plan guide removed. Undo restores it.");GUIUtility.ExitGUI();}
  } else Label("No guide loaded. Your room and objects stay in place when a guide is imported.");
  GUILayout.Label("Save design keeps the embedded guide and display settings. The guide is hidden in 3D and excluded from shielding calculations.",small);
  GUI.changed=previousChanged;
 }
 void ReplaceFloorPlan(FloorPlanData next,string message) {
  if(dirty)Commit();CancelInteraction();design.floorPlan=next;numberBuffers.Clear();Commit();FloorPlanAfterRebuild();status=message;
 }
 void LoadFloorPlanFile(string path) {
  if(string.IsNullOrWhiteSpace(path))throw new Exception("Choose a PNG/JPEG or RoomStudio.FloorPlan JSON file.");
  var info=new FileInfo(path);if(info.Length>FloorPlanCodec.MaxJsonBytes)throw new Exception("Floor-plan file exceeds 25 MB.");
  ApplyFloorPlanBytes(File.ReadAllBytes(path),Path.GetFileName(path),path);
 }
 void ApplyFloorPlanBytes(byte[] bytes,string name,string path="")=>ReplaceFloorPlan(FloorPlanCodec.Read(bytes,name,path),"Floor plan loaded. Set its scale, then trace walls in 2D.");
 void ApplyFloorPlanPayload(string payload,string name)=>ReplaceFloorPlan(FloorPlanCodec.ReadPayload(payload,name),"Floor plan loaded. Set its scale, then trace walls in 2D.");
 void ApplyFloorPlanJson(string json,string name,string path="")=>ReplaceFloorPlan(FloorPlanCodec.ReadJson(json,name,path),"Vector/image guide loaded at its declared size in metres.");
 void ValidateFloorPlanImage(FloorPlanData p)=>FloorPlanCodec.ValidateImage(p);
 void ExportFloorPlan() {
  string text=FloorPlanCodec.Write(floorPlan),name=SafeName()+"-floor-plan.json";
  string path=Path.Combine(SaveDirectory,name);File.WriteAllText(path,text);BrowserBridge.SyncFiles();BrowserBridge.Download(name,text);status="Exported guide: "+path;
 }
}
}
