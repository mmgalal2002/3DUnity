using System;
using UnityEngine;

namespace RoomStudio {
public static class EquipmentPalette {
 public sealed class Entry {
  public readonly string group,label,model;
  public Entry(string group,string label,string model){this.group=group;this.label=label;this.model=model;}
 }
 public static readonly string[] Groups={"Linac","CT","MRI","Room items"};
 public static readonly Entry[] Entries={
  new Entry("Linac","Linac HD","Linac"),new Entry("Linac","CyberKnife","Cyberknife"),
  new Entry("CT","CT","CT"),new Entry("CT","CathLab","CathLab"),new Entry("CT","Dental OPG","PlanmecaViso"),new Entry("CT","X-ray","Xray"),new Entry("CT","Mammography","Mammography"),new Entry("CT","Dental","Dental"),
  new Entry("MRI","MRI","MRI"),
  new Entry("Room items","Toilet","Toilet"),new Entry("Room items","Basin","Basin"),new Entry("Room items","Chair","Chair")
 };
 public static Item Create(string model,Vector3 position){
  var entry=Array.Find(Entries,e=>e.model==model);if(entry==null)throw new ArgumentException("Unknown equipment model.");
  bool linac=model=="Linac";
  return new Item{kind=linac?"LINAC":"Model",model=model,name=linac?"Linac HD":entry.label+" (visual only)",x=position.x,y=position.y,z=position.z,scale=linac?.75f:1};
 }
}
}
