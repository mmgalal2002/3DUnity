using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace RoomStudio {
// Web callbacks complete on Unity's main thread; WebGL cannot start a Node process.
[Preserve] public sealed class BrowserBridge : MonoBehaviour {
 static BrowserBridge instance;
 static int nextId;
 static readonly Dictionary<int,TaskCompletionSource<string>> pending=new Dictionary<int,TaskCompletionSource<string>>();
 [Serializable] class Response { public int id=0; public bool ok=false; public string payload=null,error=null; }
#if UNITY_WEBGL && !UNITY_EDITOR
 [DllImport("__Internal")] static extern void RSExecute(string receiver,int id,string enginePath,string json,string mode);
 [DllImport("__Internal")] static extern void RSChooseFile(string receiver,int id,int floorPlan,int maxJsonBytes);
 [DllImport("__Internal")] static extern void RSShowCopyableHelp(string text);
 [DllImport("__Internal")] static extern void RSDownload(string name,string text);
 [DllImport("__Internal")] static extern void RSSyncFiles(string receiver,int id);
 [DllImport("__Internal")] static extern void RSCTDiagnostics(string json);
#endif
 static Task<string> Begin(Action<string,int> launch){
  if(instance==null){instance=new GameObject("RoomStudioBrowserBridge").AddComponent<BrowserBridge>();DontDestroyOnLoad(instance.gameObject);}
  int id=++nextId;var source=new TaskCompletionSource<string>();pending.Add(id,source);
  try{launch(instance.gameObject.name,id);}catch(Exception e){pending.Remove(id);source.SetException(e);}
  return source.Task;
 }
 public static Task<string> Execute(string enginePath,string json,string mode){
#if UNITY_WEBGL && !UNITY_EDITOR
  return Begin((receiver,id)=>RSExecute(receiver,id,enginePath,json,mode));
#else
  throw new PlatformNotSupportedException("Browser calculation is only used in WebGL.");
#endif
 }
 public static Task<string> ChooseFile(bool floorPlan=false,int maxJsonBytes=25*1024*1024){
  if(maxJsonBytes<1||maxJsonBytes>25*1024*1024)throw new ArgumentOutOfRangeException(nameof(maxJsonBytes));
#if UNITY_WEBGL && !UNITY_EDITOR
  return Begin((receiver,id)=>RSChooseFile(receiver,id,floorPlan?1:0,maxJsonBytes));
#else
  throw new PlatformNotSupportedException("Browser file picker is only used in WebGL.");
#endif
 }
 public static void ShowCopyableHelp(string text){
#if UNITY_WEBGL && !UNITY_EDITOR
  RSShowCopyableHelp(text);
#else
  GUIUtility.systemCopyBuffer=text;
#endif
 }
 public static void Download(string name,string text){
#if UNITY_WEBGL && !UNITY_EDITOR
  RSDownload(name,text);
#endif
 }
 public static void SyncFiles(){
#if UNITY_WEBGL && !UNITY_EDITOR
  RSSyncFiles("",0);
#endif
 }
 public static Task<string> SyncFilesAsync(){
#if UNITY_WEBGL && !UNITY_EDITOR
  return Begin((receiver,id)=>RSSyncFiles(receiver,id));
#else
  return Task.FromResult("");
#endif
 }
 public static void CtDiagnostics(string json){
#if UNITY_WEBGL && !UNITY_EDITOR
  RSCTDiagnostics(json);
#endif
 }
 [Preserve] public void OnBrowserResponse(string json){
  var response=JsonUtility.FromJson<Response>(json);
  if(response==null||!pending.TryGetValue(response.id,out var source))return;
  pending.Remove(response.id);
  if(response.ok)source.TrySetResult(response.payload);else source.TrySetException(new Exception(response.error??"Browser operation failed."));
 }
 }
}
