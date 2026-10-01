using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace RoomStudio {
[Serializable] public class PlanCalibration {
 public bool confirmed;
 public string method="Manual",unit="m/px";
 public float value=.01f,metresPerPixel=.01f,distanceMetres=5;
 public Vector2Data a=new Vector2Data(),b=new Vector2Data();
}
[Serializable] public class PlanColorRule {
 public string id="",name="Wall category",classification="Wall",colorSpace="RGB",material="";
 public bool enabled=true;
 public int priority,minAreaPixels=4;
 public float tolerance=.08f,minLengthPixels=5,height=5,baseElevation,thicknessMm=150,densityKgM3=2350;
 public Color target=new Color(1,0,1,1),display=new Color(1,0,1,1);
}
[Serializable] public class PlanAuthoringData {
 // Zero means absent (including JsonUtility's materialized null inline DTO).
 public int version;
 public string id="",sourceFingerprint="";
 public int pixelWidth,pixelHeight;
 public float alphaThreshold=.1f;
 public PlanCalibration calibration=new PlanCalibration();
 public List<PlanColorRule> rules=new List<PlanColorRule>();
}
public static class PlanAuthoring {
 public const int MaxRules=32,MaxProcessingPixels=4*1024*1024;
 public static readonly string[] Materials={"Concrete","Steel","Lead","Gypsum","PlateGlass","Wood","Glass"};
 public static bool IsEmpty(PlanAuthoringData p)=>p==null||(p.version==0&&string.IsNullOrEmpty(p.id)&&string.IsNullOrEmpty(p.sourceFingerprint)&&(p.rules==null||p.rules.Count==0)&&!(p.calibration?.confirmed??false));
 public static string Fingerprint(FloorPlanData p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Convert.FromBase64String(p.imageBase64))).Replace("-","").ToLowerInvariant();}
 public static PlanAuthoringData Create(FloorPlanData p)=>new PlanAuthoringData{version=1,id=Guid.NewGuid().ToString("N"),sourceFingerprint=Fingerprint(p),pixelWidth=p.pixelWidth,pixelHeight=p.pixelHeight};
 public static PlanAuthoringData Clone(PlanAuthoringData p)=>JsonUtility.FromJson<PlanAuthoringData>(JsonUtility.ToJson(p));
 static void Range(float v,float min,float max,string label)=>PrecisionEditing.Range(v,min,max,"plan "+label);
 static void Text(string s,int max,string label,bool empty=false){if(s==null||(!empty&&string.IsNullOrWhiteSpace(s))||s.Length>max)throw new Exception("Invalid plan "+label+".");}
 static void ColorRange(Color c){Range(c.r,0,1,"color");Range(c.g,0,1,"color");Range(c.b,0,1,"color");Range(c.a,0,1,"alpha");}
 public static void ValidateRule(PlanColorRule r){
  if(r==null)throw new Exception("Missing color rule.");Text(r.id,128,"rule ID");Text(r.name,100,"category name");
  if(!new[]{"Wall","Opening","Reference","Ignore"}.Contains(r.classification))throw new Exception("Invalid rule classification.");
  if(r.colorSpace!="RGB"&&r.colorSpace!="HSV")throw new Exception("Invalid color space.");
  if(r.priority<0||r.priority>9999||r.minAreaPixels<1||r.minAreaPixels>65536)throw new Exception("Invalid rule priority or minimum area.");
  ColorRange(r.target);ColorRange(r.display);Range(r.tolerance,0,1,"color tolerance");Range(r.minLengthPixels,1,8192,"minimum length in pixels");
  Range(r.height,.001f,100,"wall height");Range(r.baseElevation,-100,100,"base elevation");Range(r.thicknessMm,.01f,3000,"thickness in mm");Range(r.densityKgM3,100,25000,"density in kg/m3");
  if(r.material!=null&&r.material!=""&&!Materials.Contains(r.material))throw new Exception("Unknown physical wall material.");
 }
 public static void Validate(PlanAuthoringData p,FloorPlanData guide,bool verifyFingerprint=false){
  if(IsEmpty(p))return;
  if(p.version!=1||guide==null||guide.kind!="Image")throw new Exception("Unsupported image authoring metadata.");
  Text(p.id,128,"authoring ID");if(p.sourceFingerprint==null||p.sourceFingerprint.Length!=64||p.sourceFingerprint.Any(c=>!(c>='0'&&c<='9')&&!(c>='a'&&c<='f')))throw new Exception("Invalid plan source fingerprint.");
  if(p.pixelWidth!=guide.pixelWidth||p.pixelHeight!=guide.pixelHeight)throw new Exception("Authoring metadata belongs to different image dimensions.");
  if(verifyFingerprint&&p.sourceFingerprint!=Fingerprint(guide))throw new Exception("Authoring metadata belongs to a different source image.");
  Range(p.alphaThreshold,0,1,"alpha threshold");if(p.rules==null||p.rules.Count>MaxRules)throw new Exception("At most 32 color rules are supported.");
  var ids=new HashSet<string>();foreach(var rule in p.rules){ValidateRule(rule);if(!ids.Add(rule.id))throw new Exception("Duplicate color rule ID.");}
  var c=p.calibration;if(c==null)throw new Exception("Missing calibration record.");
  if(c.method!="Manual"&&c.method!="TwoPoint")throw new Exception("Invalid calibration method.");
  if(c.unit!="m/px"&&c.unit!="mm/px"&&c.unit!="px/m")throw new Exception("Invalid calibration unit.");
  Range(c.value,.000001f,1000000,"calibration value");Range(c.metresPerPixel,.000001f,10,"calibration scale");Range(c.distanceMetres,.000001f,10000,"calibration distance");
  if(c.a==null||c.b==null)throw new Exception("Missing calibration points.");
  foreach(var point in new[]{c.a,c.b}){Range(point.x,0,p.pixelWidth,"calibration pixel X");Range(point.y,0,p.pixelHeight,"calibration pixel Y");}
  if(c.confirmed){float expected=c.method=="TwoPoint"?TwoPointScale(c.a,c.b,c.distanceMetres):ManualScale(c.value,c.unit);if(!Close(expected,c.metresPerPixel))throw new Exception("Calibration values disagree.");}
 }
 static bool Close(float a,float b)=>Math.Abs(a-b)<=Math.Max(.00000001,Math.Abs(b)*.00001);
 public static float ManualScale(float value,string unit){
  Range(value,.000001f,1000000,"scale entry");float scale=unit=="m/px"?value:unit=="mm/px"?value/1000:unit=="px/m"?1/value:throw new Exception("Choose m/px, mm/px or px/m.");
  Range(scale,.000001f,10,"metres per pixel");return scale;
 }
 public static float TwoPointScale(Vector2Data a,Vector2Data b,float metres){
  Range(metres,.000001f,10000,"measured distance");if(a==null||b==null)throw new Exception("Pick two calibration points.");
  double dx=(double)b.x-a.x,dy=(double)b.y-a.y,distance=Math.Sqrt(dx*dx+dy*dy);if(distance<.000001||double.IsNaN(distance)||double.IsInfinity(distance))throw new Exception("Calibration points must be distinct and finite.");
  float scale=(float)(metres/distance);Range(scale,.000001f,10,"metres per pixel");return scale;
 }
 public static bool CalibrationCurrent(FloorPlanData guide){
  var p=guide?.authoring;if(IsEmpty(p)||!p.calibration.confirmed)return false;
  return Close(guide.widthMeters/guide.pixelWidth,p.calibration.metresPerPixel)&&Close(guide.heightMeters/guide.pixelHeight,p.calibration.metresPerPixel);
 }
 public static void Confirm(FloorPlanData guide,PlanCalibration calibration){
  if(guide==null||guide.kind!="Image"||IsEmpty(guide.authoring))throw new Exception("Set up image authoring first.");
  float scale=calibration.method=="TwoPoint"?TwoPointScale(calibration.a,calibration.b,calibration.distanceMetres):ManualScale(calibration.value,calibration.unit);
  // Work on a clone so an invalid extent/point cannot partially resize the active guide.
  var next=FloorPlanCodec.Clone(guide);next.authoring.calibration=JsonUtility.FromJson<PlanCalibration>(JsonUtility.ToJson(calibration));
  next.authoring.calibration.confirmed=true;next.authoring.calibration.metresPerPixel=scale;
  FloorPlanCodec.Resize(next,next.pixelWidth*scale,next.pixelHeight*scale);FloorPlanCodec.Validate(next);
  guide.widthMeters=next.widthMeters;guide.heightMeters=next.heightMeters;guide.metersPerPixel=scale;guide.authoring=next.authoring;
 }
 // Source coordinates use pixel edges: top-left pixel centre is (.5,.5).
 // Image Y grows downward. Unity Z grows upward in the 2D plan; Y is elevation.
 public static Vector3 PixelToWorld(FloorPlanData guide,Vector2 pixel,float elevation=0){
  var local=new Vector3((pixel.x/guide.pixelWidth-.5f)*guide.widthMeters,0,(.5f-pixel.y/guide.pixelHeight)*guide.heightMeters);
  return new Vector3(guide.x,elevation,guide.z)+Quaternion.Euler(0,guide.rotation,0)*local;
 }
 public static Vector2 WorldToPixel(FloorPlanData guide,Vector3 world){
  var local=Quaternion.Euler(0,-guide.rotation,0)*(world-new Vector3(guide.x,world.y,guide.z));
  return new Vector2((local.x/guide.widthMeters+.5f)*guide.pixelWidth,(.5f-local.z/guide.heightMeters)*guide.pixelHeight);
 }
 public static void RequireWallRule(PlanColorRule rule,Design design){
  ValidateRule(rule);if(!rule.enabled||rule.classification!="Wall"||!Materials.Contains(rule.material))throw new Exception("Choose an enabled Wall rule and an explicit physical material.");
  if(design.linkWallsToRoom&&Math.Abs(rule.baseElevation+rule.height-design.height)>.0001f)throw new Exception("Wall rule top must match the linked room ceiling.");
 }
}
}
