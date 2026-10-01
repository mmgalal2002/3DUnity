using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 readonly DiagnosticNumber diagnosticHeight=new DiagnosticNumber();
 bool diagnosticDetails,diagnosticHistory,diagnosticWeekly;
 string diagnosticError="",diagnosticImportMachine="",diagnosticImportFingerprint="";
 Task<string> diagnosticImportTask;
 DiagnosticReadiness diagnosticReadinessView;
 bool diagnosticCalculating;
 readonly System.Collections.Generic.List<Tuple<string,string,LineRenderer>> diagnosticRays=new System.Collections.Generic.List<Tuple<string,string,LineRenderer>>();
 bool TreatmentCalculation=>DiagnosticData.Family(design.items.Find(i=>i.id==design.diagnosticCalculation?.activeMachineId))==DiagnosticMachineType.Treatment;
 void OpenCalculation(Item item=null){
  if(dirty)Commit();
  if(item!=null)DiagnosticData.Select(design,item);
  tab="Calculation";rightScroll=Vector2.zero;showQa=false;ctResultsPopup=false;
  if(Layout.compact)compactPanel="Properties";
  RebuildDiagnosticRays();
 }
 void DiagnosticNumeric(string key,string label,DiagnosticNumber number,double minimum,double maximum){
  GUILayout.Label(label,small);GUI.SetNextControlName("edit:diagnostic:"+key);
  string next=GUILayout.TextField(number.text??"",40,numberField);
  CaptureCtControl(label,"input");
  if(next!=number.text){number.text=next;GUI.changed=true;}
  if(!string.IsNullOrEmpty(number.text)&&(!number.TryGet(out double value)||value<minimum||value>maximum))
   GUILayout.Label("Enter a finite value from "+CtValue(minimum)+" to "+CtValue(maximum)+".",small);
 }
 void DiagnosticAct(Action action){
  try{action();diagnosticError="";}
  catch(Exception error){diagnosticError=error.Message;status="Calculation: "+error.Message;}
 }
 void SelectCalculationMachine(Item item){
  DiagnosticAct(()=>{
   DiagnosticData.Select(design,item);
   if(!item.locked)DiagnosticData.Configure(design,item);
   Commit();OpenCalculation();Rebuild();
  });
 }
 void ArmDiagnosticPoint(DiagnosticPointRole role){
  if(top&&(!diagnosticHeight.TryGet(out double height)||height<-100||height>100)){
   diagnosticError="Enter the placement height in metres before placing "+role+" in 2D.";GUI.FocusControl("edit:diagnostic:height");return;
  }
  var machine=design.items.Find(i=>i.id==design.diagnosticCalculation?.activeMachineId);
  if(machine!=null&&machine.locked){diagnosticError=machine.name+" is locked. Select it in Object to unlock.";return;}
  if(role==DiagnosticPointRole.Target&&machine==null){diagnosticError="Choose a machine before placing Target.";return;}
  tool="Diagnostic_"+role;wallStart=null;compactPanel="";GUI.FocusControl(null);
  status="Place "+(role==DiagnosticPointRole.Scatter?"Scatter (patient)":role.ToString())+(top?" at the entered height.":" on a surface, or at the entered height.")+" Escape cancels.";
 }
 void PlaceDiagnosticPoint(Vector3 snapped,Ray ray){
  DiagnosticAct(()=>{
   var role=(DiagnosticPointRole)Enum.Parse(typeof(DiagnosticPointRole),tool.Substring("Diagnostic_".Length));
   var data=DiagnosticData.Ensure(design);var machineItem=design.items.Find(i=>i.id==data.activeMachineId);
   if(machineItem!=null)DiagnosticData.Configure(design,machineItem);
   CtVector position;
   var hit=Physics.RaycastAll(ray).OrderBy(h=>h.distance).FirstOrDefault(h=>{
    var selectable=h.collider.GetComponentInParent<Selectable>();var item=design.items.Find(i=>i.id==selectable?.id);
    return item!=null&&!DiagnosticData.IsPoint(item)&&!CtShieldingData.IsPoint(item);
   });
   if(!top&&hit.collider!=null)position=new CtVector(hit.point.x,hit.point.y,hit.point.z);
   else{
    if(!diagnosticHeight.TryGet(out double height)||height<-100||height>100)throw new Exception("Enter a finite placement height, or choose a 3D surface. Floor height is not assumed.");
    if(top)position=new CtVector(snapped.x,height/data.metersPerUnityUnit,snapped.z);
    else{var plane=new Plane(Vector3.up,new Vector3(0,(float)(height/data.metersPerUnityUnit),0));if(!plane.Raycast(ray,out float distance))throw new Exception("The pointer does not intersect the placement-height plane.");var p=ray.GetPoint(distance);position=new CtVector(p.x,height/data.metersPerUnityUnit,p.z);}
   }
   var point=DiagnosticData.Place(design,role,data.activeMachineId,position);
   ClearSelection();selection.Add(point.id);selected=point.id;tool="Select";Commit();Rebuild();OpenCalculation(point);
  });
 }
 void DiagnosticAction(DiagnosticIssue issue){
  if(issue==null)return;
  switch(issue.actionKind){
   case "ChooseMachine":leftTab="Build";if(Layout.compact)compactPanel=design.items.Any(DiagnosticData.IsMachine)?"Properties":"Build";status="Choose equipment from the palette or select a machine below.";break;
   case "ConfigureMachine":SelectCalculationMachine(design.items.Find(i=>i.id==issue.actionTarget));break;
   case "PlaceTarget":ArmDiagnosticPoint(DiagnosticPointRole.Target);break;
   case "PlaceScatter":ArmDiagnosticPoint(DiagnosticPointRole.Scatter);break;
   case "PlaceROI":ArmDiagnosticPoint(DiagnosticPointRole.ROI);break;
   case "Input":GUI.FocusControl("edit:diagnostic:"+design.diagnosticCalculation.activeMachineId+":"+issue.inputKey);break;
   case "LoadProfile":BeginDiagnosticProfileImport();break;
   case "AssociatePoint":DiagnosticAct(()=>{DiagnosticData.Associate(design,design.items.Find(i=>i.id==issue.actionTarget),DiagnosticData.Machine(design,design.diagnosticCalculation.activeMachineId));Commit();});break;
   case "UnlockObject":case "SelectPoint":case "SelectBarrier":Choose(issue.actionTarget);if(Layout.compact)compactPanel="Properties";break;
   case "RoomBarrier":tab="Room";rightScroll=Vector2.zero;if(Layout.compact)compactPanel="Properties";status="Edit "+issue.actionTarget+" material and thickness in Room.";break;
  }
 }
 static string DiagnosticActionLabel(DiagnosticIssue issue){
  switch(issue.actionKind){
   case "ChooseMachine":return "Choose / place machine";
   case "ConfigureMachine":return "Configure this machine";
   case "PlaceTarget":return "Place Target";
   case "PlaceScatter":return "Place Scatter (patient)";
   case "PlaceROI":return "Place ROI";
   case "LoadProfile":return "Load matching machine profile";
   case "Input":return "Enter "+(issue.inputKey=="kvp"?"kVp":issue.inputKey);
   case "AssociatePoint":return "Associate this point with active machine";
   case "SelectBarrier":return "Edit affected barrier";
   case "RoomBarrier":return "Edit "+issue.actionTarget+" in Room";
   case "UnlockObject":return "Select object to unlock";
   default:return "Select affected point";
  }
 }
 bool DiagnosticUI(out bool gantryAngleChanged){
  gantryAngleChanged=false;
  var data=DiagnosticData.Ensure(design);
  var item=design.items.Find(i=>i.id==data.activeMachineId&&DiagnosticData.IsMachine(i));
  Section("Active machine");
  GUILayout.Label(item==null?"No machine selected":ObjectDisplayName(item)+" / "+DiagnosticData.Family(item),body);
  var machine=DiagnosticData.Machine(design,item?.id);
  DiagnosticProfile profile=null;DiagnosticReadiness ready=null;
  if(!TreatmentCalculation){
   try{
    if(machine==null&&item!=null&&!item.locked)machine=DiagnosticData.Configure(design,item);
    if(machine!=null&&string.IsNullOrEmpty(machine.profileId)&&!item.locked){
     var matching=DiagnosticProfiles.Matching(machine);
     if(matching.Length==1)DiagnosticProfiles.Attach(design,machine,matching[0]);
    }
    profile=DiagnosticProfiles.Resolve(machine);ready=DiagnosticCalculation.Readiness(design,profile);
   }
   catch(Exception error){diagnosticError="Model validation failed: "+error.Message;}
   diagnosticReadinessView=ready;
   Section("What to do next");
   if(ready!=null){
    GUILayout.Label(diagnosticCalculating?"Calculating the current machine/ROI...":ready.canCalculatePhysical?ready.summary:ready.NextAction.explanation,body);
    if(!ready.canCalculatePhysical&&Btn(DiagnosticActionLabel(ready.NextAction))){DiagnosticAction(ready.NextAction);GUI.changed=false;}
   }
  }
  if(!string.IsNullOrEmpty(diagnosticError))GUILayout.Label(diagnosticError,body);
  var machines=design.items.Where(DiagnosticData.IsMachine).ToArray();
  if(item==null||machines.Length>1)foreach(var candidate in machines)if(Btn(ObjectDisplayName(candidate),candidate.id==data.activeMachineId)){SelectCalculationMachine(candidate);GUI.changed=false;GUIUtility.ExitGUI();}
  if(TreatmentCalculation){
   if(item.kind!="LINAC"&&string.IsNullOrEmpty(design.sourceJson)){GUILayout.Label("This treatment equipment has no configured treatment-engine source. Its visual model cannot supply a radiation result.",body);return false;}
   GUILayout.Label("Treatment engine only. Diagnostic kVp and exposure values are not used.",small);
   bool rebuild=BeamUI(out gantryAngleChanged);CalculationPanelUI();return rebuild;
  }
  DiagnosticPlacementUI();
  foreach(var point in design.items.Where(DiagnosticData.IsPoint).Where(p=>p.diagnosticPoint.role==DiagnosticPointRole.ROI))
   if(Btn("ROI: "+ObjectDisplayName(point),point.id==data.activeRoiId)){OpenCalculation(point);GUI.changed=false;GUIUtility.ExitGUI();}
  if(machine!=null&&DiagnosticData.IsXray(machine.machineType)){
   bool enabled=GUI.enabled;GUI.enabled=enabled&&!item.locked;
   Section("Inputs");
   DiagnosticNumeric(machine.machineId+":kvp","Tube voltage (kVp)",machine.tubeVoltageKvp,double.Epsilon,double.MaxValue);
   if(profile!=null){
    GUILayout.Label(profile.machineIdentity+" / "+profile.acquisition+"\n"+profile.exposureDefinition,small);
    foreach(var requirement in profile.inputs){
     var input=machine.exposureInputs.Find(i=>i.key==requirement.key);
     if(input!=null)DiagnosticNumeric(machine.machineId+":"+requirement.key,requirement.label+" ("+requirement.unit+")",input.value,requirement.minimum,requirement.maximum);
    }
   }
   foreach(var matching in DiagnosticProfiles.Matching(machine).Where(p=>p.id!=machine.profileId||p.revision!=machine.profileVersion))
    if(Btn("Use "+matching.machineIdentity+" / "+matching.acquisition)){DiagnosticAct(()=>{DiagnosticProfiles.Attach(design,machine,matching);Commit();});GUI.changed=false;}
   if(profile!=null&&Btn("Change machine profile")){BeginDiagnosticProfileImport();GUI.changed=false;}
   GUI.enabled=enabled;
   if(item.locked&&Btn("Select "+item.name+" to unlock")){Choose(item.id);GUI.changed=false;GUIUtility.ExitGUI();}
  }
  bool calculateEnabled=GUI.enabled;GUI.enabled=calculateEnabled&&ready?.canCalculatePhysical==true&&!GUI.changed&&!diagnosticCalculating;
  if(Btn(diagnosticCalculating?"Calculating...":"Calculate")){
   DiagnosticAct(()=>StartCoroutine(CalculateDiagnostic(profile)));GUI.changed=false;
  }
  GUI.enabled=calculateEnabled;
  Section("Air kerma at ROI");
  var latest=data.currentResult?.complete==true&&data.currentResult.machineId==data.activeMachineId&&data.currentResult.roiId==data.activeRoiId?data.currentResult:data.results.LastOrDefault(r=>r.machineId==data.activeMachineId&&r.roiId==data.activeRoiId);
  bool current=!diagnosticCalculating&&latest!=null&&latest.complete&&ready?.canCalculatePhysical==true&&latest.inputFingerprint==DiagnosticCalculation.Fingerprint(design);
  if(!current){GUILayout.Label("--",metric);GUILayout.Label(diagnosticCalculating?"Calculating...":latest==null?"No result yet":"Out of date: physical inputs or profile changed. Recalculate; the saved number is historical.",small);}
  else{
   GUILayout.Label(latest.underflow?"Below numeric display range":latest.shieldedAirKermaGy.ToString("0.######E+0")+" Gy/"+latest.basis,metric);
   GUILayout.Label(latest.isZero?"Valid zero: the entered exposure or calibrated output is zero.":"Physical air kerma, not patient dose or effective dose.",small);
   diagnosticDetails=GUILayout.Toggle(diagnosticDetails," Details");
   if(diagnosticDetails){
    GUILayout.Label("Unshielded: "+CtValue(latest.unshieldedAirKermaGy)+" Gy/"+latest.basis,small);
    foreach(var row in latest.contributions){
     GUILayout.Label(row.componentId+" / "+row.originRole+" -> ROI / "+CtValue(row.distanceMeters)+" m\nK0 "+CtValue(row.unshieldedMgy)+" mGy; log B "+CtValue(row.logB)+"; Kx "+CtValue(row.shieldedMgy)+" mGy",small);
     if(row.segments.Count==0)GUILayout.Label("No shielding on this path (B = 1).",small);
     foreach(var segment in row.segments)GUILayout.Label((design.items.Find(i=>i.id==segment.barrierId)?.name??segment.barrierId)+" / "+segment.material+" / "+CtValue(segment.pathThicknessMm)+" mm / "+segment.fitId,small);
    }
    GUILayout.Label("Profile "+latest.profileId+" @ "+latest.profileVersion+"\n"+profile.provenance,small);
   }
   DiagnosticComparisonUI(machine,profile,latest);
  }
  diagnosticHistory=GUILayout.Toggle(diagnosticHistory," Saved-result history");
  if(diagnosticHistory){
   foreach(var result in data.results.ToArray()){
    GUILayout.Label("Historical: "+result.label+" / "+(result.underflow?"below range":CtValue(result.shieldedAirKermaGy))+" Gy/"+result.basis,small);
    if(Btn("Export "+result.createdUtc)){DiagnosticAct(()=>WriteOutput(SafeName()+"-diagnostic-"+result.id+".json",JsonUtility.ToJson(result,true)));GUI.changed=false;}
    if(Btn("Remove saved result "+result.createdUtc)){data.results.Remove(result);GUI.changed=true;}
   }
   if(design.ct!=null)GUILayout.Label("Legacy CT records remain in native design JSON, including Patient evaluation roles and historical scenarios. They are not calculated or reinterpreted by this workflow.",small);
  }
  return false;
 }
 System.Collections.IEnumerator CalculateDiagnostic(DiagnosticProfile profile){
  if(diagnosticCalculating)yield break;
  var snapshot=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  string fingerprint=DiagnosticCalculation.Fingerprint(snapshot);
  diagnosticCalculating=true;
  yield return null;
  DiagnosticAct(()=>{
   var result=DiagnosticCalculation.Calculate(snapshot,profile);
   if(DiagnosticCalculation.Fingerprint(design)!=fingerprint)throw new Exception("Inputs or the active machine/ROI changed while calculating. The stale result was discarded; recalculate.");
   var data=DiagnosticData.Ensure(design);data.currentResult=result;
   if(data.results.Count<50)data.results.Add(result);
   Commit();status=data.results.Count>=50?"Calculated current air kerma. History is full; export or remove an old snapshot to save further history.":"Calculated current air kerma at ROI.";
  });
  diagnosticCalculating=false;
 }
 void DiagnosticComparisonUI(DiagnosticMachine machine,DiagnosticProfile profile,DiagnosticResult result){
  var comparison=design.diagnosticCalculation.comparison;
  if(profile.basis=="exam"||profile.basis=="exposure"){
   diagnosticWeekly=GUILayout.Toggle(diagnosticWeekly," Weekly total");
   if(diagnosticWeekly){
    DiagnosticNumeric(machine.machineId+":weekly","Exposures/exams per week",machine.weeklyExposureCount,0,1e12);
    if(machine.weeklyExposureCount.TryGet(out double count)&&count>=0&&count<=1e12){
     double weekly=result.isZero||count==0?0:Math.Exp(result.logShieldedGy+Math.Log(count));
     GUILayout.Label(!CtShieldMath.IsFinite(weekly)?"Weekly total exceeds numeric range":weekly==0&&!result.isZero&&count>0?"Weekly total below numeric display range":CtValue(weekly)+" Gy/week",small);
    }
   }
  }
  comparison.requested=GUILayout.Toggle(comparison.requested," Compare with a limit");
  if(!comparison.requested)return;
  if(profile.basis=="exam"||profile.basis=="exposure")comparison.weekly=GUILayout.Toggle(comparison.weekly," Compare weekly total");
  if(comparison.weekly&&!diagnosticWeekly)DiagnosticNumeric(machine.machineId+":weekly","Exposures/exams per week",machine.weeklyExposureCount,0,1e12);
  DiagnosticNumeric(machine.machineId+":limit","Limit (Gy/"+(comparison.weekly?"week":profile.basis)+")",comparison.limitGy,0,1e12);
  comparison.occupied=GUILayout.Toggle(comparison.occupied," Occupied-person comparison only");
  if(comparison.occupied)DiagnosticNumeric(machine.machineId+":occupancy","Occupancy (comparison only)",comparison.occupancy,0,1);
  if(DiagnosticCalculation.Comparison(design,profile,out double factor,out string reason)){
   comparison.limitGy.TryGet(out double limit);
   bool meets=result.isZero||factor==0||limit>0&&result.logShieldedGy+Math.Log(factor)<=Math.Log(limit);
   GUILayout.Label(meets?"Meets selected numeric criterion; not facility approval.":"Exceeds selected numeric criterion.",small);
  }else GUILayout.Label(reason,small);
 }
 void DiagnosticPlacementUI(){
  Section("Target | Scatter (patient) | ROI");
  var machine=DiagnosticData.Machine(design,design.diagnosticCalculation?.activeMachineId);
  foreach(var role in new[]{DiagnosticPointRole.Target,DiagnosticPointRole.Scatter,DiagnosticPointRole.ROI}){
   string id=role==DiagnosticPointRole.Target?machine?.targetPointId:role==DiagnosticPointRole.Scatter?machine?.scatterPointId:design.diagnosticCalculation?.activeRoiId;
   string label=role==DiagnosticPointRole.Scatter?"Scatter (patient)":role.ToString();
   GUILayout.Label(label+": "+(design.items.Any(i=>i.id==id)?"placed":"missing"),small);
   if(Btn("Place "+label)){ArmDiagnosticPoint(role);GUI.changed=false;}
   if(!string.IsNullOrEmpty(id)&&Btn("Select "+label)){Choose(id);GUI.changed=false;}
  }
  bool changed=GUI.changed;DiagnosticNumeric("height","Placement height (m; required in 2D)",diagnosticHeight,-100,100);GUI.changed=changed;
 }
 void DiagnosticObjectControls(Item item){
  if(!DiagnosticData.IsPoint(item)&&!DiagnosticData.IsMachine(item)&&!CtShieldingData.IsPoint(item))return;
  if(Btn("Open Calculation")){OpenCalculation(item);GUI.changed=false;GUIUtility.ExitGUI();}
  if(CtShieldingData.IsPoint(item)){
   GUILayout.Label("Legacy "+item.ctPoint.role+" annotation retained without reinterpretation. Place new Target, Scatter and ROI in Calculation.",small);return;
  }
  if(!DiagnosticData.IsPoint(item))return;
  GUILayout.Label(item.diagnosticPoint.role+" / "+(item.diagnosticPoint.role==DiagnosticPointRole.Target?"physical device-local anchor":"independent world position"),small);
  if(item.diagnosticPoint.role!=DiagnosticPointRole.Target)foreach(var device in design.items.Where(DiagnosticData.IsMachine))
   if(Btn("Associate with "+device.name,item.diagnosticPoint.machineId==device.id)){DiagnosticAct(()=>{var machine=DiagnosticData.Configure(design,device);DiagnosticData.Associate(design,item,machine);Commit();});GUI.changed=false;}
 }
 void BeginDiagnosticProfileImport(){
  if(diagnosticImportTask!=null)return;
  diagnosticImportMachine=design.diagnosticCalculation?.activeMachineId??"";diagnosticImportFingerprint=DiagnosticCalculation.Fingerprint(design);
  DiagnosticAct(()=>{
#if UNITY_WEBGL && !UNITY_EDITOR
   diagnosticImportTask=BrowserBridge.ChooseFile(false,1024*1024);
#else
   string path=FloorPlanFilePicker.Open("Choose an approved diagnostic machine profile","Machine profile (JSON)\0*.json\0\0");
   if(!string.IsNullOrEmpty(path)){
    if(new FileInfo(path).Length>1024*1024)throw new Exception("Profile exceeds 1 MiB.");
    diagnosticImportTask=Task.FromResult(File.ReadAllText(path));
   }else status="Machine profile selection cancelled. The design is unchanged.";
#endif
  });
 }
 void DiagnosticUpdate(){
  DiagnosticData.Synchronize(design);
  foreach(var point in design.items.Where(DiagnosticData.IsPoint))if(objects.TryGetValue(point.id,out var root))root.transform.position=DiagnosticData.Position(design,point).UnityVector;
  foreach(var ray in diagnosticRays){
   var from=design.items.Find(i=>i.id==ray.Item1);var to=design.items.Find(i=>i.id==ray.Item2);
   if(ray.Item3==null)continue;ray.Item3.enabled=from!=null&&to!=null;
   if(ray.Item3.enabled){ray.Item3.SetPosition(0,DiagnosticData.Position(design,from).UnityVector);ray.Item3.SetPosition(1,DiagnosticData.Position(design,to).UnityVector);}
  }
  if(diagnosticImportTask==null||!diagnosticImportTask.IsCompleted)return;
  DiagnosticAct(()=>{
   string json=diagnosticImportTask.GetAwaiter().GetResult();
   if(diagnosticImportMachine!=design.diagnosticCalculation?.activeMachineId||diagnosticImportFingerprint!=DiagnosticCalculation.Fingerprint(design))throw new Exception("Machine or inputs changed while choosing the profile. Load it again for the intended machine.");
   var machine=DiagnosticData.Machine(design,diagnosticImportMachine);if(machine==null)throw new Exception("Configure a machine before loading its profile.");
   var profile=DiagnosticProfiles.Import(json);DiagnosticProfiles.Attach(design,machine,profile);Commit();
  });diagnosticImportTask=null;
 }
 void DrawDiagnosticLabels(){
  foreach(var point in design.items.Where(DiagnosticData.IsPoint)){
   var projected=cam.WorldToScreenPoint(DiagnosticData.Position(design,point).UnityVector);
   var position=new Vector2(projected.x/uiScale,(Screen.height-projected.y)/uiScale);
   if(projected.z>0&&View.Contains(position))GUI.Label(new Rect(position.x+8,position.y-20,165,28),point.diagnosticPoint.role==DiagnosticPointRole.Scatter?"Scatter (patient)":point.diagnosticPoint.role.ToString(),small);
  }
  foreach(var ray in diagnosticRays){
   if(ray.Item3==null||!ray.Item3.enabled)continue;
   var from=design.items.Find(i=>i.id==ray.Item1);var to=design.items.Find(i=>i.id==ray.Item2);
   var projected=cam.WorldToScreenPoint((ray.Item3.GetPosition(0)+ray.Item3.GetPosition(1))/2);
   var position=new Vector2(projected.x/uiScale,(Screen.height-projected.y)/uiScale);if(projected.z<=0||!View.Contains(position))continue;
   double distance=DiagnosticCalculation.Distance(DiagnosticData.Position(design,from),DiagnosticData.Position(design,to))*design.diagnosticCalculation.metersPerUnityUnit;
   GUI.Label(new Rect(position.x+6,position.y,230,24),from.diagnosticPoint.role+" -> "+to.diagnosticPoint.role+": "+CtValue(distance)+" m (3D)",small);
  }
 }
 void RebuildDiagnosticRays(){
  foreach(var ray in diagnosticRays)if(ray.Item3!=null)Destroy(ray.Item3.gameObject);
  diagnosticRays.Clear();var data=design.diagnosticCalculation;var machine=DiagnosticData.Machine(design,data?.activeMachineId);if(machine==null||world==null)return;
  foreach(var pair in new[]{Tuple.Create(machine.targetPointId,machine.scatterPointId),Tuple.Create(machine.scatterPointId,data.activeRoiId),Tuple.Create(machine.targetPointId,data.activeRoiId)}){
   if(!design.items.Any(i=>i.id==pair.Item1&&DiagnosticData.IsPoint(i))||!design.items.Any(i=>i.id==pair.Item2&&DiagnosticData.IsPoint(i)))continue;
   var line=Line("Diagnostic geometry (not radiation transport)",new Color(.5f,.7f,.8f,.6f),.014f,world);diagnosticRays.Add(Tuple.Create(pair.Item1,pair.Item2,line));
  }
 }
 void DiagnosticPointCoordinates(Item item){
   bool enabled=GUI.enabled;
   bool ownerLocked=item.diagnosticPoint.role==DiagnosticPointRole.Target&&design.items.Find(i=>i.id==item.diagnosticPoint.machineId)?.locked==true;
   GUI.enabled=enabled&&!ownerLocked;
   if(ownerLocked)GUILayout.Label("Unlock the owning machine to edit Target.",small);
   var position=DiagnosticData.Position(design,item);double units=design.diagnosticCalculation.metersPerUnityUnit;
   double[] values={position.x*units,position.y*units,position.z*units};
   var point=item.diagnosticPoint;
   if(!point.hasCoordinateDraft)point.coordinateDraft=values.Select(value=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).ToArray();
   for(int axis=0;axis<3;axis++){
    string key=item.id+":coordinate:"+axis;
    var number=new DiagnosticNumber{text=point.coordinateDraft[axis]};DiagnosticNumeric(key,"Position "+"XYZ"[axis]+" (m)",number,-10000,10000);
    if(number.text!=point.coordinateDraft[axis]){point.hasCoordinateDraft=true;point.coordinateDraft[axis]=number.text;}
    if(number.TryGet(out double value)&&Math.Abs(value)<=10000&&value!=values[axis])values[axis]=value;
   }
   if(position.x*units!=values[0]||position.y*units!=values[1]||position.z*units!=values[2])
    DiagnosticAct(()=>DiagnosticData.SetPosition(design,item,new CtVector(values[0]/units,values[1]/units,values[2]/units),true));
   if(Btn("Delete this point")){Delete();GUI.changed=false;GUIUtility.ExitGUI();}
   GUI.enabled=enabled;
 }
}
}
