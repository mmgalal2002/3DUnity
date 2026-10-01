using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RoomStudio {
// Optional native-v2 metadata. Distances are metres except the explicitly named mm step.
[Serializable] public class EditingPreferences {
 public float moveStep=.01f,resizeStep=.01f,thicknessStepMm=1,scaleStep=.01f;
 public bool gridVisible=true,gridSnap=false,wallSnap=false;
 public float gridSpacing=.1f,wallSnapTolerance=.02f;
 public string lengthAnchor="Center";
}
public struct SnapTarget {
 public Vector3 point;
 public string kind,wallId;
 public bool IsWall=>!string.IsNullOrEmpty(wallId);
}
// Shared by pointer gestures, property controls and deterministic editor/player checks.
// It never changes the design while finding a target. Snapping is not a wall connection.
public static class PrecisionEditing {
 public static void Validate(EditingPreferences p){
  if(p==null)return;
  Range(p.moveStep,.000001f,100,"move step");Range(p.resizeStep,.000001f,100,"resize step");
  Range(p.thicknessStepMm,.000001f,10000,"thickness step (mm)");Range(p.scaleStep,.000001f,1,"scale step");
  Range(p.gridSpacing,.000001f,100,"grid spacing");Range(p.wallSnapTolerance,.000001f,1,"wall snap tolerance");
  if(p.lengthAnchor!="Center"&&p.lengthAnchor!="Start"&&p.lengthAnchor!="End")throw new Exception("Unknown wall length anchor.");
 }
 public static void Range(float value,float min,float max,string name){
  if(float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max)throw new Exception("Invalid "+name+".");
 }
 public static bool TryNumber(string text,float min,float max,out float value){
  value=0;string s=(text??"").Trim();
  // A trailing decimal point/exponent/sign is still being typed, not a committed value.
  if(s.EndsWith(".",StringComparison.Ordinal)||s.EndsWith("e",StringComparison.OrdinalIgnoreCase)||s.EndsWith("+",StringComparison.Ordinal)||s.EndsWith("-",StringComparison.Ordinal))return false;
  return float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=min&&value<=max;
 }
 public static float Step(float value,float step,int direction){
  Range(step,.000001f,10000,"increment");return (float)((double)value+(double)step*direction);
 }
 public static bool IsEquipment(Item item){return item!=null&&(item.kind=="LINAC"||item.kind=="Desk"||item.kind=="Model"||item.kind=="Source")&&!CtShieldingData.IsPoint(item)&&!DiagnosticData.IsPoint(item);}
 public static void SetEquipmentScale(Item item,float scale){
  if(!IsEquipment(item))throw new Exception("Select equipment to change its scale.");
  SelectionEditing.RequireUnlocked(new[]{item});Range(scale,.05f,3,"equipment scale");item.scale=scale;
 }
 public static int EquipmentScaleDirection(KeyCode key,bool shift){
  if(key==KeyCode.Plus||key==KeyCode.KeypadPlus||(key==KeyCode.Equals&&shift))return 1;
  if(key==KeyCode.KeypadMinus||(key==KeyCode.Minus&&!shift))return -1;
  return 0;
 }
 public static void ScaleEquipment(IList<Item> items,float step,int direction){
  if(items==null||items.Count==0)throw new Exception("Select equipment to change its scale.");
  Range(step,.000001f,1,"scale increment");if(direction!=1&&direction!=-1)throw new Exception("Choose an increase or decrease in scale.");
  SelectionEditing.RequireUnlocked(items);var scales=new float[items.Count];
  for(int index=0;index<items.Count;index++){
   var item=items[index];if(!IsEquipment(item))throw new Exception("Select only equipment to change its scale.");
   Range(item.scale,.05f,3,"equipment scale");scales[index]=Mathf.Clamp(Step(item.scale,step,direction),.05f,3);
  }
  for(int index=0;index<items.Count;index++)items[index].scale=scales[index];
 }
 public static float EquipmentDragScale(float originalScale,Vector2 pivot,Vector2 start,Vector2 pointer){
  Range(originalScale,.05f,3,"equipment scale");var radius=start-pivot;
  if(radius.sqrMagnitude<.0001f)throw new Exception("The equipment scale handle is too close to its pivot.");
  float factor=1+Vector2.Dot(pointer-start,radius)/radius.sqrMagnitude;
  if(float.IsNaN(factor)||float.IsInfinity(factor))throw new Exception("Invalid equipment scale gesture.");
  return Mathf.Clamp(originalScale*factor,.05f,3);
 }
 public static float Grid(float value,float spacing){
  Range(spacing,.000001f,100,"grid spacing");
  // World X/Z, origin (0,0); equal half cells round away from zero on either side.
  double cells=Math.Abs((double)value/spacing),half=Math.Floor(cells)+.5;
  // Decimal spacings are float32; e.g. .25/.1 can be just below 2.5 on Mono.
  // Stabilize only the float-representation neighbourhood of a half cell.
  if(Math.Abs(cells-half)<=Math.Min(.0001,Math.Max(1,cells)*.0000001))cells=half;
  return (float)(Math.Sign(value)*Math.Round(cells,MidpointRounding.AwayFromZero)*spacing);
 }
 public static Vector3 Endpoint(Item wall,bool end){
  return new Vector3(wall.x,wall.y,wall.z)+Quaternion.Euler(0,wall.angle,0)*Vector3.right*(wall.length*(end?.5f:-.5f));
 }
 public static Vector3 ConstrainWallAngle(Vector3 origin,Vector3 target,bool enabled){
  if(!enabled)return target;var delta=target-origin;double length=Math.Sqrt((double)delta.x*delta.x+(double)delta.z*delta.z);
  if(length==0)return target;
  double increment=Math.PI/4,angle=Math.Round(Math.Atan2(delta.z,delta.x)/increment,MidpointRounding.AwayFromZero)*increment;
  return new Vector3(origin.x+(float)(length*Math.Cos(angle)),target.y,origin.z+(float)(length*Math.Sin(angle)));
 }
 public static void ResizeLength(Item wall,float length,string anchor){
  SelectionEditing.RequireUnlocked(new[]{wall});Range(length,.25f,60,"wall length");
  DoorGeometry.ValidateDimensions(wall,length,wall.height);
  if(anchor!="Center"&&anchor!="Start"&&anchor!="End")throw new Exception("Unknown wall length anchor.");
  var shift=Quaternion.Euler(0,wall.angle,0)*Vector3.right*((length-wall.length)*(anchor=="Start"?.5f:anchor=="End"?-.5f:0));
  Range(wall.x+shift.x,-10000,10000,"wall X");Range(wall.z+shift.z,-10000,10000,"wall Z");
  wall.x+=shift.x;wall.z+=shift.z;wall.length=length;
 }
 public static void ResizeEndpoint(Item wall,Vector3 fixedEnd,Vector3 movingEnd,bool movingEndIsEnd){
  SelectionEditing.RequireUnlocked(new[]{wall});
  var delta=movingEnd-fixedEnd;delta.y=0;Range(delta.magnitude,.25f,60,"wall length");
  DoorGeometry.ValidateDimensions(wall,delta.magnitude,wall.height);
  var center=(fixedEnd+movingEnd)*.5f;Range(center.x,-10000,10000,"wall X");Range(center.z,-10000,10000,"wall Z");
  if(!movingEndIsEnd)delta=-delta;
  wall.x=center.x;wall.z=center.z;wall.length=delta.magnitude;wall.angle=-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg;
 }
 static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b){
  var d=b-a;d.y=0;p.y=a.y=0;return a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(d.sqrMagnitude,.00000001f));
 }
 public static SnapTarget Snap(Vector3 point,EditingPreferences p,IList<Item> items,ISet<string> exclude=null){
  var result=new SnapTarget{point=point,kind="Free"};
  if(p.wallSnap){
   float best=p.wallSnapTolerance*p.wallSnapTolerance;int bestRank=2;
   // Endpoints take precedence over side edges. Equal targets use stable wall IDs.
   foreach(var wall in items){
    if(wall.kind!="Wall"||(exclude!=null&&exclude.Contains(wall.id)))continue;
    var a=Endpoint(wall,false);var b=Endpoint(wall,true);
    var normal=Quaternion.Euler(0,wall.angle,0)*Vector3.forward*(wall.shielding.thickness/2000f);
    for(int k=0;k<4;k++){
     var candidate=k==0?a:k==1?b:Closest(point,a+normal*(k==2?1:-1),b+normal*(k==2?1:-1));candidate.y=point.y;
     float distance=(candidate-point).sqrMagnitude;int rank=k<2?0:1;
     if(distance>p.wallSnapTolerance*p.wallSnapTolerance)continue;
     if(rank<bestRank||(rank==bestRank&&(distance<best-.0000000001f||(Mathf.Abs(distance-best)<.0000000001f&&string.CompareOrdinal(wall.id,result.wallId)<0)))){
      best=distance;bestRank=rank;result=new SnapTarget{point=candidate,kind=rank==0?"Wall endpoint":"Wall edge",wallId=wall.id};
     }
    }
   }
   if(result.IsWall)return result;
  }
  if(p.gridSnap){result.point.x=Grid(point.x,p.gridSpacing);result.point.z=Grid(point.z,p.gridSpacing);result.kind="Grid";}
  return result;
 }
 public static Vector3 DragDelta(Vector3 rawDelta,Vector3 pivot,IList<Vector3> anchors,EditingPreferences p,IList<Item> items,ISet<string> exclude,out SnapTarget target){
  target=new SnapTarget{point=pivot+rawDelta,kind="Free"};float best=float.PositiveInfinity;Vector3 correction=Vector3.zero;
  if(p.wallSnap)foreach(var anchor in anchors){
   var candidate=anchor+rawDelta;var snap=Snap(candidate,p,items,exclude);if(!snap.IsWall)continue;
   float distance=(snap.point-candidate).sqrMagnitude;
   if(distance<best){best=distance;target=snap;correction=snap.point-candidate;}
  }
  if(target.IsWall)return rawDelta+correction;
  if(p.gridSnap){target.point.x=Grid(target.point.x,p.gridSpacing);target.point.z=Grid(target.point.z,p.gridSpacing);target.kind="Grid";return target.point-pivot;}
  return rawDelta;
 }
}
}
