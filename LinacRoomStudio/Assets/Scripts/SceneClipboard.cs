using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
// This clipboard belongs to the scene editor. It never reads or replaces the OS text clipboard.
[Serializable] public class SceneClipboardPayload {
 public string primaryId="";
 public List<Item> items=new List<Item>();
}
public class ScenePasteResult {
 public string primaryId;
 public List<Item> items;
}
public static class SceneClipboard {
 public const float PasteOffsetMetres=.5f;
 static Item Clone(Item item)=>JsonUtility.FromJson<Item>(JsonUtility.ToJson(item));
 static string FreshId(HashSet<string> used){string id;do{id=Guid.NewGuid().ToString();}while(!used.Add(id));return id;}
 public static SceneClipboardPayload Copy(Design design,IEnumerable<string> selectedIds,string primaryId){
  var expanded=SelectionEditing.Expand(design,selectedIds);
  var items=design.items.Where(item=>expanded.Contains(item.id)).ToList();
  if(items.Count==0)throw new Exception("Select a scene object before copying.");
    if(items.Any(item=>CtShieldingData.IsPoint(item)&&item.ctPoint.role=="Scatter"))throw new Exception("Copy the CT scanner as an unconfigured device; attached scatter points cannot be copied independently.");
  return new SceneClipboardPayload{
   primaryId=expanded.Contains(primaryId)?primaryId:items[0].id,
   items=items.Select(Clone).ToList()
  };
 }
 public static ScenePasteResult PreparePaste(Design design,SceneClipboardPayload payload,int pasteNumber){
  if(design==null||design.items==null||payload==null||payload.items==null||payload.items.Count==0)throw new Exception("Copy a scene object before pasting.");
  if(pasteNumber<1)throw new Exception("Invalid paste offset.");
  if(design.items.Count+payload.items.Count>250)throw new Exception("Maximum 250 objects per project.");
  double offset=(double)PasteOffsetMetres*pasteNumber;
  var usedIds=new HashSet<string>(design.items.Select(item=>item.id));
  if(design.regions!=null)foreach(var region in design.regions)usedIds.Add(region.id);
  foreach(var item in design.items)if(item.doors!=null)foreach(var door in item.doors)usedIds.Add(door.id);
  var usedGroups=new HashSet<string>(design.items.Where(item=>!string.IsNullOrEmpty(item.groupId)).Select(item=>item.groupId));
  var groupMap=new Dictionary<string,string>(StringComparer.Ordinal);
  var itemMap=new Dictionary<string,string>(StringComparer.Ordinal);
  var copies=new List<Item>(payload.items.Count);
  foreach(var source in payload.items){
   if(source==null||string.IsNullOrEmpty(source.id)||itemMap.ContainsKey(source.id))throw new Exception("Copied selection is invalid.");
   double x=source.x+offset,z=source.z+offset;
   if(double.IsNaN(x)||double.IsInfinity(x)||Math.Abs(x)>10000||double.IsNaN(z)||double.IsInfinity(z)||Math.Abs(z)>10000)
    throw new Exception("Pasted objects would be outside the supported range.");
   var copy=Clone(source);
   copy.id=FreshId(usedIds);itemMap.Add(source.id,copy.id);
   copy.name=(string.IsNullOrEmpty(source.name)?source.kind:source.name)+(pasteNumber==1?" copy":" copy "+pasteNumber);
   copy.x=(float)x;copy.z=(float)z;copy.locked=false;
   // A pasted wall is an independent manual object. Reusing the source path
   // reference would make two item IDs claim the same generated wall.
   copy.generated=null;
    if(CtShieldingData.IsRoi(copy))foreach(var contribution in copy.ctPoint.roi.contributions){contribution.hasDirectKerma=false;contribution.hasKermaPosition=false;}
   if(string.IsNullOrEmpty(source.groupId))copy.groupId="";
   else{
    if(!groupMap.TryGetValue(source.groupId,out string groupId)){groupId=FreshId(usedGroups);groupMap.Add(source.groupId,groupId);}
    copy.groupId=groupId;
   }
   if(copy.doors!=null)foreach(var door in copy.doors)door.id=FreshId(usedIds);
   if(copy.kind=="Wall"&&copy.doors!=null&&copy.doors.Count>0)DoorGeometry.ValidateWall(copy);
   if(copy.kind=="Component")ComponentGeometry.Validate(copy.component);
   copies.Add(copy);
  }
  return new ScenePasteResult{items=copies,primaryId=itemMap.TryGetValue(payload.primaryId,out string primary)?primary:copies[0].id};
 }
 public static ScenePasteResult Paste(Design design,SceneClipboardPayload payload,int pasteNumber){
  var prepared=PreparePaste(design,payload,pasteNumber);
  design.items.AddRange(prepared.items);
  return prepared;
 }
}
}
