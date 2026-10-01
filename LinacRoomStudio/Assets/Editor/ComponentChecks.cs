using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RoomStudio;

public static class ComponentChecks {
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Require(rejected,message);}
 public static void Run(){
  var concave=new ComponentShape{type="Polygon",points=new List<Vector2Data>{new Vector2Data(0,0),new Vector2Data(4,0),new Vector2Data(4,1),new Vector2Data(1,1),new Vector2Data(1,4),new Vector2Data(0,4)}};
  foreach(var shape in new[]{new ComponentShape(),new ComponentShape{type="Rounded"},concave}){
   var footprint=ComponentGeometry.Footprint(shape);var mesh=ComponentGeometry.Mesh(shape,3);var vertices=mesh.vertices;var triangles=mesh.triangles;double volume=0;float topArea=0;
   for(int n=0;n<triangles.Length;n+=3){var a=vertices[triangles[n]];var b=vertices[triangles[n+1]];var c=vertices[triangles[n+2]];volume+=Vector3.Dot(a,Vector3.Cross(b,c))/6.0;if(a.y==3&&b.y==3&&c.y==3){var normal=Vector3.Cross(b-a,c-a);Require(normal.y>0,"Top face winding is inverted");topArea+=normal.magnitude/2;}}
   float expected=ComponentGeometry.Area(footprint);Require(Math.Abs(volume-expected*3)<.002,"Mesh volume does not match the footprint extrusion");Require(Mathf.Abs(topArea-expected)<.002f,"Cap triangulation overlaps or has a hole");UnityEngine.Object.DestroyImmediate(mesh);
  }
  Require(Mathf.Abs(ComponentGeometry.Area(ComponentGeometry.Footprint(concave))-7)<.0001f,"Concave footprint area changed");
  concave.points.Reverse();ComponentGeometry.Validate(concave);
  var invalid=ComponentLibrary.Clone(concave);invalid.points=new List<Vector2Data>{new Vector2Data(0,0),new Vector2Data(2,2),new Vector2Data(0,2),new Vector2Data(2,0)};Reject(()=>ComponentGeometry.Validate(invalid),"Crossing polygon accepted");
  invalid.points[1].x=float.NaN;Reject(()=>ComponentGeometry.Validate(invalid),"NaN polygon accepted");
  Reject(()=>ComponentGeometry.Validate(new ComponentShape{type="Rounded",radius=.1f,wallThickness=1}),"Negative inner radius accepted");
  var library=ComponentLibrary.Defaults();var preset=ComponentLibrary.FromItem(new Item{kind="Component",component=concave,height=4},"Custom concave");library.components.Add(preset);library.Validate();
    foreach(var definition in library.components){
     string unchanged=JsonUtility.ToJson(definition);var position=new Vector3(1.237f,0,-2.37f);
     var first=ComponentLibrary.Place(definition,position);var second=ComponentLibrary.Place(definition,position);
     Require(first.id!=second.id&&first.id!=definition.item.id,"Click placement must create independent scene identities");
     Require(first.x==position.x&&first.z==position.z,"Click placement changed the requested room position");
     Require(JsonUtility.ToJson(definition)==unchanged,"Click placement mutated the selected preset");
    }
  var placed=ComponentLibrary.Place(preset,new Vector3(3,2,1));Require(placed.id!=preset.item.id&&placed.x==3&&placed.y==2,"Placement reused preset identity or position");
  preset.item.component.points[0].x=99;Require(placed.component.points[0].x!=99,"Preset edit mutated an existing placement");preset.item.component=ComponentLibrary.Clone(concave);
  var design=Design.Example();design.items.Add(placed);Design.Validate(design);var restored=ComponentLibrary.Clone(design);Design.Validate(restored);Require(restored.items.Last().component.points.Count==6,"Native custom shape round-trip lost vertices");
  var single=ComponentLibrary.Parse(library.Export(preset.id));Require(single.components.Count==1&&single.components[0].name=="Custom concave","Single preset JSON export changed content");
  var imported=new ComponentLibrary();imported.Import(single.Export());imported.Import(single.Export());Require(imported.components.Count==2&&imported.components[0].id!=imported.components[1].id,"Repeated imports reused IDs");
  string before=imported.Export();Reject(()=>imported.Import("{\"format\":\"RoomStudio.Components\",\"version\":999}"),"Unknown library version accepted");Require(imported.Export()==before,"Invalid import partially mutated library");Reject(()=>ComponentLibrary.Parse("{}"),"Unrelated JSON accepted as library");
  string directory=Path.Combine(Application.temporaryCachePath,"ComponentChecks-"+Guid.NewGuid().ToString("N"));
  try{var store=new ComponentLibraryStore(directory);store.Save(library);Require(store.Load().Export()==library.Export(),"Stored library round-trip changed data");library.components.RemoveAt(0);store.Save(library);Require(ComponentLibrary.Parse(File.ReadAllText(store.FilePath+".bak")).components.Count==4,"Library backup failed");library.components.Clear();store.Save(library);Require(store.Load().components.Count==0,"An empty saved library was repopulated");}
  finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
  Debug.Log("ROOM_STUDIO_COMPONENT_CHECKS_PASSED: mesh volume/winding, concave polygons, invalid geometry, independent presets, JSON schema/import/export, native persistence and disk library backups");
 }
}
