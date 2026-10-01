using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 PlanColorMask generationMaskJob;
 PlanPathExtraction generationPathJob;
 PlanPathResult generationResult;
 FloorPlanData generationSource;
 FloorPlanData generationJobSource;
 int generationJobEpoch,generationPendingMatched,generationPendingComponents,generationPendingOverlaps;
 double generationPendingMaskMilliseconds;
 string generationJobSignature="",generationJobTargetBatchId="";
 List<GeneratedWallPath> generationPreviewPaths;
 WallConnectionPlan generationConnectionPreview;
 WallGenerationDiff generationDiffPreview;
 Design generationStagedDesign;
 WallGenerationOptions generationOptions=new WallGenerationOptions{version=1};
 string generationBatchId="",generationTargetBatchId="",generationStagedBatchId="",generationStagedBeforeJson="",generationStagedOptionsJson="";
 bool generationNewBatch;
 string generationOptionsBatchKey="";
 int generationOptionsEpoch=-1;
 string generationDiffCandidateJson="";
 bool generationPreviewGeometryValid=true;
 int generationEpoch,generationRejected;
 int generationMaskMatched,generationMaskComponents,generationMaskOverlaps,generationPathPage;
 double generationMaskMilliseconds,generationWallClockSeconds;
 readonly Dictionary<string,int> generationRuleRejected=new Dictionary<string,int>();
 float generationStartedAt;
 string generationSignature="",generationMessage="",generationSelectedPath="";
 [Serializable] sealed class PreviewPathList {public List<GeneratedWallPath> paths=new List<GeneratedWallPath>();}
 readonly List<string> previewCorrections=new List<string>();
 int previewCorrectionIndex=-1;

 void StartConnectedWallGeneration(){
  var source=floorPlan;
  if(source==null||source.kind!="Image"||PlanAuthoring.IsEmpty(source.authoring))throw new Exception("Import an image, confirm its scale and save color rules first.");
  PlanAuthoring.Validate(source.authoring,source,true);
  if(!PlanAuthoring.CalibrationCurrent(source))throw new Exception("Confirm a uniform image scale before generating walls.");
  var wallRules=source.authoring.rules.Where(rule=>rule.enabled&&rule.classification=="Wall").ToArray();
  if(wallRules.Length==0)throw new Exception("Enable at least one Wall color rule.");
  foreach(var rule in wallRules)PlanAuthoring.RequireWallRule(rule,design);
  if((long)source.pixelWidth*source.pixelHeight>PlanAuthoring.MaxProcessingPixels)throw new Exception("Wall generation supports up to 4 megapixels. Use a smaller source image.");
  if(floorPlanTexture==null)throw new Exception("The source image is not ready. Reopen the design and try again.");
  generationJobSource=FloorPlanCodec.Clone(source);
  generationJobSignature=WallGenerationData.SnapshotSignature(generationJobSource);
  generationJobEpoch=planEpoch;
  generationJobTargetBatchId=GenerationTargetBatch()?.id??"";
  generationStartedAt=Time.realtimeSinceStartup;
  generationMaskJob=new PlanColorMask(floorPlanTexture.GetPixels32(),source.pixelWidth,source.pixelHeight,source.authoring);
  generationPathJob=null;
  generationMessage="Classifying source-image colors. The design is unchanged.";
 }

 void WallGenerationUpdate(){
  if(generationMaskJob==null&&generationPathJob==null)return;
  if(generationJobEpoch!=planEpoch||dirty||floorPlan==null){
   generationMaskJob=null;generationPathJob=null;generationMessage="Generation cancelled because the design changed. Generate again to use current settings.";return;
  }
  try{
   if(generationMaskJob!=null){
    if(!generationMaskJob.Step())return;
    if(generationMaskJob.retained.Sum()==0){generationMaskJob=null;generationMessage="No retained color matches. The previous preview and design are unchanged.";return;}
    generationPendingMatched=generationMaskJob.retained.Sum();generationPendingComponents=generationMaskJob.components.Sum();generationPendingOverlaps=generationMaskJob.overlaps;generationPendingMaskMilliseconds=generationMaskJob.ElapsedMilliseconds;
    generationPathJob=new PlanPathExtraction(generationMaskJob,251);generationMaskJob=null;
   }
   if(generationPathJob!=null&&generationPathJob.Step()){
    var result=generationPathJob.Result;generationPathJob=null;
    if(result.paths.Count>250){generationMessage="More than 250 paths were detected. Increase the minimum line length or simplify the source image; the previous preview is unchanged.";return;}
    var candidates=CreateGeneratedPreviewPaths(result,generationJobSource,out int rejected);
    if(candidates.Count==0){generationMessage="No valid wall paths found. The previous preview and design are unchanged.";return;}
    generationSource=generationJobSource;generationSignature=generationJobSignature;generationEpoch=generationJobEpoch;generationTargetBatchId=generationJobTargetBatchId;
    generationMaskMatched=generationPendingMatched;generationMaskComponents=generationPendingComponents;generationMaskOverlaps=generationPendingOverlaps;generationMaskMilliseconds=generationPendingMaskMilliseconds;
    generationResult=result;generationRejected=rejected;generationPreviewPaths=candidates;generationWallClockSeconds=Time.realtimeSinceStartup-generationStartedAt;
    generationSelectedPath=candidates[0].id;generationPathPage=0;
    previewCorrections.Clear();previewCorrectionIndex=-1;RememberPreviewCorrection();
    RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();
    generationMessage="Detected "+candidates.Count+" wall paths; "+rejected+" rejected. Inspect both views, then Apply or Cancel.";
    Rebuild();
   }
  }catch(Exception e){generationMaskJob=null;generationPathJob=null;generationMessage="Generation failed: "+e.Message;}
 }

 List<GeneratedWallPath> CreateGeneratedPreviewPaths(PlanPathResult result,FloorPlanData source,out int rejected){
  var paths=WallGenerationDiffs.CreatePaths(design,source,result,out var rejections);rejected=result.rejectedSegments+rejections.Count;generationRuleRejected.Clear();
  foreach(var rejection in rejections)CountGenerationReject(rejection.ruleId,rejection.message);
  return paths;
 }
 void CountGenerationReject(string rule,string reason){string key=rule+": "+reason;if(!generationRuleRejected.ContainsKey(key))generationRuleRejected[key]=0;generationRuleRejected[key]++;}

 bool GenerationPreviewCurrent=>generationPreviewPaths!=null&&generationEpoch==planEpoch&&!dirty&&floorPlan!=null;

 WallGenerationBatch GenerationTargetBatch(){
  if(!string.IsNullOrEmpty(generationBatchId))return WallGenerationData.FindBatch(design,generationBatchId)??throw new Exception("The selected generation batch no longer exists.");
  if(generationNewBatch||floorPlan==null||PlanAuthoring.IsEmpty(floorPlan.authoring))return null;
  return design.generationBatches?.FirstOrDefault(batch=>batch.sourceFingerprint==floorPlan.authoring.sourceFingerprint);
 }
 void SelectGenerationBatch(WallGenerationBatch batch){
  ClearGenerationPreview();generationBatchId=batch?.id??"";generationNewBatch=batch==null;
  generationOptions=WallGenerationOptions.Read(batch?.connectionOptions);
  generationOptionsBatchKey=batch?.id??"";generationOptionsEpoch=planEpoch;
 }
 void GenerationSettingsUI(){
  bool previousChanged=GUI.changed;GUI.changed=false;
  GUILayout.Label("Generation batch",body);
  WallGenerationBatch selectedBatch=null;
  try{selectedBatch=GenerationTargetBatch();}catch{generationBatchId="";}
  if(selectedBatch!=null&&(generationOptionsBatchKey!=selectedBatch.id||generationOptionsEpoch!=planEpoch)){
   generationOptions=WallGenerationOptions.Read(selectedBatch.connectionOptions);generationOptionsBatchKey=selectedBatch.id;generationOptionsEpoch=planEpoch;
  }
  if(Btn("New batch",selectedBatch==null))SelectGenerationBatch(null);
  if(design.generationBatches!=null)for(int index=0;index<design.generationBatches.Count;index++){
   var batch=design.generationBatches[index];
   if(Btn("Batch "+(index+1).ToString("D3")+" · "+batch.paths.Count(path=>!path.deletedOverride)+" walls",selectedBatch?.id==batch.id))SelectGenerationBatch(batch);
  }
  var before=JsonUtility.ToJson(generationOptions);
  generationOptions.autoConnect=GUILayout.Toggle(generationOptions.autoConnect," Auto-connect walls");
  generationOptions.tolerance=Number("Connection tolerance",generationOptions.tolerance,.000001f,1,"m",-1);
  generationOptions.allowCrossCategory=GUILayout.Toggle(generationOptions.allowCrossCategory," Permit different categories");
  if(before!=JsonUtility.ToJson(generationOptions)){ClearGenerationPreview();generationMessage="Connection settings changed. Generate a fresh preview.";}
  GUI.changed=previousChanged;
 }

 void RememberPreviewCorrection(){
  if(generationPreviewPaths==null)return;
  string json=JsonUtility.ToJson(new PreviewPathList{paths=generationPreviewPaths});
  if(previewCorrectionIndex>=0&&previewCorrections[previewCorrectionIndex]==json)return;
  if(previewCorrectionIndex<previewCorrections.Count-1)previewCorrections.RemoveRange(previewCorrectionIndex+1,previewCorrections.Count-previewCorrectionIndex-1);
  previewCorrections.Add(json);if(previewCorrections.Count>50)previewCorrections.RemoveAt(0);
  previewCorrectionIndex=previewCorrections.Count-1;
 }
 void UndoPreviewCorrection(int direction){
  int next=previewCorrectionIndex+direction;if(next<0||next>=previewCorrections.Count)return;
  previewCorrectionIndex=next;generationPreviewPaths=JsonUtility.FromJson<PreviewPathList>(previewCorrections[next]).paths;
  if(!generationPreviewPaths.Any(p=>p.id==generationSelectedPath))generationSelectedPath=generationPreviewPaths.FirstOrDefault()?.id??"";
  RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();
  Rebuild();
 }

 void GenerationAction(Action action){try{action();}catch(Exception e){generationMessage=e.Message;}}
 void GenerationUI(){
  Section("Connected wall generation");
    GenerationSettingsUI();
  GUILayout.Label("Confirm calibration and save explicit wall color rules before generating. Only Apply creates shielding walls.",small);
  if(generationMaskJob!=null||generationPathJob!=null){
   string phase=generationMaskJob!=null?generationMaskJob.Phase:generationPathJob.Phase;
   float progress=generationMaskJob!=null?generationMaskJob.Progress:generationPathJob.Progress;
   GUILayout.Label(phase+" · "+(progress*100).ToString("F0")+"%",body);
   if(Btn("Cancel generation")){generationMaskJob=null;generationPathJob=null;generationMessage="Processing cancelled. The design and prior preview are unchanged.";}
  }else if(Btn("Generate connected walls"))GenerationAction(StartConnectedWallGeneration);
  if(!string.IsNullOrEmpty(generationMessage))GUILayout.Label(generationMessage,small);
  if(generationPreviewPaths==null)return;
  if(!GenerationPreviewCurrent){GUILayout.Label("Preview is stale after a design or guide change. Generate again before applying.",body);if(Btn("Clear stale preview")){ClearGenerationPreview();GUIUtility.ExitGUI();}return;}
  GUILayout.Label(generationMaskMatched+" matched pixels · "+generationMaskComponents+" components · "+generationMaskOverlaps+" overlap matches",small);
  GUILayout.Label((generationResult?.ExtractedSegments??0)+" extracted · "+generationPreviewPaths.Count(p=>!p.deletedOverride)+" proposed walls · "+generationRejected+" rejected · "+(generationResult?.unresolvedJunctions??0)+" unresolved junctions",small);
  GUILayout.Label("Processing "+(generationMaskMilliseconds+(generationResult?.elapsedMilliseconds??0)).ToString("F1")+" ms CPU · "+generationWallClockSeconds.ToString("F2")+" s elapsed",small);
  foreach(var rejectedRule in generationRuleRejected.Take(8))GUILayout.Label(rejectedRule.Key+" × "+rejectedRule.Value,small);
  if(generationResult!=null)foreach(var issue in generationResult.issues.Take(6))GUILayout.Label(issue.message,small);
  if(generationConnectionPreview!=null){GUILayout.Label(generationConnectionPreview.junctions.Count+" proposed physical joins",small);foreach(var diagnostic in generationConnectionPreview.diagnostics.Take(5))GUILayout.Label(diagnostic,small);}
  if(generationDiffPreview!=null){
   GUILayout.Label("Regeneration preview: "+generationDiffPreview.Count("Add")+" add · "+generationDiffPreview.Count("Update")+" update · "+(generationDiffPreview.Count("Delete")+generationDiffPreview.Count("DeleteOverride"))+" delete · "+generationDiffPreview.Count("Conflict")+" conflicts",body);
   foreach(var change in generationDiffPreview.changes.Where(c=>c.kind=="Conflict").Take(8))GUILayout.Label(change.pathId+": "+change.reason,small);
  }
  GUILayout.BeginHorizontal();if(Btn("Inspect 2D paths",top))SetCameraView(true);if(Btn("Inspect 3D walls",!top))SetCameraView(false);GUILayout.EndHorizontal();
  GUILayout.BeginHorizontal();if(Btn("Undo preview edit"))UndoPreviewCorrection(-1);if(Btn("Redo preview edit"))UndoPreviewCorrection(1);GUILayout.EndHorizontal();
  GUILayout.Label("Preview paths (select to correct)",body);
  var activePaths=generationPreviewPaths.Where(p=>!p.deletedOverride).ToList();const int pageSize=24;
  int pages=Mathf.Max(1,Mathf.CeilToInt(activePaths.Count/(float)pageSize));generationPathPage=Mathf.Clamp(generationPathPage,0,pages-1);
  GUILayout.BeginHorizontal();if(Btn("Previous paths"))generationPathPage=Mathf.Max(0,generationPathPage-1);GUILayout.Label("Page "+(generationPathPage+1)+" / "+pages,small);if(Btn("Next paths"))generationPathPage=Mathf.Min(pages-1,generationPathPage+1);GUILayout.EndHorizontal();
  foreach(var path in activePaths.Skip(generationPathPage*pageSize).Take(pageSize)){
   string label=path.categoryName+" · "+F(Vector3.Distance(path.aWorld,path.bWorld))+" m";
   if(Btn(label,path.id==generationSelectedPath)){generationSelectedPath=path.id;Rebuild();}
  }
  var selectedPath=generationPreviewPaths.FirstOrDefault(p=>p.id==generationSelectedPath);
  if(selectedPath!=null&&!selectedPath.deletedOverride)GenerationPathCorrectionUI(selectedPath);
  GUILayout.BeginHorizontal();if(Btn("Add path"))GenerationAction(AddPreviewPath);if(Btn("Split selected"))GenerationAction(()=>SplitPreviewPath(selectedPath));GUILayout.EndHorizontal();
  GUILayout.BeginHorizontal();if(Btn("Merge with next"))GenerationAction(()=>MergePreviewPath(selectedPath));if(Btn("Delete path"))GenerationAction(()=>DeletePreviewPath(selectedPath));GUILayout.EndHorizontal();
  GUILayout.BeginHorizontal();if(Btn("Apply connected walls"))GenerationAction(ApplyConnectedWallGeneration);if(Btn("Cancel / clear preview")){ClearGenerationPreview();GUIUtility.ExitGUI();}GUILayout.EndHorizontal();
 }

 void GenerationPathCorrectionUI(GeneratedWallPath path){
  GUILayout.Label("Correct source-pixel endpoints; preview zoom does not change dimensions.",small);
  float ax=Number("A pixel X",path.aPixel.x,-10000,10000,"px",-1),ay=Number("A pixel Y",path.aPixel.y,-10000,10000,"px",-1);
  float bx=Number("B pixel X",path.bPixel.x,-10000,10000,"px",-1),by=Number("B pixel Y",path.bPixel.y,-10000,10000,"px",-1);
  if(ax!=path.aPixel.x||ay!=path.aPixel.y||bx!=path.bPixel.x||by!=path.bPixel.y)GenerationAction(()=>UpdatePreviewEndpoints(path,new Vector2Data(ax,ay),new Vector2Data(bx,by)));
  GUILayout.BeginHorizontal();
  if(Btn("Extend +"+F(Precision.resizeStep)+" m"))GenerationAction(()=>ExtendPreviewPath(path,Precision.resizeStep));
  if(Btn("Shorten −"+F(Precision.resizeStep)+" m"))GenerationAction(()=>ExtendPreviewPath(path,-Precision.resizeStep));
  GUILayout.EndHorizontal();
  GUILayout.BeginHorizontal();if(Btn("Rotate −1°"))GenerationAction(()=>RotatePreviewPath(path,-1));if(Btn("Rotate +1°"))GenerationAction(()=>RotatePreviewPath(path,1));GUILayout.EndHorizontal();
  GUILayout.Label("Category: "+path.categoryName+" · "+path.material+" · "+F(path.thicknessMm)+" mm",small);
  foreach(var rule in generationSource.authoring.rules.Where(r=>r.enabled&&r.classification=="Wall"))if(Btn(rule.name,path.ruleId==rule.id))GenerationAction(()=>ChangePreviewRule(path,rule));
 }

 void UpdatePreviewEndpoints(GeneratedWallPath path,Vector2Data a,Vector2Data b){
  var aw=PlanAuthoring.PixelToWorld(generationSource,new Vector2(a.x,a.y),path.baseElevation);
  var bw=PlanAuthoring.PixelToWorld(generationSource,new Vector2(b.x,b.y),path.baseElevation);
  float length=Vector3.Distance(aw,bw);
  if(length<.25f||length>60f)throw new Exception("A corrected wall must be 0.25 to 60 metres long.");
  float clearance=path.thicknessMm/1000f/generationSource.authoring.calibration.metresPerPixel/2;
  if(clearance>64||generationResult.CrossesOpening(a,b,clearance))throw new Exception("The corrected wall overlaps an opening marker or exceeds the opening-check limit.");
  path.aPixel=a;path.bPixel=b;path.aWorld=aw;path.bWorld=bw;path.manuallyEdited=true;RememberPreviewCorrection();RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();Rebuild();
 }
 void ExtendPreviewPath(GeneratedWallPath path,float metres){
  var a=new Vector2(path.aPixel.x,path.aPixel.y);var b=new Vector2(path.bPixel.x,path.bPixel.y);
  var direction=(b-a).normalized;if(direction.sqrMagnitude<.5f)throw new Exception("The selected path has no usable direction.");
  var moved=b+direction*(metres/generationSource.authoring.calibration.metresPerPixel);
  UpdatePreviewEndpoints(path,path.aPixel,new Vector2Data(moved.x,moved.y));
 }
 void RotatePreviewPath(GeneratedWallPath path,float degrees){
  var a=new Vector2(path.aPixel.x,path.aPixel.y);var b=new Vector2(path.bPixel.x,path.bPixel.y);var middle=(a+b)*.5f;
  var rotation=Quaternion.Euler(0,0,degrees);
  var ra=middle+(Vector2)(rotation*(Vector3)(a-middle));var rb=middle+(Vector2)(rotation*(Vector3)(b-middle));
  UpdatePreviewEndpoints(path,new Vector2Data(ra.x,ra.y),new Vector2Data(rb.x,rb.y));
 }
 void ChangePreviewRule(GeneratedWallPath path,PlanColorRule rule){
  PlanAuthoring.RequireWallRule(rule,design);
  float clearance=rule.thicknessMm/1000f/generationSource.authoring.calibration.metresPerPixel/2;
  if(clearance>64||generationResult.CrossesOpening(path.aPixel,path.bPixel,clearance))throw new Exception("The new wall category overlaps an opening marker or exceeds the opening-check limit.");
  path.ruleId=rule.id;path.categoryName=rule.name;path.displayColor=rule.display;path.material=rule.material;path.height=rule.height;
  path.baseElevation=rule.baseElevation;path.thicknessMm=rule.thicknessMm;path.densityKgM3=rule.densityKgM3;
  path.aWorld=PlanAuthoring.PixelToWorld(generationSource,new Vector2(path.aPixel.x,path.aPixel.y),rule.baseElevation);
  path.bWorld=PlanAuthoring.PixelToWorld(generationSource,new Vector2(path.bPixel.x,path.bPixel.y),rule.baseElevation);
  path.manuallyEdited=true;RememberPreviewCorrection();RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();Rebuild();
 }
 void AddPreviewPath(){
  var rule=generationSource.authoring.rules.First(r=>r.enabled&&r.classification=="Wall");
  float x=generationSource.pixelWidth/2f,y=generationSource.pixelHeight/2f;
  float halfSpan=Mathf.Max(25,.25f/generationSource.authoring.calibration.metresPerPixel);
  if(halfSpan>x-.5f||halfSpan>generationSource.pixelWidth-x-.5f)throw new Exception("This source is too narrow to add a valid wall at its centre.");
  var a=new Vector2Data(x-halfSpan,y);var b=new Vector2Data(x+halfSpan,y);
  float clearance=rule.thicknessMm/1000f/generationSource.authoring.calibration.metresPerPixel/2;
  if(clearance>64||generationResult.CrossesOpening(a,b,clearance))throw new Exception("The added wall would overlap a confirmed opening marker.");
  var path=new GeneratedWallPath{id=Guid.NewGuid().ToString("N"),ruleId=rule.id,categoryName=rule.name,displayColor=rule.display,
   material=rule.material,height=rule.height,baseElevation=rule.baseElevation,thicknessMm=rule.thicknessMm,densityKgM3=rule.densityKgM3,
   sourceAPixel=new Vector2Data(a.x,a.y),sourceBPixel=new Vector2Data(b.x,b.y),aPixel=a,bPixel=b,manuallyEdited=true};
  path.aWorld=PlanAuthoring.PixelToWorld(generationSource,new Vector2(a.x,a.y),path.baseElevation);
  path.bWorld=PlanAuthoring.PixelToWorld(generationSource,new Vector2(b.x,b.y),path.baseElevation);
  generationPreviewPaths.Add(path);generationSelectedPath=path.id;RememberPreviewCorrection();RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();Rebuild();
 }
 void SplitPreviewPath(GeneratedWallPath path){
  if(path==null)throw new Exception("Select a path to split.");
  if(Vector3.Distance(path.aWorld,path.bWorld)<.5f)throw new Exception("Splitting would create a wall shorter than 0.25 m.");
  var middle=new Vector2Data((path.aPixel.x+path.bPixel.x)/2,(path.aPixel.y+path.bPixel.y)/2);
  var second=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(path));second.id=Guid.NewGuid().ToString("N");
  second.sourceAPixel=new Vector2Data(middle.x,middle.y);second.aPixel=new Vector2Data(middle.x,middle.y);
  path.sourceBPixel=new Vector2Data(middle.x,middle.y);path.bPixel=new Vector2Data(middle.x,middle.y);
  path.bWorld=PlanAuthoring.PixelToWorld(generationSource,new Vector2(middle.x,middle.y),path.baseElevation);
  second.aWorld=path.bWorld;path.manuallyEdited=second.manuallyEdited=true;second.itemId="";
  generationPreviewPaths.Add(second);RememberPreviewCorrection();RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();Rebuild();
 }
 void MergePreviewPath(GeneratedWallPath path){
  if(path==null)throw new Exception("Select a path to merge.");
  int index=generationPreviewPaths.IndexOf(path);var next=generationPreviewPaths.Skip(index+1).FirstOrDefault(p=>!p.deletedOverride&&p.ruleId==path.ruleId);
  if(next==null||Vector2.Distance(new Vector2(path.bPixel.x,path.bPixel.y),new Vector2(next.aPixel.x,next.aPixel.y))>2)throw new Exception("The next same-category path must meet this endpoint within 2 source pixels.");
  var a=new Vector2(path.aPixel.x,path.aPixel.y);var b=new Vector2(path.bPixel.x,path.bPixel.y);var c=new Vector2(next.bPixel.x,next.bPixel.y);
  if(Mathf.Abs(Vector2.SignedAngle(b-a,c-b))>5)throw new Exception("Only collinear paths can be merged.");
  if(Vector3.Distance(path.aWorld,next.bWorld)>60)throw new Exception("The merged wall exceeds 60 metres.");
  float clearance=path.thicknessMm/1000f/generationSource.authoring.calibration.metresPerPixel/2;
  if(clearance>64||generationResult.CrossesOpening(path.aPixel,next.bPixel,clearance))throw new Exception("The merged wall would cross an opening marker.");
  path.bPixel=new Vector2Data(next.bPixel.x,next.bPixel.y);path.sourceBPixel=new Vector2Data(next.sourceBPixel.x,next.sourceBPixel.y);path.bWorld=next.bWorld;path.manuallyEdited=true;
  next.deletedOverride=true;next.itemId="";RememberPreviewCorrection();RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();Rebuild();
 }
 void DeletePreviewPath(GeneratedWallPath path){
  if(path==null)throw new Exception("Select a path to delete.");
  path.deletedOverride=true;path.itemId="";generationSelectedPath=generationPreviewPaths.FirstOrDefault(p=>!p.deletedOverride)?.id??"";
  RememberPreviewCorrection();RefreshGenerationConnectionPreview();RefreshGenerationDiffPreview();Rebuild();
 }

 Item PreviewWall(GeneratedWallPath path){
  Vector3 delta=path.bWorld-path.aWorld;float length=new Vector2(delta.x,delta.z).magnitude;
  var wall=Design.Wall(path.categoryName,(path.aWorld.x+path.bWorld.x)/2,(path.aWorld.z+path.bWorld.z)/2,length,-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,path.height);
  wall.id=path.id;wall.y=path.baseElevation;wall.shielding=new Barrier{material=path.material,thickness=path.thicknessMm,density=path.densityKgM3};
  wall.generated=new GeneratedWallRef{pathId=path.id,ruleId=path.ruleId,categoryName=path.categoryName};return wall;
 }
 Func<Vector2,Vector2,bool> GenerationOpeningGuard(){
  float maximumThickness=generationPreviewPaths.Where(p=>!p.deletedOverride).Select(p=>p.thicknessMm).DefaultIfEmpty(0).Max();
  float clearance=maximumThickness/1000f/generationSource.authoring.calibration.metresPerPixel/2;
  if(clearance>64)throw new Exception("Wall thickness exceeds the source-pixel opening-check limit.");
  return (a,b)=>{
   var ap=PlanAuthoring.WorldToPixel(generationSource,new Vector3(a.x,0,a.y));
   var bp=PlanAuthoring.WorldToPixel(generationSource,new Vector3(b.x,0,b.y));
   return generationResult.CrossesOpening(new Vector2Data(ap.x,ap.y),new Vector2Data(bp.x,bp.y),clearance);
  };
 }
 void RefreshGenerationConnectionPreview(){
  generationConnectionPreview=null;
  if(generationPreviewPaths==null||generationSource==null||generationResult==null)return;
  var walls=generationPreviewPaths.Where(p=>!p.deletedOverride).Select(PreviewWall).ToList();
    if(!generationOptions.autoConnect||walls.Count<2||walls.Count>250)return;
    try{generationConnectionPreview=WallConnections.PreviewWalls(walls,generationOptions.tolerance,generationOptions.allowCrossCategory,GenerationOpeningGuard());}
  catch(Exception e){generationMessage="Connection preview: "+e.Message;}
 }
 void RefreshGenerationDiffPreview(){
    generationDiffPreview=null;generationDiffCandidateJson="";generationStagedDesign=null;generationStagedBatchId="";
  if(generationPreviewPaths==null||generationSource==null)return;
    var batch=string.IsNullOrEmpty(generationTargetBatchId)?null:WallGenerationData.FindBatch(design,generationTargetBatchId);
  try{
     Design staged;
     if(batch==null){
        if(!string.IsNullOrEmpty(generationTargetBatchId))throw new Exception("The selected generation batch no longer exists.");
        staged=WallGenerationDiffs.ApplyNewBatch(design,generationSource,generationPreviewPaths);generationStagedBatchId=staged.generationBatches.Last().id;
     }else{
        generationDiffPreview=WallGenerationDiffs.PreviewRegeneration(design,batch.id,generationSource,generationPreviewPaths);
        if(generationDiffPreview.HasConflicts)return;
        staged=WallGenerationDiffs.ApplyRegeneration(design,generationDiffPreview);generationStagedBatchId=batch.id;
     }
     var stagedBatch=WallGenerationData.FindBatch(staged,generationStagedBatchId);
     stagedBatch.connectionOptions=WallGenerationOptions.Read(generationOptions);
     var ids=stagedBatch.paths.Where(path=>!path.deletedOverride).Select(path=>path.itemId).ToArray();
     generationConnectionPreview=null;
     if(generationOptions.autoConnect&&ids.Length>1){
        generationConnectionPreview=WallConnections.Preview(staged,ids,generationOptions.tolerance,generationOptions.allowCrossCategory,GenerationOpeningGuard());
        if(generationConnectionPreview.HasChanges)WallConnections.Apply(staged,generationConnectionPreview,false);
     }
     Design.Validate(staged);generationStagedDesign=staged;generationStagedBeforeJson=JsonUtility.ToJson(design);generationStagedOptionsJson=JsonUtility.ToJson(generationOptions);
   generationDiffCandidateJson=JsonUtility.ToJson(new PreviewPathList{paths=generationPreviewPaths});
  }catch(Exception e){generationConnectionPreview=null;generationMessage="Generation preview: "+e.Message;}
 }
 void ApplyConnectedWallGeneration(){
  if(generationMaskJob!=null||generationPathJob!=null)throw new Exception("Wait for generation to finish or cancel processing before applying a preview.");
  if(!GenerationPreviewCurrent||generationSource==null||generationResult==null)throw new Exception("Generate a current wall preview before applying.");
  if(!generationPreviewGeometryValid)throw new Exception("The physical join preview could not be built. Resolve its diagnostic before applying.");
  if(generationSignature!=WallGenerationData.SnapshotSignature(floorPlan))throw new Exception("The image, calibration or color rules changed. Generate again before applying.");
  if(generationResult.HasBlockingIssues)throw new Exception("Resolve the blocking path/junction diagnostics before applying walls.");
  var candidates=generationPreviewPaths.ToArray();int active=candidates.Count(path=>!path.deletedOverride);
    var oldBatch=string.IsNullOrEmpty(generationTargetBatchId)?null:WallGenerationData.FindBatch(design,generationTargetBatchId);
  if(active==0&&oldBatch==null)throw new Exception("The preview has no wall paths to apply.");
  if(active>250)throw new Exception("Generation would exceed the 250-wall processing limit. Raise minimum path length or simplify the source image.");
    if(generationDiffPreview?.HasConflicts==true)throw new Exception("Regeneration has "+generationDiffPreview.Count("Conflict")+" conflicts. Manual and protected walls were left unchanged; review them before applying.");
    if(generationStagedDesign==null||generationStagedBeforeJson!=JsonUtility.ToJson(design)||generationStagedOptionsJson!=JsonUtility.ToJson(generationOptions)||generationDiffCandidateJson!=JsonUtility.ToJson(new PreviewPathList{paths=generationPreviewPaths}))throw new Exception("Review a current valid generation preview before applying.");
    var next=JsonUtility.FromJson<Design>(JsonUtility.ToJson(generationStagedDesign));Design.Validate(next);
    if(JsonUtility.ToJson(next)==JsonUtility.ToJson(design)){generationMessage="Regeneration found no changes or duplicate walls.";return;}
  design=next;qa=null;ClearSelection();generationPreviewPaths=null;generationResult=null;generationSource=null;generationJobSource=null;
    generationBatchId=generationStagedBatchId;generationNewBatch=false;
    generationMaskJob=null;generationPathJob=null;generationConnectionPreview=null;generationDiffPreview=null;generationStagedDesign=null;generationDiffCandidateJson="";previewCorrections.Clear();previewCorrectionIndex=-1;
  Commit();Rebuild();status=generationMessage=oldBatch==null?"Connected walls applied in one undo step.":"Generation batch updated in one undo step.";
 }

 void ClearGenerationPreview(){
  generationMaskJob=null;generationPathJob=null;generationResult=null;generationSource=null;generationPreviewPaths=null;
  generationJobSource=null;generationJobSignature="";generationJobTargetBatchId="";
    generationStagedDesign=null;generationStagedBatchId="";generationStagedBeforeJson="";generationStagedOptionsJson="";
  generationSignature="";generationSelectedPath="";generationRejected=0;generationDiffPreview=null;generationDiffCandidateJson="";generationConnectionPreview=null;generationMessage="Preview cleared. No walls changed.";
  Rebuild();
 }

 void GenerationAfterRebuild(){
  if(world==null||generationPreviewPaths==null||generationEpoch!=planEpoch)return;
  generationPreviewGeometryValid=true;
  var root=new GameObject("Connected-wall preview (not committed)").transform;root.SetParent(world,false);
  var materials=new Dictionary<string,Material>();
  var joined=new HashSet<string>();
  var stagedBatch=WallGenerationData.FindBatch(generationStagedDesign,generationStagedBatchId);
  var paths=stagedBatch?.paths??generationPreviewPaths;
  var walls=stagedBatch==null?paths.Where(path=>!path.deletedOverride).Select(PreviewWall).ToList():
   stagedBatch.paths.Where(path=>!path.deletedOverride).Select(path=>generationStagedDesign.items.First(wall=>wall.id==path.itemId)).ToList();
  var topology=generationStagedDesign;
  if(topology==null){
   var proposed=walls.ToDictionary(wall=>wall.id);
   if(generationConnectionPreview!=null){
   foreach(var wall in generationConnectionPreview.replacements)proposed[wall.id]=wall;
   foreach(var wall in generationConnectionPreview.additions)proposed[wall.id]=wall;
   }
   topology=new Design{items=proposed.Values.ToList(),wallJunctions=generationConnectionPreview?.junctions??new List<WallJunction>()};walls=proposed.Values.ToList();
  }
  var wallIds=new HashSet<string>(walls.Select(wall=>wall.id));
  var junctions=topology.wallJunctions?.Where(junction=>junction.arms.Any(arm=>wallIds.Contains(arm.wallId))).ToList()??new List<WallJunction>();
  Func<Item,GeneratedWallPath> pathFor=wall=>paths.FirstOrDefault(path=>path.id==(wall.generated?.pathId??wall.id));
  Func<Item,Material> materialFor=wall=>{
   var path=pathFor(wall);if(path==null)return wallMat;if(path.id==generationSelectedPath)return selectedMat;
   if(!materials.TryGetValue(path.ruleId,out var material)){material=Material(path.displayColor);materials[path.ruleId]=material;appearanceMaterials.Add(material);}
   return material;
  };
  if(junctions.Count>0){
   foreach(var junction in junctions)foreach(var arm in junction.arms){
    if(joined.Contains(arm.wallId))continue;
    var cluster=WallConnections.ConnectedCluster(topology,arm.wallId);
    if(cluster.Count<2)continue;
    try{
     var visible=cluster.Select(i=>JsonUtility.FromJson<Item>(JsonUtility.ToJson(i))).ToList();
     if(cutaway)foreach(var wall in visible)wall.height=Mathf.Min(.35f,wall.height);
    var mesh=WallConnections.BuildJoinedMesh(visible,true);componentMeshes.Add(mesh);
     var body=new GameObject("Joined physical wall preview");body.transform.SetParent(root,false);
     body.AddComponent<MeshFilter>().sharedMesh=mesh;
    body.AddComponent<MeshRenderer>().sharedMaterials=visible.Select(materialFor).ToArray();
     foreach(var member in cluster)joined.Add(member.id);
    }catch(Exception e){generationPreviewGeometryValid=false;generationMessage="Physical join preview failed: "+e.Message;}
   }
  }
  foreach(var wall in walls){
   var path=pathFor(wall);if(path==null)continue;
  var start=PrecisionEditing.Endpoint(wall,false);var end=PrecisionEditing.Endpoint(wall,true);
  Vector3 delta=end-start;float length=delta.magnitude;
   if(length<.001f)continue;
   float shown=cutaway?Mathf.Min(.35f,wall.height):wall.height;
   if(!joined.Contains(wall.id)){
    var center=(start+end)*.5f;
    var preview=Cube("Preview · "+path.categoryName,new Vector3(center.x,wall.y+shown/2,center.z),
    new Vector3(length,shown,wall.shielding.thickness/1000f),materialFor(wall),root);
    preview.transform.rotation=Quaternion.Euler(0,-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,0);
    Destroy(preview.GetComponent<Collider>());
   }
   var line=Line("Source centerline",path.displayColor,.035f,root);
  line.SetPosition(0,start+Vector3.up*(shown+.04f));line.SetPosition(1,end+Vector3.up*(shown+.04f));
  }
  foreach(var junction in junctions){
   var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Valid proposed junction";marker.transform.SetParent(root,false);
   marker.transform.position=new Vector3(junction.x,.48f,junction.z);marker.transform.localScale=Vector3.one*.12f;
   marker.GetComponent<Renderer>().sharedMaterial=selectedMat;Destroy(marker.GetComponent<Collider>());
  }
  if(generationResult!=null)foreach(var issue in generationResult.issues.Where(i=>i.blocking)){
   var component=generationResult.components.FirstOrDefault(c=>c.id==issue.sourceComponent);
   if(component==null)continue;
   var location=PlanAuthoring.PixelToWorld(generationSource,new Vector2(component.centroidPixel.x,component.centroidPixel.y));
   var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Unresolved source junction";marker.transform.SetParent(root,false);
   marker.transform.position=location+Vector3.up*.5f;marker.transform.localScale=Vector3.one*.16f;
   var red=Material(new Color(.9f,.12f,.12f));appearanceMaterials.Add(red);marker.GetComponent<Renderer>().sharedMaterial=red;Destroy(marker.GetComponent<Collider>());
  }
 }
}
}
