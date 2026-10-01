using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 CtResultSnapshot ctLatest;
 bool ctResultsPopup;
 string ctCompareFirst="",ctCompareSecond="",ctImportFingerprint="";
#if !UNITY_WEBGL || UNITY_EDITOR
 string ctImportPath="";
#endif
 Task<string> ctImportTask;
 Vector2 ctResultsScroll;
 sealed class CtMeasurement {public string sourceId,roiId;public LineRenderer line;}
 readonly List<CtMeasurement> ctMeasurements=new List<CtMeasurement>();
 readonly Dictionary<string,bool> ctInputDrafts=new Dictionary<string,bool>();
 Material ctAnnotationMaterial;
 bool CtDiagnosticsEnabled=>Application.absoluteURL.Contains("ct-tests=1")||Application.absoluteURL.Contains("ct-demo=1")||Application.absoluteURL.Contains("diagnostic-tests=1");
 [Serializable] sealed class CtControlBounds {public string label,kind;public float x,y,width,height;public bool enabled;}
 [Serializable] sealed class CtPointDiagnostic {public string id,role;public CtVector position;}
 [Serializable] sealed class CtBrowserState {public string status,tab,tool,fileName;public int savedResults,sourceCount;public bool modal;public List<CtControlBounds> controls;public List<CtPointDiagnostic> points;public List<CtRoiResult> latestRows;public DiagnosticProject diagnostic;public DiagnosticReadiness diagnosticReadiness;}
 readonly List<CtControlBounds> ctControlBounds=new List<CtControlBounds>();
 float ctDiagnosticsNext;
 void CaptureCtControl(string label,string kind){
  if(!CtDiagnosticsEnabled||Event.current.type!=EventType.Repaint)return;
  var rectangle=GUILayoutUtility.GetLastRect();var position=GUIUtility.GUIToScreenPoint(rectangle.position);
  ctControlBounds.Add(new CtControlBounds{label=label,kind=kind,x=position.x,y=position.y,width=rectangle.width*uiScale,height=rectangle.height*uiScale,enabled=GUI.enabled});
 }
 void PublishCtDiagnostics(){
  if(!CtDiagnosticsEnabled||Event.current.type!=EventType.Repaint||Time.unscaledTime<ctDiagnosticsNext)return;ctDiagnosticsNext=Time.unscaledTime+.25f;
  BrowserBridge.CtDiagnostics(JsonUtility.ToJson(new CtBrowserState{status=status,tab=tab,tool=tool,fileName=fileName,savedResults=design.ct?.results?.Count??0,sourceCount=design.ct?.sources?.Count??0,modal=showQa,
   controls=ctControlBounds,points=design.items.Where(item=>CtShieldingData.IsPoint(item)||DiagnosticData.IsPoint(item)).Select(item=>new CtPointDiagnostic{id=item.id,role=DiagnosticData.IsPoint(item)?item.diagnosticPoint.role.ToString():item.ctPoint.role,position=CtShieldingData.Position(design,item)}).ToList(),latestRows=ctLatest?.rows,diagnostic=design.diagnosticCalculation,diagnosticReadiness=tab=="Calculation"?diagnosticReadinessView:null}));
 }
 CtSourceData CtSelectedSource=>design.ct?.sources?.FirstOrDefault(source=>source.id==design.ct.selectedSourceId)??design.ct?.sources?.FirstOrDefault();
 Item CtSelectedRoi=>design.items.Find(item=>CtShieldingData.IsRoi(item)&&item.id==design.ct?.selectedRoiId);
 string CtMeasurementRoiId=>CtShieldingData.IsRoi(Current)?Current.id:CtSelectedRoi?.id??ctMeasurements.FirstOrDefault()?.roiId;
 static string CtValue(double value)=>value.ToString("G8",CultureInfo.InvariantCulture);
 static string CtFieldValue(bool present,double value,bool underflow=false)=>!present?"Not available":underflow&&value==0?"Below numeric display range":CtValue(value);
 string CtText(string key,string value,int limit=100,bool multiline=false){
  GUI.SetNextControlName("edit:ct:"+key);
  string result=multiline?GUILayout.TextArea(value??"",limit,textAreaField,GUILayout.MinHeight(52),GUILayout.MaxHeight(100)):GUILayout.TextField(value??"",limit,field);CaptureCtControl(key,"input");return result;
 }
 double CtNumber(string scope,string label,double value,double minimum,double maximum,string unit=""){
  bool before=GUI.changed;string key="edit:ct:"+scope+":"+label;
  GUILayout.Label(label+(unit.Length>0?" ("+unit+")":""),small);
  if(!numberBuffers.ContainsKey(key)||GUI.GetNameOfFocusedControl()!=key)numberBuffers[key]=value.ToString("R",CultureInfo.InvariantCulture);
  GUI.SetNextControlName(key);string text=GUILayout.TextField(numberBuffers[key],32,numberField);CaptureCtControl(label,"input");numberBuffers[key]=text;
  double next=value;bool valid=CtShieldMath.TryNumber(text,minimum,maximum,out double parsed);
  if(valid)next=parsed;
  float sliderMinimum=(float)minimum,sliderMaximum=(float)Math.Min(maximum,Math.Max(minimum+1,Math.Max(1,Math.Abs(value)*2)));
  float sliderStart=Mathf.Clamp((float)next,sliderMinimum,sliderMaximum),slider=GUILayout.HorizontalSlider(sliderStart,sliderMinimum,sliderMaximum);
  if(slider!=sliderStart){next=slider;numberBuffers[key]=next.ToString("R",CultureInfo.InvariantCulture);GUI.FocusControl(null);}
  GUI.changed=before||next!=value;
  if(!valid&&GUI.GetNameOfFocusedControl()==key)GUILayout.Label("Finite value: "+CtValue(minimum)+" to "+CtValue(maximum),small);
  return next;
 }
 void CtOptionalNumber(string scope,string label,ref bool supplied,ref double value,double minimum,double maximum,string unit){
  bool before=GUI.changed,previous=supplied;double original=value;string key=scope+":"+label;
  bool requested=ctInputDrafts.TryGetValue(key,out bool draft)?draft:supplied;
  requested=GUILayout.Toggle(requested," "+label+" supplied");ctInputDrafts[key]=requested;
  if(!requested)supplied=false;
  else{double edited=CtNumber(scope,label,value,minimum,maximum,unit);supplied=edited>=minimum&&edited<=maximum&&CtShieldMath.IsFinite(edited);if(supplied)value=edited;}
  GUI.changed=before||supplied!=previous||value!=original;
 }
 void CtSpectrumUI(CtSourceData source,bool atPoint=false){
  var scanner=design.items.Find(item=>item.id==source.deviceId);var scatter=design.items.Find(item=>item.id==source.scatterPointId);
  bool beforeEnabled=GUI.enabled;GUI.enabled=beforeEnabled&&scanner!=null&&!scanner.locked&&scatter!=null&&!scatter.locked;
  if(atPoint)GUILayout.Label(string.IsNullOrWhiteSpace(source.protocolId)?"CT source "+(design.ct.sources.IndexOf(source)+1):source.protocolId,small);
  GUILayout.Label("CT tube potential (kVp)",small);GUILayout.BeginHorizontal();
  foreach(int potential in new[]{120,140})if(Btn(potential+" kVp",source.kvp==potential)&&source.kvp!=potential){
   CtShieldingData.SetTubePotential(design,source,potential);design.ct.selectedSourceId=source.id;GUI.changed=true;
   status="CT protocol changed to "+potential+" kVp. Source applicability review required.";
  }
  GUILayout.EndHorizontal();
  if(source.kvp==0)GUILayout.Label("No tube potential selected. Choose 120 or 140 kVp.",small);
  else if(source.kvp!=120&&source.kvp!=140)GUILayout.Label("Unsupported CT spectrum: only 120 and 140 kVp fits are supplied.",small);
  if(atPoint){
   GUILayout.Label("CT secondary material fits",small);GUILayout.Label("alpha / beta: 1/mm; gamma: unitless",small);
   foreach(string material in new[]{"Lead","Concrete"}){
    if(!CtCoefficientLibrary.TryGet(source.beamFamily,material,source.kvp,out var coefficient,out var unsupported)){GUILayout.Label(material+" / "+unsupported,small);continue;}
    GUILayout.Label(material+" / "+coefficient.Id+" / p"+coefficient.ReportPage,small);GUILayout.BeginHorizontal();
    GUILayout.Label("alpha "+CtValue(coefficient.Fit.AlphaPerMm),small);GUILayout.Label("beta "+CtValue(coefficient.Fit.BetaPerMm),small);GUILayout.Label("gamma "+CtValue(coefficient.Fit.Gamma),small);GUILayout.EndHorizontal();
   }
   source.applicabilityReviewed=GUILayout.Toggle(source.applicabilityReviewed," Scanner/spectrum applicability reviewed");
  }
  GUI.enabled=beforeEnabled;
 }
 void OpenCtSettings(Item point=null,bool selectLinkedSource=true){
  if(dirty)Commit();
  if(point==null)point=CtShieldingData.IsRoi(Current)?Current:CtSelectedRoi;
  if(CtShieldingData.IsPoint(point)){
   var data=CtShieldingData.Ensure(design);
   if(CtShieldingData.IsRoi(point)){
    data.selectedRoiId=point.id;
    if(selectLinkedSource){
     var linked=data.sources.FirstOrDefault(source=>source.id==data.selectedSourceId&&point.ctPoint.roi.contributions.Any(contribution=>contribution.sourceId==source.id))
      ??data.sources.FirstOrDefault(source=>point.ctPoint.roi.contributions.Any(contribution=>contribution.sourceId==source.id));
     if(linked!=null)data.selectedSourceId=linked.id;
    }
   }else data.selectedSourceId=data.sources.FirstOrDefault(source=>source.scatterPointId==point.id)?.id??"";
  }
  if(point!=null&&Current?.id!=point.id)Choose(point.id);else Rebuild();
  tab="Calculation";showQa=ctResultsPopup=false;rightScroll=Vector2.zero;numberBuffers.Clear();tool="Select";wallStart=null;
  if(Layout.compact)compactPanel="Properties";
  status="Legacy data is retained. Use Calculation to place Target, Scatter and ROI and choose a matching calibrated profile.";
  OpenCalculation(point);
 }
 bool AddCtSource(Item scanner){
  try{
   bool existing=CtShieldingData.Active(design.ct)&&design.ct.sources.Any(source=>source.deviceId==scanner?.id);
   CtShieldingData.ConfigureSource(design,scanner);if(!existing)Commit();OpenCtSettings(selectLinkedSource:false);
   if(!existing){
    status="CT source created. Choose 120 or 140 kVp and supply validated source data.";
    if(design.items.Any(item=>CtShieldingData.IsRoi(item)&&item.locked))status+=" Protected evaluation points were not linked; unlock them to include this source.";
   }
   return true;
  }catch(Exception error){status="CT source configuration failed: "+error.Message;return false;}
 }
 void CtSourceSetupUI(){
  var scanners=design.items.Where(CtShieldingData.IsScanner).ToArray();
  if(scanners.Length==0){
   GUILayout.Label("A point alone has no CT source or energy. Place a CT scanner, then configure its source.",small);
   if(Btn("Place CT scanner")){SelectEquipment("CT");leftTab="Build";tab="CT";compactPanel="";GUI.changed=false;status="Click the room to place the CT scanner, then configure its source / energy.";GUIUtility.ExitGUI();}
   return;
  }
  if((design.ct?.sources.Count??0)==0)GUILayout.Label("Choose the scanner supplying this point. Configuration exposes its kVp and source-strength inputs.",small);
  foreach(var scanner in scanners.Where(item=>design.ct?.sources.Any(source=>source.deviceId==item.id)!=true)){
   bool enabled=GUI.enabled;GUI.enabled=enabled&&!scanner.locked;
   if(Btn("Configure "+ObjectDisplayName(scanner))){GUI.changed=false;bool configured=AddCtSource(scanner);GUI.enabled=enabled;if(configured)GUIUtility.ExitGUI();}
   GUI.enabled=enabled;if(scanner.locked)GUILayout.Label("Unlock "+ObjectDisplayName(scanner)+" to configure its CT source.",small);
  }
 }
 void PlaceCtRoi(Vector3 position,string role){
  try{
   if(design.items.Count(CtShieldingData.IsPoint)>=CtShieldingData.MaxPoints)throw new Exception("Maximum 64 calculation points.");
   if(design.items.Count>=250)throw new Exception("Maximum 250 objects per project.");
   var data=CtShieldingData.Ensure(design);var point=CtShieldingData.CreatePoint(role,new CtVector(position.x,position.y,position.z));
   foreach(var source in data.sources)point.ctPoint.roi.contributions.Add(new CtRoiContribution{sourceId=source.id});
   data.selectedRoiId=point.id;Add(point);OpenCtSettings(point);
   if(data.sources.Count==0)status="Point placed. Configure a CT scanner source to select energy and supply source strength.";
  }catch(Exception error){status=error.Message;}
 }
 void CtPlacementUI(){
  Section("Calculation points");
  if(Btn("Place ROI point",tool=="CT_ROI")){tool="CT_ROI";wallStart=null;}
  if(Btn("Place patient point",tool=="CT_Patient")){tool="CT_Patient";wallStart=null;}
  if(CtShieldingData.IsScanner(Current)&&Btn("Configure CT scatter point")){GUI.changed=false;if(AddCtSource(Current))GUIUtility.ExitGUI();}
 }
 void CtUI(){
  if(!CtShieldingData.Active(design.ct)){
   Section("CT source / energy");CtSourceSetupUI();CtPlacementUI();
   return;
  }
  var data=design.ct;var active=CtSelectedSource;
  Section("CT source / energy");
  if(active!=null){
   GUILayout.Label(string.IsNullOrWhiteSpace(active.protocolId)?"CT source "+(data.sources.IndexOf(active)+1):active.protocolId,small);
   CtSpectrumUI(active);
  }
  foreach(var source in data.sources){string label=string.IsNullOrWhiteSpace(source.protocolId)?"CT source "+(data.sources.IndexOf(source)+1):source.protocolId;if(Btn(label,CtSelectedSource?.id==source.id)){data.selectedSourceId=source.id;numberBuffers.Clear();GUI.changed=false;Rebuild();GUIUtility.ExitGUI();}}
  CtSourceSetupUI();
  Section("CT scenarios / mGy per week");
  data.scenarioName=CtText("scenarioName",data.scenarioName);
  GUILayout.BeginHorizontal();foreach(string mode in new[]{"Best","Nominal","Worst"})if(Btn(mode,data.selectedCase==mode)){data.selectedCase=mode;GUI.changed=false;}GUILayout.EndHorizontal();
  GUILayout.Label("Shielding credit",small);GUILayout.BeginHorizontal();
  foreach(string mode in new[]{"Current","WallsOff","AllOff"})if(Btn(mode=="WallsOff"?"Walls off":mode=="AllOff"?"All off":"Current",data.shieldingMode==mode)){data.shieldingMode=mode;GUI.changed=false;}
  GUILayout.EndHorizontal();
  CtPlacementUI();
  if(active!=null)CtSourceUI(active);
  Section("ROI / patient evaluation points");
  foreach(var point in design.items.Where(CtShieldingData.IsRoi).ToArray())if(Btn(ObjectDisplayName(point),data.selectedRoiId==point.id)){data.selectedRoiId=point.id;Choose(point.id);tab="CT";GUI.changed=false;}
  var roi=CtSelectedRoi;if(roi!=null)CtRoiUI(roi);
  Section("Results history");
  foreach(var result in data.results.AsEnumerable().Reverse()){
   string label=result.name+" / "+result.scenarioCase+" / "+result.shieldingMode;
   if(Btn(label,ctCompareFirst==result.id)){if(ctCompareFirst=="")ctCompareFirst=result.id;else if(ctCompareFirst!=result.id)ctCompareSecond=result.id;ctLatest=result;ctResultsPopup=showQa=true;GUI.changed=false;}
  }
  if(data.results.Count>0&&Btn("Export CT results JSON")){try{WriteOutput(SafeName()+"-ct-results.json",CtShieldingCalculation.ExportResults(data.results));status="Exported CT result history.";}catch(Exception error){status=error.Message;}GUI.changed=false;}
#if UNITY_WEBGL && !UNITY_EDITOR
  if(ctImportTask==null&&Btn("Import CT results JSON")){ctImportFingerprint=CtShieldingCalculation.InputFingerprint(design);ctImportTask=BrowserBridge.ChooseFile(maxJsonBytes:CtShieldingData.MaxResultFileBytes);GUI.changed=false;}
#else
  ctImportPath=PathInput("ct-result-path",ctImportPath);
  if(Btn("Import CT results JSON")){try{CtShieldingCalculation.ImportResults(design,File.ReadAllText(ctImportPath.Trim().Trim('"')));Commit();status="Imported CT result history.";}catch(Exception error){status="CT result import failed: "+error.Message;}GUI.changed=false;}
#endif
 }
 void CtSourceUI(CtSourceData source){
  var scanner=design.items.Find(item=>item.id==source.deviceId);var scatter=design.items.Find(item=>item.id==source.scatterPointId);
  bool beforeEnabled=GUI.enabled;GUI.enabled=beforeEnabled&&scanner!=null&&!scanner.locked&&scatter!=null&&!scatter.locked;
  string identityBefore=source.scannerId+"|"+source.protocolId+"|"+source.kvp+"|"+source.citation+"|"+source.validityDomain+"|"+source.includedComponents;
  GUILayout.Label("kVp selects attenuation fits, not source strength. Supply validated unshielded kerma below.",small);
  Section("Scanner / protocol");GUILayout.Label("Scanner identity",small);source.scannerId=CtText(source.id+"scanner",source.scannerId);
  GUILayout.Label("Protocol identity",small);source.protocolId=CtText(source.id+"protocol",source.protocolId);
  GUILayout.Label("Source citation",small);source.citation=CtText(source.id+"citation",source.citation,1000,true);
  GUILayout.Label("Validity domain / direction",small);source.validityDomain=CtText(source.id+"domain",source.validityDomain,1000,true);
  GUILayout.Label("Included nonoverlapping components",small);source.includedComponents=CtText(source.id+"components",source.includedComponents,1000,true);
  if(identityBefore!=source.scannerId+"|"+source.protocolId+"|"+source.kvp+"|"+source.citation+"|"+source.validityDomain+"|"+source.includedComponents)source.applicabilityReviewed=false;
  source.componentsConfirmed=GUILayout.Toggle(source.componentsConfirmed," Component coverage confirmed");
  source.includesRoomBarriers=GUILayout.Toggle(source.includesRoomBarriers," Source already includes room shielding");
  source.applicabilityReviewed=GUILayout.Toggle(source.applicabilityReviewed," Scanner/spectrum applicability reviewed");
  source.pathApproximationAccepted=GUILayout.Toggle(source.pathApproximationAccepted," Effective-source path approximation reviewed");
  Section("Source normalization");GUILayout.BeginHorizontal();
  if(Btn("ROI weekly field",source.mode=="DirectWeekly")&&source.mode!="DirectWeekly"){source.mode="DirectWeekly";source.applicabilityReviewed=false;GUI.changed=true;}
  if(Btn("Reference / exam",source.mode=="ReferenceExams")&&source.mode!="ReferenceExams"){source.mode="ReferenceExams";source.applicabilityReviewed=false;GUI.changed=true;}
  GUILayout.EndHorizontal();
  if(source.mode=="ReferenceExams"){
    source.isotropicReferenceAccepted=GUILayout.Toggle(source.isotropicReferenceAccepted," Isotropic reference model reviewed");
   CtOptionalNumber(source.id,"Reference kerma",ref source.hasReferenceKerma,ref source.referenceMgyPerExam,0,1e12,"mGy/exam");
   CtOptionalNumber(source.id,"Reference distance",ref source.hasReferenceDistance,ref source.referenceDistanceMeters,1e-8,1e8,"m");
   CtOptionalNumber(source.id,"Exam workload",ref source.hasWorkload,ref source.examsPerWeek,0,1e12,"exams/week");
   CtOptionalNumber(source.id,"Minimum distance",ref source.hasMinimumDistance,ref source.minimumDistanceMeters,1e-8,1e8,"m");
   CtOptionalNumber(source.id,"Maximum distance",ref source.hasMaximumDistance,ref source.maximumDistanceMeters,1e-8,1e8,"m");
  }
  if(scatter!=null){
   Section("Effective scatter anchor");var point=scatter.ctPoint;
   double oldX=point.localX,oldY=point.localY,oldZ=point.localZ;
   point.localX=CtNumber(scatter.id,"Local anchor X",point.localX,-10000,10000,"m");point.localY=CtNumber(scatter.id,"Local anchor Y",point.localY,-100,100,"m");point.localZ=CtNumber(scatter.id,"Local anchor Z",point.localZ,-10000,10000,"m");
   if(oldX!=point.localX||oldY!=point.localY||oldZ!=point.localZ)point.anchorConfirmed=false;
   point.anchorConfirmed=GUILayout.Toggle(point.anchorConfirmed," Provider's scatter anchor confirmed");
   if(Btn("Select scatter point")){Choose(scatter.id);GUI.changed=false;}
  }
  if(Btn("Remove this CT source")){
   try{SelectionEditing.Remove(design,new[]{source.scatterPointId});Commit();Rebuild();status="Removed CT source and scatter marker; saved results retained.";}catch(Exception error){status=error.Message;}GUI.changed=false;
  }
  GUI.enabled=beforeEnabled;
 }
 void CtRoiUI(Item point){
  bool beforeEnabled=GUI.enabled;GUI.enabled=beforeEnabled&&!point.locked;var data=point.ctPoint.roi;
  Section(ObjectDisplayName(point));GUILayout.Label("Purpose",small);data.purpose=CtText(point.id+"purpose",data.purpose,200);
  var position=CtShieldingData.Position(design,point);var editedPosition=new CtVector(CtNumber(point.id,"ROI X",position.x,-10000,10000,"m"),CtNumber(point.id,"ROI Y",position.y,-100,100,"m"),CtNumber(point.id,"ROI Z",position.z,-10000,10000,"m"));
  if(position.x!=editedPosition.x||position.y!=editedPosition.y||position.z!=editedPosition.z)CtShieldingData.SetPointPosition(design,point,editedPosition);
  foreach(var source in design.ct.sources){
   var contribution=data.contributions.Find(record=>record.sourceId==source.id);bool included=contribution!=null;
   bool include=GUILayout.Toggle(included," Include "+(source.protocolId==""?"CT source":source.protocolId));
   if(include&&!included){contribution=new CtRoiContribution{sourceId=source.id};data.contributions.Add(contribution);}
   else if(!include&&included)data.contributions.Remove(contribution);
  if(include)CtSpectrumUI(source,true);
   if(include&&source.mode=="DirectWeekly"){
    bool previous=contribution.hasDirectKerma;double value=contribution.directMgyPerWeek;
    CtOptionalNumber(point.id+source.id,"Unshielded ROI kerma",ref contribution.hasDirectKerma,ref contribution.directMgyPerWeek,0,1e12,"mGy/week");
    if(contribution.hasDirectKerma&&(!previous||value!=contribution.directMgyPerWeek))CtShieldingData.ConfirmDirectPosition(design,point,contribution);
    if(contribution.hasDirectKerma&&Btn("Confirm field at current ROI")){CtShieldingData.ConfirmDirectPosition(design,point,contribution);GUI.changed=true;}
   }
  }
  Section("Workload scenario factors");CtOptionalNumber(point.id,"Best factor",ref data.hasBestFactor,ref data.bestFactor,0,1,"x nominal");
  CtOptionalNumber(point.id,"Worst factor",ref data.hasWorstFactor,ref data.worstFactor,1,1e6,"x nominal");
  Section("Selected design criterion");GUILayout.BeginHorizontal();
  if(Btn("Occupied goal",data.goalConvention=="Occupied")){data.goalConvention="Occupied";GUI.changed=true;}
  if(Btn("Field limit",data.goalConvention=="FieldLimit")){data.goalConvention="FieldLimit";GUI.changed=true;}
  GUILayout.EndHorizontal();CtOptionalNumber(point.id,"Occupancy",ref data.hasOccupancy,ref data.occupancy,0,1,"");
  GUILayout.Label("Occupancy / exposure convention",small);data.occupancyConvention=CtText(point.id+"occupancy",data.occupancyConvention,1000,true);
  CtOptionalNumber(point.id,"Design goal",ref data.hasGoal,ref data.goalMgyPerWeek,0,1e12,"mGy/week");
  GUILayout.Label("Design-goal authority / source",small);data.goalSource=CtText(point.id+"goal",data.goalSource,1000,true);
  GUI.enabled=beforeEnabled;
 }
 void CtObjectControls(Item item){
  if(CtShieldingData.IsScanner(item)&&Btn("CT source / scatter settings")){GUI.changed=false;if(AddCtSource(item))GUIUtility.ExitGUI();}
  if(CtShieldingData.IsPoint(item)){
   Section("CT source / energy");
   if(Btn("CT source / energy settings")){GUI.changed=false;OpenCtSettings(item);GUIUtility.ExitGUI();}
  }
  if(CtShieldingData.IsRoi(item)){
   bool beforeEnabled=GUI.enabled;GUI.enabled=beforeEnabled&&!item.locked;
   if(design.ct.sources.Count==0)CtSourceSetupUI();
   else if(item.ctPoint.roi.contributions.Count==0)GUILayout.Label("No source is included for this point. Open CT source / energy settings and include its scanner contribution.",small);
   foreach(var contribution in item.ctPoint.roi.contributions){var source=design.ct?.sources?.Find(candidate=>candidate.id==contribution.sourceId);if(source!=null)CtSpectrumUI(source,true);}
   GUI.enabled=beforeEnabled;
  }
  if(item.kind=="Wall"&&CtSelectedSource!=null){var source=CtSelectedSource;if(CtCoefficientLibrary.TryGet(source.beamFamily,item.shielding.material,source.kvp,out var coefficient,out var unsupported))GUILayout.Label(source.kvp+" kVp CT / alpha "+CtValue(coefficient.Fit.AlphaPerMm)+", beta "+CtValue(coefficient.Fit.BetaPerMm)+" (1/mm), gamma "+CtValue(coefficient.Fit.Gamma),small);else GUILayout.Label(unsupported,small);}
 }
 void CtCalculationControls(){
  Section("CT ROI air kerma");
  if(!CtShieldingData.Active(design.ct)||design.ct.sources.Count==0){
   GUILayout.Label("Configure a CT source and its energy before calculating.",small);
   if(Btn("Configure CT source / energy")){GUI.changed=false;OpenCtSettings();GUIUtility.ExitGUI();}
  }else if(!design.items.Any(CtShieldingData.IsRoi)){
   GUILayout.Label("Place an ROI or patient evaluation point before calculating.",small);
   if(Btn("Place ROI point")){tool="CT_ROI";wallStart=null;compactPanel="";GUI.changed=false;GUIUtility.ExitGUI();}
  }else{
   if(Btn("Calculate + save scenario")){CalculateCtScenario();GUIUtility.ExitGUI();}
   if(Btn("Save best / nominal / worst")){CalculateCtCases();GUIUtility.ExitGUI();}
  }
  if(ctLatest!=null&&Btn("CT results / comparison")){ctResultsPopup=showQa=true;GUIUtility.ExitGUI();}
 }
 void RequireCtCalculationSetup(){
  if(!CtShieldingData.Active(design.ct)||design.ct.sources.Count==0)throw new Exception("Configure a CT scanner source and its energy before calculating.");
  if(!design.items.Any(CtShieldingData.IsRoi))throw new Exception("Place at least one ROI/patient evaluation point.");
 }
 void CalculateCtScenario(){
  try{RequireCtCalculationSetup();if(dirty)Commit();var result=CtShieldingCalculation.Calculate(design);CtShieldingCalculation.SaveResult(design,result);ctLatest=result;ctCompareFirst=ctCompareSecond="";ctResultsPopup=showQa=true;ctResultsScroll=Vector2.zero;Commit();status=result.rows.Any(row=>!row.complete)?"Saved incomplete CT scenario. Configure the missing inputs from the results dialog.":"CT scenario saved: "+result.rows.Count+" ROI results.";}
  catch(Exception error){status="CT calculation failed: "+error.Message;}
 }
 void CalculateCtCases(){
  try{
   RequireCtCalculationSetup();if(dirty)Commit();
   if(design.ct.results.Count+3>CtShieldingData.MaxResults)throw new Exception("Three history slots are required; no results were added.");
   var results=new[]{"Best","Nominal","Worst"}.Select(scenario=>CtShieldingCalculation.Calculate(design,scenario)).ToArray();
  foreach(var result in results)CtShieldingCalculation.SaveResult(design,result);ctLatest=results[1];ctCompareFirst=ctCompareSecond="";Commit();ctResultsPopup=showQa=true;ctResultsScroll=Vector2.zero;status=results.Any(result=>result.rows.Any(row=>!row.complete))?"Saved incomplete CT scenarios. Configure the missing inputs from the results dialog.":"Saved best, nominal and worst CT workload scenarios.";
  }catch(Exception error){status="CT scenarios failed: "+error.Message;}
 }
 void CtResultsModal(){
  float width=Mathf.Min(900,W-48);var rectangle=new Rect((W-width)/2,30,width,H-60);GUI.DrawTexture(rectangle,card);
  GUILayout.BeginArea(new Rect(rectangle.x+20,rectangle.y+16,rectangle.width-40,rectangle.height-32));GUILayout.Label("CT ROI results / air kerma",sub);
  if(Btn("Configure CT source / energy")){OpenCtSettings();GUIUtility.ExitGUI();}
  ctResultsScroll=GUILayout.BeginScrollView(ctResultsScroll);
  var records=design.ct?.results??new List<CtResultSnapshot>();
  GUILayout.BeginHorizontal();GUILayout.BeginVertical();GUILayout.Label("Comparison A",small);foreach(var record in records.AsEnumerable().Reverse())if(Btn(record.name+" / "+record.scenarioCase+" / "+record.shieldingMode,ctCompareFirst==record.id))ctCompareFirst=record.id;GUILayout.EndVertical();
  GUILayout.BeginVertical();GUILayout.Label("Comparison B",small);foreach(var record in records.AsEnumerable().Reverse())if(Btn(record.name+" / "+record.scenarioCase+" / "+record.shieldingMode,ctCompareSecond==record.id))ctCompareSecond=record.id;GUILayout.EndVertical();GUILayout.EndHorizontal();
  var first=records.Find(record=>record.id==ctCompareFirst);var second=records.Find(record=>record.id==ctCompareSecond);
  if(first!=null&&second!=null){
   Section("Saved comparison");bool matching=CtShieldingCalculation.SameInputs(first,second);
   Label(matching?"Matching source / geometry / ROI inputs":"Changed inputs or datasets: not a shielding-only comparison");
   Label(first.scenarioCase+" / "+first.shieldingMode+"  versus  "+second.scenarioCase+" / "+second.shieldingMode);
   foreach(var row in first.rows){var other=second.rows.Find(candidate=>candidate.roiId==row.roiId);if(other==null||!row.hasPhysical||!other.hasPhysical){Label(row.roiName+": incomplete or unmatched result");continue;}
    Label(row.roiName+": A "+CtFieldValue(true,row.physicalMgyPerWeek,row.underflow)+"; B "+CtFieldValue(true,other.physicalMgyPerWeek,other.underflow)+" mGy/week");
    Label("B - A: "+CtValue(other.physicalMgyPerWeek-row.physicalMgyPerWeek)+" mGy/week");
    if(row.physicalMgyPerWeek>0&&!row.underflow&&!other.underflow){Label("B / A: "+CtValue(other.physicalMgyPerWeek/row.physicalMgyPerWeek)+"; change "+CtValue(100*(other.physicalMgyPerWeek/row.physicalMgyPerWeek-1))+" %");}else Label("Ratio not evaluated: zero or underflowed baseline");
   }
  }
  var latest=second??first??ctLatest;
  if(latest!=null){
   Section(latest.name+" / "+latest.scenarioCase+" / "+latest.shieldingMode);
   if(latest.inputFingerprint!=CtShieldingCalculation.InputFingerprint(design))Label("Historical result: current calculation inputs differ.");
   GUILayout.Label(latest.createdUtc+" / "+latest.engineVersion,small);
   foreach(var row in latest.rows){
    Section(row.roiName);Label((row.hasVerdict?row.verdict:"Not evaluated")+" / "+row.status);
    var point=design.items.Find(item=>item.id==row.roiId&&CtShieldingData.IsRoi(item));
    if(point!=null&&Btn("Edit inputs for "+row.roiName)){OpenCtSettings(point);GUIUtility.ExitGUI();}
    Label("Physical: "+CtFieldValue(row.hasPhysical,row.physicalMgyPerWeek,row.underflow)+" mGy/week");
    Label("Occupied: "+CtFieldValue(row.hasOccupied,row.occupiedMgyPerWeek,!row.isZero&&row.occupancy>0&&row.occupiedMgyPerWeek==0)+" mGy/week");
    if(row.hasGoal)Label("Goal: "+CtValue(row.goalMgyPerWeek)+" mGy/week");if(row.hasVerdict)Label("Utilization: "+CtValue(row.utilization)+" / margin: "+CtValue(row.margin)+" mGy/week");
    foreach(string error in row.errors)GUILayout.Label(error,small);foreach(string flag in row.flags)GUILayout.Label(flag,small);
    foreach(var contribution in row.contributions){
     GUILayout.Label(contribution.protocolId+" / "+contribution.status,small);if(!contribution.hasResult)continue;
     GUILayout.Label("Distance "+CtValue(contribution.distanceMeters*100)+" cm / K0 "+CtValue(contribution.unshieldedMgyPerWeek)+" / B "+CtValue(contribution.transmission)+" / logB "+CtValue(contribution.logB),small);
     foreach(var segment in contribution.segments)GUILayout.Label(segment.barrierId+" / "+segment.material+" / "+CtValue(segment.pathThicknessMm)+" mm / "+segment.fitId+" / alpha "+CtValue(segment.alpha)+", beta "+CtValue(segment.beta)+", gamma "+CtValue(segment.gamma),small);
    }
   }
  }
  GUILayout.EndScrollView();GUILayout.BeginHorizontal();
  if(latest!=null&&Btn("Export selected result")){try{WriteOutput(SafeName()+"-ct-results.json",CtShieldingCalculation.ExportResults(new[]{latest}));status="Exported CT calculation record.";}catch(Exception error){status=error.Message;}}
  if(latest!=null&&Btn("Remove saved result")){design.ct.results.RemoveAll(result=>result.id==latest.id);ctLatest=null;ctCompareFirst=ctCompareSecond="";Commit();status="Removed saved CT result. Undo restores it.";}
  if(Btn("Close")){showQa=ctResultsPopup=false;GUI.FocusControl(null);}GUILayout.EndHorizontal();GUILayout.EndArea();
 }
 void CtAfterRebuild(){
  ctMeasurements.Clear();if(!CtShieldingData.Active(design.ct))return;var source=CtSelectedSource;if(source==null)return;
  if(ctAnnotationMaterial==null){var shader=Resources.Load<Shader>("CT/CTMarker");if(shader==null)throw new Exception("CT annotation shader is missing.");ctAnnotationMaterial=new Material(shader);ctAnnotationMaterial.SetFloat("_VertexTint",1);}
  foreach(var roi in design.items.Where(CtShieldingData.IsRoi)){var line=Line("CT scatter-to-ROI measurement",new Color(1,.8f,.3f),.018f,world);line.sharedMaterial=ctAnnotationMaterial;ctMeasurements.Add(new CtMeasurement{sourceId=source.id,roiId=roi.id,line=line});}
  UpdateCtMeasurements();
 }
 void UpdateCtMeasurements(){
  if(!CtShieldingData.Active(design.ct))return;
  foreach(var point in design.items.Where(item=>CtShieldingData.IsPoint(item)&&item.ctPoint.role=="Scatter"))if(objects.TryGetValue(point.id,out var root))root.transform.position=CtShieldingData.Position(design,point).UnityVector;
  foreach(var measurement in ctMeasurements){var source=design.ct.sources.Find(candidate=>candidate.id==measurement.sourceId);var scatter=design.items.Find(item=>item.id==source?.scatterPointId);var roi=design.items.Find(item=>item.id==measurement.roiId);
   if(measurement.line==null)continue;measurement.line.enabled=scatter!=null&&roi!=null;if(!measurement.line.enabled)continue;
   measurement.line.SetPosition(0,CtShieldingData.Position(design,scatter).UnityVector);measurement.line.SetPosition(1,CtShieldingData.Position(design,roi).UnityVector);
    bool active=measurement.roiId==CtMeasurementRoiId;
    measurement.line.startColor=measurement.line.endColor=active?new Color(1,.8f,.3f):new Color(.65f,.7f,.72f,.55f);
    measurement.line.startWidth=measurement.line.endWidth=active?.028f:.014f;
  }
 }
 void CtUpdate(){
  UpdateCtMeasurements();
  if(ctImportTask!=null&&ctImportTask.IsCompleted){
   try{string json=ctImportTask.GetAwaiter().GetResult();if(ctImportFingerprint!=CtShieldingCalculation.InputFingerprint(design))throw new Exception("Calculation inputs changed while the file picker was open.");CtShieldingCalculation.ImportResults(design,json);Commit();status="Imported CT result history.";}
   catch(Exception error){status="CT result import failed: "+error.Message;}ctImportTask=null;
  }
 }
 void DrawCtDistanceLabels(){
  if(CompactPanelOpen||showHelp||showFiles||showQa||showComponentEditor||showPlanAuthoring)return;
  foreach(var measurement in ctMeasurements){
    if(measurement.line==null||!measurement.line.enabled||measurement.roiId!=CtMeasurementRoiId)continue;
   var midpoint=(measurement.line.GetPosition(0)+measurement.line.GetPosition(1))/2;var projected=cam.WorldToScreenPoint(midpoint);if(projected.z<=0)continue;
   var position=new Vector2(projected.x/uiScale,(Screen.height-projected.y)/uiScale);if(!View.Contains(position))continue;
   var rectangle=StudioViewportLayout.DistanceLabel(View,position);
  var source=design.ct.sources.Find(candidate=>candidate.id==measurement.sourceId);var scatter=design.items.Find(item=>item.id==source?.scatterPointId);var roi=design.items.Find(item=>item.id==measurement.roiId);
  if(scatter==null||roi==null)continue;double distance=CtShieldingCalculation.DistanceMeters(design,scatter,roi)*100;
    bool projectedDistance=top&&Math.Abs(CtShieldingData.Position(design,scatter).y-CtShieldingData.Position(design,roi).y)>1e-10;
    GUI.DrawTexture(rectangle,card);GUI.Label(rectangle,StudioViewportLayout.DistanceText(distance,projectedDistance),body);
  }
 }
 bool TryPickCtAnnotation(Vector2 mouse,out string id,out bool pointMarker){
  id=null;pointMarker=false;if(tool!="Select"||!CtShieldingData.Active(design.ct))return false;
  float closest=12;
  foreach(var point in design.items.Where(CtShieldingData.IsPoint)){
   var projected=cam.WorldToScreenPoint(CtShieldingData.Position(design,point).UnityVector);if(projected.z<=0)continue;
   float distance=Vector2.Distance(mouse,new Vector2(projected.x/uiScale,(Screen.height-projected.y)/uiScale));
   if(distance<closest){closest=distance;id=point.id;pointMarker=true;}
  }
  if(id!=null)return true;
  foreach(var measurement in ctMeasurements){
   if(measurement.line==null||!measurement.line.enabled)continue;
   var first=cam.WorldToScreenPoint(measurement.line.GetPosition(0));var second=cam.WorldToScreenPoint(measurement.line.GetPosition(1));if(first.z<=0||second.z<=0)continue;
   var start=new Vector2(first.x/uiScale,(Screen.height-first.y)/uiScale);var end=new Vector2(second.x/uiScale,(Screen.height-second.y)/uiScale);var delta=end-start;
   float fraction=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(mouse-start,delta)/delta.sqrMagnitude):0;
   if(Vector2.Distance(mouse,start+delta*fraction)<6){id=measurement.roiId;design.ct.selectedRoiId=id;return true;}
  }
  return false;
 }
}
}