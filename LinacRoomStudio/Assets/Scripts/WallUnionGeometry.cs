using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoomStudio {

// Polygon union of connected wall rectangles on one base/height/material layer.
// Caps are a disjoint convex partition. Side faces are emitted only on the
// exposed boundary, so a corner/T/X has neither overlapping solids nor buried
// coplanar collision faces. The returned mesh is in world X/Z and local Y.
public static class WallUnionGeometry {
 const float Eps=.00000001f;
 class RectFootprint { public Item wall; public int materialIndex; public List<Vector2> polygon; public readonly List<int> triangles=new List<int>(); }
 static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
 static float Area(IList<Vector2> p){float sum=0;for(int i=0;i<p.Count;i++)sum+=Cross(p[i],p[(i+1)%p.Count]);return sum*.5f;}
 static Vector2 Point(Item wall)=>new Vector2(wall.x,wall.z);
 static List<Vector2> Rectangle(Item wall){
  if(wall==null||wall.kind!="Wall"||wall.shielding==null||wall.length<.25f||wall.shielding.thickness<=0)throw new Exception("Joined wall needs positive physical length and thickness.");
  var axis=new Vector2(Mathf.Cos(wall.angle*Mathf.Deg2Rad),-Mathf.Sin(wall.angle*Mathf.Deg2Rad));
  var normal=new Vector2(-axis.y,axis.x);var center=Point(wall);
  var half=axis*(wall.length*.5f);var side=normal*(wall.shielding.thickness/2000f);
  return new List<Vector2>{center-half-side,center+half-side,center+half+side,center-half+side};
 }
 static List<Vector2> Clip(IList<Vector2> polygon,Vector2 a,Vector2 b,bool keepLeft){
  var result=new List<Vector2>();if(polygon.Count==0)return result;
  float sign=keepLeft?1:-1;Vector2 prior=polygon[polygon.Count-1];float pd=sign*Cross(b-a,prior-a);
  foreach(var point in polygon){float cd=sign*Cross(b-a,point-a);bool wasIn=pd>=-Eps,nowIn=cd>=-Eps;
   if(wasIn!=nowIn){float t=pd/(pd-cd);result.Add(prior+(point-prior)*Mathf.Clamp01(t));}
   if(nowIn)result.Add(point);prior=point;pd=cd;
  }
  for(int n=result.Count-1;n>=0&&result.Count>1;n--)
   if(Vector2.Distance(result[n],result[(n+1)%result.Count])<=Eps)result.RemoveAt(n);
  return result;
 }
 static List<List<Vector2>> Subtract(IList<Vector2> polygon,IList<Vector2> cutter){
  var output=new List<List<Vector2>>();var inside=new List<Vector2>(polygon);
  for(int n=0;n<cutter.Count&&inside.Count>=3;n++){
   var a=cutter[n];var b=cutter[(n+1)%cutter.Count];
   var outside=Clip(inside,a,b,false);
   if(outside.Count>=3&&Mathf.Abs(Area(outside))>1e-9f)output.Add(outside);
   inside=Clip(inside,a,b,true);
  }
  return output;
 }
 static bool Inside(Vector2 point,IList<Vector2> polygon){
  for(int n=0;n<polygon.Count;n++)if(Cross(polygon[(n+1)%polygon.Count]-polygon[n],point-polygon[n])< -Eps)return false;
  return true;
 }
 static bool InsideAny(Vector2 point,IList<RectFootprint> rectangles){
  foreach(var rectangle in rectangles)if(Inside(point,rectangle.polygon))return true;return false;
 }
 static void AddParameter(List<float> values,float t){if(t>=-Eps&&t<=1+Eps)values.Add(Mathf.Clamp01(t));}
 static void Intersections(Vector2 a,Vector2 b,Vector2 c,Vector2 d,List<float> parameters){
  var u=b-a;var v=d-c;float divisor=Cross(u,v);
  if(Mathf.Abs(divisor)>Eps){float t=Cross(c-a,v)/divisor;float s=Cross(c-a,u)/divisor;if(t>=-Eps&&t<=1+Eps&&s>=-Eps&&s<=1+Eps)AddParameter(parameters,t);return;}
  if(Mathf.Abs(Cross(c-a,u))>Eps)return;
  float denominator=Vector2.Dot(u,u);if(denominator<=Eps)return;
  AddParameter(parameters,Vector2.Dot(c-a,u)/denominator);AddParameter(parameters,Vector2.Dot(d-a,u)/denominator);
 }
 static string Key(Vector2 a,Vector2 b){
  long ax=(long)Math.Round(a.x*100000),ay=(long)Math.Round(a.y*100000),bx=(long)Math.Round(b.x*100000),by=(long)Math.Round(b.y*100000);
  string first=ax+","+ay,second=bx+","+by;
  return string.CompareOrdinal(first,second)<0?first+"|"+second:second+"|"+first;
 }
 static void Triangle(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,List<int> materialIndices=null){
  int index=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(index);indices.Add(index+1);indices.Add(index+2);
  if(materialIndices!=null){materialIndices.Add(index);materialIndices.Add(index+1);materialIndices.Add(index+2);}
 }
 static Vector3 V(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
 public static Mesh Build(IList<Item> walls,bool partitionByWall=false){
  if(walls==null||walls.Count<2||walls.Count>250)throw new Exception("A joined mesh needs 2–250 wall members.");
  var rectangles=walls.Select((wall,index)=>new RectFootprint{wall=wall,materialIndex=index,polygon=Rectangle(wall)}).ToList();
  if(partitionByWall)rectangles=rectangles.OrderBy(rectangle=>rectangle.wall.id,StringComparer.Ordinal).ToList();
  float baseY=walls[0].y,height=walls[0].height;string material=walls[0].shielding.material;float density=walls[0].shielding.density;
  foreach(var wall in walls){
   if(Mathf.Abs(wall.y-baseY)>.0001f||Mathf.Abs(wall.height-height)>.0001f||wall.shielding.material!=material||Mathf.Abs(wall.shielding.density-density)>.01f||wall.doors!=null&&wall.doors.Count>0)
    throw new Exception("Joined mesh requires one base, height and physical material, with no openings.");
  }
  var vertices=new List<Vector3>();var indices=new List<int>();var earlier=new List<RectFootprint>();
  foreach(var rectangle in rectangles){
   var pieces=new List<List<Vector2>>{rectangle.polygon};
   foreach(var previous in earlier){
    var next=new List<List<Vector2>>();foreach(var piece in pieces)next.AddRange(Subtract(piece,previous.polygon));pieces=next;
   }
   foreach(var piece in pieces){
    if(piece.Count<3||Area(piece)<=1e-9f)continue;
    for(int k=1;k<piece.Count-1;k++){
    Triangle(vertices,indices,V(piece[0],baseY),V(piece[k],baseY),V(piece[k+1],baseY),partitionByWall?rectangle.triangles:null);
    Triangle(vertices,indices,V(piece[0],baseY+height),V(piece[k+1],baseY+height),V(piece[k],baseY+height),partitionByWall?rectangle.triangles:null);
    }
   }
   earlier.Add(rectangle);
  }
  var emitted=new HashSet<string>();float minThickness=walls.Min(w=>w.shielding.thickness)/1000f;
  float probe=Mathf.Min(.00005f,minThickness*.1f);
  foreach(var source in rectangles)for(int edge=0;edge<4;edge++){
   var a=source.polygon[edge];var b=source.polygon[(edge+1)%4];var parameters=new List<float>{0,1};
   foreach(var other in rectangles)for(int side=0;side<4;side++)
    Intersections(a,b,other.polygon[side],other.polygon[(side+1)%4],parameters);
   parameters.Sort();var distinct=new List<float>();foreach(var t in parameters)if(distinct.Count==0||t-distinct[distinct.Count-1]>Eps)distinct.Add(t);
   for(int n=0;n<distinct.Count-1;n++){
    float low=distinct[n],high=distinct[n+1];if(high-low<=Eps)continue;
    var p=a+(b-a)*low;var q=a+(b-a)*high;var midpoint=(p+q)*.5f;
    var normal=new Vector2(-(b-a).y,(b-a).x).normalized;
    bool insideLeft=InsideAny(midpoint+normal*probe,rectangles),insideRight=InsideAny(midpoint-normal*probe,rectangles);
    if(insideLeft==insideRight)continue;
    if(!insideLeft){var swap=p;p=q;q=swap;}
    if(!emitted.Add(Key(p,q)))continue;
    Triangle(vertices,indices,V(p,baseY),V(p,baseY+height),V(q,baseY+height),partitionByWall?source.triangles:null);
    Triangle(vertices,indices,V(p,baseY),V(q,baseY+height),V(q,baseY),partitionByWall?source.triangles:null);
   }
  }
  if(indices.Count==0)throw new Exception("Joined walls have no usable physical footprint.");
  var mesh=new Mesh{name="Joined wall footprint"};
  if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
  mesh.SetVertices(vertices);
  if(partitionByWall){mesh.subMeshCount=walls.Count;foreach(var rectangle in rectangles)mesh.SetTriangles(rectangle.triangles,rectangle.materialIndex);}
  else mesh.SetTriangles(indices,0);
  mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 public static float TopArea(Mesh mesh,float elevation){
  if(mesh==null)throw new Exception("Missing joined wall mesh.");
  float area=0;var vertices=mesh.vertices;var triangles=mesh.triangles;
  for(int n=0;n<triangles.Length;n+=3){var a=vertices[triangles[n]];var b=vertices[triangles[n+1]];var c=vertices[triangles[n+2]];
   if(Mathf.Abs(a.y-elevation)>.0001f||Mathf.Abs(b.y-elevation)>.0001f||Mathf.Abs(c.y-elevation)>.0001f)continue;
   area+=Mathf.Abs(Cross(new Vector2(b.x-a.x,b.z-a.z),new Vector2(c.x-a.x,c.z-a.z)))*.5f;
  }
  return area;
 }
 public static float SideArea(Mesh mesh){
  if(mesh==null)throw new Exception("Missing joined wall mesh.");
  float area=0;var vertices=mesh.vertices;var triangles=mesh.triangles;
  for(int n=0;n<triangles.Length;n+=3){var a=vertices[triangles[n]];var b=vertices[triangles[n+1]];var c=vertices[triangles[n+2]];
   if(Mathf.Abs(a.y-b.y)<.0001f&&Mathf.Abs(b.y-c.y)<.0001f)continue;
   area+=Vector3.Cross(b-a,c-a).magnitude*.5f;
  }
  return area;
 }
}
}
