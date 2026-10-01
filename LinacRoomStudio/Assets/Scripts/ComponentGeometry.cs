using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
[Serializable] public class ComponentShape {
 public string type="Trapezoid";
 public float width=4,topWidth=2,depth=1,radius=2,wallThickness=.25f,sweep=90;
 public int segments=24;
 public List<Vector2Data> points=new List<Vector2Data>();
}

// Shape coordinates are a local X/Z footprint in metres, extruded along Y.
public static class ComponentGeometry {
 const float Epsilon=.00001f;
 static void Range(float value,float min,float max){if(float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max)throw new Exception("Shape dimensions must be finite and within the displayed limits.");}
 public static List<Vector2> Footprint(ComponentShape shape){
  if(shape==null)throw new Exception("Missing component shape.");
  var result=new List<Vector2>();
  switch(shape.type){
   case "Trapezoid":
    Range(shape.width,.05f,60);Range(shape.topWidth,.05f,60);Range(shape.depth,.05f,60);
    result.Add(new Vector2(-shape.width/2,-shape.depth/2));result.Add(new Vector2(shape.width/2,-shape.depth/2));
    result.Add(new Vector2(shape.topWidth/2,shape.depth/2));result.Add(new Vector2(-shape.topWidth/2,shape.depth/2));break;
   case "Rounded":
    Range(shape.radius,.1f,30);Range(shape.wallThickness,.02f,10);Range(shape.sweep,5,330);
    if(shape.wallThickness>=2*shape.radius-.02f||shape.segments<4||shape.segments>48)throw new Exception("Curved wall needs a positive inner radius and 4–48 segments.");
    for(int n=0;n<=shape.segments;n++){float a=(-shape.sweep/2+shape.sweep*n/shape.segments)*Mathf.Deg2Rad;result.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(shape.radius+shape.wallThickness/2));}
    for(int n=shape.segments;n>=0;n--){float a=(-shape.sweep/2+shape.sweep*n/shape.segments)*Mathf.Deg2Rad;result.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(shape.radius-shape.wallThickness/2));}break;
   case "Polygon":
    if(shape.points==null||shape.points.Count<3||shape.points.Count>64)throw new Exception("A custom footprint needs 3–64 vertices.");
    foreach(var p in shape.points){if(p==null)throw new Exception("Missing polygon vertex.");Range(p.x,-100,100);Range(p.y,-100,100);result.Add(new Vector2(p.x,p.y));}break;
   default:throw new Exception("Unknown component shape.");
  }
  ValidatePolygon(result);
  // Collinear points are legal editor handles; omit them from triangulation.
  bool removed=true;while(removed&&result.Count>3){removed=false;for(int i=0;i<result.Count;i++){var a=result[(i+result.Count-1)%result.Count];var b=result[i];var c=result[(i+1)%result.Count];if(Mathf.Abs(Cross(b-a,c-b))<Epsilon){result.RemoveAt(i);removed=true;break;}}}
  if(Area(result)<0)result.Reverse();return result;
 }
 public static void Validate(ComponentShape shape){var p=Footprint(shape);Triangles(p);}
 static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
 public static float Area(IList<Vector2> p){float area=0;for(int i=0;i<p.Count;i++)area+=Cross(p[i],p[(i+1)%p.Count]);return area/2;}
 static bool OnSegment(Vector2 a,Vector2 b,Vector2 p)=>Mathf.Abs(Cross(b-a,p-a))<Epsilon&&p.x>=Mathf.Min(a.x,b.x)-Epsilon&&p.x<=Mathf.Max(a.x,b.x)+Epsilon&&p.y>=Mathf.Min(a.y,b.y)-Epsilon&&p.y<=Mathf.Max(a.y,b.y)+Epsilon;
 static bool Intersects(Vector2 a,Vector2 b,Vector2 c,Vector2 d){float abC=Cross(b-a,c-a),abD=Cross(b-a,d-a),cdA=Cross(d-c,a-c),cdB=Cross(d-c,b-c);return (abC*abD<0&&cdA*cdB<0)||OnSegment(a,b,c)||OnSegment(a,b,d)||OnSegment(c,d,a)||OnSegment(c,d,b);}
 static void ValidatePolygon(IList<Vector2> p){
  for(int i=0;i<p.Count;i++){
   int next=(i+1)%p.Count;if(Vector2.Distance(p[i],p[next])<.002f)throw new Exception("Footprint has duplicate or very close vertices.");
   var prior=p[(i+p.Count-1)%p.Count];if(Mathf.Abs(Cross(p[i]-prior,p[next]-p[i]))<Epsilon&&Vector2.Dot(p[i]-prior,p[next]-p[i])<0)throw new Exception("Footprint edges cannot fold back over each other.");
   for(int j=i+1;j<p.Count;j++){int end=(j+1)%p.Count;if(j==next||end==i)continue;if(Intersects(p[i],p[next],p[j],p[end]))throw new Exception("Footprint edges cannot cross or touch other edges.");}
  }
  if(Mathf.Abs(Area(p))<.0001f)throw new Exception("Footprint must enclose an area.");
 }
 static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c)=>Cross(b-a,p-a)>=-Epsilon&&Cross(c-b,p-b)>=-Epsilon&&Cross(a-c,p-c)>=-Epsilon;
 public static List<int> Triangles(IList<Vector2> p){
  var remaining=Enumerable.Range(0,p.Count).ToList();var triangles=new List<int>();
  while(remaining.Count>3){bool found=false;for(int k=0;k<remaining.Count;k++){
   int a=remaining[(k+remaining.Count-1)%remaining.Count],b=remaining[k],c=remaining[(k+1)%remaining.Count];
   if(Cross(p[b]-p[a],p[c]-p[b])<=Epsilon)continue;
   if(remaining.Any(n=>n!=a&&n!=b&&n!=c&&Inside(p[n],p[a],p[b],p[c])))continue;
   triangles.AddRange(new[]{a,b,c});remaining.RemoveAt(k);found=true;break;
  }if(!found)throw new Exception("Cannot triangulate this footprint. Separate overlapping or nearly collinear points.");}
  triangles.AddRange(remaining);return triangles;
 }
 public static Mesh Mesh(ComponentShape shape,float height){
  Range(height,.05f,100);var p=Footprint(shape);var cap=Triangles(p);var vertices=new List<Vector3>();var indices=new List<int>();
  Action<Vector3,Vector3,Vector3> triangle=(a,b,c)=>{int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.AddRange(new[]{n,n+1,n+2});};
  Func<int,float,Vector3> v=(i,y)=>new Vector3(p[i].x,y,p[i].y);
  for(int i=0;i<cap.Count;i+=3){triangle(v(cap[i],0),v(cap[i+1],0),v(cap[i+2],0));triangle(v(cap[i],height),v(cap[i+2],height),v(cap[i+1],height));}
  for(int i=0;i<p.Count;i++){int j=(i+1)%p.Count;triangle(v(i,0),v(i,height),v(j,height));triangle(v(i,0),v(j,height),v(j,0));}
  var mesh=new Mesh{name="Component footprint extrusion"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
}
}
