using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 void PlanAuthoringSmokeChecks(){
  PlanAuthoringChecks.Run();string original=JsonUtility.ToJson(design);string oldTool=tool;tool="Select";
  Action<bool,string> need=(ok,message)=>{if(!ok)throw new Exception("Plan authoring runtime: "+message);};
  Action finish=()=>{int turns=0;while(planJob!=null){PlanAuthoringUpdate();if(++turns>20000)throw new Exception("Mask processing stalled");}};
  try{
   ReplaceFloorPlan(PlanAuthoringChecks.Fixture(),"Fixture imported");int itemCount=design.items.Count;
   string before=JsonUtility.ToJson(design);int count=history.Count,index=historyIndex;OpenPlanAuthoring();
   need(before==JsonUtility.ToJson(design)&&count==history.Count&&index==historyIndex,"opening panel changed design/history");
   planDraft.rules.Add(new PlanColorRule{id="magenta",target=Color.magenta,display=Color.magenta,material="Concrete",height=design.height,tolerance=.01f});
   planDraft.calibration=new PlanCalibration{method="TwoPoint",a=new Vector2Data(20.5f,100.5f),b=new Vector2Data(520.5f,100.5f),distanceMetres=5};
   StartPlanMask();finish();need(planMask!=null&&planMask.retained.Sum()==2800,"preview without confirmed scale");
   need(before==JsonUtility.ToJson(design)&&count==history.Count&&index==historyIndex,"preview mutated design/history");
   var previous=planMask;StartPlanMask();PlanAuthoringUpdate();planJob=null;finish();need(planMask==previous,"cancel replaced prior mask");
   StartPlanMask();planDraft.rules[0].tolerance=.02f;PlanAuthoringUpdate();need(planJob==null&&planMask==previous,"late completion after rule edit");
   planDraft.rules[0].target=Color.blue;StartPlanMask();finish();need(planMask==previous&&planMessage.Contains("No retained matches"),"empty result erased prior preview");planDraft.rules[0].target=Color.magenta;
   need(before==JsonUtility.ToJson(design)&&index==historyIndex,"cancel or empty result mutated design");
   ApplyPlanSettings(true);need(PlanAuthoring.CalibrationCurrent(floorPlan)&&floorPlan.authoring.rules.Count==1,"confirmed settings did not apply");
   need(design.items.Count==itemCount&&historyIndex==index+1,"settings created geometry or multiple undo records");
   StartPlanMask();Undo(-1);need(planJob==null&&PlanAuthoring.IsEmpty(floorPlan.authoring),"undo retained authoring or stale work");
   bool rejected=false;try{ApplyPlanSettings(false);}catch{rejected=true;}need(rejected,"stale draft applied after undo");
   Undo(1);need(PlanAuthoring.CalibrationCurrent(floorPlan)&&floorPlan.authoring.rules.Count==1,"redo lost calibration/rules");
   ClosePlanAuthoring();OpenPlanAuthoring();planDraft.rules[0].name="Unapplied";ClosePlanAuthoring();need(floorPlan.authoring.rules[0].name!="Unapplied","closing saved draft edits");
   string saved=JsonUtility.ToJson(design);LoadJson(saved,"REQ5 authoring roundtrip");need(PlanAuthoring.CalibrationCurrent(floorPlan)&&floorPlanTexture.width==640&&floorPlan.authoring.rules[0].material=="Concrete","embedded source roundtrip");
   // Deterministic artifacts also support real player/browser UI checks.
   Directory.CreateDirectory(SaveDirectory);File.WriteAllText(Path.Combine(SaveDirectory,"REQ5-color-fixture.json"),JsonUtility.ToJson(design,true));
   File.WriteAllBytes(Path.Combine(SaveDirectory,"REQ5-color-fixture.png"),Convert.FromBase64String(floorPlan.imageBase64));BrowserBridge.SyncFiles();
   Debug.Log("ROOM_STUDIO_PLAN_AUTHORING_RUNTIME_PASSED: draft/preview invariance, unconfirmed detection, cooperative cancellation, stale/empty results, one-step apply undo-redo, discard and embedded-image native load");
  }finally{ClosePlanAuthoring();tool=oldTool;design=JsonUtility.FromJson<Design>(original);ClearSelection();Commit();Rebuild();}
 }
}
}
