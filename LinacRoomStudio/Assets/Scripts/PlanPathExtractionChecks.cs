using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public static class PlanPathExtractionChecks {
 static void Need(bool condition,string message){if(!condition)throw new Exception("Plan path regression: "+message);}
 static void Near(float value,float expected,float tolerance,string message)=>Need(Math.Abs(value-expected)<=tolerance,message+" ("+value+" versus "+expected+")");
 static readonly Color32 White=new Color32(255,255,255,255),Magenta=new Color32(255,0,255,255),Green=new Color32(0,255,0,255),Red=new Color32(255,0,0,255);
 static PlanColorRule Rule(string id,Color color,string classification="Wall")=>new PlanColorRule{id=id,name=id,classification=classification,target=color,display=color,material=classification=="Wall"?"Concrete":"",tolerance=.01f,minAreaPixels=3,minLengthPixels=3};
 static Color32[] Canvas(int width,int height){var pixels=new Color32[width*height];for(int i=0;i<pixels.Length;i++)pixels[i]=White;return pixels;}
 static void Pixel(Color32[] pixels,int width,int height,int x,int y,Color32 color){if(x>=0&&x<width&&y>=0&&y<height)pixels[(height-1-y)*width+x]=color;}
 static void H(Color32[] p,int w,int h,int x0,int x1,int y0,int y1,Color32 c){for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)Pixel(p,w,h,x,y,c);}
 static void V(Color32[] p,int w,int h,int x0,int x1,int y0,int y1,Color32 c)=>H(p,w,h,x0,x1,y0,y1,c);
 static PlanColorMask Mask(Color32[] pixels,int width,int height,params PlanColorRule[] rules){var model=new PlanAuthoringData();model.rules.AddRange(rules);var mask=new PlanColorMask(pixels,width,height,model);int steps=0;while(!mask.Step(4096))if(++steps>20000)throw new Exception("Mask fixture stalled.");return mask;}
 static PlanPathResult Extract(PlanColorMask mask){var job=new PlanPathExtraction(mask);Need(!job.Step(1)&&job.Progress>0,"cooperative extraction first slice");int steps=0;while(!job.Step(4096))if(++steps>200000)throw new Exception("Path fixture stalled.");return job.Result;}
 static Vector2 A(PlanExtractedPath p)=>new Vector2(p.aPixel.x,p.aPixel.y);
 static Vector2 B(PlanExtractedPath p)=>new Vector2(p.bPixel.x,p.bPixel.y);
 public static void Run(){
  var wall=Rule("magenta",Color.magenta);const int w=160,h=120;
  var pixels=Canvas(w,h);H(pixels,w,h,10,109,20,24,Magenta);Pixel(pixels,w,h,2,2,Magenta);
  var mask=Mask(pixels,w,h,wall);var result=Extract(mask);
  Need(result.wallMaskPixels==500&&result.paths.Count==1&&result.rejectedSegments==0&&!result.HasBlockingIssues,"thick horizontal stroke must yield one centerline; isolated noise rejected: mask="+result.wallMaskPixels+" paths="+result.paths.Count+" rejected="+result.rejectedSegments+" segments="+string.Join(";",result.paths.Select(p=>p.aPixel.x+","+p.aPixel.y+" -> "+p.bPixel.x+","+p.bPixel.y))+" issues="+string.Join(";",result.issues.Select(i=>i.message)));
  var line=result.paths[0];Near(line.lengthPixels,99,1,"stroke endpoint extent");Near(A(line).y,22.5f,1,"stroke centerline Y");Near(B(line).y,22.5f,1,"stroke centerline Y end");
  Need(line.id==PlanPathExtraction.Extract(mask).paths[0].id,"unchanged extraction must retain path ID");
  Need(result.components.Count==1&&result.components[0].areaPixels==500&&result.components[0].classification=="Wall","source component statistics");

  pixels=Canvas(w,h);V(pixels,w,h,28,32,10,99,Magenta);V(pixels,w,h,42,46,10,99,Magenta);
  result=Extract(Mask(pixels,w,h,wall));
  Need(result.paths.Count==2&&!result.HasBlockingIssues,"nearby parallel thick lines must remain separate");
  Need(Math.Abs(A(result.paths[0]).x-A(result.paths[1]).x)>10,"parallel centerlines collapsed");

  pixels=Canvas(w,h);for(int x=12;x<=95;x++)for(int offset=-1;offset<=1;offset++)Pixel(pixels,w,h,x,x+offset,Magenta);
  result=Extract(Mask(pixels,w,h,wall));
  Need(result.paths.Count==1&&!result.HasBlockingIssues,"diagonal thick stroke must yield one path");
  Near(Math.Abs(A(result.paths[0]).x-A(result.paths[0]).y),0,1.5f,"diagonal start fidelity");
  Near(Math.Abs(B(result.paths[0]).x-B(result.paths[0]).y),0,1.5f,"diagonal end fidelity");

  pixels=Canvas(w,h);H(pixels,w,h,15,115,15,18,Magenta);H(pixels,w,h,15,115,85,88,Magenta);
  V(pixels,w,h,15,18,15,88,Magenta);V(pixels,w,h,112,115,15,88,Magenta);V(pixels,w,h,60,63,15,88,Magenta);
  result=Extract(Mask(pixels,w,h,wall));
  Need(result.paths.Count>=7&&result.paths.Count<=12,"rectangle and partition centerlines missing or badly fragmented: "+result.paths.Count);
  Need(!result.HasBlockingIssues,"clean rectangle/partition reported unsupported topology");

  pixels=Canvas(w,h);H(pixels,w,h,10,55,50,54,Magenta);H(pixels,w,h,61,110,50,54,Magenta);H(pixels,w,h,56,60,50,54,Red);
  var opening=Rule("opening",Color.red,"Opening");result=Extract(Mask(pixels,w,h,wall,opening));
  Need(result.paths.Count==2&&result.openingMarkers.Count==1,"opening marker must split wall and survive diagnostics");
  Need(result.CrossesOpening(new Vector2Data(40,52.5f),new Vector2Data(75,52.5f)),"opening crossing guard missed marker");
  Need(!result.CrossesOpening(new Vector2Data(10,30),new Vector2Data(75,30)),"opening crossing guard false positive");

  RasterQualityChecks();
    PipelineChecks();
  UnityEngine.Debug.Log("ROOM_STUDIO_PLAN_PATH_CHECKS_PASSED: cooperative original-pixel extraction, thick/diagonal/parallel strokes, rectangle partition, deterministic IDs, noise, alpha, anti-aliasing, JPEG, opening guard");
 }
 static void RasterQualityChecks(){
  const int width=160,height=120;var rule=Rule("raster-quality",Color.magenta);rule.tolerance=.2f;rule.minAreaPixels=24;
  var pixels=Canvas(width,height);H(pixels,width,height,24,135,49,55,Magenta);
  var edge=new Color32(255,64,255,255);H(pixels,width,height,24,135,48,48,edge);H(pixels,width,height,24,135,56,56,edge);
  V(pixels,width,height,23,23,49,55,edge);V(pixels,width,height,136,136,49,55,edge);
  Pixel(pixels,width,height,3,3,Magenta);H(pixels,width,height,20,40,90,90,new Color32(255,0,255,0));
  var mask=Mask(pixels,width,height,rule);var result=Extract(mask);
  Need(mask.transparent==21&&result.paths.Count==1&&!result.HasBlockingIssues,"anti-aliased stroke or transparent colored noise classification");
  CheckRasterEndpoints(result.paths[0],1.5f,"anti-aliased");
  H(pixels,width,height,20,40,90,90,White);
  rule.tolerance=.1f;
  var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var decoded=new Texture2D(2,2,TextureFormat.RGBA32,false);
  try{
   texture.SetPixels32(pixels);texture.Apply();
   foreach(int quality in new[]{90,75}){
    byte[] bytes=texture.EncodeToJPG(quality);var source=FloorPlanCodec.Read(bytes,"raster-quality.jpg");
    Need(source.pixelWidth==width&&source.pixelHeight==height&&decoded.LoadImage(bytes),"JPEG import/decode dimensions at quality "+quality);
    result=Extract(Mask(decoded.GetPixels32(),width,height,rule));
    Need(result.paths.Count==1&&!result.HasBlockingIssues,"JPEG must retain one thick-stroke centerline at quality "+quality+": paths="+result.paths.Count+" rejected="+result.rejectedSegments+" segments="+string.Join(";",result.paths.Select(path=>path.aPixel.x+","+path.aPixel.y+" -> "+path.bPixel.x+","+path.bPixel.y))+" issues="+string.Join(";",result.issues.Select(issue=>issue.code+": "+issue.message)));
    CheckRasterEndpoints(result.paths[0],2,"JPEG quality "+quality);
   }
  }finally{
   if(Application.isPlaying){UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(decoded);}
   else{UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(decoded);}
  }
  UnityEngine.Debug.Log("ROOM_STUDIO_RASTER_QUALITY_CHECKS_PASSED: anti-aliased endpoints within 1.5 source pixels at RGB tolerance 0.2; JPEG qualities 90/75 within 2 pixels at RGB tolerance 0.1; minimum component area 24 pixels; transparent and isolated noise excluded");
 }
 static void CheckRasterEndpoints(PlanExtractedPath path,float tolerance,string label){
  Near(Mathf.Min(A(path).x,B(path).x),24.5f,tolerance,label+" start X");
  Near(Mathf.Max(A(path).x,B(path).x),135.5f,tolerance,label+" end X");
  Near(A(path).y,52.5f,tolerance,label+" centerline Y");
  Near(B(path).y,52.5f,tolerance,label+" centerline Y end");
 }
 static void PipelineChecks(){
    const int width=160,height=120;var pixels=Canvas(width,height);
    H(pixels,width,height,10,110,10,10,Magenta);H(pixels,width,height,10,110,90,90,Magenta);
    V(pixels,width,height,10,10,10,90,Magenta);V(pixels,width,height,110,110,10,90,Magenta);V(pixels,width,height,60,60,10,90,Magenta);
    V(pixels,width,height,145,145,10,100,Green);
    var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);FloorPlanData source;
    try{texture.SetPixels32(pixels);texture.Apply();source=FloorPlanCodec.Read(texture.EncodeToPNG(),"network-fixture.png");}
    finally{if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}
    source.authoring=PlanAuthoring.Create(source);
    var magenta=Rule("network",Color.magenta);var green=Rule("parallel",Color.green);magenta.height=green.height=5;green.thicknessMm=100;
    source.authoring.rules.Add(magenta);source.authoring.rules.Add(green);
    PlanAuthoring.Confirm(source,new PlanCalibration{method="Manual",unit="m/px",value=.01f});
    source.x=2.37f;source.z=-1.237f;source.rotation=37;
    var mask=Mask(pixels,width,height,magenta,green);var result=Extract(mask);
    Need(result.paths.Count==8&&!result.HasBlockingIssues,"clean two-color rectangle/partition must yield eight paths without tracing");
    var expectedEndpoints=new[]{new Vector2(10.5f,10.5f),new Vector2(60.5f,10.5f),new Vector2(110.5f,10.5f),
     new Vector2(10.5f,90.5f),new Vector2(60.5f,90.5f),new Vector2(110.5f,90.5f),new Vector2(145.5f,10.5f),new Vector2(145.5f,100.5f)};
    foreach(var path in result.paths)foreach(var endpoint in new[]{A(path),B(path)})
     Need(expectedEndpoints.Min(expected=>Vector2.Distance(expected,endpoint))<=1,"clean network extraction exceeded one original pixel endpoint tolerance");
    var before=Design.Example();string unchanged=JsonUtility.ToJson(before);
    var candidates=WallGenerationDiffs.CreatePaths(before,source,result,out var rejections);
    Need(candidates.Count==8&&rejections.Count==0,"clean calibrated paths were rejected");
    Near(candidates.Sum(path=>Vector3.Distance(path.aWorld,path.bWorld)),5.3f,.02f,"calibrated fixture total wall length");
    Need(candidates.Count(path=>path.ruleId=="network"&&path.thicknessMm==150)==7&&candidates.Count(path=>path.ruleId=="parallel"&&path.thicknessMm==100)==1,"category/physical thickness mismatch");
    foreach(var path in candidates){
     var restored=PlanAuthoring.WorldToPixel(source,path.aWorld);
     Near(Vector2.Distance(restored,new Vector2(path.sourceAPixel.x,path.sourceAPixel.y)),0,.001f,"pipeline source/world transform applied more than once");
    }
    var applied=WallGenerationDiffs.ApplyNewBatch(before,source,candidates);var batch=applied.generationBatches.Single();
    var joins=WallConnections.Preview(applied,batch.paths.Select(path=>path.itemId));WallConnections.Apply(applied,joins,false);Design.Validate(applied);
    Need(JsonUtility.ToJson(before)==unchanged&&applied.items.Count==15,"image workflow mutated the source design or duplicated walls");
    Need(applied.wallJunctions.Count==6&&applied.wallJunctions.Count(junction=>junction.kind=="T")==2,"clean partition did not create physical corner/T topology");
    var connected=WallConnections.ConnectedCluster(applied,batch.paths.First(path=>path.ruleId=="network").itemId);
    Need(connected.Count==7,"network connection swallowed the parallel category or left a wall disconnected");
    var mesh=WallConnections.BuildJoinedMesh(connected);
    try{Near(WallUnionGeometry.TopArea(mesh,5),.615f,.001f,"closed partition union footprint area");}
    finally{if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);}
    var repeat=WallGenerationDiffs.PreviewRegeneration(applied,batch.id,source,candidates);
    Need(!repeat.HasConflicts&&repeat.Count("Unchanged")==8,"unchanged clean-image regeneration produced changes/conflicts");
    var roundtrip=JsonUtility.FromJson<Design>(JsonUtility.ToJson(WallGenerationDiffs.ApplyRegeneration(applied,repeat)));Design.Validate(roundtrip);
    Need(roundtrip.items.Count==15&&roundtrip.wallJunctions.Count==6,"native roundtrip lost joined generated walls");
    UnityEngine.Debug.Log("ROOM_STUDIO_GENERATION_PIPELINE_CHECKS_PASSED: calibrated two-color PNG, 8 walls, transformed 5.3 m total, 6 physical joins, partition union, no tracing/duplicates, native roundtrip; mask/extraction CPU "+(mask.ElapsedMilliseconds+result.elapsedMilliseconds).ToString("F2")+" ms; endpoint tolerance 1 source pixel");
 }
}
}
