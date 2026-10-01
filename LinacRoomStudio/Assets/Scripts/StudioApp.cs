using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RoomStudio {
internal readonly struct StudioViewportLayout {
 internal readonly float scale,width,height;
 internal readonly bool compact;
 StudioViewportLayout(int screenWidth,int screenHeight){
  compact=screenWidth<1000||screenHeight<600;
  scale=compact?Mathf.Min(1,screenWidth/320f):Mathf.Max(.4f,Mathf.Min(screenWidth/1440f,screenHeight/850f));
  width=screenWidth/scale;height=screenHeight/scale;
 }
 internal static StudioViewportLayout Create(int screenWidth,int screenHeight){
  if(screenWidth<=0||screenHeight<=0)throw new ArgumentOutOfRangeException("screen dimensions");
  return new StudioViewportLayout(screenWidth,screenHeight);
 }
 internal Rect Scene=>new Rect(compact?12:342,76,Mathf.Max(1,width-(compact?24:700)),Mathf.Max(1,height-120));
 internal Rect LeftPanel=>new Rect(12,76,compact?width-24:318,Mathf.Max(1,height-120));
 internal Rect RightPanel=>compact?LeftPanel:new Rect(width-346,76,334,Mathf.Max(1,height-120));
 internal Rect LeftContent=>new Rect(LeftPanel.x+16,88,LeftPanel.width-32,Mathf.Max(1,height-144));
 internal Rect RightContent=>new Rect(RightPanel.x+16,88,RightPanel.width-32,Mathf.Max(1,height-(compact?144:330)));
 internal Rect Calculation=>new Rect(width-330,height-225,302,177);
 internal static Rect DistanceLabel(Rect scene,Vector2 position){
  float labelWidth=Mathf.Min(150,scene.width-12),maximumY=scene.yMax-30;
  return new Rect(Mathf.Clamp(position.x-labelWidth/2,scene.x+6,scene.xMax-labelWidth-6),Mathf.Clamp(position.y-30,Mathf.Min(scene.y+86,maximumY),maximumY),labelWidth,24);
 }
 internal static string DistanceText(double centimetres,bool projected)=>centimetres.ToString("0.###",CultureInfo.InvariantCulture)+" cm"+(projected?" (3D)":"");
}
public partial class StudioApp : MonoBehaviour {
 System.Threading.Tasks.Task<string> qaTask; QaResponse qa; string qaJson,qaInput,qaError; bool showQa; Vector2 qaScroll;
 System.Threading.Tasks.Task<string> fileTask; bool loadingSource=false;
 System.Threading.Tasks.Task<string> workspaceTask; string workspaceMode,exportPath;
 sealed class BrowserFileWrite {public System.Threading.Tasks.Task<string> completion;public string message,designJson;}
 readonly List<BrowserFileWrite> browserFileWrites=new List<BrowserFileWrite>();
#if !UNITY_WEBGL || UNITY_EDITOR
 string sourcePath="";
#endif
 Design design; Camera cam; Transform world; GameObject roof; Material wallMat,floorMat,ceilingMat,accentMat,gridMat,selectedMat,lineMat;
 readonly Dictionary<string,string> numberBuffers=new Dictionary<string,string>();
 readonly Dictionary<string,GameObject> objects=new Dictionary<string,GameObject>();
 readonly Dictionary<string,Texture2D> shieldTextures=new Dictionary<string,Texture2D>();
 readonly List<string> history=new List<string>(); int historyIndex=-1;
 readonly HashSet<string> selection=new HashSet<string>();
 readonly List<Material> appearanceMaterials=new List<Material>();
 bool multiSelect,marquee,marqueeAdd,dragMoved; Vector2 marqueeStart,marqueeEnd;
 Vector3 dragStart; readonly Dictionary<string,Vector3> dragPositions=new Dictionary<string,Vector3>();
 float selectionRotation;
 string pendingToggleOff="";
 string selected="",tool="Select",tab="Room",status="Ready. Draw walls or select equipment.",fileName="Bunker-concept";
 bool top=true,cutaway=true,showRoof=false,showBeam=true,dragging,dirty;
 Vector3? wallStart; Vector3 focus=Vector3.zero; float zoom=19,yaw=35,pitch=55,uiScale=1;
 Vector2 leftScroll,rightScroll; GUIStyle title,sub,body,small,button,field,numberField,textAreaField,section,metric; Texture2D panel,card,buttonTex,activeTex,inputTex;
 float W=>Screen.width/uiScale; float H=>Screen.height/uiScale;
 StudioViewportLayout Layout=>StudioViewportLayout.Create(Screen.width,Screen.height);
 Rect View=>Layout.Scene;
 string compactPanel="";
 bool CompactPanelOpen=>Layout.compact&&compactPanel!="";
 string runtimeTestDirectory;
 string SaveDirectory=>runtimeTestDirectory??Path.Combine(Application.persistentDataPath,"Designs");
 Item Current=>design.items.Find(i=>i.id==selected);
 List<Item> SelectedItems=>design.items.Where(i=>selection.Contains(i.id)).ToList();
 string ObjectTypeLabel(Item item){
  if(item==null)return "Object";
  if(item.kind=="Wall")return "Wall";
  if(item.kind=="LINAC")return "Linac HD";
  if(item.kind=="Desk")return "Workstation";
  if(item.kind=="Component")return "Component";
  if(item.kind=="Source")return "Source";
  if(CtShieldingData.IsPoint(item))return item.ctPoint.role=="Scatter"?"CT scatter":item.ctPoint.role=="Patient"?"Patient point":"ROI point";
  var entry=Array.Find(EquipmentPalette.Entries,e=>e.model==item.model);
  return entry==null?"Object":entry.label;
 }
 string ObjectDisplayName(Item target){
  if(target==null)return "Object 000";
  string type=ObjectTypeLabel(target);int number=0;
  foreach(var item in design.items){if(ObjectTypeLabel(item)==type)number++;if(item.id==target.id)break;}
  return type+" "+number.ToString("000",CultureInfo.InvariantCulture);
 }
 bool AdditiveSelection=>multiSelect||Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand);
 LineRenderer beam,linePreview; string[] files=new string[0]; bool showFiles,showHelp;

 void Start(){
  #if !UNITY_WEBGL || UNITY_EDITOR
  if(Environment.GetCommandLineArgs().Contains("--smoke-test")||Environment.GetCommandLineArgs().Contains("--ct-smoke-test")){runtimeTestDirectory=Path.Combine(Application.temporaryCachePath,"RoomStudioSmoke-"+Guid.NewGuid().ToString("N"));Debug.Log("ROOM_STUDIO_SMOKE_SAVE_DIRECTORY: "+runtimeTestDirectory);}
  #endif
  Application.targetFrameRate=60;design=Design.Example();
  wallMat=Material(new Color(.34f,.43f,.51f));floorMat=Material(Color.white);ceilingMat=Material(Color.white);accentMat=Material(new Color(.10f,.85f,.75f));selectedMat=Material(new Color(.18f,.73f,.72f));gridMat=Material(new Color(.24f,.32f,.39f));lineMat=new Material(Shader.Find("Sprites/Default"));
  cam=new GameObject("Camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.078f,.11f);cam.nearClipPlane=.05f;cam.farClipPlane=500;
  var light=new GameObject("Key light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.85f;light.transform.rotation=Quaternion.Euler(45,-35,0);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.28f,.31f,.35f);
  var fill=new GameObject("Fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.25f;fill.transform.rotation=Quaternion.Euler(65,140,0);
  Directory.CreateDirectory(SaveDirectory);InitializeComponents();Rebuild();Remember();
  if(Application.absoluteURL.Contains("ct-demo=1")){design=CtShieldingChecks.DemoFixture();ClearSelection();Commit();Rebuild();tab="CT";status="DEMO_ONLY CT arithmetic fixture; not a clinical case.";}
  if(Application.absoluteURL.Contains("ct-tests=1"))StartCoroutine(CtBrowserSmokeTest());
  if(Application.absoluteURL.Contains("feature-tests=1"))StartCoroutine(FeatureSmokeTest());
  #if !UNITY_WEBGL || UNITY_EDITOR
  if(Environment.GetCommandLineArgs().Contains("--smoke-test"))StartCoroutine(SmokeTest());
  if(Environment.GetCommandLineArgs().Contains("--ct-smoke-test"))StartCoroutine(CtOnlySmokeTest());
#endif
 }
 Material Material(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",.15f);return m;}
 void ApplyShieldAppearance(Material material,Barrier barrier){
  string type=barrier==null||string.IsNullOrEmpty(barrier.material)?"Concrete":barrier.material;
  if(type=="PlateGlass")type="Glass";
  material.color=Color.white;material.mainTexture=ShieldTexture(type);material.mainTextureScale=new Vector2(4,4);
  material.SetFloat("_Metallic",type=="Steel"?.75f:type=="Lead"?.35f:0);material.SetFloat("_Glossiness",type=="Glass"?.8f:type=="Steel"?.55f:.2f);
 }
 Texture2D ShieldTexture(string type){
  if(shieldTextures.TryGetValue(type,out var cached))return cached;
  const int size=32;Color baseColor=type=="Steel"?new Color(.28f,.38f,.48f):type=="Lead"?new Color(.18f,.2f,.24f):type=="Gypsum"?new Color(.72f,.69f,.62f):type=="Glass"?new Color(.2f,.58f,.7f):type=="Wood"?new Color(.45f,.25f,.12f):new Color(.52f,.55f,.56f);
  var pixels=new Color[size*size];
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   Color color=baseColor;
   if(type=="Concrete"){
    float noise=(Mathf.Sin(x*1.7f+y*.91f)+1)*.5f;color=Color.Lerp(baseColor,Color.white,.04f+noise*.1f);if((x*13+y*7)%43==0)color=Color.Lerp(color,Color.black,.22f);
   }else if(type=="Steel"){
    float sheen=(Mathf.Sin((x+y)*.55f)+1)*.5f;color=Color.Lerp(baseColor,new Color(.68f,.78f,.86f),sheen*.16f);if((x-y+size*2)%16<1)color=Color.Lerp(color,Color.black,.18f);
   }else if(type=="Lead"){
    bool tile=((x/4+y/4)%2)==0;color=Color.Lerp(baseColor,new Color(.3f,.33f,.38f),tile?.12f:0);if((x+y)%11==0)color=Color.Lerp(color,Color.black,.16f);
   }else if(type=="Gypsum"){
    float seam=y%12<1?.3f:.02f;color=Color.Lerp(baseColor,new Color(.38f,.36f,.32f),seam);
   }else if(type=="Glass"){
    bool highlight=(x-y+size*2)%14<2;color=Color.Lerp(baseColor,new Color(.7f,.95f,1),highlight?.32f:.03f);
   }else if(type=="Wood"){
    float grain=(Mathf.Sin(y*.5f+Mathf.Sin(x*.15f)*1.5f)+1)*.5f;color=Color.Lerp(baseColor,new Color(.76f,.45f,.23f),grain*.28f);if((x*3+y)%29==0)color=Color.Lerp(color,new Color(.15f,.07f,.03f),.3f);
   }
   pixels[y*size+x]=color;
  }
  var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Shielding "+type+" texture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};texture.SetPixels(pixels);texture.Apply();shieldTextures[type]=texture;return texture;
 }
 GameObject Cube(string name,Vector3 pos,Vector3 size,Material mat,Transform parent){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
 LineRenderer Line(string name,Color color,float width,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=lineMat;l.startColor=l.endColor=color;l.startWidth=l.endWidth=width;l.positionCount=2;return l;}
 void Rebuild(){
  CtShieldingData.SynchronizeAnchors(design);
  StopGantryTween();gantryPivot=null;linacModelRoot=null;beamWindow=null;beam=null;linePreview=null;
  selection.RemoveWhere(id=>!design.items.Any(i=>i.id==id));
  foreach(var material in appearanceMaterials)Destroy(material);appearanceMaterials.Clear();
  foreach(var mesh in componentMeshes)Destroy(mesh);componentMeshes.Clear();
 if(world!=null){world.gameObject.SetActive(false);Destroy(world.gameObject);}objects.Clear();beamWindow=null;world=new GameObject("Generated room").transform;
  ApplyShieldAppearance(floorMat,design.floor);ApplyShieldAppearance(ceilingMat,design.ceiling);
  Cube("Floor shielding",new Vector3(0,-design.floor.thickness/2000f,0),new Vector3(design.width,design.floor.thickness/1000f,design.depth),floorMat,world);
  roof=Cube("Ceiling shielding",new Vector3(0,design.height+design.ceiling.thickness/2000f,0),new Vector3(design.width,design.ceiling.thickness/1000f,design.depth),ceilingMat,world);roof.SetActive(showRoof);
  if(Precision.gridVisible)for(int n=-40;n<=40;n++){var l=Line("Grid",new Color(.2f,.29f,.35f),n%5==0?.022f:.009f,world);l.SetPosition(0,new Vector3(n,.015f,-40));l.SetPosition(1,new Vector3(n,.015f,40));var r=Line("Grid",new Color(.2f,.29f,.35f),n%5==0?.022f:.009f,world);r.SetPosition(0,new Vector3(-40,.015f,n));r.SetPosition(1,new Vector3(40,.015f,n));}
  bool linacRigConfigured=false;
  foreach(var i in design.items){
  var root=new GameObject(ObjectDisplayName(i));root.transform.SetParent(world);root.transform.position=CtShieldingData.IsPoint(i)?CtShieldingData.Position(design,i).UnityVector:new Vector3(i.x,i.y,i.z);root.transform.rotation=Quaternion.Euler(0,i.angle,0);root.AddComponent<Selectable>().id=i.id;
   if(i.kind=="Wall")BuildWall(i,root.transform);
   else if(i.kind=="Component")BuildComponent(i,root);
   else {
    var prefab=Resources.Load<GameObject>(i.kind=="LINAC"?"Linac":i.kind=="Desk"?"Desk":i.model);GameObject model;if(prefab==null){model=Cube("Missing model placeholder",Vector3.up*.5f,Vector3.one,accentMat,root.transform);status="Model missing: run Room Studio > Prepare scene in Unity.";}else model=Instantiate(prefab,root.transform);model.transform.localScale=Vector3.one*i.scale;
    var renderers=model.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);if(!CtShieldingData.IsPoint(i))model.transform.position+=Vector3.up*(root.transform.position.y-b.min.y);
    if(CtShieldingData.IsPoint(i))foreach(var renderer in renderers){var material=new Material(Resources.Load<Shader>("CT/CTMarker"));material.color=i.ctPoint.role=="Scatter"?new Color(1,.6f,.15f):i.ctPoint.role=="Patient"?new Color(.95f,.4f,.5f):new Color(.15f,.85f,.8f);renderer.sharedMaterial=material;appearanceMaterials.Add(material);}
    if(i.kind=="LINAC"&&!linacRigConfigured){linacRigConfigured=true;ConfigureGantryRig(model.transform);if(beamWindow==null)status="Linac HD Beam_Window missing; beam illustration hidden. Reprepare the model in Unity.";}
    // A separate interaction volume keeps selection independent from model detail.
    var col=root.AddComponent<BoxCollider>();var localBounds=new Bounds(root.transform.InverseTransformPoint(renderers[0].bounds.center),Vector3.zero);foreach(var r in renderers){var rb=r.bounds;for(int c=0;c<8;c++)localBounds.Encapsulate(root.transform.InverseTransformPoint(rb.center+Vector3.Scale(rb.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1))));}col.center=localBounds.center;col.size=localBounds.size;
    if(selection.Contains(i.id)){var ring=Line("Selection",new Color(.2f,1,.85f),.045f,root.transform);ring.loop=true;ring.positionCount=4;float a=Mathf.Max(localBounds.size.x/2,.7f),bsize=Mathf.Max(localBounds.size.z/2,.7f);for(int k=0;k<4;k++)ring.SetPosition(k,root.transform.TransformPoint(new Vector3(k<2?-a:a,.04f,k==0||k==3?-bsize:bsize)));}
    if(i.kind=="Desk"||i.kind=="Source"){var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Assessment point";marker.transform.SetParent(root.transform,false);marker.transform.localPosition=Vector3.up*i.assessmentHeight;marker.transform.localScale=Vector3.one*.16f;marker.GetComponent<Renderer>().sharedMaterial=accentMat;Destroy(marker.GetComponent<Collider>());}
   }
   ApplyOpacity(root,i.Opacity);objects[i.id]=root;
  }
  BuildJoinedWallMeshes();
  if(design.regions!=null)foreach(var region in design.regions){if(region.points==null||region.points.Count<3)continue;var outline=Line(region.name,new Color(.7f,.5f,1),.035f,world);outline.loop=true;outline.positionCount=region.points.Count;float regionY=region.scope=="Ceiling"?design.height+.05f:region.scope=="Floor"?-.05f:.05f;for(int k=0;k<region.points.Count;k++)outline.SetPosition(k,new Vector3(region.points[k].x,regionY,region.points[k].y));}
  componentDrawing=Line("Custom footprint",new Color(.2f,1,.85f),.05f,world);componentDrawing.enabled=false;
  FloorPlanAfterRebuild();
  GenerationAfterRebuild();
  WallConnectionAfterRebuild();
  CtAfterRebuild();
  beam=Line("Beam direction (illustration only)",new Color(1,.66f,.2f),.065f,world);linePreview=Line("Wall preview",new Color(.2f,1,.85f),.12f,world);linePreview.enabled=false;UpdateBeam();
 }
 void ApplyOpacity(GameObject root,float opacity){
  if(opacity>=1)return;
  var copies=new Dictionary<Material,Material>();
  foreach(var renderer in root.GetComponentsInChildren<Renderer>()){
   if(renderer is LineRenderer)continue; // Selection stays visible even at zero opacity.
   renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   var materials=renderer.sharedMaterials;
   for(int n=0;n<materials.Length;n++){
    var source=materials[n];if(!copies.TryGetValue(source,out var copy)){
     copy=new Material(source);var color=copy.color;color.a*=opacity;copy.color=color;
     copy.SetFloat("_Mode",2);copy.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);copy.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);copy.SetInt("_ZWrite",0);
     copy.DisableKeyword("_ALPHATEST_ON");copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");copy.EnableKeyword("_ALPHABLEND_ON");copy.SetOverrideTag("RenderType","Transparent");copy.renderQueue=3000;
     copies[source]=copy;appearanceMaterials.Add(copy);
    }materials[n]=copy;
   }renderer.sharedMaterials=materials;
  }
 }
 Vector3 Isocentre(){var i=design.items.Find(x=>x.kind=="LINAC");if(i==null)return Vector3.zero;return new Vector3(i.x,i.y+design.isocentreHeight,i.z)+Quaternion.Euler(0,i.angle,0)*new Vector3(design.isoOffsetX,0,design.isoOffsetZ);}
 void UpdateBeam(){UpdateBeamIllustration();}
 void SetCameraView(bool planView){top=planView;compactPanel="";cameraWalkVelocity=Vector3.zero;if(planView){cutaway=true;Rebuild();}status=planView?"2D plan view. Press V to switch to 3D.":"3D view. Hold arrow keys to move the camera; press V for 2D plan.";}
 void ToggleCameraView(){SetCameraView(!top);}
 void Update(){
  UpdateBrowserFileWrites();
  FloorPlanUpdate();
  CtUpdate();
  if(componentImportTask!=null&&componentImportTask.IsCompleted){try{ImportComponents(componentImportTask.GetAwaiter().GetResult());componentError="";}catch(Exception e){componentError=status="Component import failed: "+e.Message;}componentImportTask=null;}
  if(fileTask!=null&&fileTask.IsCompleted){
   try{string json=fileTask.GetAwaiter().GetResult();if(loadingSource)ImportSource(json);else LoadJson(json,"Imported-design");}
   catch(Exception e){status="File import failed: "+e.Message;}fileTask=null;
  }
  if(workspaceTask!=null&&workspaceTask.IsCompleted){try{var result=JsonUtility.FromJson<WorkspaceResponse>(workspaceTask.GetAwaiter().GetResult());if(workspaceMode=="import"){Design.Validate(result.design);if(!Design.IsEmptyFloorPlan(result.design.floorPlan)&&result.design.floorPlan.kind=="Image")ValidateFloorPlanImage(result.design.floorPlan);design=result.design;ClearSelection();wallStart=null;qa=null;Commit();Rebuild();status=result.summary;}else{string temp=exportPath+".tmp";File.WriteAllText(temp,result.json);if(File.Exists(exportPath))File.Copy(exportPath,exportPath+".bak",true);File.Copy(temp,exportPath,true);File.Delete(temp);BrowserBridge.SyncFiles();BrowserBridge.Download(Path.GetFileName(exportPath),result.json);status="Exported ProShield workspace: "+Path.GetFileName(exportPath);}}catch(Exception e){status="Workspace "+workspaceMode+" failed: "+e.Message;}workspaceTask=null;}

  if(qaTask!=null&&qaTask.IsCompleted){try{qaJson=qaTask.GetAwaiter().GetResult();qa=JsonUtility.FromJson<QaResponse>(qaJson);if(qa==null||qa.rows==null)throw new Exception("The calculation returned no readable report.");qaError=null;qaScroll=Vector2.zero;showQa=true;status="Reference calculation complete.";}catch(Exception e){qa=null;qaError=e.Message;showQa=true;status="QA failed: "+e.Message;}qaTask=null;}
  if(cam==null)return;uiScale=Layout.scale;
  if(CompactPanelOpen)cameraWalkVelocity=Vector3.zero;else UpdateCameraWalk();
  var v=View;cam.rect=new Rect(v.x/W,(H-v.y-v.height)/H,v.width/W,v.height/H);cam.orthographic=top;cam.orthographicSize=zoom*.55f;cam.transform.position=top?focus+Vector3.up*50:focus+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-zoom);cam.transform.rotation=top?Quaternion.Euler(90,0,0):Quaternion.LookRotation(focus-cam.transform.position);
  Vector2 mouse=new Vector2(Input.mousePosition.x/uiScale,(Screen.height-Input.mousePosition.y)/uiScale);bool inView=v.Contains(mouse)&&!CompactPanelOpen&&!showHelp&&!showFiles&&!showQa&&!showComponentEditor&&!showPlanAuthoring&&!showReset;
  if(inView&&!scalingEquipment){zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y,5,90);if(Input.GetMouseButton(1)){if(top){focus+=new Vector3(-Input.GetAxis("Mouse X"),0,-Input.GetAxis("Mouse Y"))*zoom*.025f;}else{yaw+=Input.GetAxis("Mouse X")*3;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,15,85);}}}
  Vector3 rawPoint=Vector3.zero;bool onPlane=new Plane(Vector3.up,Vector3.zero).Raycast(cam.ScreenPointToRay(Input.mousePosition),out float enter);if(onPlane)rawPoint=cam.ScreenPointToRay(Input.mousePosition).GetPoint(enter);
  rawPoint.x=Mathf.Clamp(rawPoint.x,-10000,10000);rawPoint.z=Mathf.Clamp(rawPoint.z,-10000,10000);
  pointerSnap=PrecisionEditing.Snap(rawPoint,Precision,design.items,resizingWall||dragging?selection:null);
  bool angleSnap=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
  if(tool=="Wall"&&wallStart.HasValue&&angleSnap)pointerSnap=new SnapTarget{point=PrecisionEditing.ConstrainWallAngle(wallStart.Value,pointerSnap.point,true),kind="45 degree angle"};
  Vector3 p=pointerSnap.point;showSnapTarget=inView&&onPlane&&tool!="Select"&&pointerSnap.kind!="Free";
  if(linePreview!=null){linePreview.enabled=wallStart.HasValue&&inView;if(wallStart.HasValue){linePreview.SetPosition(0,wallStart.Value+Vector3.up*.1f);linePreview.SetPosition(1,p+Vector3.up*.1f);}}
  UpdateComponentDrawing(p,inView);
  ProcessScenePointerEvents();
  if(marquee&&Input.GetMouseButton(0))marqueeEnd=new Vector2(Mathf.Clamp(mouse.x,v.xMin,v.xMax),Mathf.Clamp(mouse.y,v.yMin,v.yMax));
  if(dragging&&Input.GetMouseButton(0)&&inView&&onPlane)UpdatePrecisionDrag(rawPoint);
  if(resizingWall&&Input.GetMouseButton(0)&&!Input.GetMouseButtonDown(0)&&inView&&onPlane)UpdateWallResize(rawPoint,angleSnap);
  if(scalingEquipment&&Input.GetMouseButton(0)&&!Input.GetMouseButtonDown(0)&&inView)UpdateEquipmentScale(mouse);

 }
 void FinishDrag(){if(dragging){if(dragMoved){if(dirty)Commit();}else if(pendingToggleOff!="")Choose(pendingToggleOff,true);}pendingToggleOff="";dragging=dragMoved=false;dragConnectedBefore=null;}
 void ClearSelection(){selection.Clear();selected="";selectionRotation=0;dragging=marquee=dragMoved=resizingWall=scalingEquipment=false;resizeOriginal=scaleOriginal=null;dragConnectedBefore=resizeConnectedBefore=null;wallConnectionPreview=null;pendingToggleOff="";numberBuffers.Clear();}
 void Choose(string id,bool toggle=false,bool preserve=false){
  if(dirty)Commit();wallConnectionPreview=null;var members=SelectionEditing.Expand(design,new[]{id});
  bool remove=toggle&&members.All(selection.Contains);
  if(!toggle&&!(preserve&&selection.Contains(id)))selection.Clear();
  if(remove)selection.ExceptWith(members);else selection.UnionWith(members);
  selected=selection.Contains(id)?id:selection.FirstOrDefault()??"";selectionRotation=0;numberBuffers.Clear();tab="Object";Rebuild();
 }
 void SelectAll(){selection.UnionWith(design.items.Select(i=>i.id));selected=selection.FirstOrDefault()??"";selectionRotation=0;tab="Object";Rebuild();}
 Rect MarqueeRect=>Rect.MinMaxRect(Mathf.Min(marqueeStart.x,marqueeEnd.x),Mathf.Min(marqueeStart.y,marqueeEnd.y),Mathf.Max(marqueeStart.x,marqueeEnd.x),Mathf.Max(marqueeStart.y,marqueeEnd.y));
 void FinishMarquee(){
  if(!marqueeAdd)selection.Clear();var rect=MarqueeRect;
  if(rect.width>4||rect.height>4)foreach(var item in design.items){
   var colliders=objects[item.id].GetComponentsInChildren<Collider>();
   if(colliders.Length==0&&(item.kind!="Wall"||!IsJoinedWall(item.id)))continue;
   var bounds=colliders.Length>0?colliders[0].bounds:JoinedWallBounds(item);foreach(var collider in colliders)bounds.Encapsulate(collider.bounds);
   var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);bool visible=false;
   for(int k=0;k<8;k++){var point=cam.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1)));if(point.z<=0)continue;visible=true;var screen=new Vector2(point.x/uiScale,(Screen.height-point.y)/uiScale);min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);}
   if(visible&&rect.Overlaps(Rect.MinMaxRect(min.x,min.y,max.x,max.y)))selection.UnionWith(SelectionEditing.Expand(design,new[]{item.id}));
  }
  selected=selection.FirstOrDefault()??"";selectionRotation=0;numberBuffers.Clear();if(selection.Count>0)tab="Object";Rebuild();
 }
 void GroupSelection(bool unlock){if(selection.Count==0||(!unlock&&selection.Count<2))return;try{
  if(!unlock&&SelectedItems.Any(i=>IsJoinedWall(i.id)))throw new Exception("Detach connected wall junctions before grouping these walls.");
  if(unlock)SelectionEditing.Ungroup(design,selection);else SelectionEditing.Group(design,selection);
  Commit();Rebuild();status=unlock?"Group unlocked. Objects can be selected separately.":"Locked together. Selecting any member selects the whole group.";
 }catch(Exception e){status=e.Message;}}
 void Add(Item i){if(design.items.Count>=250){status="Maximum 250 objects per project.";return;}design.items.Add(i);ClearSelection();selected=i.id;selection.Add(i.id);Commit();Rebuild();}
 void Delete(){if(selection.Count==0)return;try{
  var next=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  WallConnections.Detach(next,selection);
  WallGenerationData.MarkDeletedPaths(next,next.items.Where(i=>selection.Contains(i.id)).ToList());
  SelectionEditing.Remove(next,selection);Design.Validate(next);
  design=next;ClearSelection();Commit();Rebuild();
 }catch(Exception e){status=e.Message;}}
 void Remember(){var s=JsonUtility.ToJson(design);if(historyIndex>=0&&history[historyIndex]==s)return;InvalidatePlanWork();if(historyIndex<history.Count-1)history.RemoveRange(historyIndex+1,history.Count-historyIndex-1);history.Add(s);if(history.Count>80)history.RemoveAt(0);historyIndex=history.Count-1;}
 void Commit(){CtShieldingData.SynchronizeAnchors(design);WallGenerationData.MarkManualEdits(design);Remember();dirty=false;status="Design updated. Save to keep your changes.";}
 void Undo(int direction){
  if(dirty)Commit();int n=historyIndex+direction;if(n<0||n>=history.Count)return;
  var preferences=Precision;var selectedIds=selection.ToArray();string primary=selected;historyIndex=n;
  design=JsonUtility.FromJson<Design>(history[n]);design.editing=preferences;history[n]=JsonUtility.ToJson(design);ClearSelection();
  selection.UnionWith(SelectionEditing.Expand(design,selectedIds));selected=selection.Contains(primary)?primary:selection.FirstOrDefault()??"";
  InvalidatePlanWork();dirty=false;Rebuild();status=direction<0?"Undo complete.":"Redo complete.";
 }
 string SafeName(){var n=new string(fileName.Where(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_').Take(60).ToArray());return string.IsNullOrEmpty(n)?"Bunker-concept":n;}
 void Save(){try{Design.Validate(design);string json=JsonUtility.ToJson(design,true);string path=Path.Combine(SaveDirectory,SafeName()+".json");string temp=path+".tmp";File.WriteAllText(temp,json);bool previous=File.Exists(path);if(previous)File.Copy(path,path+".bak",true);File.Copy(temp,path,true);File.Delete(temp);BrowserBridge.Download(Path.GetFileName(path),json);SynchronizeFileWrite("Saved "+Path.GetFileName(path)+(previous?". Previous version kept as .bak.":"."));}catch(Exception e){status="Save failed: "+e.Message;}}
 void SynchronizeFileWrite(string success){
#if UNITY_WEBGL && !UNITY_EDITOR
  browserFileWrites.Add(new BrowserFileWrite{completion=BrowserBridge.SyncFilesAsync(),message=success,designJson=JsonUtility.ToJson(design)});status="Saving to browser storage...";
#else
  BrowserBridge.SyncFiles();status=success;
#endif
 }
 void UpdateBrowserFileWrites(){
  string failure=null;
  for(int index=0;index<browserFileWrites.Count;index++){
   var write=browserFileWrites[index];if(!write.completion.IsCompleted)continue;
   browserFileWrites.RemoveAt(index--);
   try{write.completion.GetAwaiter().GetResult();status=write.message;if(write.designJson!=JsonUtility.ToJson(design))status+=" Current design has unsaved changes.";}
   catch(Exception error){failure="Browser storage save failed: "+error.Message+". Keep the downloaded JSON before reloading.";}
  }
  if(failure!=null)status=failure;
 }
 [Serializable] class FileHeader { public int version=0; }
 void Load(string path){try{LoadJson(File.ReadAllText(path),Path.GetFileNameWithoutExtension(path));}catch(Exception e){status="Load failed: "+e.Message;}}
 void LoadJson(string json,string name){var header=JsonUtility.FromJson<FileHeader>(json);if(header==null||(header.version!=1&&header.version!=2))throw new Exception("Choose a saved room design, not a QA report.");var d=JsonUtility.FromJson<Design>(json);Design.Migrate(d);if(Design.IsEmptyFloorPlan(d.floorPlan))d.floorPlan=null;Design.Validate(d);if(d.floorPlan!=null&&d.floorPlan.kind=="Image")ValidateFloorPlanImage(d.floorPlan);design=d;ClearSelection();wallStart=null;fileName=name;qa=null;Commit();Rebuild();showFiles=false;status="Loaded "+fileName+". Undo returns to your previous design.";}
 void WriteOutput(string name,string text){File.WriteAllText(Path.Combine(SaveDirectory,name),text);BrowserBridge.Download(name,text);SynchronizeFileWrite("Browser storage updated for "+name+".");}
 void Export(){try{var lines=new List<string>{"Workstation,X_m,Y_m,Z_m,Occupancy,Distance_to_isocentre_m,Dose_Gy_per_week,Dose_Sv_per_week,Status"};var machine=design.items.Find(i=>i.kind=="LINAC");foreach(var i in design.items.Where(x=>x.kind=="Desk")){Vector3 p=new Vector3(i.x,i.y+i.assessmentHeight,i.z);lines.Add(string.Join(",",Csv(ObjectDisplayName(i)),F(i.x),F(p.y),F(i.z),F(i.occupancy),machine==null?"":F(Vector3.Distance(p,Isocentre())),"","","NOT_CALCULATED: validated physics data required"));}WriteOutput(SafeName()+"-workstations.csv",string.Join("\n",lines));WriteOutput(SafeName()+"-report.txt","LINAC ROOM STUDIO - GEOMETRY REPORT\n\nNo radiation dose has been calculated. No safety classification is available.\nBeam illustration is not a radiation simulation. Model scale and isocentre require calibration.\n\n"+JsonUtility.ToJson(design,true));status="Exported workstation CSV and full design report to the Designs folder.";}catch(Exception e){status="Export failed: "+e.Message;}}
 static string F(float f)=>f.ToString("0.#########",CultureInfo.InvariantCulture);
 static string Csv(string s)=>"\""+(s??"").Replace("\"","\"\"")+"\"";

 Texture2D Texture(Color c){var t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t;}
 void Styles(){if(title!=null)return;panel=Texture(new Color(.065f,.085f,.12f));card=Texture(new Color(.10f,.13f,.175f));buttonTex=Texture(new Color(.15f,.20f,.26f));activeTex=Texture(new Color(.10f,.40f,.39f));inputTex=Texture(new Color(.045f,.064f,.09f));
  body=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true,normal={textColor=new Color(.85f,.9f,.95f)}};small=new GUIStyle(body){fontSize=12,normal={textColor=new Color(.55f,.65f,.74f)}};title=new GUIStyle(body){fontSize=24,fontStyle=FontStyle.Bold};sub=new GUIStyle(body){fontSize=17,fontStyle=FontStyle.Bold};section=new GUIStyle(small){fontSize=11,fontStyle=FontStyle.Bold,normal={textColor=new Color(.25f,.83f,.74f)},margin=new RectOffset(0,0,18,8)};metric=new GUIStyle(title){fontSize=30};
  button=new GUIStyle(GUI.skin.button){fontSize=13,wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,8,8,8),margin=new RectOffset(0,0,3,3),normal={background=buttonTex,textColor=Color.white},hover={background=activeTex,textColor=Color.white},active={background=activeTex,textColor=Color.white}};
  field=new GUIStyle(GUI.skin.textField){fontSize=14,padding=new RectOffset(8,8,6,6),normal={background=inputTex,textColor=Color.white},focused={background=buttonTex,textColor=Color.white}};
  numberField=new GUIStyle(field){fontSize=12,padding=new RectOffset(4,4,5,5)};
  textAreaField=new GUIStyle(GUI.skin.textArea){fontSize=13,wordWrap=true,padding=new RectOffset(6,6,5,5),normal={background=inputTex,textColor=Color.white},focused={background=buttonTex,textColor=Color.white}};
 }
 bool Btn(string text,bool active=false){var old=button.normal.background;button.normal.background=active?activeTex:buttonTex;bool result=GUILayout.Button(text,button);CaptureCtControl(text,"button");button.normal.background=old;if(result)GUI.FocusControl(null);return result;}
 void Label(string t)=>GUILayout.Label(t,body);
 void Section(string t)=>GUILayout.Label(t.ToUpperInvariant(),section);
 float Slider(string label,float value,float min,float max,string unit="")=>Number(label,value,min,max,unit);
 float Number(string label,float value,float min,float max,string unit="",float step=0){
  bool changedBefore=GUI.changed;string key="edit:"+tab+selected+label+numberIndex++;if(!numberBuffers.ContainsKey(key)||GUI.GetNameOfFocusedControl()!=key)numberBuffers[key]=value.ToString("R",CultureInfo.InvariantCulture);
  GUILayout.BeginHorizontal();GUILayout.Label(label+" "+unit,body,GUILayout.Width(Layout.compact?100:125));GUI.SetNextControlName(key);string previous=numberBuffers[key],visible=previous.Length>12?previous.Substring(0,12):previous;string s=GUILayout.TextField(visible,12,numberField,GUILayout.Width(112));bool textChanged=s!=visible;if(textChanged||previous.Length<=12)numberBuffers[key]=s;GUILayout.EndHorizontal();
  float result=value;bool valid=PrecisionEditing.TryNumber(s,min,max,out float f);
  if(textChanged&&valid)result=f;
  // Extend to a pre-existing imported value without silently clamping it on display.
  float low=Mathf.Min(min,result),high=Mathf.Max(max,result);bool logarithmic=low>0&&high/low>=1000;
  float position=logarithmic?Mathf.Log(result/low)/Mathf.Log(high/low):Mathf.InverseLerp(low,high,result);
  float increment=step==0?NumberStep(label,unit):step;
  GUILayout.BeginHorizontal();bool minus=increment>0&&GUILayout.Button(new GUIContent("−","Decrease by "+F(increment)+" "+unit),GUILayout.Width(24));
  float next=GUILayout.HorizontalSlider(position,0,1,GUILayout.Height(20));
  bool plus=increment>0&&GUILayout.Button(new GUIContent("+","Increase by "+F(increment)+" "+unit),GUILayout.Width(24));GUILayout.EndHorizontal();
  if(next!=position){
   float raw=logarithmic?low*Mathf.Pow(high/low,next):Mathf.Lerp(low,high,next);
   result=Mathf.Clamp(raw,min,max);numberBuffers[key]=result.ToString("R",CultureInfo.InvariantCulture);GUI.FocusControl(null);
  }
  if(minus||plus){float stepped=PrecisionEditing.Step(result,increment,plus?1:-1);if(stepped>=min&&stepped<=max)result=stepped;numberBuffers[key]=result.ToString("R",CultureInfo.InvariantCulture);GUI.FocusControl(null);}
  if(!valid&&GUI.GetNameOfFocusedControl()==key)GUILayout.Label("Enter "+F(min)+" to "+F(max)+" "+unit,small);
  GUI.changed=changedBefore||result!=value;return result;
 }
 int numberIndex;
 void ShieldUI(Barrier b){var names=CtCoefficientLibrary.MaterialIds;for(int row=0;row<3;row++){GUILayout.BeginHorizontal();for(int col=0;col<2;col++){var n=names[row*2+col];if(Btn(CtCoefficientLibrary.MaterialLabel(n),b.material==n)){b.material=n;b.ctApplicabilityReviewed=false;GUI.changed=true;}}GUILayout.EndHorizontal();}if(b.material=="Glass")GUILayout.Label("Lead glass (legacy): not plate glass",small);b.thickness=Number("Thickness",b.thickness,.01f,3000,"mm");b.density=Number("Density",b.density,100,25000,"kg/m3");if(CtShieldingData.Active(design.ct)){b.shieldingEnabled=GUILayout.Toggle(b.shieldingEnabled," Shielding enabled (CT)");b.ctApplicabilityReviewed=GUILayout.Toggle(b.ctApplicabilityReviewed," CT material applicability reviewed");}GUILayout.Label("Material and density are user inputs, not verified shielding data.",small);}
 void OnGUI(){if(design==null)return;uiScale=Layout.scale;if(CtDiagnosticsEnabled&&Event.current.type==EventType.Repaint)ctControlBounds.Clear();CaptureScenePointer(Event.current);HandleKeyboardEvent(Event.current);numberIndex=0;Styles();bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled&&!showHelp&&!showFiles&&!showQa&&!showComponentEditor&&!showPlanAuthoring&&!showReset&&!dragging&&!resizingWall&&!scalingEquipment;GUI.matrix=Matrix4x4.Scale(Vector3.one*uiScale);
  GUI.DrawTexture(new Rect(0,0,W,68),panel);
  if(Layout.compact){
   GUI.Label(new Rect(16,6,W-32,26),"LINAC / ROOM STUDIO",sub);
   GUILayout.BeginArea(new Rect(12,34,W-24,32));GUILayout.BeginHorizontal();
   foreach(string pane in new[]{"Scene","Build","Properties"})if(Btn(pane,compactPanel==(pane=="Scene"?"":pane))){compactPanel=pane=="Scene"?"":pane;GUIUtility.ExitGUI();}
   GUILayout.EndHorizontal();GUILayout.EndArea();
  }else{GUI.Label(new Rect(24,12,330,32),"LINAC / ROOM STUDIO",title);GUI.Label(new Rect(365,22,360,25),"INTERACTIVE FACILITY PLANNER",small);GUI.Label(new Rect(W-340,20,325,28),"CONCEPT DESIGN / REFERENCE QA",section);}
  if(!Layout.compact||compactPanel=="Build"){
  GUI.DrawTexture(Layout.LeftPanel,panel);string placementToolBefore=tool;
  GUILayout.BeginArea(Layout.LeftContent);leftScroll=GUILayout.BeginScrollView(leftScroll);
  GUILayout.Label("Design workspace",sub);GUILayout.Label("Geometry: metres. Shielding: millimetres.",small);
  GUILayout.BeginHorizontal();foreach(var name in new[]{"Build","Components","Floor plan"})if(Btn(name,leftTab==name)){leftTab=name;GUI.changed=false;}GUILayout.EndHorizontal();
  if(leftTab=="Components")ComponentsUI();else if(leftTab=="Floor plan")FloorPlanUI();else{Section("01 / Build");
  foreach(var t in new[]{"Select","Wall","Desk"})if(Btn(t=="Select"?"Select & move":t=="Wall"?"Draw a wall":"Place workstation",tool==t)){tool=t;wallStart=null;}
  EquipmentPaletteUI();CtPlacementUI();}
  PrecisionUI();GUILayout.Space(10);GUILayout.BeginHorizontal();if(Btn("Undo"))Undo(-1);if(Btn("Redo"))Undo(1);GUILayout.EndHorizontal();
  Section("02 / View");GUILayout.BeginHorizontal();if(Btn("2D plan",top))SetCameraView(true);if(Btn("3D view",!top))SetCameraView(false);GUILayout.EndHorizontal();
  bool c=GUILayout.Toggle(cutaway," Cutaway walls");bool r=GUILayout.Toggle(showRoof," Show ceiling");showBeam=GUILayout.Toggle(showBeam," Show beam direction");if(c!=cutaway||r!=showRoof){cutaway=c;showRoof=r;Rebuild();}UpdateBeam();if(Btn("Fit room")){focus=Vector3.zero;zoom=Mathf.Max(design.width,design.depth)*1.7f;}
  ProjectUI();
  if(!string.IsNullOrEmpty(design.importSummary))GUILayout.Label(design.importSummary,small);
  if(Btn("Help & controls / F1"))ToggleHelp();
  GUILayout.Space(15);GUILayout.Label("V: toggle 2D / 3D view\nC: toggle cutaway walls\nArrows: nudge in 2D / move camera in 3D\nCtrl+Z / Ctrl+Y: undo / redo\nCtrl+V: paste copied objects\nCtrl+L: protect / unlock objects\nDelete: remove selection\nF1: all shortcuts and help\nShift / Ctrl + click: multi-select\nDrag empty space: box select\nCtrl+G / Ctrl+Shift+G: group / ungroup\nR / Shift+R: rotate selection ±15 degrees\nRight drag: pan or orbit / Wheel: zoom\nEsc: cancel movement or drawing",small);
  GUILayout.EndScrollView();GUILayout.EndArea();
  if(Layout.compact&&tool!=placementToolBefore&&tool!="Select"){compactPanel="";GUIUtility.ExitGUI();}
  }
  if(!Layout.compact||compactPanel=="Properties"){
  GUI.DrawTexture(Layout.RightPanel,panel);
  GUILayout.BeginArea(Layout.RightContent);GUILayout.BeginHorizontal();foreach(var t in new[]{"Room","Object","Beam","CT"})if(Btn(t,tab==t))tab=t;GUILayout.EndHorizontal();rightScroll=GUILayout.BeginScrollView(rightScroll);bool changedBefore=GUI.changed;GUI.changed=false;
  Design connectedBefore=tab=="Object"&&SelectedItems.Any(i=>IsJoinedWall(i.id))?JsonUtility.FromJson<Design>(JsonUtility.ToJson(design)):null;
  bool beamPanelRebuild=false,gantryAngleChanged=false;
  if(tab=="Room")RoomUI();else if(tab=="Object")ObjectUI();else if(tab=="CT")CtUI();else beamPanelRebuild=BeamUI(out gantryAngleChanged);
  bool edit=GUI.changed;GUI.changed=changedBefore||edit;if(edit){
   if(tab=="Beam"){
    dirty=true;
    if(beamPanelRebuild)Rebuild();
    else{if(gantryAngleChanged)TweenGantryTo(design.gantry);UpdateBeam();}
   }else{
    try{PropagateObjectPanelEdit(connectedBefore);dirty=true;Rebuild();}
    catch(Exception e){if(connectedBefore!=null)design=connectedBefore;dirty=false;status=e.Message;Rebuild();}
   }
  }if(dirty&&!dragging&&!resizingWall&&!scalingEquipment&&!Input.GetMouseButton(0)){Commit();}
  if(Layout.compact)CalculationPanelUI();
  GUILayout.EndScrollView();GUILayout.EndArea();
  if(!Layout.compact){GUILayout.BeginArea(Layout.Calculation);CalculationPanelUI();GUILayout.EndArea();}
  }
  if(!CompactPanelOpen){
  var view=View;GUI.BeginGroup(view);
  GUI.Label(new Rect(14,12,view.width-28,28),top?"TOP VIEW   /   X–Z PLANE":"3D PREVIEW   /   ORBIT WITH RIGHT MOUSE",small);GUI.Label(new Rect(14,38,view.width-28,40),tool=="Wall"?(wallStart.HasValue?"Click the wall end point. Esc cancels.":"Click the wall start point."):tool=="Door"?"Click along the selected wall to cut and place a door. Esc cancels.":tool=="Polygon"?"Click footprint corners; use Finish footprint or Enter. Esc cancels.":tool=="Desk"||tool=="LINAC"||tool=="Model"||tool=="Component"?"Click the floor grid to place the selected component or equipment.":selection.Count+" selected · Shift-click to add · Drag empty space to box select",body);
  foreach(var i in design.items.Where(i=>i.kind=="Desk")){var p=cam.WorldToScreenPoint(new Vector3(i.x,i.y+i.assessmentHeight+.3f,i.z));var rect=new Rect(p.x/uiScale-55,(Screen.height-p.y)/uiScale-22,160,24);if(p.z>0&&view.Overlaps(rect))GUI.Label(new Rect(rect.x-view.x,rect.y-view.y,rect.width,rect.height),ObjectDisplayName(i),small);}
  GUI.EndGroup();
  DrawPrecisionOverlay();
  DrawCtDistanceLabels();
  if(marquee){var rectangle=MarqueeRect;var color=GUI.color;GUI.color=new Color(.2f,1,.85f,.18f);GUI.DrawTexture(rectangle,Texture2D.whiteTexture);GUI.color=new Color(.2f,1,.85f,.9f);GUI.DrawTexture(new Rect(rectangle.x,rectangle.y,rectangle.width,1),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rectangle.x,rectangle.yMax,rectangle.width,1),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rectangle.x,rectangle.y,1,rectangle.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rectangle.xMax,rectangle.y,1,rectangle.height),Texture2D.whiteTexture);GUI.color=color;}
  }
  GUI.DrawTexture(new Rect(0,H-34,W,34),panel);GUI.Label(new Rect(20,H-29,W-40,25),status,small);
  GUI.enabled=previousEnabled;
  if(showHelp||showFiles||showReset)Modal();
  if(showQa)QaModal();
  if(showComponentEditor&&!showHelp)ComponentEditorModal();
  if(showPlanAuthoring&&!showHelp)PlanAuthoringModal();
  PublishCtDiagnostics();
 }
 void CalculationPanelUI(){
  if(tab=="CT"){CtCalculationControls();return;}
  Section("Reference barrier QA");
  bool allowCalculation=GUI.enabled;GUI.enabled=allowCalculation&&qaTask==null;
  if(Btn(qaTask==null?"Calculate reference QA":"Calculating reference QA...")){BeginQa();GUIUtility.ExitGUI();}
  GUI.enabled=allowCalculation;
  if(qa!=null&&Btn("View latest QA results")){showQa=true;GUIUtility.ExitGUI();}
  if(qa!=null&&qaInput!=JsonUtility.ToJson(design))GUILayout.Label("Design changed. Recalculate for current results.",small);
  GUILayout.Label("Results open in a popup. Recalculate after editing.",small);
 }
 void BeginQa(){
  if(qaTask!=null)return;
  ctResultsPopup=false;
  showFiles=showHelp=false;showQa=true;qaError=null;qaScroll=Vector2.zero;dragging=false;
  try{Design.Validate(design);qaInput=JsonUtility.ToJson(design);qa=null;qaTask=ReferenceQa.Calculate(Path.Combine(Application.streamingAssetsPath,"ProShield"),qaInput);status="Calculating reference QA...";}
  catch(Exception e){qa=null;qaError=e.Message;status="QA failed: "+e.Message;}
 }
 void QaModal(){
  if(ctResultsPopup){CtResultsModal();return;}
  var rect=new Rect((W-Mathf.Min(820,W-64))/2,50,Mathf.Min(820,W-64),H-100);
  GUI.DrawTexture(rect,card);GUILayout.BeginArea(new Rect(rect.x+24,rect.y+20,rect.width-48,rect.height-40));
  GUILayout.Label("Reference QA results",title);
  qaScroll=GUILayout.BeginScrollView(qaScroll);
  if(qaTask!=null)Label("Calculating reference QA...");
  else if(!string.IsNullOrEmpty(qaError))Label("Calculation failed: "+qaError);
  else if(qa!=null){
   if(qaInput!=JsonUtility.ToJson(design))Label("These results are out of date. Recalculate for the current design.");
   Label(qa.summary);GUILayout.Label(qa.notice,small);
   foreach(var row in qa.rows){
    Section(row.workstationName+" / "+row.equipmentName);
    Label((row.passed?"PASS":"EXCEEDS DESIGN GOAL")+"  |  "+row.shieldedAirKerma.ToString("G5",CultureInfo.InvariantCulture)+" mGy/week");
    GUILayout.Label("Design goal: "+row.designGoal.ToString("G5",CultureInfo.InvariantCulture)+" mGy/week; required shielding: "+row.requiredThickness.ToString("F1",CultureInfo.InvariantCulture)+" mm "+row.barrierMaterial+"; additional: "+row.additionalThickness.ToString("F1",CultureInfo.InvariantCulture)+" mm",small);
   }
  }
  GUILayout.EndScrollView();GUILayout.Space(12);GUILayout.BeginHorizontal();
  GUI.enabled=qaTask==null;if(Btn("Recalculate")){BeginQa();GUIUtility.ExitGUI();}GUI.enabled=true;
  if(qa!=null&&Btn("Export QA JSON")){try{WriteOutput(SafeName()+"-qa.json",qaJson);status="Exported reference QA JSON.";}catch(Exception e){status="QA export failed: "+e.Message;}}
  if(Btn("Close")){showQa=false;GUIUtility.ExitGUI();}GUILayout.EndHorizontal();GUILayout.EndArea();
 }
 void ImportSource(string json){workspaceMode="import";workspaceTask=ReferenceQa.Execute(Path.Combine(Application.streamingAssetsPath,"ProShield"),json,"import");status="Importing source workspace...";}
 void ProjectUI(){
  Section("03 / Project");fileName=TextInput("fileName",fileName);
  if(Btn("Save design   /   Ctrl+S"))Save();
  if(Btn("Load saved design")){files=Directory.GetFiles(SaveDirectory,"*.json").Where(p=>!p.EndsWith("-qa.json",StringComparison.OrdinalIgnoreCase)&&!p.EndsWith("-proshield.json",StringComparison.OrdinalIgnoreCase)&&!p.EndsWith("-ct-results.json",StringComparison.OrdinalIgnoreCase)).OrderByDescending(File.GetLastWriteTimeUtc).ToArray();showFiles=true;}
#if UNITY_WEBGL && !UNITY_EDITOR
  if(fileTask==null&&Btn("Upload saved design")){loadingSource=false;fileTask=BrowserBridge.ChooseFile();}
#else
  if(Btn("Open saved files"))Application.OpenURL(new Uri(SaveDirectory).AbsoluteUri);
#endif
  if(Btn("Export report + CSV"))Export();
  if(Btn("Reset")){BeginRoomReset();GUI.changed=false;}
  Section("ProShield JSON");
#if !UNITY_WEBGL || UNITY_EDITOR
  sourcePath=PathInput("sourcePath",sourcePath);GUILayout.Label("Paste the full path to a source room or workspace JSON.",small);
#else
  GUILayout.Label("Choose a source room or workspace JSON from your device.",small);
#endif
  if(workspaceTask==null&&fileTask==null){
   if(Btn("Import source JSON")){
    try{
#if UNITY_WEBGL && !UNITY_EDITOR
     loadingSource=true;fileTask=BrowserBridge.ChooseFile();
#else
     ImportSource(File.ReadAllText(sourcePath.Trim().Trim('"')));
#endif
    }catch(Exception e){status="Import failed: "+e.Message;}
   }
   if(Btn("Export ProShield JSON")){try{Design.Validate(design);workspaceMode="export";exportPath=Path.Combine(SaveDirectory,SafeName()+"-proshield.json");workspaceTask=ReferenceQa.Execute(Path.Combine(Application.streamingAssetsPath,"ProShield"),JsonUtility.ToJson(design),"export");status="Exporting workspace snapshot...";}catch(Exception e){status="Export failed: "+e.Message;}}
  }else GUILayout.Label("Workspace operation in progress...",small);
 }
 void RoomUI(){Section("Room footprint");design.name=TextInput("designName",design.name);
  bool linked=GUILayout.Toggle(design.linkWallsToRoom," Link walls to room size");
  if(linked!=design.linkWallsToRoom){bool old=design.linkWallsToRoom;design.linkWallsToRoom=linked;try{if(linked)Design.ResizeRoom(design,design.width,design.depth,design.height);}catch(Exception e){design.linkWallsToRoom=old;GUI.changed=false;status=e.Message;}}
  float width=Number("Width X",design.width,3,60,"m"),depth=Number("Depth Z",design.depth,3,60,"m"),height=Number("Ceiling height",design.height,2,12,"m");
  if(width!=design.width||depth!=design.depth||height!=design.height){try{Design.ResizeRoom(design,width,depth,height);}catch(Exception e){GUI.changed=false;status=e.Message;}}
  GUILayout.Label(design.linkWallsToRoom?"Walls, floor and ceiling resize together. Equipment and assessment regions keep their positions.":"Walls are unlinked. Room changes resize only the floor and ceiling.",small);
  Section("Floor shielding");ShieldUI(design.floor);Section("Ceiling shielding");ShieldUI(design.ceiling);
  Section("Occupied regions");
  if(design.regions==null)design.regions=new List<RegionItem>();
  foreach(var region in design.regions.ToArray()){
   region.name=TextInput("region:"+region.id,region.name);
   GUILayout.BeginHorizontal();foreach(var scope in new[]{"Wall","Floor","Ceiling"})if(Btn(scope,region.scope==scope)){region.scope=scope;GUI.changed=true;}GUILayout.EndHorizontal();
   region.occupancy=Slider("Occupancy",region.occupancy,0,1);region.designGoal=Number("Design goal",region.designGoal,.0001f,1000,"mGy/wk");
   for(int pointIndex=0;pointIndex<region.points.Count;pointIndex++){var point=region.points[pointIndex];point.x=Number("Point "+(pointIndex+1)+" X",point.x,-100,100,"m");point.y=Number("Point "+(pointIndex+1)+" Z",point.y,-100,100,"m");}
   if(Btn("Remove region")){design.regions.Remove(region);GUI.changed=true;}
  }
  if(design.regions.Count<100&&Btn("Add occupied rectangle")){design.regions.Add(new RegionItem{points=new List<Vector2Data>{new Vector2Data(-1,-1),new Vector2Data(1,-1),new Vector2Data(1,1),new Vector2Data(-1,1)}});GUI.changed=true;}
  GUILayout.Label("Explicit regions override workstation occupancy for their scope. Samples outside those regions are skipped by the reference engine.",small);
  Section("Layout");GUILayout.Label(design.items.Count(i=>i.kind=="Wall")+" walls  /  "+design.items.Count(i=>i.kind=="Desk")+" workstations",body);GUILayout.Label("Select a wall in Object to cut and place a door. Draw maze walls with the wall tool.",small);
 }
 void ObjectUI(){
  bool before=GUI.changed;multiSelect=GUILayout.Toggle(multiSelect," Multi-select (click to add / remove)");GUI.changed=before;
  GUILayout.BeginHorizontal();if(Btn("Select all")){SelectAll();GUI.changed=false;}if(Btn("Clear")){ClearSelection();Rebuild();GUI.changed=false;}GUILayout.EndHorizontal();
  ClipboardControls();
  var items=SelectedItems;var i=Current;bool objectEnabled=GUI.enabled;
  WallConnectionUI();
  if(items.Count==0){Section("No object selected");Label("Shift-click objects or drag a box in the scene. You can also select objects from the list below.");}
  else{
   Section(items.Count+" object"+(items.Count==1?"":"s")+" selected");
   SelectionLockCheckbox(items);
   bool protectedSelection=items.Any(item=>item.locked);
   if(protectedSelection)GUILayout.Label("Protected: unlock to edit, move, group or delete. Protection is saved with the design.",small);
   GUI.enabled=objectEnabled&&!protectedSelection;
   bool changedBeforeNudge=GUI.changed;NudgeUI();GUI.changed=changedBeforeNudge;
   float opacity=items[0].Opacity*100;bool mixed=items.Any(item=>Mathf.Abs(item.Opacity*100-opacity)>.01f);
   if(mixed)GUILayout.Label("Mixed opacity. Adjust to apply to all selected objects.",small);
   float nextOpacity=Number("Opacity",opacity,0,100,"%");if(nextOpacity!=opacity)foreach(var item in items)item.Opacity=nextOpacity/100;
   if(mixed&&Btn("Apply this opacity to all")){foreach(var item in items)item.Opacity=nextOpacity/100;GUI.changed=true;}
   bool locked=items.Count>1&&!string.IsNullOrEmpty(items[0].groupId)&&items.All(item=>item.groupId==items[0].groupId);
   bool enabled=GUI.enabled;GUI.enabled=enabled&&items.Count>1&&!locked;if(Btn("Lock together")){GroupSelection(false);GUI.changed=false;}GUI.enabled=enabled&&items.Any(item=>!string.IsNullOrEmpty(item.groupId));if(Btn("Unlock group")){GroupSelection(true);GUI.changed=false;}GUI.enabled=enabled;
   if(locked)GUILayout.Label("Locked group: move and rotate as one piece. Unlock to edit individual members.",small);
  }
  if(items.Count>1){
   Section("Move selection");var center=SelectionEditing.Center(items);
   float x=Number("Center X",center.x,-100,100,"m"),z=Number("Center Z",center.z,-100,100,"m");
   bool enabled=GUI.enabled;bool linkedWall=design.linkWallsToRoom&&items.Any(item=>item.kind=="Wall");GUI.enabled=enabled&&!linkedWall;
   float y=Number("Center Y",center.y,-20,20,"m");GUI.enabled=enabled;if(linkedWall)GUILayout.Label("Unlink walls in Room to move this selection vertically.",small);
   var delta=new Vector3(x-center.x,y-center.y,z-center.z);if(delta!=Vector3.zero)try{SelectionEditing.Move(design,items,delta);}catch(Exception e){status=e.Message;}
  float rotation=Number("Rotate together",selectionRotation,-180,180,"deg");if(rotation!=selectionRotation)try{SelectionEditing.Rotate(items,rotation-selectionRotation,design);selectionRotation=rotation;}catch(Exception e){status=e.Message;}
   GUILayout.Label("Positions use the selection center. Relative spacing is preserved.",small);
   }else if(i!=null){
   if(CtShieldingData.IsPoint(i))CtObjectControls(i);
    bool attachedScatter=CtShieldingData.IsPoint(i)&&i.ctPoint.role=="Scatter",protectedCtOwner=design.items.Any(point=>CtShieldingData.IsPoint(point)&&point.ctPoint.ownerId==i.id&&point.locked);bool transformEnabled=GUI.enabled;GUI.enabled=transformEnabled&&!attachedScatter&&!protectedCtOwner;
  Section(ObjectDisplayName(i));i.x=Number("Position X",i.x,-100,100,"m");i.z=Number("Position Z",i.z,-100,100,"m");
  float nextBase=Number("Base height Y",i.y,-20,i.kind=="Wall"&&design.linkWallsToRoom?Mathf.Min(20,design.height-.001f):20,"m");
  if(nextBase!=i.y){
   try{if(i.kind=="Wall"&&design.linkWallsToRoom){DoorGeometry.ValidateDimensions(i,i.length,design.height-nextBase);i.height=design.height-nextBase;}i.y=nextBase;}
   catch(Exception e){GUI.changed=false;status=e.Message;}
  }
  i.angle=string.IsNullOrEmpty(design.sourceJson)?Slider("Rotation",i.angle,0,360,"deg"):Number("Rotation",i.angle,-100000,100000,"deg");
  GUI.enabled=transformEnabled;
  if(i.kind=="Wall"){WallLengthUI(i);
   if(design.linkWallsToRoom){float next=Number("Height",i.height,.001f,100,"m");if(next!=i.height){try{Design.ResizeRoom(design,design.width,design.depth,i.y+next);}catch(Exception e){GUI.changed=false;status=e.Message;}}GUILayout.Label("Wall top is linked to the ceiling. Height changes update every wall.",small);}
   else{float nextHeight=Number("Height",i.height,.25f,12,"m");if(nextHeight!=i.height)try{DoorGeometry.ValidateDimensions(i,i.length,nextHeight);i.height=nextHeight;}catch(Exception e){GUI.changed=false;status=e.Message;}}
   Section("Wall shielding");ShieldUI(i.shielding);DoorUI(i);}
  else if(i.kind=="Component")ComponentObjectUI(i);
  else{i.scale=Number("Model scale",i.scale,.05f,3);GUILayout.Label("Visual scale is not calibrated. Confirm actual equipment dimensions before planning clearances.",small);}
  if(i.kind=="Desk"){i.assessmentHeight=Number("Point height",i.assessmentHeight,0,5,"m");i.occupancy=Slider("Occupancy",i.occupancy,0,1);i.isControlled=GUILayout.Toggle(i.isControlled," Controlled area");var machine=design.items.Find(x=>x.kind=="LINAC");if(machine!=null){GUILayout.Space(10);GUILayout.Label(F(Vector3.Distance(new Vector3(i.x,i.y+i.assessmentHeight,i.z),Isocentre()))+" m",metric);GUILayout.Label("Distance to configured isocentre",small);}GUILayout.Label("The turquoise dot is the assessment point. No dose value is available.",small);}
  if(!CtShieldingData.IsPoint(i))CtObjectControls(i);
 }
 if(items.Count>0&&Btn("Delete selection ("+items.Count+")")){Delete();GUI.changed=false;}
 GUI.enabled=objectEnabled;
 Section("Scene objects");foreach(var item in design.items.ToArray())if(Btn((selection.Contains(item.id)?"✓ ":"")+ObjectDisplayName(item)+(item.locked?" [protected]":"")+(!string.IsNullOrEmpty(item.groupId)?" [group]":""),selection.Contains(item.id))){Choose(item.id,AdditiveSelection);GUI.changed=false;}
 }
 bool BeamUI(out bool gantryAngleChanged){
  gantryAngleChanged=false;Section("Beam configuration");if(!string.IsNullOrEmpty(design.sourceJson)){Label("Imported beam, energy, source components and maze inputs are preserved from the source JSON. Move or rotate sources in Object. Detailed beam controls are not yet available for imported rooms.");return false;}GUILayout.Label("Linac HD",body);GUILayout.Label("The gantry assembly and beam line animate around the configured isocentre. Reference QA continues to use the stored gantry angle.",small);
  float previousIsocentreHeight=design.isocentreHeight,previousIsoOffsetX=design.isoOffsetX,previousIsoOffsetZ=design.isoOffsetZ;
  design.energy=Number("Photon energy",design.energy,1,25,"MV");design.workload=Number("Workload",design.workload,0,100000,"Gy/wk");GUILayout.Label("Weekly workload at 1 m in Gy/week, converted to cGy/week for the reference engine.",small);
  float previousGantry=design.gantry;design.gantry=Slider("Gantry angle",design.gantry,0,360,"deg");gantryAngleChanged=design.gantry!=previousGantry;
  design.fieldX=Number("Field X",design.fieldX,1,50,"cm");design.fieldY=Number("Field Y",design.fieldY,1,50,"cm");Section("Isocentre calibration");design.isocentreHeight=Number("Height from base",design.isocentreHeight,.1f,10,"m");design.isoOffsetX=Number("Local offset X",design.isoOffsetX,-10,10,"m");design.isoOffsetZ=Number("Local offset Z",design.isoOffsetZ,-10,10,"m");design.sourceDistance=Number("Source distance",design.sourceDistance,.1f,3,"m");GUILayout.Label("The beam preview follows the moving window and aims at the configured isocentre. Reference QA uses its existing model inputs.",small);
  return design.isocentreHeight!=previousIsocentreHeight||design.isoOffsetX!=previousIsoOffsetX||design.isoOffsetZ!=previousIsoOffsetZ;
 }
 void Modal(){if(showReset){ResetModal();return;}if(showHelp){HelpModal();return;}var rect=new Rect(W/2-310,H/2-220,620,440);GUI.DrawTexture(rect,card);GUILayout.BeginArea(new Rect(rect.x+24,rect.y+20,572,400));GUILayout.Label("Load a saved design",title);GUILayout.Space(15);
  {if(files.Length==0)Label("No saved designs yet. Close this window and save your first design.");foreach(var f in files.Take(8))if(Btn(Path.GetFileNameWithoutExtension(f)))Load(f);if(files.Length>8)Label("Open saved files to manage additional designs.");}
  GUILayout.FlexibleSpace();if(Btn("Close")){showFiles=showHelp=false;}GUILayout.EndArea();
 }
 void SelectionSmokeChecks(){
  string original=JsonUtility.ToJson(design);var a=design.items[5];var b=design.items[6];
  Choose(a.id);Choose(b.id,true);if(selection.Count!=2)throw new Exception("Multi-selection failed");
  // IMGUI may commit on mouse release before Update processes the release event.
  dragging=dragMoved=dirty=true;pendingToggleOff=a.id;Commit();FinishDrag();if(selection.Count!=2)throw new Exception("Drag release changed multi-selection");
  GroupSelection(false);string group=a.groupId;ClearSelection();Choose(b.id);if(selection.Count!=2)throw new Exception("Group selection failed");
  Vector3 oldCenter=SelectionEditing.Center(SelectedItems);SelectionEditing.Move(design,SelectedItems,new Vector3(2,0,1));Commit();Rebuild();Undo(-1);if(Vector3.Distance(SelectionEditing.Center(design.items.Where(i=>i.groupId==group).ToList()),oldCenter)>.0001f)throw new Exception("Group move undo failed");Undo(1);Choose(b.id);
  foreach(var item in SelectedItems)item.Opacity=.35f;Commit();Rebuild();
  foreach(var item in SelectedItems)foreach(var renderer in objects[item.id].GetComponentsInChildren<MeshRenderer>())foreach(var mat in renderer.sharedMaterials)if(!mat.IsKeywordEnabled("_ALPHABLEND_ON")||mat.color.a>.3501f||mat.GetInt("_ZWrite")!=0)throw new Exception("Opacity rendering failed");
  LoadJson(JsonUtility.ToJson(design),"Selection-smoke");Choose(b.id);if(selection.Count!=2||Mathf.Abs(Current.Opacity-.35f)>.0001f)throw new Exception("Selection persistence failed");
  GroupSelection(true);ClearSelection();Choose(b.id);if(selection.Count!=1)throw new Exception("Unlock failed");
  Undo(-1);Choose(b.id);if(selection.Count!=2)throw new Exception("Undo unlock failed");Delete();if(design.items.Any(i=>i.id==a.id||i.id==b.id))throw new Exception("Multi-delete failed");Undo(-1);if(design.items.Count!=7)throw new Exception("Undo multi-delete failed");
  design=JsonUtility.FromJson<Design>(original);ClearSelection();Commit();Rebuild();Debug.Log("ROOM_STUDIO_SELECTION_RUNTIME_PASSED");
 }
 System.Collections.IEnumerator SmokeTest(){
  var steps=SmokeSteps();
  while(true){object next=null;bool hasNext=false;try{hasNext=steps.MoveNext();if(hasNext)next=steps.Current;}catch(Exception e){Debug.LogError("ROOM_STUDIO_RUNTIME_SMOKE_FAILED: "+e);Application.Quit(1);yield break;}if(!hasNext)yield break;yield return next;}
 }
 System.Collections.IEnumerator SmokeSteps(){
  yield return null;Design.Validate(design);if(objects.Count!=7)throw new Exception("Initial scene generation failed");
  Add(new Item{kind="Desk",name="Test workstation",x=4,z=3,scale=.33f});var testId=selected;Undo(-1);if(design.items.Any(i=>i.id==testId))throw new Exception("Undo failed");Undo(1);if(!design.items.Any(i=>i.id==testId))throw new Exception("Redo failed");
  Choose(testId);Delete();if(design.items.Any(i=>i.id==testId))throw new Exception("Delete failed");
  SelectionSmokeChecks();
  ShortcutSmokeChecks();
  PaletteSmokeChecks();
  ComponentSmokeChecks();
  FloorPlanSmokeChecks();
  PrecisionSmokeChecks();
  PlanAuthoringSmokeChecks();
  WallConnectionSceneSmokeChecks();
  WallGenerationSmokeChecks();
  DoorSmokeChecks();
  BeamSmokeChecks();
  string json=JsonUtility.ToJson(design);var round=JsonUtility.FromJson<Design>(json);Design.Validate(round);if(round.items.Count!=7)throw new Exception("Roundtrip failed");
  fileName="Automated-smoke-test";Save();if(!File.Exists(Path.Combine(SaveDirectory,SafeName()+".json")))throw new Exception("Native smoke save failed");Load(Path.Combine(SaveDirectory,SafeName()+".json"));Export();
  if(!File.Exists(Path.Combine(SaveDirectory,SafeName()+"-workstations.csv")))throw new Exception("Export failed");
  foreach(var name in new[]{"CT","PlanmecaViso","CathLab","Cyberknife","Mammography","MRI","Dental","Xray","Toilet","Basin","Chair"}){Add(new Item{kind="Model",model=name,name="Smoke "+name});Delete();}
  var calculation=ReferenceQa.Calculate(Path.Combine(Application.streamingAssetsPath,"ProShield"),JsonUtility.ToJson(design));
  while(!calculation.IsCompleted)yield return null;
  var response=JsonUtility.FromJson<QaResponse>(calculation.GetAwaiter().GetResult());
  if(response==null||response.rows==null||response.rows.Length==0)throw new Exception("Native Unity QA deserialization failed");
  foreach(var row in response.rows)if(double.IsNaN(row.shieldedAirKerma)||double.IsInfinity(row.shieldedAirKerma))throw new Exception("Invalid QA value");
  BeginQa();while(qaTask!=null)yield return null;
  if(!showQa||qa==null||qa.rows.Length==0)throw new Exception("QA result popup did not open");
  string firstInput=qaInput;double firstDose=qa.rows[0].shieldedAirKerma;showQa=false;
  design.workload*=2;BeginQa();while(qaTask!=null)yield return null;
  if(!showQa||qa==null||qaInput==firstInput||Math.Abs(qa.rows[0].shieldedAirKerma-2*firstDose)>1e-9)throw new Exception("QA recalculation did not use the changed design");
  ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../qa-popup.png"));yield return new WaitForSeconds(1);
  showQa=false;design.workload/=2;Debug.Log("ROOM_STUDIO_QA_POPUP_SMOKE_PASSED");
  var sourceImport=ReferenceQa.Execute(Path.Combine(Application.streamingAssetsPath,"ProShield"),File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ProShield","import-fixture.json")),"import");
  while(!sourceImport.IsCompleted)yield return null;
  var imported=JsonUtility.FromJson<WorkspaceResponse>(sourceImport.GetAwaiter().GetResult());Design.Validate(imported.design);
  design=imported.design;Commit();Rebuild();string preserved=design.sourceJson;
  fileName="Automated-import-smoke-test";Save();if(!File.Exists(Path.Combine(SaveDirectory,SafeName()+".json")))throw new Exception("Imported smoke save failed");Load(Path.Combine(SaveDirectory,SafeName()+".json"));if(design.sourceJson!=preserved)throw new Exception("Source JSON lost during native save/load");
  var sourceExport=ReferenceQa.Execute(Path.Combine(Application.streamingAssetsPath,"ProShield"),JsonUtility.ToJson(design),"export");while(!sourceExport.IsCompleted)yield return null;
  var exported=JsonUtility.FromJson<WorkspaceResponse>(sourceExport.GetAwaiter().GetResult());if(string.IsNullOrEmpty(exported.json))throw new Exception("Source export missing");File.WriteAllText(Path.Combine(SaveDirectory,"Automated-import-roundtrip.json"),exported.json);
  var sourceQa=ReferenceQa.Calculate(Path.Combine(Application.streamingAssetsPath,"ProShield"),JsonUtility.ToJson(design));while(!sourceQa.IsCompleted)yield return null;
  var sourceResponse=JsonUtility.FromJson<QaResponse>(sourceQa.GetAwaiter().GetResult());if(sourceResponse.rows==null||sourceResponse.rows.Length==0)throw new Exception("Imported source QA failed");
  ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../import-preview.png"));yield return new WaitForSeconds(1);
  Debug.Log("ROOM_STUDIO_IMPORT_SMOKE_PASSED");design=Design.Example();Rebuild();
  top=false;cutaway=true;Rebuild();yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../preview.png"));yield return new WaitForSeconds(1);Debug.Log("ROOM_STUDIO_RUNTIME_SMOKE_PASSED");Application.Quit();
 }
}
}
