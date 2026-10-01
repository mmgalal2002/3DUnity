using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
// Cooperative CPU work only. Call Step from an editor/player coroutine or Update;
// it neither accesses Unity objects nor changes scene/native design data.
public sealed class PlanColorMask {
 public readonly int width,height;
 public readonly PlanColorRule[] rules;
 public readonly byte[] owners; // 0 = unmatched/filtered, otherwise rule index + 1.
 public readonly int[] matched,retained,components,rejectedComponents;
 public int overlaps,transparent,removedPixels;
 public bool Done {get;private set;}
 public double ElapsedMilliseconds=>watch.Elapsed.TotalMilliseconds;
 public string Phase=>scan==null?"Classifying colors":"Filtering connected components";
 public float Progress=>Done?1:scan==null?.5f*classified/owners.Length:.5f+.5f*visitedCount/owners.Length;
 readonly Color32[] pixels;
 readonly float alpha;
 readonly Vector3[] targets;
 readonly Stopwatch watch=new Stopwatch();
 int classified,search,head,tail,visitedCount;
 byte componentRule;
 bool[] scan;
 int[] queue;
 public PlanColorMask(Color32[] pixels,int width,int height,PlanAuthoringData model){
  if(width<1||height<1||width>FloorPlanCodec.MaxDimension||height>FloorPlanCodec.MaxDimension||(long)width*height>PlanAuthoring.MaxProcessingPixels||pixels==null||pixels.Length!=(long)width*height)throw new Exception("Color processing supports at most 4 megapixels and 8192 pixels per side.");
  if(model==null||model.rules==null||model.rules.Count>PlanAuthoring.MaxRules)throw new Exception("At most 32 color rules are supported.");
  foreach(var rule in model.rules)PlanAuthoring.ValidateRule(rule);
  if(model.rules.Select(r=>r.id).Distinct().Count()!=model.rules.Count)throw new Exception("Duplicate rule identity.");
  PrecisionEditing.Range(model.alphaThreshold,0,1,"alpha threshold");
  this.pixels=pixels;this.width=width;this.height=height;alpha=model.alphaThreshold;
  rules=model.rules.Where(r=>r.enabled).OrderBy(r=>r.priority).ThenBy(r=>r.id,StringComparer.Ordinal).Select(r=>JsonUtility.FromJson<PlanColorRule>(JsonUtility.ToJson(r))).ToArray();
  if(rules.Length==0)throw new Exception("Enable at least one color rule.");
  owners=new byte[pixels.Length];targets=rules.Select(r=>r.colorSpace=="HSV"?Hsv(r.target):new Vector3(r.target.r,r.target.g,r.target.b)).ToArray();
  matched=new int[rules.Length];retained=new int[rules.Length];components=new int[rules.Length];rejectedComponents=new int[rules.Length];
 }
 static Vector3 Hsv(Color c){Color.RGBToHSV(c,out float h,out float s,out float v);return new Vector3(h,s,v);}
 public static bool Matches(Color32 pixel,PlanColorRule rule){
  var c=(Color)pixel;var value=rule.colorSpace=="HSV"?Hsv(c):new Vector3(c.r,c.g,c.b);var target=rule.colorSpace=="HSV"?Hsv(rule.target):new Vector3(rule.target.r,rule.target.g,rule.target.b);
  return DistanceSquared(value,target,rule.colorSpace=="HSV")<=rule.tolerance*rule.tolerance;
 }
 static float DistanceSquared(Vector3 value,Vector3 target,bool hsv){var d=value-target;if(hsv){float h=Math.Abs(d.x);d.x=2*Math.Min(h,1-h);}return d.sqrMagnitude/3;}
 public bool Step(int budget=4096){
  if(budget<1||budget>65536)throw new ArgumentOutOfRangeException(nameof(budget));if(Done)return true;
  watch.Start();try{while(budget-->0&&!Done){
   if(classified<owners.Length){
    int index=classified++;var color=(Color)pixels[index];if(color.a<=0||color.a<alpha){transparent++;continue;}
    var rgb=new Vector3(color.r,color.g,color.b);Vector3 hsv=default;bool haveHsv=false;int first=-1,hits=0;
    for(int r=0;r<rules.Length;r++){
     bool useHsv=rules[r].colorSpace=="HSV";if(useHsv&&!haveHsv){hsv=Hsv(color);haveHsv=true;}
     if(DistanceSquared(useHsv?hsv:rgb,targets[r],useHsv)>rules[r].tolerance*rules[r].tolerance)continue;
     if(first<0)first=r;hits++;
    }
    if(first>=0){owners[index]=(byte)(first+1);matched[first]++;}if(hits>1)overlaps++;
    continue;
   }
   if(scan==null){scan=new bool[owners.Length];queue=new int[owners.Length];}
   if(head<tail){
    int index=queue[head++],x=index%width,y=index/width;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
     int nx=x+dx,ny=y+dy;if((dx==0&&dy==0)||nx<0||nx>=width||ny<0||ny>=height)continue;
     int next=ny*width+nx;if(!scan[next]&&owners[next]==componentRule){scan[next]=true;visitedCount++;queue[tail++]=next;}
    }
    if(head==tail){int r=componentRule-1;if(tail<rules[r].minAreaPixels){for(int i=0;i<tail;i++)owners[queue[i]]=0;removedPixels+=tail;rejectedComponents[r]++;}else{retained[r]+=tail;components[r]++;}}
   }else{
    if(search==owners.Length){Done=true;break;}
    int index=search++;if(scan[index])continue;scan[index]=true;visitedCount++;
    if(owners[index]!=0){componentRule=owners[index];head=0;tail=1;queue[0]=index;}
   }
  }}finally{watch.Stop();}
  return Done;
 }
 // Memory-only diagnostic image, downsampled for display. Detection always uses
 // original pixels. Pixels use Unity bottom-up storage; image picking reverses Y.
 public Color32[] Preview(int selectedRule,out int previewWidth,out int previewHeight){
  if(!Done)throw new Exception("Finish processing before displaying a mask.");
  float scale=Math.Min(1,1024f/Math.Max(width,height));previewWidth=Math.Max(1,(int)Math.Ceiling(width*scale));previewHeight=Math.Max(1,(int)Math.Ceiling(height*scale));
  var output=new Color32[previewWidth*previewHeight];var colors=rules.Select(r=>(Color32)r.display).ToArray();
  for(int y=0;y<previewHeight;y++)for(int x=0;x<previewWidth;x++){
   int source=Math.Min(height-1,(int)((y+.5f)*height/previewHeight))*width+Math.Min(width-1,(int)((x+.5f)*width/previewWidth));int rule=owners[source]-1;
   output[y*previewWidth+x]=rule>=0&&(selectedRule<0||selectedRule==rule)?colors[rule]:new Color32(24,29,37,255);
  }
  return output;
 }
}
}
