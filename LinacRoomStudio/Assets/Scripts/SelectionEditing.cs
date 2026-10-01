using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
// Selection is transient; group membership and appearance belong to the saved design.
public static class SelectionEditing {
 public static void RequireUnlocked(IEnumerable<Item> items){
  if(items.Any(i=>i.locked))throw new Exception("Selection contains locked objects. Unlock objects before editing or deleting.");
 }
 public static bool ToggleLock(Design design,IEnumerable<string> ids){
  var expanded=Expand(design,ids);var items=design.items.Where(i=>expanded.Contains(i.id)).ToList();
  bool value=!items.All(i=>i.locked);SetLock(design,ids,value);return value;
 }
 public static void SetLock(Design design,IEnumerable<string> ids,bool locked){
  var expanded=Expand(design,ids);
  foreach(var item in design.items)if(expanded.Contains(item.id))item.locked=locked;
 }
 public static HashSet<string> Expand(Design design,IEnumerable<string> ids){
  var selected=new HashSet<string>(ids);
  var groups=new HashSet<string>(design.items.Where(i=>selected.Contains(i.id)&&!string.IsNullOrEmpty(i.groupId)).Select(i=>i.groupId));
  return new HashSet<string>(design.items.Where(i=>selected.Contains(i.id)||(!string.IsNullOrEmpty(i.groupId)&&groups.Contains(i.groupId))).Select(i=>i.id));
 }
 public static Vector3 Center(IList<Item> items){
  return items.Count==0?Vector3.zero:new Vector3(items.Average(i=>i.x),items.Average(i=>i.y),items.Average(i=>i.z));
 }
 public static void Move(Design design,IList<Item> items,Vector3 delta){
  RequireUnlocked(items);
    CtShieldingData.RequireTransform(design,items);
  if(delta.y!=0&&design.linkWallsToRoom&&items.Any(i=>i.kind=="Wall"))throw new Exception("Unlink walls from room size before moving them vertically as a group.");
  foreach(var item in items)CheckPosition(new Vector3(item.x,item.y,item.z)+delta);
  foreach(var item in items){item.x+=delta.x;item.y+=delta.y;item.z+=delta.z;}
 }
 public static void Rotate(IList<Item> items,float degrees,Design design=null){
  RequireUnlocked(items);
  CtShieldingData.RequireTransform(design,items);
  if(float.IsNaN(degrees)||float.IsInfinity(degrees))throw new Exception("Invalid rotation.");
  var center=Center(items);var rotation=Quaternion.Euler(0,degrees,0);
  var positions=items.Select(i=>center+rotation*(new Vector3(i.x,i.y,i.z)-center)).ToArray();
  foreach(var position in positions)CheckPosition(position);
  for(int n=0;n<items.Count;n++){items[n].x=positions[n].x;items[n].z=positions[n].z;items[n].angle=Mathf.Repeat(items[n].angle+degrees,360);}
 }
 public static void Group(Design design,IEnumerable<string> ids){
  var expanded=Expand(design,ids);if(expanded.Count<2)return;
  RequireUnlocked(design.items.Where(i=>expanded.Contains(i.id)));
    CtShieldingData.RequireTransform(design,design.items.Where(i=>expanded.Contains(i.id)));
  string group=Guid.NewGuid().ToString();foreach(var item in design.items)if(expanded.Contains(item.id))item.groupId=group;
 }
 public static void Ungroup(Design design,IEnumerable<string> ids){
  var expanded=Expand(design,ids);RequireUnlocked(design.items.Where(i=>expanded.Contains(i.id)));foreach(var item in design.items)if(expanded.Contains(item.id))item.groupId="";
 }
 public static void Remove(Design design,IEnumerable<string> ids){
    var expanded=CtShieldingData.RemovalIds(design,Expand(design,ids));CtShieldingData.RemoveReferences(design,expanded);design.items.RemoveAll(i=>expanded.Contains(i.id));
 }
 static void CheckPosition(Vector3 p){
  if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z)||Mathf.Abs(p.x)>10000||Mathf.Abs(p.y)>10000||Mathf.Abs(p.z)>10000)throw new Exception("Movement would put an object outside the supported range.");
 }
}
}
