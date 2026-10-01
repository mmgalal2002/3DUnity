using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomStudio {
[Serializable] public class Barrier {
 // Thickness is stored in millimetres in the project file. Density is kg/m3.
 public string material="Concrete";
 public float thickness=150, density=2350;
 public bool shieldingEnabled=true,ctApplicabilityReviewed;
}
[Serializable] public class RegionItem {
 public string id=Guid.NewGuid().ToString(), name="Occupied region", scope="Wall";
 public float occupancy=1, designGoal=0.02f;
 public List<Vector2Data> points=new List<Vector2Data>();
}
[Serializable] public class Vector2Data { public float x,y; public Vector2Data(){} public Vector2Data(float x,float y){this.x=x;this.y=y;} }
[Serializable] public class FloorPlanSegment { public Vector2Data start=new Vector2Data(); public Vector2Data end=new Vector2Data(); }
[Serializable] public class FloorPlanData {
 public string kind="Image",sourceName="",sourcePath="",imageBase64="";
 public int pixelWidth,pixelHeight;
 public float metersPerPixel=.01f,widthMeters=10,heightMeters=10,opacity=.35f,x,z,rotation;
 public bool visible=true;
 public PlanAuthoringData authoring=null;
 public List<FloorPlanSegment> segments=new List<FloorPlanSegment>();
}
[Serializable] public class Item {
 public string id=Guid.NewGuid().ToString(), kind="Wall", name="Wall", model="CT"; public bool isControlled=true;
 // Zero transparency preserves fully opaque objects in older version-2 designs.
 public string groupId="";
 // Optional version-2 editor metadata. Missing in legacy/v2 files means unlocked.
 // Protection is independent of group membership and is excluded from canonical JSON.
 public bool locked=false;
 // Optional geometry for reusable custom components; older version-2 designs omit it.
 public ComponentShape component=null;
 // Optional wall-local door openings. Older native designs have no doors.
 public List<DoorOpening> doors=null;
 // Optional Request 5 path/batch identity. Missing in legacy/v2 files means
 // this is a hand-authored wall, independent of floor-plan generation.
 public GeneratedWallRef generated=null;
 public CtPointData ctPoint=null;
 public float transparency=0;
 public float Opacity { get=>1-transparency; set=>transparency=1-value; }
 public float x,z,y,angle,length=4,height=4,scale=1,occupancy=1,assessmentHeight=1.2f;
 public Barrier shielding=new Barrier();
}
[Serializable] public class Design {
 public string sourceJson="",sourceProjectionJson="",importSummary="";
 public int version=2, schemaVersion=2, documentRevision=1;
 public string calculationEngineVersion="ProShield-Unity-0.1";
 public string name="Bunker concept",selectedModel="CT";
 public float width=12,depth=10,height=5;
 public bool linkWallsToRoom=true;
 public Barrier floor=new Barrier(),ceiling=new Barrier();
 public FloorPlanData floorPlan=null;
 public List<WallGenerationBatch> generationBatches=new List<WallGenerationBatch>();
 public List<WallJunction> wallJunctions=new List<WallJunction>();
 public EditingPreferences editing=new EditingPreferences();
 public CtProjectData ct=null;
 public List<Item> items=new List<Item>();
 public List<RegionItem> regions=new List<RegionItem>();
 public string selectedTool="Select", selectedEquipmentType="linac", selectedWallMaterial="Concrete";
 public float sampleDistance=0.3f;
 public float energy=6,workload=500,gantry=0,fieldX=20,fieldY=20,isocentreHeight=1.3f,isoOffsetX=0,isoOffsetZ=-1.6f,sourceDistance=1;
 public string machine="Linac HD (user supplied model)";
 public static Design Example() {
  var d=new Design();
  d.items.Add(Wall("West wall",-6,0,10,90,5));
  d.items.Add(Wall("East wall",6,0,10,90,5));
  d.items.Add(Wall("North wall",0,5,12,0,5));
  d.items.Add(Wall("South wall",-1.5f,-5,9,0,5));
  d.items.Add(Wall("Maze return",2,-2.5f,5,90,5));
  d.items.Add(new Item{kind="LINAC",name="Linac HD",x=-2,z=1,scale=.75f});
  d.items.Add(new Item{kind="Desk",name="Workstation 01",x=8.5f,z=-3,scale=.33f});
  d.floor.thickness=200; d.ceiling.thickness=200; d.floor.density=2350; d.ceiling.density=2350;
  d.regions.Add(new RegionItem{name="Control room",scope="Wall",occupancy=.25f,designGoal=.1f,points=new List<Vector2Data>{new Vector2Data(6.2f,-4.5f),new Vector2Data(11,-4.5f),new Vector2Data(11,4.5f),new Vector2Data(6.2f,4.5f)}});
  return d;
 }
 public static Design ResetContents(Design current){
  if(current==null)throw new Exception("No room is available to reset.");
  var next=JsonUtility.FromJson<Design>(JsonUtility.ToJson(current));
  next.items=new List<Item>();next.regions=new List<RegionItem>();next.generationBatches=new List<WallGenerationBatch>();next.wallJunctions=new List<WallJunction>();
  next.floorPlan=null;next.ct=null;next.sourceJson="";next.sourceProjectionJson="";next.importSummary="";next.selectedTool="Select";
  next.width=Mathf.Max(3,next.width);next.depth=Mathf.Max(3,next.depth);next.height=Mathf.Max(2,next.height);
  Validate(next);return next;
 }
 public static Item Wall(string name,float x,float z,float length,float angle,float height) {return new Item{name=name,x=x,z=z,length=length,angle=angle,height=height};}
 // Resize endpoint vectors, not just lengths, so diagonal and maze walls remain connected.
 public static void ResizeRoom(Design d,float width,float depth,float height){
  Check(width,.001f,100);Check(depth,.001f,100);Check(height,.001f,100);
  if(d.linkWallsToRoom){
   double sx=width/d.width,sz=depth/d.depth;
   foreach(var wall in d.items)if(wall.kind=="Wall"){
    if(wall.locked&&(width!=d.width||depth!=d.depth||height-wall.y!=wall.height))throw new Exception("Unlock protected walls before resizing the linked room or ceiling.");
    Check(height-wall.y,.001f,100);Check((float)(wall.x*sx),-10000,10000);Check((float)(wall.z*sz),-10000,10000);
    double radians=wall.angle*Math.PI/180,dx=Math.Cos(radians)*wall.length*sx,dz=-Math.Sin(radians)*wall.length*sz;
    float resizedLength=(float)Math.Sqrt(dx*dx+dz*dz);Check(resizedLength,0,10000);
    DoorGeometry.ValidateDimensions(wall,resizedLength,height-wall.y);
   }
   if(d.wallJunctions!=null)foreach(var junction in d.wallJunctions){Check((float)(junction.x*sx),-10000,10000);Check((float)(junction.z*sz),-10000,10000);}
   foreach(var wall in d.items)if(wall.kind=="Wall"){
    if(width!=d.width||depth!=d.depth){
     double radians=wall.angle*Math.PI/180,dx=Math.Cos(radians)*wall.length*sx,dz=-Math.Sin(radians)*wall.length*sz;
     wall.x*=(float)sx;wall.z*=(float)sz;wall.length=(float)Math.Sqrt(dx*dx+dz*dz);
     if(wall.length>0)wall.angle=(float)(-Math.Atan2(dz,dx)*180/Math.PI);
    }
    wall.height=height-wall.y;
   }
   if(d.wallJunctions!=null)foreach(var junction in d.wallJunctions){junction.x*=(float)sx;junction.z*=(float)sz;}
  }
  d.width=width;d.depth=depth;d.height=height;
 }
 public static void Migrate(Design d){
  if(d==null)throw new Exception("Missing project data.");
  if(d.version!=1&&d.version!=2)throw new Exception("Unsupported project version.");
  if(d.version==1){
   if(d.floor==null||d.ceiling==null||d.items==null)throw new Exception("Incomplete legacy project.");
   foreach(var i in d.items)if(i==null||i.shielding==null)throw new Exception("Incomplete legacy object.");
   d.floor.thickness*=1000;d.ceiling.thickness*=1000;foreach(var i in d.items)i.shielding.thickness*=1000;
   d.version=2;d.schemaVersion=2;
  }
 }
 public static void Validate(Design d) {
  if(d==null || d.version!=2 || d.schemaVersion!=2 || d.items==null || d.items.Count>250) throw new Exception("Invalid or unsupported project file. Legacy files must be migrated before validation.");
  bool imported=!string.IsNullOrEmpty(d.sourceJson);if(imported&&string.IsNullOrEmpty(d.sourceProjectionJson))throw new Exception("Missing source projection.");
  Check(d.width,imported?.001f:3,100);Check(d.depth,imported?.001f:3,100);Check(d.height,imported?.001f:2,100); Shield(d.floor);Shield(d.ceiling);
  Check(d.energy,1,25);Check(d.workload,0,100000);Check(d.gantry,0,360);Check(d.fieldX,1,50);Check(d.fieldY,1,50);Check(d.isocentreHeight,.1f,10);Check(d.isoOffsetX,-10,10);Check(d.isoOffsetZ,-10,10);Check(d.sourceDistance,.1f,3);Check(d.sampleDistance,.05f,2);
    var ids=new HashSet<string>();
  foreach(var i in d.items){
   if(i==null || string.IsNullOrEmpty(i.id) || !ids.Add(i.id) || (i.kind!="Wall" && i.kind!="LINAC" && i.kind!="Desk" && i.kind!="Model" && i.kind!="Source" && i.kind!="Component")) throw new Exception("Invalid object record.");
  if((i.kind=="Model"||i.kind=="Source") && Array.IndexOf(new[]{"Linac","CT","PlanmecaViso","CathLab","Cyberknife","Mammography","MRI","Dental","Xray","Toilet","Basin","Chair","Dot"},i.model)<0)throw new Exception("Unknown visual model.");
   Check(i.x,-10000,10000);Check(i.z,-10000,10000);Check(i.y,-10000,10000);Check(i.angle,-100000,100000);Check(i.length,imported?0:.25f,10000);Check(i.height,.001f,100);Check(i.scale,.05f,3);Check(i.occupancy,0,1);Check(i.assessmentHeight,0,5);Shield(i.shielding);
   if(i.kind=="Component"){Check(i.height,.05f,100);ComponentGeometry.Validate(i.component);}
   if(i.doors!=null&&i.doors.Count>0)DoorGeometry.ValidateWall(i);
   Check(i.transparency,0,1);if(i.groupId!=null&&i.groupId.Length>128)throw new Exception("Invalid group ID.");
  }
  if(d.regions!=null){if(d.regions.Count>100)throw new Exception("Too many regions.");foreach(var r in d.regions){if(r==null||r.points==null||(!imported&&r.points.Count<3)||r.points.Count>100||string.IsNullOrEmpty(r.id)||!ids.Add(r.id)||(r.scope!="Wall"&&r.scope!="Floor"&&r.scope!="Ceiling"))throw new Exception("Invalid occupied region.");Check(r.occupancy,0,1);Check(r.designGoal,.0001f,1000);foreach(var p in r.points){if(p==null)throw new Exception("Missing region point.");Check(p.x,-10000,10000);Check(p.y,-10000,10000);}}}
  ValidateFloorPlan(d.floorPlan);
  PrecisionEditing.Validate(d.editing);
  WallGenerationData.Validate(d);
  WallConnections.ValidateStored(d);
  CtShieldingData.Validate(d);
 }
 static void ValidateFloorPlan(FloorPlanData p){if(!IsEmptyFloorPlan(p))FloorPlanCodec.Validate(p);}
 static void Check(float f,float min,float max){if(float.IsNaN(f)||float.IsInfinity(f)||f<min||f>max)throw new Exception("A project value is outside the supported range.");}
 static void Shield(Barrier b){if(b==null || (b.material!="Concrete"&&b.material!="Steel"&&b.material!="Lead"&&b.material!="Gypsum"&&b.material!="Glass"&&b.material!="PlateGlass"&&b.material!="Wood"))throw new Exception("Unknown shielding material.");Check(b.thickness,0,100000);Check(b.density,100,25000);}
 // JsonUtility materializes a null inline DTO with field defaults during round-trip.
 public static bool IsEmptyFloorPlan(FloorPlanData p){return p==null||((string.IsNullOrEmpty(p.kind)||p.kind=="Image")&&p.pixelWidth==0&&p.pixelHeight==0&&string.IsNullOrEmpty(p.imageBase64)&&string.IsNullOrEmpty(p.sourceName)&&string.IsNullOrEmpty(p.sourcePath)&&(p.segments==null||p.segments.Count==0));}
 public static void ValidateFloorPlanForEditor(FloorPlanData p){ValidateFloorPlan(p);}
}
public class Selectable : MonoBehaviour { public string id; }
}
