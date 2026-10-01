using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Allows an already-open, licensed editor to run repeatable builds without another
// Unity process. Write {"command":"windows"|"webgl"|"checks"} to the request file.
[InitializeOnLoad]
public static class BuildRequestRunner {
 [Serializable] class Request {public string command="";}
 static bool running;
 static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../work/editor-build"));
 static BuildRequestRunner(){EditorApplication.update+=Poll;}
 static void Poll() {
  if(running||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
  string path=Path.Combine(Folder,"request.json");if(!File.Exists(path))return;
  Request request;try{request=JsonUtility.FromJson<Request>(File.ReadAllText(path));}catch{return;}
  if(request==null||Array.IndexOf(new[]{"windows","webgl","checks"},request.command)<0)return;
  running=true;File.Delete(path);
  string output=Path.Combine(Folder,request.command+".log");var log=new StringBuilder();
  Application.LogCallback capture=(message,stack,type)=>{log.AppendLine(type+": "+message);if(type==LogType.Error||type==LogType.Exception)log.AppendLine(stack);};
  Application.logMessageReceived+=capture;
  try {
   Debug.Log("BUILD_REQUEST_STARTED: "+request.command+" "+DateTime.UtcNow.ToString("O"));
   if(request.command=="windows")BuildStudio.Build();else if(request.command=="webgl")BuildStudio.BuildWebGL();else BuildStudio.RunChecks();
   Debug.Log("BUILD_REQUEST_SUCCEEDED: "+request.command);
  }catch(Exception e){Debug.LogException(e);log.AppendLine("BUILD_REQUEST_FAILED: "+request.command);}
  finally {Application.logMessageReceived-=capture;File.WriteAllText(output,log.ToString());running=false;}
 }
}
