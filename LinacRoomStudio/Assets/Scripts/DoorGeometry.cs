using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomStudio {
// Door positions are measured along a wall's local X axis from its centre, in metres.
// A door belongs to its wall, so ordinary wall moves and rotations retain the cutout.
[Serializable] public class DoorOpening {
 public string id=Guid.NewGuid().ToString(), name="Door";
 public float center, width=1, height=2.1f;
 public Barrier panel=new Barrier{material="Steel",thickness=45,density=7850};
 public float leadLiningMm=0;
}

public struct WallSolid {
 public float center,width,bottom,height;
 public WallSolid(float center,float width,float bottom,float height){this.center=center;this.width=width;this.bottom=bottom;this.height=height;}
}

public static class DoorGeometry {
 public const int MaxDoorsPerWall=8;
 const float MinJamb=.1f,MinPier=.08f;
 static void Range(float value,float low,float high,string label){
  if(float.IsNaN(value)||float.IsInfinity(value)||value<low||value>high)throw new Exception(label+" must be between "+low+" and "+high+".");
 }
 public static DoorOpening New(float center,float height){return new DoorOpening{center=center,height=Mathf.Min(2.1f,height)};}
 public static float MinimumWidth(float wallLength){return Mathf.Min(.5f,wallLength*.5f);}
 static float Jamb(float wallLength){return Mathf.Min(MinJamb,wallLength*.05f);}
 public static float PlacementWidth(float requestedWidth,float wallLength){
  Range(wallLength,.25f,10000,"Wall length");Range(requestedWidth,.05f,4,"Door width");
  return requestedWidth>wallLength?wallLength*.9f:requestedWidth;
 }
 public static void ValidateWall(Item wall){
  if(wall==null||wall.kind!="Wall")throw new Exception("Doors can only be placed in walls.");
  ValidateDimensions(wall,wall.length,wall.height);
 }
 public static void ValidateDimensions(Item wall,float length,float height){
  if(wall.doors==null||wall.doors.Count==0)return;
  if(wall.kind!="Wall")throw new Exception("Only a wall can own door openings.");
  Range(length,.25f,10000,"Wall length");Range(height,.001f,100,"Wall height");
  if(wall.doors.Count>MaxDoorsPerWall)throw new Exception("A wall supports at most eight doors.");
  var ids=new HashSet<string>();var sorted=new List<DoorOpening>(wall.doors);
  sorted.Sort((a,b)=>a==null?(b==null?0:-1):(b==null?1:a.center.CompareTo(b.center)));
  float end=-length/2;
  foreach(var door in sorted){
   if(door==null||string.IsNullOrWhiteSpace(door.id)||!ids.Add(door.id)||string.IsNullOrWhiteSpace(door.name)||door.name.Length>128)throw new Exception("Invalid or duplicate door record.");
    Range(door.center,-10000,10000,"Door position");Range(door.width,MinimumWidth(length),4,"Door width");Range(door.height,.5f,height,"Door height");
    if(door.panel==null||Array.IndexOf(new[]{"Concrete","Steel","Lead","Gypsum","Glass","PlateGlass","Wood"},door.panel.material)<0)throw new Exception("Choose a supported physical door material.");
   Range(door.panel.thickness,1,3000,"Door panel thickness (mm)");Range(door.panel.density,100,25000,"Door density (kg/m3)");Range(door.leadLiningMm,0,100,"Door lead lining (mm)");
   float start=door.center-door.width/2,finish=door.center+door.width/2;
    float jamb=Jamb(length);
    if(start< -length/2+jamb-0.00001f||finish>length/2-jamb+0.00001f)throw new Exception("Leave at least "+jamb+" m of wall at both ends of the door.");
   if(start<end+(end==-length/2?0:MinPier)-0.00001f)throw new Exception("Doors must not overlap; leave at least 0.08 m of wall between them.");
   end=finish;
  }
 }
 public static float LocalCenter(Item wall,Vector3 worldPoint){
  var local=Quaternion.Inverse(Quaternion.Euler(0,wall.angle,0))*(worldPoint-new Vector3(wall.x,wall.y,wall.z));
  if(Mathf.Abs(local.z)>Mathf.Max(.4f,wall.shielding.thickness/2000f+.2f))throw new Exception("Click close to the selected wall to place its door.");
  return local.x;
 }
 public static void Add(Item wall,DoorOpening door){
  if(wall==null||wall.kind!="Wall"||wall.locked)throw new Exception("Select and unlock a wall before placing a door.");
    if(door==null)throw new Exception("Choose a door to place.");
    float width=PlacementWidth(door.width,wall.length);
    if(width!=door.width){
     Range(door.center,-wall.length/2,wall.length/2,"Door placement point");
     door=JsonUtility.FromJson<DoorOpening>(JsonUtility.ToJson(door));door.width=width;
     float limit=Mathf.Max(0,wall.length/2-Jamb(wall.length)-width/2);door.center=Mathf.Clamp(door.center,-limit,limit);
    }
  var previous=wall.doors;wall.doors=previous==null?new List<DoorOpening>():new List<DoorOpening>(previous);
  wall.doors.Add(door);
  try{ValidateWall(wall);}catch{wall.doors=previous;throw;}
 }
 public static void Update(Item wall,string id,DoorOpening replacement){
  if(wall==null||wall.kind!="Wall"||wall.locked)throw new Exception("Unlock the wall before editing its doors.");
  int index=wall.doors==null?-1:wall.doors.FindIndex(x=>x.id==id);if(index<0)throw new Exception("Door no longer exists.");
  var previous=wall.doors;wall.doors=new List<DoorOpening>(previous);wall.doors[index]=replacement;
  try{ValidateWall(wall);}catch{wall.doors=previous;throw;}
 }
 public static void Remove(Item wall,string id){
  if(wall==null||wall.kind!="Wall"||wall.locked)throw new Exception("Unlock the wall before removing its doors.");
  int index=wall.doors==null?-1:wall.doors.FindIndex(x=>x.id==id);if(index<0)throw new Exception("Door no longer exists.");
  var next=new List<DoorOpening>(wall.doors);next.RemoveAt(index);wall.doors=next;
 }
 public static List<WallSolid> Solids(Item wall,float displayedHeight){
  ValidateWall(wall);Range(displayedHeight,.001f,wall.height,"Displayed wall height");
  var result=new List<WallSolid>();
  if(wall.doors==null||wall.doors.Count==0){result.Add(new WallSolid(0,wall.length,0,displayedHeight));return result;}
  var doors=new List<DoorOpening>(wall.doors);doors.Sort((a,b)=>a.center.CompareTo(b.center));
  float cursor=-wall.length/2;
  foreach(var door in doors){
   float start=door.center-door.width/2,end=door.center+door.width/2;
   if(start>cursor+0.00001f)result.Add(new WallSolid((cursor+start)/2,start-cursor,0,displayedHeight));
   if(displayedHeight>door.height+0.00001f)result.Add(new WallSolid(door.center,door.width,door.height,displayedHeight-door.height));
   cursor=end;
  }
  if(cursor<wall.length/2-0.00001f)result.Add(new WallSolid((cursor+wall.length/2)/2,wall.length/2-cursor,0,displayedHeight));
  return result;
 }
}
}
