using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RoomStudio {
// Portable guide metadata. Parsing and decoding finish before the editor is mutated.
public static class FloorPlanCodec {
 public const int MaxFileBytes=18*1024*1024, MaxJsonBytes=25*1024*1024;
 public const int MaxDimension=8192, MaxPixels=16*1024*1024, MaxSegments=2000;
 [Serializable] sealed class GuideFile {
  public string format;
  public int version;
  public float widthMeters,heightMeters;
  public string imageBase64;
  public int pixelWidth,pixelHeight;
  public List<FloorPlanSegment> segments;
 }
 public static FloorPlanData Read(byte[] bytes,string name,string path="") {
  if(bytes==null||bytes.Length==0||bytes.Length>MaxJsonBytes)throw new Exception("Choose a non-empty floor plan smaller than 25 MB.");
  // UTF-8 BOM and whitespace are allowed on JSON; binary images never become strings.
  int offset=bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191?3:0;
  int first=offset;while(first<bytes.Length&&(bytes[first]==32||bytes[first]==9||bytes[first]==10||bytes[first]==13))first++;
  if(first<bytes.Length&&bytes[first]=='{')return ReadJson(new UTF8Encoding(false,true).GetString(bytes,offset,bytes.Length-offset),name,path);
  ImageSize(bytes,out int width,out int height);
  var result=new FloorPlanData{kind="Image",sourceName=name??"",sourcePath=path??"",pixelWidth=width,pixelHeight=height,
   widthMeters=width*.01f,heightMeters=height*.01f,metersPerPixel=.01f,imageBase64=Convert.ToBase64String(bytes)};
  Validate(result);ValidateImage(result);return result;
 }
 public static FloorPlanData ReadPayload(string payload,string name) {
  if(string.IsNullOrWhiteSpace(payload)||payload.Length>MaxJsonBytes)throw new Exception("The selected floor plan is empty or exceeds 25 MB.");
  if(payload.StartsWith("data:image/",StringComparison.OrdinalIgnoreCase)) {
   int comma=payload.IndexOf(',');
   if(comma<0||!payload.Substring(0,comma).EndsWith(";base64",StringComparison.OrdinalIgnoreCase))throw new Exception("Invalid image payload.");
   return Read(Convert.FromBase64String(payload.Substring(comma+1)),name);
  }
  return ReadJson(payload,name);
 }
 public static FloorPlanData ReadJson(string json,string name,string path="") {
  if(string.IsNullOrWhiteSpace(json)||Encoding.UTF8.GetByteCount(json)>MaxJsonBytes)throw new Exception("Floor-plan JSON must be non-empty and smaller than 25 MB.");
  var file=JsonUtility.FromJson<GuideFile>(json.TrimStart('\uFEFF',' ','\r','\n','\t'));
  if(file==null||file.format!="RoomStudio.FloorPlan"||file.version!=1)throw new Exception("Expected RoomStudio.FloorPlan version 1 JSON.");
  var result=new FloorPlanData{kind=string.IsNullOrEmpty(file.imageBase64)?"Vector":"Image",sourceName=name??"",sourcePath=path??"",
   widthMeters=file.widthMeters,heightMeters=file.heightMeters,pixelWidth=file.pixelWidth,pixelHeight=file.pixelHeight,
   imageBase64=file.imageBase64??"",segments=file.segments??new List<FloorPlanSegment>()};
  if(result.kind=="Image")result.metersPerPixel=result.widthMeters/result.pixelWidth;
  Validate(result);if(result.kind=="Image")ValidateImage(result);return result;
 }
 public static string Write(FloorPlanData guide) {
  Validate(guide);
  return JsonUtility.ToJson(new GuideFile{format="RoomStudio.FloorPlan",version=1,widthMeters=guide.widthMeters,heightMeters=guide.heightMeters,
   pixelWidth=guide.pixelWidth,pixelHeight=guide.pixelHeight,imageBase64=guide.imageBase64,segments=guide.segments},true);
 }
 public static FloorPlanData Clone(FloorPlanData guide)=>JsonUtility.FromJson<FloorPlanData>(JsonUtility.ToJson(guide));
 public static void Validate(FloorPlanData p) {
  if(p==null)return;
  if(p.kind!="Image"&&p.kind!="Vector")throw new Exception("Unsupported floor-plan guide type.");
  if(p.sourceName==null||p.sourceName.Length>260||p.sourcePath==null||p.sourcePath.Length>1000)throw new Exception("Invalid floor-plan source metadata.");
  Range(p.opacity,0,1,"opacity");Range(p.x,-10000,10000,"position X");Range(p.z,-10000,10000,"position Z");Range(p.rotation,-3600,3600,"rotation");
  Range(p.widthMeters,.001f,10000,"width");Range(p.heightMeters,.001f,10000,"height");
  if(p.kind=="Image") {
   Range(p.metersPerPixel,.000001f,10,"metres per pixel");
   if(string.IsNullOrEmpty(p.imageBase64)||p.imageBase64.Length>MaxJsonBytes)throw new Exception("Floor-plan image data is missing or too large.");
   byte[] bytes;try{bytes=Convert.FromBase64String(p.imageBase64);}catch(FormatException){throw new Exception("Floor-plan image data is not valid base64.");}
   ImageSize(bytes,out int width,out int height);
   if(width!=p.pixelWidth||height!=p.pixelHeight)throw new Exception("Floor-plan image dimensions do not match its declared pixel size.");
   if(Mathf.Abs(p.widthMeters/p.pixelWidth-p.metersPerPixel)>Mathf.Max(.0000001f,p.metersPerPixel*.00001f))throw new Exception("Floor-plan width and metres-per-pixel scale disagree.");
   if(p.segments!=null&&p.segments.Count>0)throw new Exception("An image guide cannot also contain vector segments.");
  } else {
   if(!string.IsNullOrEmpty(p.imageBase64))throw new Exception("A vector guide cannot contain image data.");
   if(p.segments==null||p.segments.Count<1||p.segments.Count>MaxSegments)throw new Exception("A vector guide needs 1 to 2000 segments.");
   foreach(var s in p.segments) {
    if(s==null||s.start==null||s.end==null)throw new Exception("Missing floor-plan segment endpoint.");
    foreach(var point in new[]{s.start,s.end}) {
     Range(point.x,-p.widthMeters/2-.0001f,p.widthMeters/2+.0001f,"segment X within centred extent");
     Range(point.y,-p.heightMeters/2-.0001f,p.heightMeters/2+.0001f,"segment Y within centred extent");
    }
    if(s.start.x==s.end.x&&s.start.y==s.end.y)throw new Exception("Floor-plan segments must have length.");
   }
  }
  PlanAuthoring.Validate(p.authoring,p,true);
 }
 public static void Resize(FloorPlanData p,float width,float height) {
  Range(width,.001f,10000,"width");Range(height,.001f,10000,"height");
  if(p.kind=="Vector")foreach(var s in p.segments)foreach(var point in new[]{s.start,s.end}){point.x*=width/p.widthMeters;point.y*=height/p.heightMeters;}
  else p.metersPerPixel=width/p.pixelWidth;
  p.widthMeters=width;p.heightMeters=height;
 }
 public static Texture2D DecodeImage(FloorPlanData p) {
  var bytes=Convert.FromBase64String(p.imageBase64);ImageSize(bytes,out int width,out int height);
  if(width!=p.pixelWidth||height!=p.pixelHeight)throw new Exception("Floor-plan image dimensions do not match the declared pixel size.");
  var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Floor-plan image",wrapMode=TextureWrapMode.Clamp};
  try {
   if(!texture.LoadImage(bytes,false)||texture.width!=width||texture.height!=height)throw new Exception("Floor-plan image could not be decoded.");
   return texture;
  } catch {Release(texture);throw;}
 }
 public static void ValidateImage(FloorPlanData p){Release(DecodeImage(p));}
 static void Release(UnityEngine.Object obj){if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
 static void Range(float value,float min,float max,string label){if(float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max)throw new Exception("Invalid floor-plan "+label+".");}
 static uint U32(byte[] b,int offset)=>(uint)b[offset]<<24|(uint)b[offset+1]<<16|(uint)b[offset+2]<<8|b[offset+3];
 // Bound decoded allocation BEFORE Unity's native image decoder sees the bytes.
 public static void ImageSize(byte[] bytes,out int width,out int height) {
  width=height=0;
  if(bytes==null||bytes.Length<24||bytes.Length>MaxFileBytes)throw new Exception("PNG/JPEG images must be non-empty and at most 18 MB.");
  if(bytes[0]==137&&bytes[1]==80&&bytes[2]==78&&bytes[3]==71&&bytes[4]==13&&bytes[5]==10&&bytes[6]==26&&bytes[7]==10) {
   if(U32(bytes,8)!=13||U32(bytes,12)!=0x49484452)throw new Exception("Invalid PNG header.");
   uint w=U32(bytes,16),h=U32(bytes,20);if(w>MaxDimension||h>MaxDimension)throw new Exception("Image dimensions exceed 8192 pixels.");width=(int)w;height=(int)h;
  } else if(bytes[0]==255&&bytes[1]==216) {
   int at=2;
   while(at+3<bytes.Length) {
    if(bytes[at++]!=255)throw new Exception("Invalid JPEG header.");
    while(at<bytes.Length&&bytes[at]==255)at++;
    if(at>=bytes.Length)break;int marker=bytes[at++];
    if(marker==217||marker==218)break;
    if(marker==1||(marker>=208&&marker<=215))continue;
    if(at+1>=bytes.Length)break;int length=(bytes[at]<<8)|bytes[at+1];
    if(length<2||length>bytes.Length-at)throw new Exception("Truncated JPEG header.");
    if(marker>=192&&marker<=207&&marker!=196&&marker!=200&&marker!=204) {
     if(length<8)throw new Exception("Invalid JPEG frame.");height=(bytes[at+3]<<8)|bytes[at+4];width=(bytes[at+5]<<8)|bytes[at+6];break;
    }
    at+=length;
   }
  } else throw new Exception("Unsupported image. Use PNG or JPEG, or RoomStudio.FloorPlan JSON.");
  if(width<1||height<1||width>MaxDimension||height>MaxDimension||(long)width*height>MaxPixels)throw new Exception("Image limit: 8192 pixels per side and 16 megapixels total.");
 }
}
}
