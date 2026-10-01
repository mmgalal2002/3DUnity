using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 bool showPlanAuthoring,planShowMask;
 string planPane="Calibration",planPick="",planRuleId="",planMessage="";
 PlanAuthoringData planDraft;
 int planEpoch,planDraftEpoch,planJobEpoch,planMaskEpoch,planMaskRule=-1;
 string planJobSignature="",planMaskSignature="",planPixelSource="";
 Vector2 planScroll,planPreviewScroll,planPan;
 float planZoom=1;
 Texture2D planMaskTexture;
 Color32[] planPixels;
 PlanColorMask planJob,planMask;
 void InvalidatePlanWork(){planEpoch++;if(planJob!=null){planJob=null;planMessage="Processing cancelled because the design changed. Preview again for the current image/settings.";}}
 string PlanSignature()=>planDraft==null||floorPlan==null?"":JsonUtility.ToJson(planDraft)+"|"+floorPlan.x+"|"+floorPlan.z+"|"+floorPlan.rotation+"|"+floorPlan.widthMeters+"|"+floorPlan.heightMeters;
 void PlanAuthoringUpdate(){
  if(planJob==null)return;
  if(!showPlanAuthoring||dirty||planJobEpoch!=planEpoch||planJobSignature!=PlanSignature()){planJob=null;planMessage="Processing cancelled: source, settings or history changed.";return;}
  try{if(planJob.Step()){
   var complete=planJob;planJob=null;
   if(complete.retained.Sum()==0){planMessage="No retained matches. The previous mask is unchanged; adjust rules or minimum area.";return;}
   planMask=complete;planMaskEpoch=planEpoch;planMaskSignature=planJobSignature;planMaskRule=-1;RefreshPlanMaskTexture();planShowMask=true;
   planMessage="Mask ready in "+complete.ElapsedMilliseconds.ToString("F1")+" ms of processing. No walls created.";
  }}catch(Exception e){planJob=null;planMessage="Mask preview failed: "+e.Message;}
 }
 void OpenPlanAuthoring(){
  if(floorPlan==null||floorPlan.kind!="Image")return;CancelInteraction();
  planDraft=PlanAuthoring.IsEmpty(floorPlan.authoring)?PlanAuthoring.Create(floorPlan):PlanAuthoring.Clone(floorPlan.authoring);
  planDraftEpoch=planEpoch;planRuleId=planDraft.rules.FirstOrDefault()?.id??"";planPick="";planMessage="Changes stay in this panel until applied. Mask preview never creates walls.";
  planZoom=1;planPan=Vector2.zero;planShowMask=false;showPlanAuthoring=true;planScroll=planPreviewScroll=Vector2.zero;
 }
 void ClosePlanAuthoring(){showPlanAuthoring=false;planJob=null;planPick="";planDraft=null;planMask=null;planPixels=null;planPixelSource="";if(planMaskTexture!=null)Destroy(planMaskTexture);planMaskTexture=null;}
 void ApplyPlanSettings(bool confirmCalibration){
  if(planDraftEpoch!=planEpoch)throw new Exception("The design changed while this panel was open. Close and reopen it before applying settings.");
  var guide=FloorPlanCodec.Clone(floorPlan);guide.authoring=PlanAuthoring.Clone(planDraft);
  if(confirmCalibration)PlanAuthoring.Confirm(guide,guide.authoring.calibration);
  FloorPlanCodec.Validate(guide);ReplaceFloorPlan(guide,confirmCalibration?"Uniform image calibration confirmed.":"Image authoring settings saved. No walls created.");
  planDraft=PlanAuthoring.Clone(floorPlan.authoring);planDraftEpoch=planEpoch;planMessage=status;planPick="";
 }
 void PlanAction(Action action){try{action();}catch(Exception e){planMessage=e.Message;}}
 void StartPlanMask(){
  if(planDraftEpoch!=planEpoch)throw new Exception("The design changed. Close and reopen the image tools.");
  var p=floorPlan;PlanAuthoring.Validate(planDraft,p,true);
  if((long)p.pixelWidth*p.pixelHeight>PlanAuthoring.MaxProcessingPixels)throw new Exception("Mask processing currently supports up to 4 megapixels. Import a smaller source image; the tracing guide limit remains 16 MP.");
  if(planPixels==null||planPixelSource!=p.imageBase64){planPixels=floorPlanTexture.GetPixels32();planPixelSource=p.imageBase64;}
  var next=new PlanColorMask(planPixels,p.pixelWidth,p.pixelHeight,planDraft);
  planJob=next;planJobEpoch=planEpoch;planJobSignature=PlanSignature();planMessage="Processing original-resolution pixels...";
 }
 void RefreshPlanMaskTexture(){
  if(planMaskTexture!=null)Destroy(planMaskTexture);
  var pixels=planMask.Preview(planMaskRule,out int width,out int height);
  planMaskTexture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Color-rule mask preview",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};planMaskTexture.SetPixels32(pixels);planMaskTexture.Apply();
 }
 void PlanAuthoringModal(){
  if(floorPlan==null||floorPlan.kind!="Image"||planDraft==null){ClosePlanAuthoring();return;}
  var rect=new Rect((W-Mathf.Min(1100,W-48))/2,25,Mathf.Min(1100,W-48),H-50);GUI.DrawTexture(rect,card);
  GUILayout.BeginArea(new Rect(rect.x+20,rect.y+16,rect.width-40,rect.height-32));GUILayout.Label("Floor plan · calibration and color rules",title);
  // Bound both content panes so status/progress lines can never push Apply/Close
  // outside the window. Each pane scrolls independently on smaller displays.
  float bodyHeight=Mathf.Max(220,rect.height-155);
  GUILayout.BeginHorizontal(GUILayout.Height(bodyHeight));GUILayout.BeginVertical(GUILayout.Width((rect.width-56)*.52f));
  planPreviewScroll=GUILayout.BeginScrollView(planPreviewScroll,GUILayout.Height(bodyHeight));
  GUILayout.BeginHorizontal();if(Btn("Original image",!planShowMask))planShowMask=false;if(Btn("Color mask",planShowMask)&&planMask!=null)planShowMask=true;GUILayout.EndHorizontal();
  var preview=GUILayoutUtility.GetRect(200,Mathf.Max(140,bodyHeight-245),GUILayout.ExpandWidth(true));DrawPlanImage(preview);
  GUILayout.BeginHorizontal();if(Btn("Fit / reset view")){planZoom=1;planPan=Vector2.zero;}if(Btn("Zoom +"))planZoom=Mathf.Min(8,planZoom*1.4f);if(Btn("Zoom −"))planZoom=Mathf.Max(1,planZoom/1.4f);GUILayout.EndHorizontal();
  GUILayout.Label("Wheel zooms; right-drag pans. Picks use original pixel centres. Image top maps to +Z before guide rotation.",small);
  if(planPick!="")GUILayout.Label("Click the original image to pick "+(planPick=="Color"?"a rule color":"calibration point "+planPick)+".",body);
  if(planMask!=null&&planShowMask&& (planMaskEpoch!=planEpoch||planMaskSignature!=PlanSignature()))GUILayout.Label("Previous mask is stale. Preview again after editing.",body);
  if(planJob!=null){GUILayout.Label(planJob.Phase+" · "+(planJob.Progress*100).ToString("F0")+"%",body);if(Btn("Cancel processing")){planJob=null;planMessage="Processing cancelled. The prior preview and design are unchanged.";}}
  else if(Btn("Preview color mask"))PlanAction(StartPlanMask);
  if(planMask!=null){GUILayout.Label(planMask.retained.Sum()+" retained pixels · "+planMask.components.Sum()+" components · "+planMask.overlaps+" overlapping matches · "+planMask.removedPixels+" noise pixels removed",small);}
  GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.BeginVertical();GUILayout.BeginHorizontal();
  foreach(string pane in new[]{"Calibration","Color rules"})if(Btn(pane,planPane==pane)){planPane=pane;planScroll=Vector2.zero;planPick="";}
  GUILayout.EndHorizontal();planScroll=GUILayout.BeginScrollView(planScroll,GUILayout.Height(bodyHeight-38));
  if(planPane=="Calibration")PlanCalibrationUI();else PlanRulesUI();
  GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.EndHorizontal();GUILayout.FlexibleSpace();
  GUILayout.Label(planMessage,small);GUILayout.BeginHorizontal();
  if(Btn("Apply settings"))PlanAction(()=>ApplyPlanSettings(false));
  if(Btn("Close / discard unapplied edits")){ClosePlanAuthoring();GUIUtility.ExitGUI();}
  GUILayout.EndHorizontal();GUILayout.EndArea();
 }
 void PlanCalibrationUI(){
  var c=planDraft.calibration;Section("Confirmed real-world scale");
  GUILayout.Label(PlanAuthoring.CalibrationCurrent(floorPlan)?"Current guide scale is confirmed.":"Scale is unconfirmed or guide dimensions changed. Wall generation must wait for confirmation.",body);
  GUILayout.Label("Confirming applies a uniform scale to both guide dimensions. It never resizes the room or existing objects.",small);
  GUILayout.BeginHorizontal();foreach(string method in new[]{"Manual","TwoPoint"})if(Btn(method=="Manual"?"Explicit units":"Two points",c.method==method)){c.method=method;c.confirmed=false;planPick="";}GUILayout.EndHorizontal();
  if(c.method=="Manual"){
   GUILayout.BeginHorizontal();foreach(string unit in new[]{"m/px","mm/px","px/m"})if(Btn(unit,c.unit==unit)){c.unit=unit;c.confirmed=false;}GUILayout.EndHorizontal();
   float value=Number("Scale value",c.value,.000001f,1000000,c.unit,-1);if(value!=c.value){c.value=value;c.confirmed=false;}
  }else{
   GUILayout.BeginHorizontal();if(Btn("Pick point A")){planPick="A";planShowMask=false;}if(Btn("Pick point B")){planPick="B";planShowMask=false;}GUILayout.EndHorizontal();
   float ax=Number("A pixel X",c.a.x,0,planDraft.pixelWidth,"px",-1),ay=Number("A pixel Y",c.a.y,0,planDraft.pixelHeight,"px",-1);
   float bx=Number("B pixel X",c.b.x,0,planDraft.pixelWidth,"px",-1),by=Number("B pixel Y",c.b.y,0,planDraft.pixelHeight,"px",-1);
   float distance=Number("Measured distance",c.distanceMetres,.000001f,10000,"m",-1);
   if(ax!=c.a.x||ay!=c.a.y||bx!=c.b.x||by!=c.b.y||distance!=c.distanceMetres){c.a=new Vector2Data(ax,ay);c.b=new Vector2Data(bx,by);c.distanceMetres=distance;c.confirmed=false;}
  }
  bool valid=true;float scale=0;try{scale=c.method=="Manual"?PlanAuthoring.ManualScale(c.value,c.unit):PlanAuthoring.TwoPointScale(c.a,c.b,c.distanceMetres);}catch(Exception e){valid=false;GUILayout.Label(e.Message,small);}
  if(valid){GUILayout.Label(F(scale)+" m/px · "+F(1/scale)+" px/m",body);GUILayout.Label("Result: "+F(scale*planDraft.pixelWidth)+" × "+F(scale*planDraft.pixelHeight)+" m",small);}
  bool enabled=GUI.enabled;GUI.enabled=enabled&&valid;if(Btn("Confirm uniform scale + save settings"))PlanAction(()=>ApplyPlanSettings(true));GUI.enabled=enabled;
  GUILayout.Label("500 pixels over 5 metres = 0.01 m/px. Rotation/translation use the existing guide controls. Preview zoom/pan never alters calibration.",small);
 }
 void PlanRulesUI(){
  Section("Rules and priority");GUILayout.Label("Lower priority number wins; ties use stable rule IDs. Each pixel has at most one owner. Opening/reference/ignore rules never create walls.",small);
  GUILayout.BeginHorizontal();if(Btn("Add rule")){
   if(planDraft.rules.Count>=PlanAuthoring.MaxRules)planMessage="At most 32 rules.";
   else{var rule=new PlanColorRule{id=Guid.NewGuid().ToString("N"),priority=planDraft.rules.Count*10,height=design.height};planDraft.rules.Add(rule);planRuleId=rule.id;numberBuffers.Clear();GUIUtility.ExitGUI();}
  }
  var active=planDraft.rules.Find(r=>r.id==planRuleId);
  if(active!=null&&Btn("Duplicate")){if(planDraft.rules.Count<PlanAuthoring.MaxRules){var copy=JsonUtility.FromJson<PlanColorRule>(JsonUtility.ToJson(active));copy.id=Guid.NewGuid().ToString("N");copy.name+=" copy";planDraft.rules.Add(copy);planRuleId=copy.id;numberBuffers.Clear();GUIUtility.ExitGUI();}else planMessage="At most 32 rules.";}
  GUILayout.EndHorizontal();
  foreach(var rule in planDraft.rules.OrderBy(r=>r.priority).ThenBy(r=>r.id,StringComparer.Ordinal)){if(Btn((rule.enabled?"":"[off] ")+rule.name+" · "+rule.priority,planRuleId==rule.id)){planRuleId=rule.id;numberBuffers.Clear();}}
  if(active==null){GUILayout.Label("Add a rule, pick a color, then preview its mask.",body);return;}
  active.name=TextInput("plan-rule-name",active.name);active.enabled=GUILayout.Toggle(active.enabled," Enabled");
  active.priority=Mathf.RoundToInt(Number("Priority",active.priority,0,9999,"",1));
  foreach(string category in new[]{"Wall","Opening","Reference","Ignore"})if(Btn(category,active.classification==category))active.classification=category;
  var swatch=GUILayoutUtility.GetRect(80,22);var previous=GUI.color;GUI.color=active.target;GUI.DrawTexture(swatch,Texture2D.whiteTexture);GUI.color=previous;
  if(Btn("Pick color from image")){planPick="Color";planShowMask=false;}
  active.target=PlanColorUI("Target",active.target);
  GUILayout.BeginHorizontal();foreach(string space in new[]{"RGB","HSV"})if(Btn(space,active.colorSpace==space))active.colorSpace=space;GUILayout.EndHorizontal();
  active.tolerance=Number("Color tolerance",active.tolerance,0,1,"0–1",-1);
  GUILayout.Label(active.colorSpace=="HSV"?"Normalized RMS distance; hue wraps around red (0/1).":"Normalized RMS distance in decoded sRGB channels.",small);
  planDraft.alphaThreshold=Number("Minimum alpha",planDraft.alphaThreshold,0,1,"0–1",-1);
  active.minAreaPixels=Mathf.RoundToInt(Number("Minimum area",active.minAreaPixels,1,65536,"px²",1));
  active.minLengthPixels=Number("Minimum path length",active.minLengthPixels,1,8192,"px",-1);
  GUILayout.Label("Minimum area removes isolated color matches; minimum path length filters extracted walls.",small);
  Section("Wall properties");active.height=Number("Wall height",active.height,.001f,100,"m");active.baseElevation=Number("Base elevation",active.baseElevation,-100,100,"m");active.thicknessMm=Number("Wall thickness",active.thicknessMm,.01f,3000,"mm");active.densityKgM3=Number("Density",active.densityKgM3,100,25000,"kg/m3");
  GUILayout.Label("Physical material: "+(string.IsNullOrEmpty(active.material)?"choose explicitly":active.material),body);
    foreach(string material in CtCoefficientLibrary.MaterialIds)if(Btn(CtCoefficientLibrary.MaterialLabel(material),active.material==material))active.material=material;
  if(active.classification=="Wall")try{PlanAuthoring.RequireWallRule(active,design);}catch(Exception e){GUILayout.Label(e.Message,small);}
  Section("Display color");active.display=PlanColorUI("Display",active.display);
  if(planMask!=null){int index=Array.FindIndex(planMask.rules,r=>r.id==active.id);if(index>=0){GUILayout.Label(planMask.matched[index]+" matched / "+planMask.retained[index]+" retained pixels; "+planMask.components[index]+" components, "+planMask.rejectedComponents[index]+" rejected",small);if(Btn("Show this rule's mask")){planMaskRule=index;RefreshPlanMaskTexture();planShowMask=true;}}if(Btn("Show all mask categories")){planMaskRule=-1;RefreshPlanMaskTexture();planShowMask=true;}}
  if(Btn("Delete draft rule")){planDraft.rules.Remove(active);planRuleId=planDraft.rules.FirstOrDefault()?.id??"";planPick="";numberBuffers.Clear();GUIUtility.ExitGUI();}
 }
 Color PlanColorUI(string prefix,Color color){return new Color(Number(prefix+" red",color.r*255,0,255,"",1)/255,Number(prefix+" green",color.g*255,0,255,"",1)/255,Number(prefix+" blue",color.b*255,0,255,"",1)/255,1);}
 void DrawPlanImage(Rect viewport){
  GUI.DrawTexture(viewport,inputTex);GUI.BeginGroup(viewport);
  float fit=Mathf.Min(viewport.width/floorPlan.pixelWidth,viewport.height/floorPlan.pixelHeight),scale=fit*planZoom;
  var size=new Vector2(floorPlan.pixelWidth*scale,floorPlan.pixelHeight*scale);var imageRect=new Rect((viewport.width-size.x)/2+planPan.x,(viewport.height-size.y)/2+planPan.y,size.x,size.y);
  var texture=planShowMask?planMaskTexture:floorPlanTexture;if(texture!=null)GUI.DrawTexture(imageRect,texture,ScaleMode.StretchToFill);
  var e=Event.current;if(new Rect(0,0,viewport.width,viewport.height).Contains(e.mousePosition)){
   if(e.type==EventType.ScrollWheel){planZoom=Mathf.Clamp(planZoom*Mathf.Pow(1.15f,-e.delta.y),1,8);e.Use();}
   else if(e.type==EventType.MouseDrag&&e.button==1){planPan+=e.delta;e.Use();}
   else if(e.type==EventType.MouseDown&&e.button==0&&planPick!=""&&!planShowMask&&imageRect.Contains(e.mousePosition)){
    int x=Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.x-imageRect.x)/scale),0,floorPlan.pixelWidth-1),y=Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.y-imageRect.y)/scale),0,floorPlan.pixelHeight-1);
    if(planPick=="Color"){
     var rule=planDraft.rules.Find(r=>r.id==planRuleId);var color=floorPlanTexture.GetPixel(x,floorPlan.pixelHeight-1-y);
     if(color.a<=0||color.a<planDraft.alphaThreshold)planMessage="That pixel is transparent or below the minimum alpha. Pick an opaque stroke.";
     else if(rule!=null){rule.target=new Color(color.r,color.g,color.b,1);planMessage="Sampled original pixel "+x+", "+y+". Apply settings to save.";planPick="";}
    }else{var point=new Vector2Data(x+.5f,y+.5f);if(planPick=="A"){planDraft.calibration.a=point;planPick="B";}else{planDraft.calibration.b=point;planPick="";}planDraft.calibration.confirmed=false;}
    numberBuffers.Clear();e.Use();
   }
  }
  if(!planShowMask&&planDraft.calibration.method=="TwoPoint"){
   var c=planDraft.calibration;int n=0;foreach(var p in new[]{c.a,c.b}){var point=imageRect.position+new Vector2(p.x,p.y)*scale;var color=GUI.color;GUI.color=Color.yellow;GUI.DrawTexture(new Rect(point.x-4,point.y-4,8,8),Texture2D.whiteTexture);GUI.color=color;GUI.Label(new Rect(point.x+6,point.y-18,25,22),n++==0?"A":"B",small);}
  }
  GUI.EndGroup();
 }
}
}
