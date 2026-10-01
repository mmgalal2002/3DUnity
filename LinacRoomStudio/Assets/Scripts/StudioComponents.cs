using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 string leftTab="Build",componentId="",componentSearch="",componentName="",componentEditId="",componentError="",libraryError="";
#if !UNITY_WEBGL || UNITY_EDITOR
 string componentImportPath="";
#endif
 bool showComponentEditor,componentEditObject;
 ComponentLibrary componentLibrary;
 ComponentLibraryStore componentStore;
 Item componentDraft;
 Vector2 componentEditorScroll;
 readonly List<Vector3> componentPoints=new List<Vector3>();
 readonly List<Mesh> componentMeshes=new List<Mesh>();
 LineRenderer componentDrawing;
 System.Threading.Tasks.Task<string> componentImportTask;
 ComponentPreset ActiveComponent=>componentLibrary?.components.Find(p=>p.id==componentId);
 void InitializeComponents(){
  componentStore=new ComponentLibraryStore(Path.Combine(Application.persistentDataPath,"Components"));
  try{componentLibrary=componentStore.Load();componentId=componentLibrary.components.FirstOrDefault()?.id??"";}
  catch(Exception e){componentLibrary=new ComponentLibrary();libraryError="Component library could not be loaded: "+e.Message;status=libraryError;}
 }
 void CommitLibrary(ComponentLibrary next){
  if(!string.IsNullOrEmpty(libraryError))throw new Exception(libraryError+" Restore components.json from its .bak file before editing the library.");
  componentStore.Save(next);componentLibrary=next;BrowserBridge.SyncFiles();
 }
 void ComponentAction(Action action){try{action();componentError="";}catch(Exception e){componentError=e.Message;status=e.Message;}GUI.changed=false;}
 void SelectComponent(string id){
  var preset=componentLibrary.components.Find(candidate=>candidate.id==id);if(preset==null){status="Select a saved component first.";return;}
  CancelInteraction();componentId=id;componentError="";tool="Component";if(Layout.compact)compactPanel="";status="Click the room to place "+preset.name+".";
 }
 void ComponentsUI(){
  Section("Predefined components");
  GUILayout.Label("Reusable walls and custom footprints. Preset edits affect future placements; placed objects keep their own geometry.",small);
  if(!string.IsNullOrEmpty(libraryError))GUILayout.Label(libraryError,small);
  componentSearch=TextInput("componentSearch",componentSearch);GUILayout.Label("Filter by component name",small);
  foreach(var preset in componentLibrary.components.Where(p=>p.name.IndexOf(componentSearch,StringComparison.OrdinalIgnoreCase)>=0).ToArray())if(Btn(preset.name,componentId==preset.id)){SelectComponent(preset.id);GUI.changed=false;}
  if(componentLibrary.components.Count==0)Label("No saved components. Create a shape or import a component JSON file.");
  var active=ActiveComponent;
  if(active!=null){
   GUILayout.Label(active.item.kind=="Wall"?"Straight wall • reference QA supported":"Custom geometry • reference QA unavailable",small);
  if(Btn("Place selected component")){SelectComponent(active.id);GUI.changed=false;}
   if(Btn("Edit / rename preset"))OpenComponentEditor(active.item,active.name,active.id,false);
   if(Btn("Duplicate preset"))OpenComponentEditor(active.item,active.name+" copy","",false);
   if(Btn("Delete preset"))ComponentAction(()=>{var next=ComponentLibrary.Clone(componentLibrary);next.components.RemoveAll(p=>p.id==componentId);CommitLibrary(next);componentId=next.components.FirstOrDefault()?.id??"";status="Preset deleted. Placed objects are unchanged; the previous library is kept as components.json.bak.";});
   if(Btn("Export preset JSON"))ComponentAction(()=>ExportComponents(active.id));
  }
  Section("Create a component");
  if(Btn("New trapezoidal wall"))NewComponent("Trapezoid","Trapezoidal wall");
  if(Btn("New rounded wall"))NewComponent("Rounded","Rounded wall");
  if(Btn("New custom polygon"))NewComponent("Polygon","Custom polygon");
  if(Btn("Draw custom footprint")){CancelInteraction();tool="Polygon";top=true;status="Click 3 or more footprint corners, then Finish footprint. Esc cancels.";}
  if(tool=="Polygon"){
   Label(componentPoints.Count+" footprint vertices");
   if(Btn("Remove last vertex")&&componentPoints.Count>0)componentPoints.RemoveAt(componentPoints.Count-1);
   if(Btn("Finish footprint"))ComponentAction(FinishComponentDrawing);
   if(Btn("Cancel footprint"))CancelInteraction();
  }
  if(selection.Count==1&&Current!=null&&(Current.kind=="Wall"||Current.kind=="Component")&&Btn("Save selected object as preset"))OpenComponentEditor(Current,Current.name,"",false);
  GUILayout.Label("Drawing uses the independent snap controls below.",small);
  Section("Library files");
  if(Btn("Export entire library JSON"))ComponentAction(()=>ExportComponents(null));
#if UNITY_WEBGL && !UNITY_EDITOR
  if(componentImportTask==null&&Btn("Import component JSON"))componentImportTask=BrowserBridge.ChooseFile();
#else
  componentImportPath=PathInput("componentImportPath",componentImportPath);GUILayout.Label("Full path to an exported component JSON",small);
  if(Btn("Import component JSON"))ComponentAction(()=>ImportComponents(File.ReadAllText(componentImportPath.Trim().Trim('"'))));
  if(Btn("Open component files")){Directory.CreateDirectory(componentStore.DirectoryPath);Application.OpenURL(new Uri(componentStore.DirectoryPath).AbsoluteUri);}
#endif
  if(!string.IsNullOrEmpty(componentError))GUILayout.Label(componentError,small);
  GUILayout.Label("Custom shapes are preserved in native designs and preset JSON. Reference QA and canonical ProShield export cannot represent these shapes and will explain that limitation.",small);
 }
 void ExportComponents(string id){
  string filename=id==null?"component-library.json":"component-"+Guid.NewGuid().ToString("N")+".json";string json=componentLibrary.Export(id);
  string directory=Path.Combine(componentStore.DirectoryPath,"Exports");Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,filename),json);BrowserBridge.SyncFiles();BrowserBridge.Download(filename,json);status="Exported "+filename+" to Components/Exports; WebGL also downloads the JSON.";
 }
 void ImportComponents(string json){var next=ComponentLibrary.Clone(componentLibrary);int before=next.components.Count;next.Import(json);CommitLibrary(next);componentId=next.components.LastOrDefault()?.id??"";status="Imported "+(next.components.Count-before)+" components. Existing presets and placed objects are unchanged.";}
 void NewComponent(string type,string name){var shape=new ComponentShape{type=type};if(type=="Polygon")shape.points=new List<Vector2Data>{new Vector2Data(-2,-1),new Vector2Data(2,-1),new Vector2Data(2,1),new Vector2Data(-2,1)};OpenComponentEditor(new Item{kind="Component",name=name,height=3,component=shape},name,"",false);}
 void OpenComponentEditor(Item item,string name,string id,bool sceneObject){
  CancelInteraction();componentDraft=ComponentLibrary.Clone(item);componentName=name;componentEditId=id;componentEditObject=sceneObject;componentError="";componentEditorScroll=Vector2.zero;numberBuffers.Clear();showComponentEditor=true;showHelp=showFiles=showQa=false;
 }
 void SaveComponentDraft(){
  var candidate=ComponentLibrary.FromItem(componentDraft,componentName.Trim());
  if(componentEditObject){
   var item=design.items.Find(i=>i.id==componentEditId);if(item==null)throw new Exception("This scene object no longer exists.");SelectionEditing.RequireUnlocked(new[]{item});
   item.name=candidate.name;item.component=ComponentLibrary.Clone(candidate.item.component);item.height=candidate.item.height;item.length=candidate.item.length;item.shielding=ComponentLibrary.Clone(candidate.item.shielding);Commit();Rebuild();status="Component geometry updated. Undo restores the previous shape.";
  }else{
   var next=ComponentLibrary.Clone(componentLibrary);int index=next.components.FindIndex(p=>p.id==componentEditId);
   if(index>=0){candidate.id=componentEditId;next.components[index]=candidate;}else{if(next.components.Count>=100)throw new Exception("The library supports at most 100 presets.");next.components.Add(candidate);}
   CommitLibrary(next);componentId=candidate.id;status="Saved preset: "+candidate.name+". Existing placed objects are unchanged.";
  }
  showComponentEditor=false;componentError="";numberBuffers.Clear();
 }
 void PlaceComponent(Vector3 position){if(ActiveComponent==null){status="Select a component in the library first.";return;}Add(ComponentLibrary.Place(ActiveComponent,position));tab="Object";}
 void AddComponentPoint(Vector3 point){
  if(componentPoints.Count>=3&&Vector3.Distance(point,componentPoints[0])<.2f){try{FinishComponentDrawing();}catch(Exception e){status=e.Message;}return;}
  if(componentPoints.Count>=64){status="Maximum 64 footprint vertices.";return;}
  if(componentPoints.Count==0||Vector3.Distance(point,componentPoints[componentPoints.Count-1])>.01f)componentPoints.Add(point);
 }
 void FinishComponentDrawing(){
  if(componentPoints.Count<3)throw new Exception("Place at least 3 footprint vertices before finishing.");
  var center=new Vector3(componentPoints.Average(p=>p.x),0,componentPoints.Average(p=>p.z));
  var shape=new ComponentShape{type="Polygon",points=componentPoints.Select(p=>new Vector2Data(p.x-center.x,p.z-center.z)).ToList()};ComponentGeometry.Validate(shape);
  if(design.items.Count>=250)throw new Exception("Maximum 250 objects per project.");
  Add(new Item{kind="Component",name="Custom polygon",x=center.x,z=center.z,height=3,component=shape});componentPoints.Clear();tool="Select";tab="Object";status="Custom shape created. Edit its geometry or save it as a preset in Components.";
 }
 void UpdateComponentDrawing(Vector3 point,bool inView){
  if(componentDrawing==null)return;componentDrawing.enabled=tool=="Polygon"&&componentPoints.Count>0;if(!componentDrawing.enabled)return;
  componentDrawing.positionCount=componentPoints.Count+(inView?1:0);for(int n=0;n<componentPoints.Count;n++)componentDrawing.SetPosition(n,componentPoints[n]+Vector3.up*.08f);if(inView)componentDrawing.SetPosition(componentPoints.Count,point+Vector3.up*.08f);
 }
 void BuildComponent(Item item,GameObject root){
  float height=cutaway?Mathf.Min(.35f,item.height):item.height;var mesh=ComponentGeometry.Mesh(item.component,height);componentMeshes.Add(mesh);
  root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=selection.Contains(item.id)?selectedMat:wallMat;root.AddComponent<MeshCollider>().sharedMesh=mesh;
  if(selection.Contains(item.id)){var points=ComponentGeometry.Footprint(item.component);var outline=Line("Component selection",new Color(.2f,1,.85f),.045f,root.transform);outline.loop=true;outline.positionCount=points.Count;for(int n=0;n<points.Count;n++)outline.SetPosition(n,root.transform.TransformPoint(new Vector3(points[n].x,height+.02f,points[n].y)));}
 }
 void ComponentObjectUI(Item item){
  Label(item.component.type+" footprint • "+item.height.ToString("0.##")+" m high");
  if(Btn("Edit component geometry"))OpenComponentEditor(item,item.name,item.id,true);
  if(Btn("Save as predefined component")){leftTab="Components";OpenComponentEditor(item,item.name,"",false);}
  GUILayout.Label("Custom geometry is independent of linked room dimensions. Native save/load retains the full shape. Reference QA and canonical export do not support it yet.",small);
 }
 void ComponentEditorModal(){
  var rect=new Rect((W-Mathf.Min(920,W-48))/2,34,Mathf.Min(920,W-48),H-68);GUI.DrawTexture(rect,card);
  GUILayout.BeginArea(new Rect(rect.x+20,rect.y+16,rect.width-40,rect.height-32));GUILayout.Label(componentEditObject?"Edit placed component":"Predefined component editor",title);
  GUILayout.BeginHorizontal();GUILayout.BeginVertical(GUILayout.Width((rect.width-56)*.53f));
  componentEditorScroll=GUILayout.BeginScrollView(componentEditorScroll);
  Label("Component name");componentName=TextInput("presetName",componentName);
  componentDraft.height=Number("Height",componentDraft.height,.05f,100,"m");
  if(componentDraft.kind=="Wall"){componentDraft.length=Number("Length",componentDraft.length,.25f,60,"m");}
  else{
   var shape=componentDraft.component;Section(shape.type+" footprint");
   if(shape.type=="Trapezoid"){shape.width=Number("Front width",shape.width,.05f,60,"m");shape.topWidth=Number("Back width",shape.topWidth,.05f,60,"m");shape.depth=Number("Depth",shape.depth,.05f,60,"m");}
   else if(shape.type=="Rounded"){shape.radius=Number("Center radius",shape.radius,.1f,30,"m");shape.wallThickness=Number("Wall width",shape.wallThickness,.02f,10,"m");shape.sweep=Number("Arc sweep",shape.sweep,5,330,"deg");shape.segments=Mathf.RoundToInt(Number("Segments",shape.segments,4,48));}
   else{
    GUILayout.Label("Vertices follow the footprint perimeter in local X/Z metres. Concave shapes are supported; crossing edges and holes are not.",small);
    for(int n=0;n<shape.points.Count;n++){
     var point=shape.points[n];point.x=Number("Vertex "+(n+1)+" X",point.x,-100,100,"m");point.y=Number("Vertex "+(n+1)+" Z",point.y,-100,100,"m");
     GUILayout.BeginHorizontal();if(shape.points.Count>3&&Btn("Remove vertex")){shape.points.RemoveAt(n);numberBuffers.Clear();GUIUtility.ExitGUI();}
     if(shape.points.Count<64&&Btn("Insert after")){var after=shape.points[(n+1)%shape.points.Count];shape.points.Insert(n+1,new Vector2Data((point.x+after.x)/2,(point.y+after.y)/2));numberBuffers.Clear();GUIUtility.ExitGUI();}GUILayout.EndHorizontal();
    }
   }
   if(shape.type!="Polygon"&&Btn("Convert to editable polygon"))ComponentAction(()=>{var points=ComponentGeometry.Footprint(shape);if(points.Count>64)throw new Exception("Reduce curved-wall segments to 31 or fewer before converting.");shape.points=points.Select(p=>new Vector2Data(p.x,p.y)).ToList();shape.type="Polygon";numberBuffers.Clear();});
  }
  Section("Material properties");
  if(componentDraft.kind=="Wall")ShieldUI(componentDraft.shielding);
  else{
  foreach(var material in CtCoefficientLibrary.MaterialIds)if(Btn(CtCoefficientLibrary.MaterialLabel(material),componentDraft.shielding.material==material))componentDraft.shielding.material=material;
   componentDraft.shielding.density=Number("Density",componentDraft.shielding.density,100,25000,"kg/m3");GUILayout.Label("The footprint defines physical width. Material and density are saved with the geometry.",small);
  }
  GUILayout.EndScrollView();GUILayout.EndVertical();
  GUILayout.BeginVertical();GUILayout.Label("Footprint preview",sub);var preview=GUILayoutUtility.GetRect(260,260,GUILayout.ExpandWidth(true));DrawComponentPreview(preview);
  GUILayout.Label(componentDraft.kind=="Wall"?"This preset is a straight wall supported by reference QA.":"The shape is extruded to the chosen height. Custom shapes are saved as geometry; the current reference engine cannot assess their shielding.",small);
  GUILayout.Label(componentEditObject?"Apply changes edits this object only. Ctrl+Z restores its previous geometry.":"Saving changes the library only. Existing placed objects are independent copies.",small);
  if(!string.IsNullOrEmpty(componentError))GUILayout.Label(componentError,body);GUILayout.EndVertical();GUILayout.EndHorizontal();
  GUILayout.FlexibleSpace();GUILayout.BeginHorizontal();if(Btn(componentEditObject?"Apply to object":"Save predefined component")){ComponentAction(SaveComponentDraft);GUIUtility.ExitGUI();}
  if(Btn("Cancel / Escape")){showComponentEditor=false;GUIUtility.ExitGUI();}GUILayout.EndHorizontal();GUILayout.EndArea();
 }
 void DrawComponentPreview(Rect rect){
  GUI.DrawTexture(rect,inputTex);List<Vector2> points;
  try{points=componentDraft.kind=="Wall"?new List<Vector2>{new Vector2(-componentDraft.length/2,-componentDraft.shielding.thickness/2000),new Vector2(componentDraft.length/2,-componentDraft.shielding.thickness/2000),new Vector2(componentDraft.length/2,componentDraft.shielding.thickness/2000),new Vector2(-componentDraft.length/2,componentDraft.shielding.thickness/2000)}:ComponentGeometry.Footprint(componentDraft.component);}
  catch(Exception e){GUI.Label(new Rect(rect.x+12,rect.y+12,rect.width-24,rect.height-24),e.Message,body);return;}
  if(componentDraft.kind=="Component"&&componentDraft.component.type=="Polygon")points=componentDraft.component.points.Select(p=>new Vector2(p.x,p.y)).ToList();
  float minX=points.Min(p=>p.x),maxX=points.Max(p=>p.x),minZ=points.Min(p=>p.y),maxZ=points.Max(p=>p.y);float scale=Mathf.Min((rect.width-48)/Mathf.Max(maxX-minX,.02f),(rect.height-48)/Mathf.Max(maxZ-minZ,.02f));
  Func<Vector2,Vector2> map=p=>rect.center+new Vector2(p.x-(minX+maxX)/2,-p.y+(minZ+maxZ)/2)*scale;
  // Draw in the current GUI group's coordinates. Rotating GUI.matrix around a
  // local pivot misplaces lines when the whole interface is scaled.
  var color=GUI.color;
  for(int n=0;n<points.Count;n++){
   var a=map(points[n]);var b=map(points[(n+1)%points.Count]);int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y))));
   GUI.color=new Color(.2f,1,.85f);
   for(int step=0;step<=steps;step++){var p=Vector2.Lerp(a,b,(float)step/steps);GUI.DrawTexture(new Rect(p.x-1,p.y-1,2,2),Texture2D.whiteTexture);}
   GUI.color=color;if(points.Count<=16)GUI.Label(new Rect(a.x+4,a.y-18,35,20),(n+1).ToString(),small);
  }
 }
}
}
