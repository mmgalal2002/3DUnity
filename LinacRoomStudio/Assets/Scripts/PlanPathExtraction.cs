using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RoomStudio {

[Serializable] public sealed class PlanSourceComponent {
 public string id="",ruleId="",classification="";
 public int areaPixels,minX,minY,maxX,maxY;
 public Vector2Data centroidPixel=new Vector2Data();
 public Vector2Data aPixel=new Vector2Data(),bPixel=new Vector2Data();
 public float orientationDegrees;
}

[Serializable] public sealed class PlanExtractedPath {
 public string id="",ruleId="",sourceComponent="";
 public Vector2Data aPixel=new Vector2Data(),bPixel=new Vector2Data();
 public int sourceAreaPixels,minX,minY,maxX,maxY;
 public float lengthPixels;
}

[Serializable] public sealed class PlanPathIssue {
 public string code="",ruleId="",sourceComponent="",message="";
 public bool blocking;
 public int count;
}

public sealed class PlanPathResult {
 public readonly List<PlanSourceComponent> components=new List<PlanSourceComponent>();
 public readonly List<PlanSourceComponent> openingMarkers=new List<PlanSourceComponent>();
 public readonly List<PlanExtractedPath> paths=new List<PlanExtractedPath>();
 public readonly List<PlanPathIssue> issues=new List<PlanPathIssue>();
 public int wallMaskPixels,skeletonPixels,rejectedSegments,unresolvedJunctions;
 public double elapsedMilliseconds;
 public bool HasBlockingIssues=>issues.Any(i=>i.blocking);
 public int ExtractedSegments=>paths.Count;
 byte[] maskOwners;
 bool[] openingRules;
 int sourceWidth,sourceHeight;
 internal void SetOpeningMask(PlanColorMask mask){maskOwners=mask.owners;openingRules=mask.rules.Select(r=>r.classification=="Opening").ToArray();sourceWidth=mask.width;sourceHeight=mask.height;}
 // Pixel-accurate marker guard for a proposed centerline, including endpoints.
 // A caller may pass a positive clearance to protect a wider proposed footprint.
 public bool CrossesOpening(Vector2Data a,Vector2Data b,float clearancePixels=0){
  if(maskOwners==null||a==null||b==null||float.IsNaN(clearancePixels)||clearancePixels<0||clearancePixels>64||float.IsNaN(a.x)||float.IsNaN(a.y)||float.IsNaN(b.x)||float.IsNaN(b.y)||float.IsInfinity(a.x)||float.IsInfinity(a.y)||float.IsInfinity(b.x)||float.IsInfinity(b.y))throw new ArgumentException("Invalid opening check.");
  float dx=b.x-a.x,dy=b.y-a.y;double length=Math.Sqrt((double)dx*dx+(double)dy*dy);
  if(length>32768)throw new ArgumentException("Opening check segment exceeds the supported source extent.");
  if(openingMarkers.Count==0)return false;
  int steps=Math.Max(1,(int)Math.Ceiling(length*2));
  for(int step=0;step<=steps;step++){
   float x=a.x+dx*step/steps,y=a.y+dy*step/steps;
   int left=Math.Max(0,Mathf.FloorToInt(x-clearancePixels)),right=Math.Min(sourceWidth-1,Mathf.FloorToInt(x+clearancePixels));
   int top=Math.Max(0,Mathf.FloorToInt(y-clearancePixels)),bottom=Math.Min(sourceHeight-1,Mathf.FloorToInt(y+clearancePixels));
   for(int py=top;py<=bottom;py++)for(int px=left;px<=right;px++){
    int owner=maskOwners[(sourceHeight-1-py)*sourceWidth+px]-1;
    if(owner>=0&&openingRules[owner])return true;
   }
  }
  return false;
 }
}

// Original-resolution, mask-only authoring stage. The mask owns every pixel once,
// so this stage cannot create duplicate walls from overlapping color tolerances.
// A job can be discarded between Step calls without modifying the guide or design.
public sealed class PlanPathExtraction {
 public const int MaxPaths=8192,MaxWallPixels=1000000,MaxThinningCycles=64,MaxTracePixels=65536;
 const int MaxThinVisits=80000000;
 static readonly int[] Dx={0,1,1,1,0,-1,-1,-1},Dy={1,1,0,-1,-1,-1,0,1};
 readonly PlanColorMask mask;
 readonly byte[] skeleton;
 readonly List<int> active=new List<int>();
 readonly List<int> removals=new List<int>();
 readonly Stopwatch watch=new Stopwatch();
 readonly int maxPaths;
 int phase,cursor,subpass,cycle,cycleRemoved,thinVisits;
 int[] componentId,cluster;
 byte[] degree,visited;
 readonly List<Vector2> centres=new List<Vector2>{Vector2.zero};
 readonly HashSet<string> pathIds=new HashSet<string>(StringComparer.Ordinal);
 readonly List<int> componentQueue=new List<int>();
 int componentScan,componentHead,projectionHead,componentOwner,componentMinX,componentMinY,componentMaxX,componentMaxY;
 double sx,sy,sxx,syy,sxy,cx,cy,orientation,ux,uy,low,high;
 int graphCursor;
 bool nodePass=true;
 public PlanPathResult Result {get;}=new PlanPathResult();
 public bool Done=>phase==8;
 public string Phase=>phase==0?"Collecting wall mask":phase==1||phase==2?"Finding stroke centerlines":phase==3||phase==4?"Measuring source components":phase==5||phase==6?"Resolving centerline junctions":phase==7?"Tracing straight paths":"Complete";
 public float Progress=>Done?1:phase==0?.12f*cursor/skeleton.Length:phase==1||phase==2?.12f+.53f*Math.Min(1,(cycle+((float)cursor/Math.Max(1,active.Count)))/12f):phase==3?.65f:phase==4?.65f+.12f*componentScan/skeleton.Length:phase==5?.77f+.06f*cursor/Math.Max(1,active.Count):phase==6?.83f+.07f*cursor/Math.Max(1,active.Count):.90f+.10f*(graphCursor+(nodePass?0:active.Count))/(2f*Math.Max(1,active.Count));

 public PlanPathExtraction(PlanColorMask mask,int maxPaths=MaxPaths){
  if(mask==null||!mask.Done)throw new Exception("Finish the color mask before extracting paths.");
  if(maxPaths<1||maxPaths>MaxPaths)throw new ArgumentOutOfRangeException(nameof(maxPaths));
  this.mask=mask;this.maxPaths=maxPaths;skeleton=new byte[mask.owners.Length];Result.SetOpeningMask(mask);
 }
 public static PlanPathResult Extract(PlanColorMask mask,int maxPaths=MaxPaths){
  var job=new PlanPathExtraction(mask,maxPaths);
  while(!job.Step(65536)){}
  return job.Result;
 }
 public bool Step(int budget=4096){
  if(budget<1||budget>65536)throw new ArgumentOutOfRangeException(nameof(budget));
  if(Done)return true;
  watch.Start();
  try{while(budget>0&&!Done){
   if(phase==0){
    int start=cursor,end=Math.Min(skeleton.Length,cursor+budget);
    while(cursor<end){int p=cursor++,r=mask.owners[p]-1;if(r>=0&&mask.rules[r].classification=="Wall"){skeleton[p]=mask.owners[p];active.Add(p);}}
    budget-=cursor-start;
    // The scan above may finish in a partial slice; the next call continues at cursor.
    if(cursor==skeleton.Length){
     Result.wallMaskPixels=active.Count;
     if(active.Count>MaxWallPixels){Issue("WallPixelLimit","","",true,"Wall mask exceeds the one-million-pixel extraction limit.",active.Count);phase=8;}
     else if(active.Count==0)phase=3;
     else{phase=1;cursor=0;}
    }
    // A collection slice is intentionally one Step call, keeping mask copies bounded.
    break;
   }
   if(phase==1){
    int processed=0;
    while(cursor<active.Count&&processed<budget){
     int p=active[cursor++];processed++;
     if(skeleton[p]!=0&&ShouldRemove(p,subpass))removals.Add(p);
    }
    budget-=processed;thinVisits+=processed;
    if(thinVisits>MaxThinVisits){Issue("ThinningWorkLimit","","",true,"Centerline extraction exceeded its bounded thinning work limit.",thinVisits);phase=8;break;}
    if(cursor==active.Count){phase=2;cursor=0;}
    continue;
   }
   if(phase==2){
    int processed=0;
    while(cursor<removals.Count&&processed<budget){skeleton[removals[cursor++]]=0;processed++;}
    budget-=Math.Max(1,processed);
    if(cursor==removals.Count){
     cycleRemoved+=removals.Count;removals.Clear();cursor=0;
     if(subpass==0){subpass=1;phase=1;}
     else{
      subpass=0;cycle++;
      if(cycleRemoved==0)phase=3;
      else if(cycle>=MaxThinningCycles){Issue("ThickOrFilledShape","","",true,"Stroke did not converge to a centerline within 64 thinning cycles.",cycle);phase=8;}
      else phase=1;
      cycleRemoved=0;
     }
    }
    continue;
   }
   if(phase==3){componentId=new int[skeleton.Length];degree=new byte[skeleton.Length];visited=new byte[skeleton.Length];cluster=new int[skeleton.Length];phase=4;continue;}
   if(phase==4){StepComponents(ref budget);continue;}
   if(phase==5){
    int processed=0;while(cursor<active.Count&&processed<budget){int p=active[cursor++];processed++;if(skeleton[p]!=0){Result.skeletonPixels++;for(int d=0;d<8;d++)if(Link(p,d))degree[p]++;}}
    budget-=processed;if(cursor==active.Count){phase=6;cursor=0;}continue;
   }
   if(phase==6){StepClusters(ref budget);continue;}
   if(phase==7){StepTraces(ref budget);continue;}
  }}finally{watch.Stop();if(Done)Result.elapsedMilliseconds=watch.Elapsed.TotalMilliseconds;}
  return Done;
 }
 void Issue(string code,string rule,string component,bool blocking,string message,int count=1){Result.issues.Add(new PlanPathIssue{code=code,ruleId=rule,sourceComponent=component,blocking=blocking,message=message,count=count});}
 int Neighbor(int p,int direction){int x=p%mask.width,y=p/mask.width,nx=x+Dx[direction],ny=y+Dy[direction];return nx<0||ny<0||nx>=mask.width||ny>=mask.height?-1:ny*mask.width+nx;}
 bool Same(int p,int direction){int n=Neighbor(p,direction);return n>=0&&skeleton[n]!=0&&skeleton[n]==skeleton[p];}
 bool ShouldRemove(int p,int pass){
  // Zhang-Suen thinning, using the eight pixels of the same winning rule.
  bool n=Same(p,0),ne=Same(p,1),e=Same(p,2),se=Same(p,3),s=Same(p,4),sw=Same(p,5),w=Same(p,6),nw=Same(p,7);
  int neighbours=(n?1:0)+(ne?1:0)+(e?1:0)+(se?1:0)+(s?1:0)+(sw?1:0)+(w?1:0)+(nw?1:0);
  if(neighbours<2||neighbours>6)return false;
  int turns=(!n&&ne?1:0)+(!ne&&e?1:0)+(!e&&se?1:0)+(!se&&s?1:0)+(!s&&sw?1:0)+(!sw&&w?1:0)+(!w&&nw?1:0)+(!nw&&n?1:0);
  if(turns!=1)return false;
  return pass==0?!(n&&e&&s)&&!(e&&s&&w):!(n&&e&&w)&&!(n&&s&&w);
 }
 // Suppress diagonal graph links when an orthogonal skeleton pixel already
 // joins those pixels. This removes false branches at square corners while
 // retaining actual one-pixel diagonal strokes.
 bool Link(int p,int direction){
  int n=Neighbor(p,direction);if(n<0||skeleton[p]==0||skeleton[n]!=skeleton[p])return false;
  if((direction&1)==0)return true;
  int before=(direction+7)&7,after=(direction+1)&7;
  return !Same(p,before)&&!Same(p,after);
 }
 Vector2 Point(int p)=>new Vector2(p%mask.width+.5f,mask.height-p/mask.width-.5f);
 void StepComponents(ref int budget){
  while(budget>0&&phase==4){
   if(componentOwner==0){
    if(componentScan==mask.owners.Length){phase=5;cursor=0;break;}
    int first=componentScan++,owner=mask.owners[first];budget--;
    if(owner==0||componentId[first]!=0)continue;
    string classification=mask.rules[owner-1].classification;
    if(classification!="Wall"&&classification!="Opening")continue;
    componentOwner=owner;componentQueue.Clear();componentQueue.Add(first);componentId[first]=Result.components.Count+1;
    componentHead=projectionHead=0;componentMinX=mask.width;componentMinY=mask.height;componentMaxX=componentMaxY=0;
    sx=sy=sxx=syy=sxy=0;continue;
   }
   if(componentHead<componentQueue.Count){
    int p=componentQueue[componentHead++],x=p%mask.width,y=mask.height-1-p/mask.width;budget--;
    componentMinX=Math.Min(componentMinX,x);componentMaxX=Math.Max(componentMaxX,x);componentMinY=Math.Min(componentMinY,y);componentMaxY=Math.Max(componentMaxY,y);
    double px=x+.5,py=y+.5;sx+=px;sy+=py;sxx+=px*px;syy+=py*py;sxy+=px*py;
    for(int d=0;d<8;d++){
     int n=Neighbor(p,d);if(n>=0&&componentId[n]==0&&mask.owners[n]==componentOwner){componentId[n]=Result.components.Count+1;componentQueue.Add(n);}
    }
    continue;
   }
   if(projectionHead==0){
    double count=componentQueue.Count;cx=sx/count;cy=sy/count;
    double covX=sxx/count-cx*cx,covY=syy/count-cy*cy,covXY=sxy/count-cx*cy;
    orientation=.5*Math.Atan2(2*covXY,covX-covY);ux=Math.Cos(orientation);uy=Math.Sin(orientation);
    low=double.PositiveInfinity;high=double.NegativeInfinity;
   }
   if(projectionHead<componentQueue.Count){
    int p=componentQueue[projectionHead++];budget--;
    double x=p%mask.width+.5,y=mask.height-p/mask.width-.5,along=(x-cx)*ux+(y-cy)*uy;
    low=Math.Min(low,along);high=Math.Max(high,along);continue;
   }
   string ruleId=mask.rules[componentOwner-1].id,kind=mask.rules[componentOwner-1].classification;
   var component=new PlanSourceComponent{
    id="component-"+StableHash(ruleId+"|"+componentMinX+","+componentMinY+","+componentMaxX+","+componentMaxY+"|"+componentQueue.Count),ruleId=ruleId,classification=kind,
    areaPixels=componentQueue.Count,minX=componentMinX,minY=componentMinY,maxX=componentMaxX,maxY=componentMaxY,
    centroidPixel=new Vector2Data((float)cx,(float)cy),orientationDegrees=(float)(orientation*180/Math.PI),
    aPixel=new Vector2Data((float)(cx+low*ux),(float)(cy+low*uy)),bPixel=new Vector2Data((float)(cx+high*ux),(float)(cy+high*uy))
   };
   Result.components.Add(component);if(kind=="Opening")Result.openingMarkers.Add(component);
   componentOwner=0;componentQueue.Clear();
  }
 }
 void StepClusters(ref int budget){
  int processed=0;
  while(cursor<active.Count&&processed<budget){
   int p=active[cursor++];processed++;
   if(skeleton[p]==0||degree[p]==2||cluster[p]!=0)continue;
   int id=centres.Count;var queue=new List<int>{p};cluster[p]=id;Vector2 sum=Vector2.zero;int maxDegree=0;
   for(int head=0;head<queue.Count;head++){
    int current=queue[head];sum+=Point(current);maxDegree=Math.Max(maxDegree,degree[current]);
    for(int d=0;d<8;d++){int n=Neighbor(current,d);if(n>=0&&cluster[n]==0&&skeleton[n]==skeleton[p]&&degree[n]!=2&&Link(current,d)){cluster[n]=id;queue.Add(n);}}
    if(queue.Count>64){Issue("WideJunction",mask.rules[skeleton[p]-1].id,Result.components[componentId[p]-1].id,true,"Junction region exceeds 64 centerline pixels.",queue.Count);break;}
   }
   centres.Add(sum/queue.Count);
   if(maxDegree>4){Result.unresolvedJunctions++;Issue("AmbiguousJunction",mask.rules[skeleton[p]-1].id,Result.components[componentId[p]-1].id,true,"A centerline pixel has more than four branches.",maxDegree);}
  }
  budget-=processed;if(cursor==active.Count){phase=7;cursor=0;graphCursor=0;nodePass=true;}
 }
 void StepTraces(ref int budget){
  int processed=0,traces=0;
  while(graphCursor<active.Count&&processed<budget&&traces<32){
   int p=active[graphCursor++];processed++;
   if(skeleton[p]==0||(nodePass?degree[p]==2:degree[p]!=2))continue;
   for(int d=0;d<8;d++)if(Link(p,d)&&(visited[p]&(1<<d))==0){
    int n=Neighbor(p,d);if(nodePass&&cluster[p]!=0&&cluster[n]==cluster[p]){MarkEdge(p,d,visited);continue;}
    Trace(p,d,degree,cluster,centres,componentId,visited,pathIds);traces++;
    if(Result.paths.Count>=maxPaths){Issue("SegmentLimit","","",true,"Extraction reached the configured segment limit.",maxPaths);phase=8;break;}
   }
   if(phase==8)break;
  }
  budget-=processed;
  if(phase!=8&&graphCursor==active.Count){if(nodePass){nodePass=false;graphCursor=0;}else phase=8;}
 }
 void MarkEdge(int p,int d,byte[] visited){int n=Neighbor(p,d);visited[p]|=(byte)(1<<d);if(n>=0)visited[n]|=(byte)(1<<((d+4)&7));}
 void Trace(int start,int direction,byte[] degree,int[] cluster,List<Vector2> centres,int[] componentId,byte[] visited,HashSet<string> ids){
  byte owner=skeleton[start];string ruleId=mask.rules[owner-1].id;
  int component=componentId[start];var source=Result.components[component-1];
  var raw=new List<Vector2>{cluster[start]>0?centres[cluster[start]]:Point(start)};
  int current=start,d=direction,previous=-1,end=start;bool closed=false,invalid=false;
  while(true){
   int next=Neighbor(current,d);if(next<0||!Link(current,d)){invalid=true;break;}
   MarkEdge(current,d,visited);previous=current;current=next;end=next;
   raw.Add(cluster[next]>0?centres[cluster[next]]:Point(next));
   if(next==start){closed=true;break;}
   if(cluster[next]>0)break;
   if(raw.Count>MaxTracePixels){Issue("TraceLimit",ruleId,source.id,true,"A centerline path exceeds 65,536 source pixels.",raw.Count);invalid=true;break;}
   int chosen=-1;
   for(int k=0;k<8;k++)if(Link(next,k)&&Neighbor(next,k)!=previous){chosen=k;break;}
   if(chosen<0){invalid=true;break;}
   if((visited[next]&(1<<chosen))!=0){invalid=true;break;}
   d=chosen;
  }
  if(invalid){Issue("BrokenSkeleton",ruleId,source.id,true,"Centerline topology could not be traced cleanly.");return;}
  if(!closed&&raw.Count>=2){
   if(degree[start]==1)raw[0]=ExtendTip(start,raw[0]-raw[1],owner);
   if(degree[end]==1)raw[raw.Count-1]=ExtendTip(end,raw[raw.Count-1]-raw[raw.Count-2],owner);
  }
  var simplified=Simplify(raw,closed);
  if(simplified.Count-1>256){Issue("ComplexPath",ruleId,source.id,true,"Path needs more than 256 straight segments; inspect curves/noisy strokes.",simplified.Count-1);return;}
  if(simplified.Count-1>8){Issue("ComplexPath",ruleId,source.id,true,"Path has many turns; inspect for curves or noisy strokes.",simplified.Count-1);}
  for(int i=1;i<simplified.Count;i++){
   Vector2 a=simplified[i-1],b=simplified[i];float length=Vector2.Distance(a,b);
   if(length<mask.rules[owner-1].minLengthPixels){
    Result.rejectedSegments++;
    bool partOfNetwork=simplified.Count>2||degree[start]>=3||degree[end]>=3;
    Issue("ShortSegment",ruleId,source.id,partOfNetwork,partOfNetwork?"A short segment within a connected network was rejected; reduce the rule minimum or correct the path.":"Isolated segment is shorter than the rule's minimum path length.");
    continue;
   }
   if(length<.01f){Result.rejectedSegments++;Issue("DegenerateSegment",ruleId,source.id,true,"A zero-length segment was rejected.");continue;}
   // Endpoints, not component enumeration order, define identity. The same
   // source/rules/settings therefore produce identical path IDs on regeneration.
   if(a.x>b.x||(a.x==b.x&&a.y>b.y)){var swap=a;a=b;b=swap;}
   string key=ruleId+"|"+Coordinate(a.x)+","+Coordinate(a.y)+"|"+Coordinate(b.x)+","+Coordinate(b.y);
   string id="path-"+StableHash(key);
   if(!ids.Add(id)){Issue("DuplicatePath",ruleId,source.id,true,"Two extracted segments have the same source endpoints.");continue;}
   Result.paths.Add(new PlanExtractedPath{id=id,ruleId=ruleId,sourceComponent=source.id,aPixel=new Vector2Data(a.x,a.y),bPixel=new Vector2Data(b.x,b.y),sourceAreaPixels=source.areaPixels,minX=source.minX,minY=source.minY,maxX=source.maxX,maxY=source.maxY,lengthPixels=length});
  }
 }
 Vector2 ExtendTip(int pixel,Vector2 outward,byte owner){
  Vector2 current=Point(pixel),dir=outward.normalized;
  if(dir.sqrMagnitude<.5f)return current;
  // Follow the stroke through its terminal cap. The final coordinate remains
  // at an original pixel centre, never in a downsampled preview.
  for(int step=0;step<128;step++){
   Vector2 candidate=current+dir;int x=Mathf.FloorToInt(candidate.x),y=mask.height-1-Mathf.FloorToInt(candidate.y);
   if(x<0||x>=mask.width||y<0||y>=mask.height||mask.owners[y*mask.width+x]!=owner)break;
   current=candidate;
  }
  return current;
 }
 static string Coordinate(float x)=>Math.Round(x,3,MidpointRounding.AwayFromZero).ToString("F3",CultureInfo.InvariantCulture);
 static string StableHash(string text){
  // FNV-1a on UTF-16 code units is deterministic across Mono and IL2CPP.
  ulong h=14695981039346656037UL;
  foreach(char c in text){h^=(byte)c;h*=1099511628211UL;h^=(byte)(c>>8);h*=1099511628211UL;}
  return h.ToString("x16",CultureInfo.InvariantCulture);
 }
 static List<Vector2> Simplify(List<Vector2> raw,bool closed){
  if(raw.Count<=2)return raw;
  var keep=new bool[raw.Count];keep[0]=keep[raw.Count-1]=true;
  var ranges=new Stack<Vector2Int>();
  if(closed){int pivot=1;float far=0;for(int i=1;i<raw.Count-1;i++){float d=(raw[i]-raw[0]).sqrMagnitude;if(d>far){far=d;pivot=i;}}keep[pivot]=true;ranges.Push(new Vector2Int(0,pivot));ranges.Push(new Vector2Int(pivot,raw.Count-1));}
  else ranges.Push(new Vector2Int(0,raw.Count-1));
  const float epsilonSquared=1.0f;
  while(ranges.Count>0){
   var range=ranges.Pop();int best=-1;float max=epsilonSquared;
   Vector2 a=raw[range.x],b=raw[range.y],ab=b-a;float denominator=ab.sqrMagnitude;
   for(int i=range.x+1;i<range.y;i++){
    float t=denominator<.000001f?0:Mathf.Clamp01(Vector2.Dot(raw[i]-a,ab)/denominator);
    float distance=(raw[i]-(a+t*ab)).sqrMagnitude;
    if(distance>max){max=distance;best=i;}
   }
   if(best>=0){keep[best]=true;ranges.Push(new Vector2Int(range.x,best));ranges.Push(new Vector2Int(best,range.y));}
  }
  var simple=new List<Vector2>();for(int i=0;i<raw.Count;i++)if(keep[i])simple.Add(raw[i]);
  // Short corner diagonals left by square-stroke thinning should meet the
  // neighbouring long centerlines at their geometric intersection.
  for(int i=1;i+2<simple.Count;i++){
   Vector2 before=simple[i-1],a=simple[i],b=simple[i+1],after=simple[i+2];
   if(Vector2.Distance(a,b)>3||Vector2.Distance(before,a)<4||Vector2.Distance(b,after)<4)continue;
   if(!IntersectLines(before,a,b,after,out Vector2 join)||Vector2.Distance(join,(a+b)*.5f)>4)continue;
   simple[i]=join;simple.RemoveAt(i+1);i=Math.Max(0,i-2);
  }
  for(int i=1;i+1<simple.Count;i++){
   Vector2 a=simple[i]-simple[i-1],b=simple[i+1]-simple[i];
   if(a.sqrMagnitude<.0001f||b.sqrMagnitude<.0001f||Mathf.Abs(Cross(a.normalized,b.normalized))<.035f&&Vector2.Dot(a,b)>0){simple.RemoveAt(i);i--;}
  }
  return simple;
 }
 static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
 static bool IntersectLines(Vector2 a,Vector2 b,Vector2 c,Vector2 d,out Vector2 point){Vector2 u=b-a,v=d-c;float determinant=Cross(u,v);if(Mathf.Abs(determinant)<.2f){point=Vector2.zero;return false;}point=a+u*(Cross(c-a,v)/determinant);return true;}
}
}
