using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public static class WallConnectionChecks {
 static void Require(bool condition,string message){if(!condition)throw new Exception("Wall connection regression: "+message);}
 static void Near(float a,float b,string message,float tolerance=.0001f){Require(Mathf.Abs(a-b)<=tolerance,message+" ("+a+" vs "+b+")");}
 static Item Wall(float x,float z,float length,float angle){var wall=Design.Wall("Join fixture",x,z,length,angle,3);wall.shielding.thickness=150;return wall;}
 static Design DesignWith(params Item[] walls)=>new Design{items=new List<Item>(walls)};
 static bool Reject(Action action){try{action();return false;}catch{return true;}}
 static void DisposeMesh(Mesh mesh){if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);}
 public static void Run(){
  var a=Wall(-1,0,2,0);var b=Wall(1.015f,0,2,0);var design=DesignWith(a,b);
  string unchanged=JsonUtility.ToJson(design);
  var plan=WallConnections.Preview(design,new[]{a.id,b.id});
  Require(plan.HasChanges&&plan.junctions.Single().kind=="Collinear","0.015 m gap did not produce one collinear join");
  Require(JsonUtility.ToJson(design)==unchanged,"preview mutated the design");
  WallConnections.Apply(design,plan);Design.Validate(design);
  Near(Vector3.Distance(PrecisionEditing.Endpoint(design.items[0],true),PrecisionEditing.Endpoint(design.items[1],false)),0,"joined centerline seam");
  var mesh=WallConnections.BuildJoinedMesh(WallConnections.ConnectedCluster(design,a.id));
  Near(WallUnionGeometry.TopArea(mesh,3),4.015f*.15f,"collinear physical footprint area");
  Near(WallUnionGeometry.SideArea(mesh),2*(4.015f+.15f)*3,"collinear union has no buried seam faces",.002f);
  DisposeMesh(mesh);
  Require(Reject(()=>WallConnections.Apply(design,plan)),"stale plan applied twice");
  var beforeEdit=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  var edited=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  var editedWall=edited.items.Find(i=>i.id==a.id);var editedFixed=PrecisionEditing.Endpoint(editedWall,false);
  PrecisionEditing.ResizeEndpoint(editedWall,editedFixed,PrecisionEditing.Endpoint(editedWall,true)+Vector3.right*.01f,true);
  var propagated=WallConnections.PropagateEdit(beforeEdit,edited,a.id);
  Near(Vector3.Distance(PrecisionEditing.Endpoint(propagated.items.Find(i=>i.id==a.id),true),PrecisionEditing.Endpoint(propagated.items.Find(i=>i.id==b.id),false)),0,"connected endpoint edit propagated");
  Near(propagated.wallJunctions[0].x,PrecisionEditing.Endpoint(propagated.items.Find(i=>i.id==a.id),true).x,"junction followed endpoint");
  beforeEdit.items.Find(i=>i.id==b.id).locked=true;edited.items.Find(i=>i.id==b.id).locked=true;
  Require(Reject(()=>WallConnections.PropagateEdit(beforeEdit,edited,a.id)),"locked neighbor accepted propagated edit");

  a=Wall(-1,0,2,0);b=Wall(1.03f,0,2,0);design=DesignWith(a,b);
  plan=WallConnections.Preview(design,new[]{a.id,b.id});Require(!plan.HasChanges,"0.03 m gap bridged with 0.02 m tolerance");
  a=Wall(0,0,2,0);b=Wall(1.015f,-1,2,90);design=DesignWith(a,b);
  plan=WallConnections.Preview(design,new[]{a.id,b.id});Require(plan.HasChanges&&plan.junctions.Single().kind=="Corner","L corner was not joined");
  WallConnections.Apply(design,plan);mesh=WallConnections.BuildJoinedMesh(WallConnections.ConnectedCluster(design,a.id));
  Near(WallUnionGeometry.TopArea(mesh,3),.5955f,"L footprint union includes overlap once",.001f);DisposeMesh(mesh);

  a=Wall(0,0,4,0);b=Wall(0,-1.085f,2,90);design=DesignWith(a,b);
  plan=WallConnections.Preview(design,new[]{a.id,b.id});Require(plan.HasChanges&&plan.junctions.Single().kind=="T","T junction was not found at wall boundary");
  WallConnections.Apply(design,plan);Require(design.wallJunctions[0].arms.Any(arm=>arm.endpoint=="Interior"),"T host topology missing");
  mesh=WallConnections.BuildJoinedMesh(WallConnections.ConnectedCluster(design,a.id));
  Near(WallUnionGeometry.TopArea(mesh,3),.9015f,"T footprint union includes branch once",.0002f);DisposeMesh(mesh);

  a=Wall(0,0,4,0);b=Wall(0,0,4,90);design=DesignWith(a,b);
  plan=WallConnections.Preview(design,new[]{a.id,b.id});Require(plan.HasChanges&&plan.junctions.Single().kind=="X"&&plan.additions.Count==2,"X crossing did not split both walls");
  WallConnections.Apply(design,plan);Require(design.items.Count==4&&design.wallJunctions[0].arms.Count==4,"X split/topology count");
  mesh=WallConnections.BuildJoinedMesh(WallConnections.ConnectedCluster(design,a.id));
  Near(WallUnionGeometry.TopArea(mesh,3),1.1775f,"X footprint union excludes duplicate crossing",.0002f);DisposeMesh(mesh);

  foreach(int armCount in new[]{3,4}){
   var arms=new List<Item>{Wall(-1,0,2,0),Wall(1,0,2,0),Wall(0,-1,2,90)};
   if(armCount==4)arms.Add(Wall(0,1,2,90));
   design=DesignWith(arms.ToArray());unchanged=JsonUtility.ToJson(design);
   plan=WallConnections.Preview(design,arms.Select(wall=>wall.id));
   Require(plan.junctions.Count==1&&plan.junctions[0].arms.Count==armCount&&plan.junctions[0].kind==(armCount==3?"T":"X"),"coincident extracted branches were treated as ambiguous");
   Require(JsonUtility.ToJson(design)==unchanged&&plan.additions.Count==0,"already split endpoint preview mutated or duplicated walls");
   WallConnections.Apply(design,plan);Design.Validate(design);
   mesh=WallConnections.BuildJoinedMesh(WallConnections.ConnectedCluster(design,arms[0].id));
   Near(WallUnionGeometry.TopArea(mesh,3),armCount==3?.88875f:1.1775f,"multi-arm junction physical union",.0002f);DisposeMesh(mesh);
  }
  a=Wall(-1,0,2,0);b=Wall(1.01f,0,2,0);var competing=Wall(0,-1.01f,2,90);design=DesignWith(a,b,competing);
  Require(!WallConnections.Preview(design,design.items.Select(wall=>wall.id)).HasChanges,"competing non-coincident endpoints lost ambiguity protection");

  a=Wall(-1,0,2,0);b=Wall(1,0,2,0);
  a.generated=new GeneratedWallRef{batchId="preview",pathId="left",ruleId="magenta",categoryName="A",displayColor=Color.magenta};
  b.generated=new GeneratedWallRef{batchId="preview",pathId="right",ruleId="green",categoryName="B",displayColor=Color.green};
  Require(!WallConnections.PreviewWalls(new[]{a,b}).HasChanges,"cross-category join lacked explicit permission");
  Require(WallConnections.PreviewWalls(new[]{a,b},.02f,true).HasChanges,"explicit cross-category join was rejected");
  mesh=WallConnections.BuildJoinedMesh(new[]{a,b},true);
  Require(mesh.subMeshCount==2&&mesh.GetTriangles(0).Length>0&&mesh.GetTriangles(1).Length>0,"joined categories lost separate surface materials");
  Near(WallUnionGeometry.TopArea(mesh,3),.6f,"category submeshes duplicated physical caps");
  Near(WallUnionGeometry.SideArea(mesh),2*(4+.15f)*3,"category submeshes duplicated seam/collision surfaces",.002f);DisposeMesh(mesh);

  foreach(var fixture in new[]{new{angle=45f,secondThickness=150f},new{angle=45f,secondThickness=250f},new{angle=20f,secondThickness=150f}}){
   a=Wall(0,0,2,0);b=Wall(0,0,2,fixture.angle);b.shielding.thickness=fixture.secondThickness;
   design=DesignWith(a,b);plan=WallConnections.Preview(design,new[]{a.id,b.id});
   Require(plan.HasChanges&&plan.junctions.Single().kind=="X","diagonal or unequal-thickness crossing was not joined");
   WallConnections.Apply(design,plan);Design.Validate(design);
   mesh=WallConnections.BuildJoinedMesh(WallConnections.ConnectedCluster(design,a.id));
   float firstWidth=a.shielding.thickness/1000f,secondWidth=b.shielding.thickness/1000f;
   float expectedArea=2*firstWidth+2*secondWidth-firstWidth*secondWidth/Mathf.Sin(fixture.angle*Mathf.Deg2Rad);
   Near(WallUnionGeometry.TopArea(mesh,3),expectedArea,"diagonal/unequal-thickness physical footprint union",.0005f);
   Require(mesh.vertices.All(vertex=>!float.IsNaN(vertex.x)&&!float.IsNaN(vertex.y)&&!float.IsNaN(vertex.z)
    &&!float.IsInfinity(vertex.x)&&!float.IsInfinity(vertex.y)&&!float.IsInfinity(vertex.z)),"diagonal union has non-finite vertices");
   DisposeMesh(mesh);
  }

  // X splitting a generated wall must create matching persisted paths rather
  // than reusing a path ID or severing the original source-image reference.
  a=Wall(0,0,4,0);b=Wall(0,0,4,90);design=DesignWith(a,b);
  var source=PlanAuthoringChecks.Fixture();
  var batch=new WallGenerationBatch{id="fixture-batch",sourceFingerprint=PlanAuthoring.Fingerprint(source),snapshotSignature=WallGenerationData.SnapshotSignature(source),
   pixelWidth=source.pixelWidth,pixelHeight=source.pixelHeight,sourceSnapshot=source};
  foreach(var wall in new[]{a,b}){
   string id="path-"+wall.id;
   var start=PrecisionEditing.Endpoint(wall,false);var end=PrecisionEditing.Endpoint(wall,true);
   var sourceA=new Vector2Data(100,100);var sourceB=new Vector2Data(500,100);
   wall.generated=new GeneratedWallRef{batchId=batch.id,pathId=id,ruleId="fixture-rule",categoryName="Fixture",displayColor=Color.magenta,sourceAPixel=sourceA,sourceBPixel=sourceB};
   batch.paths.Add(new GeneratedWallPath{id=id,itemId=wall.id,ruleId="fixture-rule",categoryName="Fixture",material="Concrete",displayColor=Color.magenta,
    sourceAPixel=new Vector2Data(100,100),sourceBPixel=new Vector2Data(500,100),aPixel=new Vector2Data(100,100),bPixel=new Vector2Data(500,100),
    aWorld=start,bWorld=end,lengthPixels=400,height=3,thicknessMm=150,densityKgM3=2350});
  }
  design.generationBatches.Add(batch);Design.Validate(design);
  plan=WallConnections.Preview(design,new[]{a.id,b.id});WallConnections.Apply(design,plan);Design.Validate(design);
  Require(design.generationBatches[0].paths.Count==4&&design.items.All(item=>design.generationBatches[0].paths.Any(path=>path.itemId==item.id)),"generated X split lost path provenance");

  a=Wall(-1,0,2,0);b=Wall(1.015f,0,2,0);a.locked=true;design=DesignWith(a,b);
  plan=WallConnections.Preview(design,new[]{a.id,b.id});Require(plan.HasChanges,"locked anchor did not allow unlocked neighbor to connect");
  WallConnections.Apply(design,plan);Near(design.items[0].x,-1,"locked anchor moved");
  a=Wall(-1,0,2,0);b=Wall(1.015f,0,2,0);a.groupId="fixture";design=DesignWith(a,b);
  Require(!WallConnections.Preview(design,new[]{a.id,b.id}).HasChanges,"group wall geometry changed without rigid group review");
  a=Wall(-1,0,2,0);b=Wall(1.015f,0,2,0);a.doors=new List<DoorOpening>{DoorGeometry.New(0,3)};design=DesignWith(a,b);
  Require(!WallConnections.Preview(design,new[]{a.id,b.id}).HasChanges,"door-bearing wall was connected");
  a=Wall(-1,0,2,0);b=Wall(1.015f,0,2,0);design=DesignWith(a,b);
  Require(!WallConnections.Preview(design,new[]{a.id,b.id},.02f,false,(from,to)=>true).HasChanges,"confirmed opening guard was bypassed");
  Debug.Log("ROOM_STUDIO_WALL_CONNECTION_CHECKS_PASSED: bounded gap, pure/stale preview, L/T/X/diagonal/acute/unequal-thickness union areas, topology, locked anchor, group/door/opening guards");
 }
}
}
