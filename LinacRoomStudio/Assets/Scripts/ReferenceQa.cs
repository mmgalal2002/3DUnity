using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RoomStudio {
[Serializable] public class QaRow {
 public string workstationName,equipmentName,calculationStandard,barrierMaterial;
 public double shieldedAirKerma,unshieldedAirKerma,requiredThickness,additionalThickness,designGoal;
 public bool passed;
}
[Serializable] public class QaResponse { public string engine,notice,summary; public QaRow[] rows; }
[Serializable] public class WorkspaceResponse { public Design design; public string summary,json; }
public static class ReferenceQa {
 public static Task<string> Calculate(string enginePath,string designJson){return Execute(enginePath,designJson,"qa");}
 public static Task<string> Execute(string enginePath,string designJson,string mode){
 if(mode!="qa"&&mode!="import"&&mode!="export")throw new ArgumentException("Unknown reference operation");
#if UNITY_WEBGL && !UNITY_EDITOR
 return BrowserBridge.Execute(enginePath,designJson,mode);
#else
 return Task.Run(()=>{
  string folder=Path.Combine(Path.GetTempPath(),"RoomStudioQA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  try{
   string input=Path.Combine(folder,"input.json"),output=Path.Combine(folder,"output.json");File.WriteAllText(input,designJson);
   var info=new ProcessStartInfo{FileName=Path.Combine(enginePath,"node.exe"),Arguments="\""+Path.Combine(enginePath,"runner.mjs")+"\" \""+input+"\" \""+output+"\" "+mode,UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
   using(var p=Process.Start(info)){
    var error=p.StandardError.ReadToEndAsync();
    if(!p.WaitForExit(120000)){p.Kill();throw new Exception("Calculation timed out.");}
    if(p.ExitCode!=0)throw new Exception(error.GetAwaiter().GetResult());
   }
   return File.ReadAllText(output);
  }finally{Directory.Delete(folder,true);}
 });
#endif
 }
}
}
