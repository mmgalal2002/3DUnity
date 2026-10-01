using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using RoomStudio;

[InitializeOnLoad]
public static class BuildStudio {
 static void EnsureFolders(){
  foreach(var folder in new[]{"Assets/Scenes","Assets/Resources","Assets/Generated"})Directory.CreateDirectory(folder);
  AssetDatabase.Refresh();
  // Runtime Shader.Find calls need a build dependency so Unity does not strip these shaders.
  foreach(var shaderName in new[]{"Standard","Sprites/Default"}){
   string path="Assets/Resources/"+shaderName.Replace('/','_')+"-Runtime.mat";
   if(AssetDatabase.LoadAssetAtPath<Material>(path)==null){var shader=Shader.Find(shaderName);if(shader==null)throw new Exception("Required shader unavailable: "+shaderName);AssetDatabase.CreateAsset(new Material(shader),path);}
  }
  // Include the runtime fade keyword variant used by object opacity controls.
  const string fadePath="Assets/Resources/Standard-Fade-Runtime.mat";
  if(AssetDatabase.LoadAssetAtPath<Material>(fadePath)==null){var fade=new Material(Shader.Find("Standard"));fade.SetFloat("_Mode",2);fade.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);fade.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);fade.SetInt("_ZWrite",0);fade.EnableKeyword("_ALPHABLEND_ON");fade.SetOverrideTag("RenderType","Transparent");fade.renderQueue=3000;fade.color=new Color(1,1,1,.5f);AssetDatabase.CreateAsset(fade,fadePath);}
 }
 static readonly string[] ModelNames={"Linac","Desk","CT","PlanmecaViso","CathLab","Cyberknife","Mammography","MRI","Dental","Xray","Toilet","Basin","Chair","Dot"};
 static readonly string[] TrackedModelNames={"Linac","CT","PlanmecaViso","Cyberknife","MRI","Toilet","Basin","Chair","Dot"};
 static bool IsTrackedModel(string name){return Array.IndexOf(TrackedModelNames,name)>=0;}
 static string SourceHash(string name){var bytes=File.ReadAllBytes("Assets/ModelData/"+name+".json");int length=bytes.Length;while(length>0&&(bytes[length-1]=='\r'||bytes[length-1]=='\n'))length--;using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes,0,length)).Replace("-","").ToLowerInvariant();}
 static bool NeedsImport(string name){
  if(!File.Exists("Assets/Resources/"+name+".prefab"))return true;
  if(!IsTrackedModel(name))return false;
  string marker="Assets/Generated/"+name+"/source.sha256";
  return !File.Exists(marker)||File.ReadAllText(marker).Trim()!=SourceHash(name);
 }
 static void EnsureModels(){EnsureFolders();foreach(var name in ModelNames)if(NeedsImport(name))Import(name);AssetDatabase.SaveAssets();}
 static BuildStudio(){EditorApplication.delayCall+=()=>{if(!Application.isBatchMode){EnsureModels();if(!File.Exists("Assets/Scenes/RoomStudio.unity"))Prepare();}};}
 [MenuItem("Room Studio/Prepare scene")]
 public static void Prepare(){
  EnsureModels();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Room Studio").AddComponent<StudioApp>();
  if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/RoomStudio.unity"))throw new Exception("Could not save RoomStudio scene");AssetDatabase.SaveAssets();
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/RoomStudio.unity",true)};
 }
 [Serializable] public class Model {public Mat[] materials; public Part[] parts;}
 [Serializable] public class Mat {public string name; public float[] color,emission; public float metallic,roughness;}
 [Serializable] public class Part {public string name; public float[] vertices,normals,position,rotation,scale;public int[] indices;public int material;}
 static Vector3 V(float[] a,int k=0){return new Vector3(a[k],a[k+1],a[k+2]);}
 static void Import(string name){
  string folder="Assets/Generated/"+name;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  foreach(var path in Directory.GetFiles(folder,"Material*.mat"))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
  foreach(var path in Directory.GetFiles(folder,"Mesh*.asset"))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
  var data=JsonUtility.FromJson<Model>(File.ReadAllText("Assets/ModelData/"+name+".json"));var materials=new Material[data.materials.Length];
  for(int k=0;k<materials.Length;k++){var m=data.materials[k];var mat=new Material(Shader.Find("Standard"));mat.name=m.name;mat.color=new Color(m.color[0],m.color[1],m.color[2],m.color[3]);mat.SetFloat("_Metallic",m.metallic);mat.SetFloat("_Glossiness",1-m.roughness); if(m.emission!=null){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(m.emission[0],m.emission[1],m.emission[2]));}AssetDatabase.CreateAsset(mat,folder+"/Material"+k+".mat");materials[k]=mat;}
  var root=new GameObject(name);
  for(int k=0;k<data.parts.Length;k++){var p=data.parts[k];var go=new GameObject(p.name);go.transform.SetParent(root.transform);go.transform.localPosition=V(p.position);go.transform.localRotation=new Quaternion(p.rotation[0],p.rotation[1],p.rotation[2],p.rotation[3]);go.transform.localScale=V(p.scale);var mesh=new Mesh{name=p.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};var vv=new Vector3[p.vertices.Length/3];for(int n=0;n<vv.Length;n++)vv[n]=V(p.vertices,n*3);mesh.vertices=vv;mesh.triangles=p.indices;if(p.normals.Length==p.vertices.Length){var nn=new Vector3[vv.Length];for(int n=0;n<nn.Length;n++)nn[n]=V(p.normals,n*3);mesh.normals=nn;}else mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/Mesh"+k+".asset");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[p.material];}
  if(name=="Cyberknife"||name=="Basin"){
   var renderers=root.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
   var offset=new Vector3(-bounds.center.x,0,-bounds.center.z);
   foreach(Transform child in root.transform)child.localPosition+=offset;
  }
  if(name=="Dot"){
   var renderers=root.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
   float diameter=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));if(diameter<=0)throw new Exception("Dot source has no display extent.");
   foreach(Transform child in root.transform){child.localPosition=(child.localPosition-bounds.center)/diameter;child.localScale/=diameter;}
  }
  PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
  if(IsTrackedModel(name))File.WriteAllText(folder+"/source.sha256",SourceHash(name)+"\n");
 }
 [MenuItem("Room Studio/Build Windows app")]
 public static void Build(){
  EnsureModels();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Room Studio").AddComponent<StudioApp>();
  if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/RoomStudio.unity"))throw new Exception("Could not save RoomStudio scene");AssetDatabase.SaveAssets();
  PlayerSettings.companyName="RoomStudio";PlayerSettings.productName="LINAC Room Studio";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  RunChecks();
  var options=new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/RoomStudio.unity"},locationPathName="../Windows/LinacRoomStudio.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};
  var report=BuildPipeline.BuildPlayer(options);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
  Debug.Log("ROOM_STUDIO_BUILD_SUCCESS");
 }
 [MenuItem("Room Studio/Test Windows app")]
 public static void TestWindowsApp(){
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Windows"));
  string app=Path.Combine(folder,"LinacRoomStudio.exe");if(!File.Exists(app))throw new Exception("Build the Windows app first.");
  System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=app,Arguments="--smoke-test -logFile \""+Path.Combine(folder,"runtime-smoke.log")+"\"",WorkingDirectory=folder,UseShellExecute=false,CreateNoWindow=true});
 }
 [MenuItem("Room Studio/Build WebGL app")]
 public static void BuildWebGL(){
  Prepare();RunChecks();
  PlayerSettings.companyName="RoomStudio";PlayerSettings.productName="LINAC Room Studio";
  PlayerSettings.defaultWebScreenWidth=1600;PlayerSettings.defaultWebScreenHeight=1000;PlayerSettings.runInBackground=true;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
  PlayerSettings.WebGL.template="PROJECT:RoomStudio";
  PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
  PlayerSettings.WebGL.dataCaching=true;
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/RoomStudio.unity"},locationPathName="../WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
  // The generated WebGL distribution runs the engine in a browser worker.
  string desktopRuntime=Path.GetFullPath("../WebGL/StreamingAssets/ProShield/node.exe");
  if(File.Exists(desktopRuntime))File.Delete(desktopRuntime);
  RefreshWebGLRevision();
  Debug.Log("ROOM_STUDIO_WEBGL_BUILD_SUCCESS");
 }
 [MenuItem("Room Studio/Refresh WebGL cache revision")]
 public static void RefreshWebGLRevision(){
  // Incremental builds can reuse preprocessed template output, including Date.now().
  string path=Path.GetFullPath("../WebGL/index.html");
  string html=File.ReadAllText(path),revision=Guid.NewGuid().ToString("N");
  var loader=new System.Text.RegularExpressions.Regex("(Build/[^\"?]+\\.loader\\.js\\?v=)[^\"]+");
  var config=new System.Text.RegularExpressions.Regex("(const buildRevision=')[^']+(')");
  if(loader.Matches(html).Count!=1||config.Matches(html).Count!=1)throw new Exception("WebGL launcher revision markers missing or ambiguous");
  html=loader.Replace(html,m=>m.Groups[1].Value+revision);
  html=config.Replace(html,m=>m.Groups[1].Value+revision+m.Groups[2].Value);
  File.WriteAllText(path,html);
  Debug.Log("ROOM_STUDIO_WEBGL_REVISION_UPDATED: "+revision);
 }
 [MenuItem("Room Studio/Run editor checks")]
 public static void RunChecks(){
  EnsureModels();
  SelectionChecks.Run();
  DoorChecks.Run();
  PrecisionChecks.Run();
  PlanAuthoringChecks.Run();
  PlanPathExtractionChecks.Run();
  WallConnectionChecks.Run();
  WallGenerationChecks.Run();
  CheckCTAssets();
  DiagnosticChecks.Run();
  ComponentChecks.Run();
    FloorPlanChecks.Run();
  if(string.Join(",",EquipmentPalette.Groups)!="Linac,CT,MRI,Room items")throw new Exception("Palette group order changed");
  if(string.Join(",",System.Array.ConvertAll(EquipmentPalette.Entries,e=>e.group+":"+e.label+":"+e.model))!="Linac:Linac HD:Linac,Linac:CyberKnife:Cyberknife,CT:CT:CT,CT:CathLab:CathLab,CT:Dental OPG:PlanmecaViso,CT:X-ray:Xray,CT:Mammography:Mammography,CT:Dental:Dental,MRI:MRI:MRI,Room items:Toilet:Toilet,Room items:Basin:Basin,Room items:Chair:Chair")throw new Exception("Palette hierarchy or model IDs changed");
  foreach(var entry in EquipmentPalette.Entries){var item=EquipmentPalette.Create(entry.model,Vector3.zero);if(item.model!=entry.model||item.kind!=(entry.model=="Linac"?"LINAC":"Model"))throw new Exception("Palette source role changed");}
  Debug.Log("ROOM_STUDIO_PALETTE_CHECKS_PASSED: exact hierarchy, stable IDs and source roles; all 14 model resources checked below");
  var d=Design.Example();Design.Validate(d);var clone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(d));Design.Validate(clone);if(clone.items.Count!=7||clone.items[5].kind!="LINAC")throw new Exception("Save roundtrip failed");
  clone.width=float.NaN;bool rejected=false;try{Design.Validate(clone);}catch{rejected=true;}if(!rejected)throw new Exception("NaN accepted");
  clone=Design.Example();clone.items.Add(clone.items[0]);rejected=false;try{Design.Validate(clone);}catch{rejected=true;}if(!rejected)throw new Exception("Duplicate ID accepted");
  foreach(string name in ModelNames){var p=Resources.Load<GameObject>(name);if(p==null||p.GetComponentsInChildren<MeshRenderer>().Length==0)throw new Exception("Missing model "+name);}
  foreach(var expected in new[]{new{model="Linac",parts=208},new{model="CT",parts=37},new{model="PlanmecaViso",parts=32},new{model="Cyberknife",parts=58},new{model="MRI",parts=45},new{model="Toilet",parts=2},new{model="Basin",parts=13},new{model="Chair",parts=4}})
   if(Resources.Load<GameObject>(expected.model).GetComponentsInChildren<MeshFilter>().Length!=expected.parts)throw new Exception("Supplied "+expected.model+" geometry did not import completely.");
  clone=Design.Example();clone.items[0].y=1;Design.ResizeRoom(clone,24,15,7);Design.Validate(clone);
  foreach(var wall in clone.items)if(wall.kind=="Wall"&&Math.Abs(wall.y+wall.height-7)>.0001)throw new Exception("Wall ceiling link failed");
  if(Math.Abs(clone.items[0].length-15)>.0001||Math.Abs(clone.items[2].length-24)>.0001)throw new Exception("Room footprint link failed");
  clone.linkWallsToRoom=false;clone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(clone));if(clone.linkWallsToRoom)throw new Exception("Wall link setting was not saved");
  Debug.Log("ROOM_STUDIO_CHECKS_PASSED: model imports, project serialization, invalid numeric data, duplicate IDs, linked walls and ceiling");
 }
 public static void CheckCTAssets(){
  EnsureModels();CtShieldingChecks.Run();
  var prefab=Resources.Load<GameObject>("Dot");if(prefab==null||prefab.GetComponentsInChildren<MeshFilter>().Length!=1)throw new Exception("Dot model did not import completely.");
  var instance=UnityEngine.Object.Instantiate(prefab);var bounds=instance.GetComponentInChildren<MeshRenderer>().bounds;
  try{if(Vector3.Distance(bounds.center,Vector3.zero)>.00001f||Math.Abs(bounds.size.y-1)>.00001f)throw new Exception("Dot display normalization is incorrect.");}
  finally{UnityEngine.Object.DestroyImmediate(instance);}
  Debug.Log("ROOM_STUDIO_CT_ASSET_CHECKS_PASSED: supplied Dot mesh, centered unit display prefab, separate coefficient resources");
 }
}
