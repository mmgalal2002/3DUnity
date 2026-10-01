using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public static class PlanAuthoringChecks {
 static void Need(bool ok,string message){if(!ok)throw new Exception("Plan authoring regression: "+message);}
 static void Near(float a,float b,string message)=>Need(Math.Abs(a-b)<.0001f,message+" ("+a+" vs "+b+")");
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Need(rejected,message);}
 public static FloorPlanData Fixture(){
  var texture=new Texture2D(640,240,TextureFormat.RGBA32,false);var pixels=new Color32[640*240];
  for(int y=0;y<240;y++)for(int x=0;x<640;x++)pixels[y*640+x]=new Color32(255,255,255,255);
  for(int y=20;y<25;y++)for(int x=40;x<600;x++)pixels[y*640+x]=new Color32(255,0,255,255);
  for(int y=60;y<200;y++)for(int x=70;x<75;x++)pixels[y*640+x]=new Color32(0,255,0,255);
  pixels[10*640+10]=new Color32(255,0,255,255);pixels[11*640+11]=new Color32(255,0,255,0);
  texture.SetPixels32(pixels);texture.Apply();var guide=FloorPlanCodec.Read(texture.EncodeToPNG(),"Req5-color-fixture.png");Release(texture);return guide;
 }
 static void Release(UnityEngine.Object o){if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
 static PlanColorRule Rule(string id,Color target)=>new PlanColorRule{id=id,target=target,display=target,material="Concrete",tolerance=.01f};
 static void Finish(PlanColorMask job){int turns=0;while(!job.Step(4096))if(++turns>20000)throw new Exception("Mask processing did not terminate.");}
 public static void Run(){
  var guide=Fixture();guide.authoring=PlanAuthoring.Create(guide);var model=guide.authoring;PlanAuthoring.Validate(model,guide,true);
  Need(!PlanAuthoring.CalibrationCurrent(guide),"new scale silently confirmed");
  var c=new PlanCalibration{method="TwoPoint",a=new Vector2Data(20.5f,100.5f),b=new Vector2Data(520.5f,100.5f),distanceMetres=5};
  PlanAuthoring.Confirm(guide,c);Near(guide.metersPerPixel,.01f,"500px / 5m");Need(PlanAuthoring.CalibrationCurrent(guide),"confirmation missing");
  var a=PlanAuthoring.PixelToWorld(guide,new Vector2(10.5f,20.5f));var b=PlanAuthoring.PixelToWorld(guide,new Vector2(110.5f,20.5f));Near(Vector3.Distance(a,b),1,"100 px = 1 m");Need(a.z>0,"image Y inversion");
  guide.x=2.37f;guide.z=-1.237f;guide.rotation=37;
  var pixel=new Vector2(117.5f,201.5f);var world=PlanAuthoring.PixelToWorld(guide,pixel,1.7f);var restored=PlanAuthoring.WorldToPixel(guide,world);Near(Vector2.Distance(pixel,restored),0,"rotated translated coordinate roundtrip");Near(world.y,1.7f,"elevation");
  Near(PlanAuthoring.ManualScale(10,"mm/px"),.01f,"mm conversion");Near(PlanAuthoring.ManualScale(100,"px/m"),.01f,"inverse units");
  foreach(float invalid in new[]{0,-1,float.NaN,float.PositiveInfinity})Reject(()=>PlanAuthoring.ManualScale(invalid,"m/px"),"invalid scale");
  string before=JsonUtility.ToJson(guide);Reject(()=>PlanAuthoring.Confirm(guide,new PlanCalibration{method="TwoPoint",a=new Vector2Data(10,10),b=new Vector2Data(10,10)}),"coincident points");Need(before==JsonUtility.ToJson(guide),"failed calibration mutated guide");
  guide.heightMeters*=2;Need(!PlanAuthoring.CalibrationCurrent(guide),"nonuniform size accepted as current calibration");PlanAuthoring.Confirm(guide,c);
  var magenta=Rule("magenta",Color.magenta);var green=Rule("green",Color.green);model=guide.authoring;model.rules.Add(magenta);model.rules.Add(green);
  var texture=FloorPlanCodec.DecodeImage(guide);var pixels=texture.GetPixels32();Release(texture);
  var job=new PlanColorMask(pixels,guide.pixelWidth,guide.pixelHeight,model);before=JsonUtility.ToJson(guide);Need(!job.Step(1)&&job.Progress>0,"cooperative first slice");Finish(job);Need(before==JsonUtility.ToJson(guide),"detection mutated metadata");
  int mi=Array.FindIndex(job.rules,r=>r.id=="magenta"),gi=Array.FindIndex(job.rules,r=>r.id=="green");Need(job.retained[mi]==560*5&&job.retained[gi]==140*5,"fixture mask counts");Need(job.components[mi]==1&&job.components[gi]==1&&job.removedPixels==1&&job.transparent==1,"noise/alpha/components");
  var duplicate=Rule("earlier",Color.magenta);duplicate.priority=0;model.rules.Add(duplicate);job=new PlanColorMask(pixels,640,240,model);Finish(job);Need(job.overlaps==2801&&job.retained.Sum()==3500,"overlap did not assign one owner");Need(job.retained[Array.FindIndex(job.rules,r=>r.id=="earlier")]==2800,"stable-ID priority tie");
  duplicate.classification="Ignore";duplicate.priority=1;job=new PlanColorMask(pixels,640,240,model);Finish(job);Need(job.retained[Array.FindIndex(job.rules,r=>r.id=="magenta")]==2800,"priority ordering");
  Need(PlanColorMask.Matches(new Color32(255,0,8,255),new PlanColorRule{colorSpace="HSV",target=new Color(1,.02f,0),tolerance=.03f}),"circular red hue");
  var diagonal=new Color32[81];for(int n=0;n<9;n++)diagonal[n*9+n]=(Color32)Color.magenta;var tiny=new PlanAuthoringData();tiny.rules.Add(magenta);job=new PlanColorMask(diagonal,9,9,tiny);Finish(job);Need(job.components[0]==1&&job.retained[0]==9,"8-connected diagonal preservation");
  Reject(()=>new PlanColorMask(new Color32[1],8192,8192,tiny),"processing allocation bound");
  var copy=FloorPlanCodec.Clone(guide);FloorPlanCodec.Validate(copy);Need(copy.authoring.sourceFingerprint==model.sourceFingerprint&&copy.authoring.rules.Count==3,"native rules/calibration persistence");
  copy.authoring.sourceFingerprint=new string('0',64);Reject(()=>FloorPlanCodec.Validate(copy),"wrong source fingerprint");
  var portable=FloorPlanCodec.ReadJson(FloorPlanCodec.Write(guide),"guide.json");Need(PlanAuthoring.IsEmpty(portable.authoring),"guide v1 overloaded with authoring data");
  var design=Design.Example();design.floorPlan=guide;var round=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(round);Need(round.items.Count==7&&round.floorPlan.authoring.rules.Count==3,"design item invariance and authoring roundtrip");
  var unchosen=Rule("unchosen",Color.magenta);unchosen.material="";Reject(()=>PlanAuthoring.RequireWallRule(unchosen,design),"material inferred from color");unchosen.material="Concrete";unchosen.height=3;Reject(()=>PlanAuthoring.RequireWallRule(unchosen,design),"linked-ceiling rule silently accepted");
  Debug.Log("ROOM_STUDIO_PLAN_AUTHORING_CHECKS_PASSED: calibration/units/coordinates, invalid/nonuniform scale, mask counts/noise/alpha/priority/HSV/diagonals, source fingerprint, native/guide compatibility and physical-material guards");
 }
}
}
