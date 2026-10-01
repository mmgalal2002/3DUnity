using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
[Serializable] public class ComponentPreset {
 public string id=Guid.NewGuid().ToString(),name="New component";
 public Item item;
}
[Serializable] public class ComponentLibrary {
 public string format="RoomStudio.Components";
 public int version=1;
 public List<ComponentPreset> components=new List<ComponentPreset>();
 [Serializable] class Header { public string format=null; public int version=0; }
 public static T Clone<T>(T value)=>ReferenceEquals(value,null)?default(T):JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
 public static ComponentLibrary Defaults(){
  var library=new ComponentLibrary();
  library.components.Add(FromItem(new Item{kind="Component",name="Trapezoidal wall",height=3,component=new ComponentShape()},"Trapezoidal wall"));
  library.components.Add(FromItem(new Item{kind="Component",name="Rounded wall",height=3,component=new ComponentShape{type="Rounded"}},"Rounded wall"));
  library.components.Add(FromItem(Design.Wall("Straight wall",0,0,4,0,3),"Straight wall"));return library;
 }
 public static ComponentPreset FromItem(Item item,string name){
  var preset=new ComponentPreset{name=name,item=Clone(item)};preset.item.id=Guid.NewGuid().ToString();preset.item.name=name;preset.item.x=preset.item.y=preset.item.z=0;preset.item.groupId="";preset.item.locked=false;ValidatePreset(preset);return preset;
 }
 public static Item Place(ComponentPreset preset,Vector3 position){ValidatePreset(preset);var item=Clone(preset.item);item.id=Guid.NewGuid().ToString();item.name=preset.name;item.groupId="";item.locked=false;item.x=position.x;item.y=position.y;item.z=position.z;return item;}
 public static void ValidatePreset(ComponentPreset p){
  if(p==null||string.IsNullOrWhiteSpace(p.id)||p.id.Length>128||string.IsNullOrWhiteSpace(p.name)||p.name.Length>80||p.item==null||(p.item.kind!="Wall"&&p.item.kind!="Component"))throw new Exception("A component needs a name (1–80 characters) and wall or custom-shape geometry.");
  Design.Validate(new Design{items=new List<Item>{p.item}});
 }
 public void Validate(){
  if(format!="RoomStudio.Components"||version!=1||components==null||components.Count>100)throw new Exception("Unsupported component library; maximum 100 presets.");
  var ids=new HashSet<string>();foreach(var p in components){ValidatePreset(p);if(!ids.Add(p.id))throw new Exception("Duplicate component ID.");}
 }
 public static ComponentLibrary Parse(string json){
  if(string.IsNullOrWhiteSpace(json)||json.Length>2*1024*1024)throw new Exception("Choose a component JSON file smaller than 2 MB.");
  var header=JsonUtility.FromJson<Header>(json);if(header==null||header.format!="RoomStudio.Components"||header.version!=1)throw new Exception("Choose a Room Studio component export (version 1).");
  var result=JsonUtility.FromJson<ComponentLibrary>(json);result.Validate();return result;
 }
 public string Export(string id=null){Validate();var output=id==null?this:new ComponentLibrary{components=components.Where(p=>p.id==id).ToList()};if(id!=null&&output.components.Count==0)throw new Exception("Select a saved component first.");return JsonUtility.ToJson(output,true);}
 public void Import(string json){
  var imported=Parse(json);if(components.Count+imported.components.Count>100)throw new Exception("Import would exceed the 100-preset limit.");
  var additions=imported.components.Select(p=>{var copy=Clone(p);copy.id=Guid.NewGuid().ToString();copy.item.id=Guid.NewGuid().ToString();return copy;}).ToList();components.AddRange(additions);
 }
}
public sealed class ComponentLibraryStore {
 public readonly string DirectoryPath;
 public string FilePath=>Path.Combine(DirectoryPath,"components.json");
 public ComponentLibraryStore(string directory){DirectoryPath=directory;}
 public ComponentLibrary Load()=>File.Exists(FilePath)?ComponentLibrary.Parse(File.ReadAllText(FilePath)):ComponentLibrary.Defaults();
 public void Save(ComponentLibrary library){
  string json=library.Export();Directory.CreateDirectory(DirectoryPath);string temp=FilePath+".tmp";File.WriteAllText(temp,json);
  if(File.Exists(FilePath))File.Copy(FilePath,FilePath+".bak",true);File.Copy(temp,FilePath,true);File.Delete(temp);
 }
}
}
