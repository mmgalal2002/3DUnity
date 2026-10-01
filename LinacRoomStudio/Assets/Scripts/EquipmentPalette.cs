using System;
using UnityEngine;

namespace RoomStudio {
public static class EquipmentPalette {
 public sealed class Entry {
  public readonly string group,label,model;
  public readonly DiagnosticMachineType machineType;
  public Entry(string group,string label,string model,DiagnosticMachineType machineType=DiagnosticMachineType.Unconfigured){this.group=group;this.label=label;this.model=model;this.machineType=machineType;}
 }
 public static readonly string[] Groups={"Linac","CT","MRI","Room items"};
 public static readonly Entry[] Entries={
  new Entry("Linac","Linac HD","Linac",DiagnosticMachineType.Treatment),new Entry("Linac","CyberKnife","Cyberknife",DiagnosticMachineType.Treatment),
  new Entry("CT","CT","CT",DiagnosticMachineType.ConventionalCt),new Entry("CT","CathLab","CathLab",DiagnosticMachineType.Fluoroscopy),new Entry("CT","Dental OPG","PlanmecaViso",DiagnosticMachineType.DentalPanoramic),new Entry("CT","X-ray","Xray",DiagnosticMachineType.GeneralRadiography),new Entry("CT","Mammography","Mammography",DiagnosticMachineType.Mammography),new Entry("CT","Dental","Dental",DiagnosticMachineType.DentalIntraoral),
  new Entry("MRI","MRI","MRI",DiagnosticMachineType.Mri),
  new Entry("Room items","Toilet","Toilet"),new Entry("Room items","Basin","Basin"),new Entry("Room items","Chair","Chair")
 };
 public static Item Create(string model,Vector3 position){
  var entry=Array.Find(Entries,e=>e.model==model);if(entry==null)throw new ArgumentException("Unknown equipment model.");
  bool linac=model=="Linac";
  var item=new Item{kind=linac?"LINAC":"Model",model=model,name=linac?"Linac HD":entry.label+" (visual only)",x=position.x,y=position.y,z=position.z,scale=linac?.75f:1};
  item.machineType=entry.machineType;return item;
 }
}
}
