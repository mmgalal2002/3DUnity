using System;
using System.Collections.Generic;
using RoomStudio;
using UnityEngine;

public static class FloorPlanChecks {
 static void Require(bool condition,string message){if(!condition)throw new Exception("Floor-plan regression: "+message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Require(rejected,message);}
 public static void Run(){
  var design=Design.Example();var originalItems=design.items.Count;
  var legacy=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Migrate(legacy);Design.Validate(legacy);Require(legacy.items.Count==originalItems,"version-2 design without a guide");
  design.floorPlan=new FloorPlanData{kind="Vector",sourceName="trace.json",widthMeters=12,heightMeters=8,opacity=.4f,x=1,z=-2,rotation=15,visible=true,segments=new List<FloorPlanSegment>{new FloorPlanSegment{start=new Vector2Data(-6,-4),end=new Vector2Data(6,-4)}}};
  Design.Validate(design);var restored=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(restored);Require(restored.floorPlan!=null&&restored.floorPlan.kind=="Vector"&&restored.floorPlan.segments.Count==1,"vector native round-trip");Require(restored.items.Count==originalItems,"guide became a scene item");
  var invalid=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));invalid.floorPlan.kind="Image";invalid.floorPlan.pixelWidth=2;invalid.floorPlan.pixelHeight=2;invalid.floorPlan.widthMeters=1;invalid.floorPlan.heightMeters=1;invalid.floorPlan.metersPerPixel=.5f;invalid.floorPlan.imageBase64="not-base64";Reject(()=>Design.Validate(invalid),"malformed image base64 accepted");
  invalid=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));invalid.floorPlan.segments[0].start.x=float.NaN;Reject(()=>Design.Validate(invalid),"non-finite vector coordinate accepted");
  invalid=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));invalid.floorPlan.opacity=2;Reject(()=>Design.Validate(invalid),"out-of-range guide opacity accepted");
  Reject(()=>FloorPlanCodec.ReadJson("{}","bad.json"),"missing format accepted");
  Reject(()=>FloorPlanCodec.ReadJson("{\"format\":\"RoomStudio.FloorPlan\",\"version\":1}","bad.json"),"missing extent/segments accepted");
  invalid=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));invalid.floorPlan.segments.Clear();Reject(()=>Design.Validate(invalid),"empty vector accepted");
  invalid=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));invalid.floorPlan.segments[0].end.x=7;Reject(()=>Design.Validate(invalid),"segment outside declared extent accepted");
  var texture=new Texture2D(4,2,TextureFormat.RGB24,false);texture.SetPixels(new[]{Color.red,Color.white,Color.white,Color.blue,Color.red,Color.white,Color.white,Color.blue});texture.Apply();
  byte[] png=texture.EncodeToPNG(),jpg=texture.EncodeToJPG();UnityEngine.Object.DestroyImmediate(texture);
  foreach(var bytes in new[]{png,jpg}){var guide=FloorPlanCodec.Read(bytes,"image");Require(guide.pixelWidth==4&&guide.pixelHeight==2&&Mathf.Abs(guide.widthMeters-.04f)<.00001f,"image scale or decoding");var copy=FloorPlanCodec.ReadJson(FloorPlanCodec.Write(guide),"copy");Require(copy.imageBase64==guide.imageBase64,"embedded image guide export");guide.pixelWidth=5;Reject(()=>FloorPlanCodec.Validate(guide),"mismatched image header accepted");}
  var huge=(byte[])png.Clone();huge[16]=0;huge[17]=1;huge[18]=0;huge[19]=0;Reject(()=>FloorPlanCodec.Read(huge,"huge.png"),"oversized allocation not rejected before decode");
  var inconsistent=FloorPlanCodec.Read(png,"image.png");inconsistent.metersPerPixel=1;Reject(()=>FloorPlanCodec.Validate(inconsistent),"inconsistent scale accepted");
  var resized=FloorPlanCodec.Clone(design.floorPlan);FloorPlanCodec.Resize(resized,24,16);FloorPlanCodec.Validate(resized);Require(resized.segments[0].end.x==12&&resized.segments[0].start.y==-8,"vector coordinates did not resize");
  Require(Resources.Load<Shader>("FloorPlanGuide")!=null,"guide shader not included");
  Debug.Log("ROOM_STUDIO_FLOORPLAN_CHECKS_PASSED: legacy/native/guide round-trips, PNG/JPEG dimensions, scale, malformed files, bounded allocation, vector resize and shader inclusion");
 }
}
