using System;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace RoomStudio {
public static class BeamVisual {
 // The supplied Versa prefab has a horizontal Beam_Window mesh. Its lower face
 // is the visible aperture, so use the instantiated transform after model scale
 // and floor alignment instead of a room-level isocentre offset.
 public static Vector3 Aperture(Transform window){
  if(window==null||window.name!="Beam_Window")throw new Exception("The Versa beam window is unavailable.");
  var mesh=window.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)throw new Exception("The Versa beam window mesh is unavailable.");
  var bounds=mesh.bounds;
  return window.TransformPoint(new Vector3(bounds.center.x,bounds.min.y-.01f,bounds.center.z));
 }
 public static Vector3 Direction(Transform window,Vector3 isocentre){
  if(window==null||float.IsNaN(isocentre.x)||float.IsNaN(isocentre.y)||float.IsNaN(isocentre.z)||float.IsInfinity(isocentre.x)||float.IsInfinity(isocentre.y)||float.IsInfinity(isocentre.z))throw new Exception("Invalid beam direction input.");
  var direction=isocentre-Aperture(window);if(direction.sqrMagnitude<.0000001f)throw new Exception("Beam aperture overlaps the isocentre.");
  return direction.normalized;
 }
}

public partial class StudioApp {
 Transform beamWindow,gantryPivot,linacModelRoot;
 Tween gantryTween;
 Quaternion gantryRestLocalRotation;
 float visualGantryAngle;
 static readonly string[] HeadAndArmPartPrefixes={"Beam_Window","Collimator_","Head_","Imaging_Base_Pad_","KV_","Left_Detector_","Left_Imaging_Pivot","Organic_Arm_","Right_Imaging_Pivot","Right_Source_Arm_","Touchguard_"};
 Transform FindBeamWindow(Transform model){return model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Beam_Window");}
 Transform FindModelPart(Transform model,string name){return model.GetComponentsInChildren<Transform>(true).FirstOrDefault(part=>part.name==name);}
 bool IsCouchPart(Transform part){
    return part.name.StartsWith("Couch_",StringComparison.Ordinal)||part.name.StartsWith("Table_",StringComparison.Ordinal)||part.name.StartsWith("Bellows_",StringComparison.Ordinal);}
 bool IsHeadOrArmPart(Transform part){return !IsCouchPart(part)&&HeadAndArmPartPrefixes.Any(prefix=>part.name.StartsWith(prefix,StringComparison.Ordinal));}
 void ConfigureGantryRig(Transform model){
  beamWindow=FindBeamWindow(model);linacModelRoot=model;if(beamWindow==null)return;
  var modelParts=model.GetComponentsInChildren<Transform>(true).Where(part=>part!=model).ToArray();
  foreach(var part in modelParts)part.SetParent(model,true);
  var parts=modelParts.Where(IsHeadOrArmPart).ToArray();
  if(!parts.Contains(beamWindow))throw new Exception("Beam_Window is not part of the rotating head and arm assembly.");
  var pivot=new GameObject("Gantry motion pivot").transform;pivot.SetParent(model,false);pivot.position=Isocentre();pivot.rotation=beamWindow.rotation;
  foreach(var part in parts)part.SetParent(pivot,true);
  gantryPivot=pivot;gantryRestLocalRotation=pivot.localRotation;visualGantryAngle=0;SetGantryVisualAngle(design.gantry);
 }
 void SetGantryVisualAngle(float angle){
  visualGantryAngle=angle;
  if(gantryPivot!=null)gantryPivot.localRotation=gantryRestLocalRotation*Quaternion.AngleAxis(angle,Vector3.forward);
  UpdateBeamIllustration();
 }
 void TweenGantryTo(float angle){
  if(gantryPivot==null){SetGantryVisualAngle(angle);return;}
  StopGantryTween();float destination=visualGantryAngle+Mathf.DeltaAngle(visualGantryAngle,angle);
  if(Mathf.Abs(destination-visualGantryAngle)<.001f){SetGantryVisualAngle(destination);return;}
  gantryTween=DOTween.To(()=>visualGantryAngle,SetGantryVisualAngle,destination,.35f).SetEase(Ease.OutCubic).SetTarget(gantryPivot.gameObject).OnComplete(()=>gantryTween=null);
 }
 void StopGantryTween(){if(gantryTween==null)return;gantryTween.Kill(false);gantryTween=null;}
 void UpdateBeamIllustration(){
  if(beam==null)return;
  bool available=showBeam&&string.IsNullOrEmpty(design.sourceJson)&&design.items.Any(x=>x.kind=="LINAC")&&beamWindow!=null;
  beam.enabled=available;if(!available)return;
  var start=BeamVisual.Aperture(beamWindow);
  var direction=BeamVisual.Direction(beamWindow,Isocentre());
  beam.SetPosition(0,start);beam.SetPosition(1,start+direction*Mathf.Max(3,design.sourceDistance+2));
 }
 void BeamSmokeChecks(){
  var machine=design.items.Find(x=>x.kind=="LINAC");if(machine==null)return;
  string original=JsonUtility.ToJson(design);bool oldShow=showBeam;
  try{
   showBeam=true;design.gantry=0;Rebuild();
    if(beamWindow==null||gantryPivot==null||!beam.enabled)throw new Exception("Versa gantry assembly or beam window missing from the placed model.");
   var start=BeamVisual.Aperture(beamWindow);
    var iso=Isocentre();
   if(Vector3.Distance(beam.GetPosition(0),start)>.0001f)throw new Exception("Beam does not originate at the gantry window.");
    if(Vector3.Dot((beam.GetPosition(1)-start).normalized,(iso-start).normalized)<.999f)throw new Exception("Zero-degree beam is not directed at the isocentre.");
   machine.x+=1.37f;machine.z-=.42f;machine.angle+=37;machine.scale=.9f;Rebuild();
    var initialAperture=BeamVisual.Aperture(beamWindow);var radius=Vector3.Distance(initialAperture,Isocentre());var basePosition=linacModelRoot.position;var baseRotation=linacModelRoot.rotation;
    if(Vector3.Distance(beam.GetPosition(0),initialAperture)>.0001f||Vector3.Distance(start,initialAperture)<.1f||radius<.1f)throw new Exception("Beam did not follow the placed model transform or configured isocentre.");
   var movingParts=new[]{"Head_Main_Collar","Organic_Arm_Main","Right_Source_Arm_A","Left_Detector_Arm_A","Touchguard_Outer_Ring","Touchguard_Metal_Inner_Ring","Touchguard_Rib_00"}.Select(name=>FindModelPart(linacModelRoot,name)).ToArray();
    var couchParts=linacModelRoot.GetComponentsInChildren<Transform>(true).Where(IsCouchPart).ToArray();
    var couchBox=FindModelPart(linacModelRoot,"Couch_Mechanics_Box");var couchBed=FindModelPart(linacModelRoot,"Couch_Carbon_Top");
    if(couchParts.Length==0||!couchParts.Any(part=>part.name=="Couch_Vent_1_00")||couchBox==null||couchBed==null)throw new Exception("Versa under-bed couch assembly is incomplete.");
    var boxRenderer=couchBox.GetComponent<Renderer>();var bedRenderer=couchBed.GetComponent<Renderer>();
    if(boxRenderer==null||bedRenderer==null||boxRenderer.bounds.max.y>=bedRenderer.bounds.min.y)throw new Exception("Versa under-bed couch box is not below the carbon bed.");
   var stationarySupportParts=new[]{"Gantry_Outer_Ring","Gantry_Inner_Fascia","Bore_Metal_Frame","Bore_Cyan_Ring_Outer","Outer_Ring_Seam","Outer_Fascia_Bolt_00","Couch_Base","Couch_Base_Shadow","Bellows_00","Bellows_19","Table_Side_Rail_1","Rear_Floor_Foot","Rear_Lower_Column"}.Select(name=>FindModelPart(linacModelRoot,name)).ToArray();
   var stationaryParts=linacModelRoot.GetComponentsInChildren<Transform>(true).Where(part=>part!=linacModelRoot&&part!=gantryPivot&&!IsHeadOrArmPart(part)).ToArray();
   if(movingParts.Any(part=>part==null||!part.IsChildOf(gantryPivot))||stationarySupportParts.Any(part=>part==null||!stationaryParts.Contains(part))||stationaryParts.Length==0||couchParts.Any(part=>!stationaryParts.Contains(part)))throw new Exception("Versa head, arms, touchguard, stationary housing, or couch checks could not find their correctly grouped model parts.");
    var movingPartRotations=movingParts.Select(part=>part.rotation).ToArray();var stationaryPartPositions=stationaryParts.Select(part=>part.position).ToArray();var stationaryPartRotations=stationaryParts.Select(part=>part.rotation).ToArray();
    foreach(float angle in new[]{90f,180f,270f,0f}){
     design.gantry=angle;TweenGantryTo(angle);if(gantryTween!=null)gantryTween.Complete();
     var aperture=BeamVisual.Aperture(beamWindow);var direction=(Isocentre()-aperture).normalized;
     if(Mathf.Abs(Vector3.Distance(aperture,Isocentre())-radius)>.001f)throw new Exception("Gantry aperture left its fixed-isocentre orbit.");
     if(Vector3.Distance(beam.GetPosition(0),aperture)>.0001f||Vector3.Dot((beam.GetPosition(1)-aperture).normalized,direction)<.999f)throw new Exception("Beam did not follow the moving aperture and aim at isocentre.");
     if(Vector3.Distance(linacModelRoot.position,basePosition)>.0001f||Quaternion.Angle(linacModelRoot.rotation,baseRotation)>.001f)throw new Exception("Gantry animation moved the stationary LINAC root.");
      for(int partIndex=0;partIndex<stationaryParts.Length;partIndex++)if(Vector3.Distance(stationaryParts[partIndex].position,stationaryPartPositions[partIndex])>.0001f||Quaternion.Angle(stationaryParts[partIndex].rotation,stationaryPartRotations[partIndex])>.001f)throw new Exception("Gantry angle moved stationary LINAC part "+stationaryParts[partIndex].name+".");
     if(angle==90f){
      for(int partIndex=0;partIndex<movingParts.Length;partIndex++)if(Quaternion.Angle(movingParts[partIndex].rotation,movingPartRotations[partIndex])<1f)throw new Exception("Ninety-degree gantry angle did not rotate "+movingParts[partIndex].name+".");
      if(Vector3.Distance(aperture,initialAperture)<.1f)throw new Exception("Ninety-degree gantry angle did not move the aperture.");
     }
    }
    design.gantry=90;TweenGantryTo(90);SetGantryVisualAngle(45);design.gantry=270;TweenGantryTo(270);
    if(gantryTween==null)throw new Exception("Gantry angle change did not create a retargetable tween.");
    gantryTween.Complete();if(Mathf.Abs(Mathf.DeltaAngle(visualGantryAngle,270))>.001f)throw new Exception("Gantry tween did not retarget from its current pose.");
    design.gantry=90;TweenGantryTo(90);SetGantryVisualAngle(45);Rebuild();
    if(gantryTween!=null||Mathf.Abs(Mathf.DeltaAngle(visualGantryAngle,design.gantry))>.001f)throw new Exception("Rebuild did not stop the old tween and restore the saved angle.");
   showBeam=false;UpdateBeam();if(beam.enabled)throw new Exception("Beam visibility toggle failed.");
      Debug.Log("ROOM_STUDIO_VERSA_BEAM_CHECKS_PASSED: animated head, arms and touchguard, stationary gantry housing, bore, couch, bellows and floor supports, fixed isocentre, beam aim, retargeting, rebuild and visibility");
  }finally{design=JsonUtility.FromJson<Design>(original);showBeam=oldShow;Rebuild();}
 }
}
}
