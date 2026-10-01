using System;
using System.IO;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 const string FloorPlanTestJson="{\"format\":\"RoomStudio.FloorPlan\",\"version\":1,\"widthMeters\":12,\"heightMeters\":8,\"segments\":[{\"start\":{\"x\":-6,\"y\":-4},\"end\":{\"x\":6,\"y\":-4}},{\"start\":{\"x\":6,\"y\":-4},\"end\":{\"x\":6,\"y\":4}}]}";
 static void FloorPlanRequire(bool condition,string message){if(!condition)throw new Exception("Floor plan: "+message);}
 void FloorPlanSmokeChecks() {
  string original=JsonUtility.ToJson(design);bool previousTop=top;string previousName=fileName;
  try {
   design.floorPlan=null;Commit();top=true;Rebuild();
   LoadJson(JsonUtility.ToJson(design),"Floor-plan-legacy");FloorPlanRequire(floorPlan==null,"legacy version 2 guide omission");
   int count=design.items.Count,index=historyIndex;
   var image=new Texture2D(4,2,TextureFormat.RGB24,false);
   image.SetPixels(new[]{Color.red,Color.red,Color.white,Color.white,Color.blue,Color.blue,Color.white,Color.white});image.Apply();
   byte[] png=image.EncodeToPNG(),jpeg=image.EncodeToJPG();Destroy(image);
   ApplyFloorPlanPayload("data:image/png;base64,"+Convert.ToBase64String(png),"smoke.png");
   FloorPlanRequire(floorPlan.kind=="Image"&&Mathf.Abs(floorPlan.widthMeters-.04f)<.00001f&&Mathf.Abs(floorPlan.heightMeters-.02f)<.00001f,"source-pixel scale");
   FloorPlanRequire(historyIndex==index+1,"import must create an undo step");
   Undo(-1);FloorPlanRequire(floorPlan==null,"undo image import");Undo(1);
   FloorPlanRequire(floorPlan!=null&&floorPlanGuideObject!=null,"redo image import");
   var cached=floorPlanTexture;
   var next=FloorPlanCodec.Clone(floorPlan);FloorPlanCodec.Resize(next,8,4);next.opacity=.6f;next.x=1.5f;next.z=-.5f;next.rotation=22;
   ReplaceFloorPlan(next,"Smoke settings");
   FloorPlanRequire(floorPlanTexture==cached,"display changes decoded image again");
   FloorPlanRequire(Mathf.Abs(floorPlanMaterial.color.a-.6f)<.00001f&&Mathf.Abs(floorPlanMesh.bounds.size.x-8)<.00001f,"rendered opacity / dimensions");
   FloorPlanRequire(floorPlanGuideObject.GetComponent<Collider>()==null&&floorPlanGuideObject.GetComponent<Selectable>()==null&&design.items.Count==count,"guide must not intercept selection or become geometry");
   top=false;FloorPlanUpdate();FloorPlanRequire(!floorPlanGuideObject.activeSelf,"guide visible in 3D");
   top=true;FloorPlanUpdate();FloorPlanRequire(floorPlanGuideObject.activeSelf,"guide missing in 2D");
   string saved=JsonUtility.ToJson(design);LoadJson(saved,"Floor-plan-smoke");
   FloorPlanRequire(JsonUtility.ToJson(design)==saved,"native embedded image and settings round-trip");
   string path=Path.Combine(SaveDirectory,"Floor-plan-smoke.json");File.WriteAllText(path,saved);LoadJson(File.ReadAllText(path),"Floor-plan-smoke");
   FloorPlanRequire(JsonUtility.ToJson(design)==saved,"filesystem save/load");
   foreach(var bad in new[]{"{}","{\"format\":\"RoomStudio.FloorPlan\",\"version\":1}","{\"format\":\"RoomStudio.FloorPlan\",\"version\":1,\"widthMeters\":1,\"heightMeters\":1,\"pixelWidth\":2,\"pixelHeight\":2,\"imageBase64\":\"not-an-image\"}"}) {
    bool rejected=false;index=historyIndex;try{ApplyFloorPlanJson(bad,"bad.json");}catch{rejected=true;}
    FloorPlanRequire(rejected&&historyIndex==index&&JsonUtility.ToJson(design)==saved,"invalid import mutated design/history");
   }
   ApplyFloorPlanBytes(jpeg,"smoke.jpg");FloorPlanRequire(floorPlan.pixelWidth==4&&floorPlan.pixelHeight==2,"JPEG import");
   ApplyFloorPlanJson(FloorPlanTestJson,"smoke-vector.json");
   var vector=FloorPlanCodec.Clone(floorPlan);FloorPlanCodec.Resize(vector,24,16);ReplaceFloorPlan(vector,"Scaled vector");
   FloorPlanRequire(floorPlan.segments[0].end.x==12&&floorPlan.segments[0].start.y==-8,"vector resize must scale trace coordinates");
   FloorPlanRequire(design.items.Count==count,"vector became a design item");
   var exported=FloorPlanCodec.ReadJson(FloorPlanCodec.Write(floorPlan),"exported.json");
   FloorPlanRequire(exported.widthMeters==24&&exported.segments[0].end.x==12,"guide JSON export/import");
   var start=floorPlanGuideObject.transform.TransformPoint(new Vector3(-12,0,-8));
   var end=floorPlanGuideObject.transform.TransformPoint(new Vector3(12,0,-8));
   Add(Design.Wall("Traced wall",(start.x+end.x)/2,(start.z+end.z)/2,Vector3.Distance(start,end),0,design.height));
   FloorPlanRequire(Current.length==24&&design.items.Count==count+1,"scaled tracing");Undo(-1);
   ReplaceFloorPlan(null,"Clear guide");FloorPlanRequire(floorPlan==null&&floorPlanGuideObject==null,"clear guide");
   Undo(-1);FloorPlanRequire(floorPlan!=null&&floorPlanGuideObject!=null,"undo clear");Undo(1);FloorPlanRequire(floorPlan==null,"redo clear");
   Debug.Log("ROOM_STUDIO_FLOORPLAN_RUNTIME_PASSED: PNG/JPEG, scale, opacity, undo/redo, invalid replacement, 2D-only guide, scaled vector tracing, guide export and native filesystem persistence");
  } finally {design=JsonUtility.FromJson<Design>(original);top=previousTop;fileName=previousName;ClearSelection();ClearFloorPlanTexture();Commit();Rebuild();}
 }
}
}
