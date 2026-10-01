using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RoomStudio {
[Serializable] public sealed class DiagnosticExposureDefinition {
 public string key="",label="",unit="";
 public double minimum,maximum;
}
[Serializable] public sealed class DiagnosticFit {
 public string id="",material="",spectrumId="",provenance="";
 public double alpha,beta,gamma;
 public ArcherFit Archer=>new ArcherFit(alpha,beta,gamma);
}
[Serializable] public sealed class DiagnosticSample {
 public DiagnosticPointRole origin;
 public CtVector offsetMeters=new CtVector();
 public double weight;
}
[Serializable] public sealed class DiagnosticComponent {
 public string id="",kind="",provider="ReferencePointKerma",spectrumId="",normalization="ProfileExposure",exposureKey="";
 public double referenceMgy,referenceDistanceMeters,minimumDistanceMeters,maximumDistanceMeters;
 public bool patientInverseSquare;
 public double referencePatientDistanceMeters,minimumPatientDistanceMeters,maximumPatientDistanceMeters;
 public string angularModel="",validityDomain="",provenance="";
 public List<DiagnosticSample> samples=new List<DiagnosticSample>();
 public List<DiagnosticFit> fits=new List<DiagnosticFit>();
}
[Serializable] public sealed class DiagnosticProfile {
 public int version=1;
 public string id="",revision="",machineIdentity="",acquisition="",spectrumId="",exposureDefinition="",basis="",provenance="";
 public DiagnosticMachineType machineType;
 public double tubeVoltageKvp;
 public string quantity="airKerma",unit="mGy";
 public bool demoOnly,includesRoomBarriers;
 public List<string> requiredComponents=new List<string>();
 public List<DiagnosticExposureDefinition> inputs=new List<DiagnosticExposureDefinition>();
 public List<DiagnosticComponent> components=new List<DiagnosticComponent>();
}
[Serializable] public sealed class DiagnosticTrustEntry { public string id="",revision="",sha256=""; }
[Serializable] public sealed class DiagnosticTrustManifest {
 public int version=1;
 public List<DiagnosticTrustEntry> profiles=new List<DiagnosticTrustEntry>();
}
public static class DiagnosticProfiles {
 static readonly Dictionary<string,string> installed=new Dictionary<string,string>();
 static DiagnosticTrustManifest trust;
 public static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
 static void Load(){
  if(trust!=null)return;
  var manifest=Resources.Load<TextAsset>("DiagnosticProfiles/trusted-profiles");
  var next=manifest==null?new DiagnosticTrustManifest():JsonUtility.FromJson<DiagnosticTrustManifest>(manifest.text);
  if(next==null||next.version!=1||next.profiles==null||next.profiles.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id)||string.IsNullOrWhiteSpace(p.revision)||p.sha256.Length!=64)||next.profiles.Select(p=>p.id+"@"+p.revision).Distinct().Count()!=next.profiles.Count)
   throw new Exception("Invalid independently managed diagnostic profile trust manifest.");
  var catalogue=new Dictionary<string,string>();
  foreach(var asset in Resources.LoadAll<TextAsset>("DiagnosticProfiles/Catalogue")){
   var profile=ParseApproved(asset.text,next);catalogue.Add(profile.id+"@"+profile.revision,asset.text);
  }
  foreach(var entry in catalogue)installed.Add(entry.Key,entry.Value);
  trust=next;
 }
 public static DiagnosticProfile Import(string json){
  Load();
  var profile=ParseApproved(json,trust);
  installed[profile.id+"@"+profile.revision]=json;return profile;
 }
 static DiagnosticProfile ParseApproved(string json,DiagnosticTrustManifest manifest){
  if(string.IsNullOrWhiteSpace(json)||Encoding.UTF8.GetByteCount(json)>1024*1024)throw new Exception("Machine profile is empty or exceeds 1 MiB.");
  var profile=JsonUtility.FromJson<DiagnosticProfile>(json);
  Validate(profile);
  string hash=Hash(json);
  if(profile.demoOnly||!manifest.profiles.Any(entry=>entry.id==profile.id&&entry.revision==profile.revision&&entry.sha256==hash))
   throw new Exception("This profile/version is not in the independently approved catalogue. Ask the model provider to supply an approved catalogue release; an approval checkbox cannot establish validity.");
  return profile;
 }
 public static DiagnosticProfile Resolve(DiagnosticMachine machine){
  Load();if(machine==null||string.IsNullOrEmpty(machine.profileId))return null;
  if(!installed.TryGetValue(machine.profileId+"@"+machine.profileVersion,out string json)||Hash(json)!=machine.profileHash)return null;
  return JsonUtility.FromJson<DiagnosticProfile>(json);
 }
 public static string InstalledHash(DiagnosticProfile profile){Load();return Hash(installed[profile.id+"@"+profile.revision]);}
 public static string SnapshotJson(DiagnosticProfile profile){
  Load();if(installed.TryGetValue(profile.id+"@"+profile.revision,out string json))return json;
  if(profile.demoOnly)return JsonUtility.ToJson(profile);
  throw new Exception("The calculation profile is no longer installed.");
 }
 public static DiagnosticProfile[] Matching(DiagnosticMachine machine){
  Load();
  return installed.Values.Select(JsonUtility.FromJson<DiagnosticProfile>).Where(p=>p.machineType==machine.machineType&&machine.tubeVoltageKvp.TryGet(out double kvp)&&p.tubeVoltageKvp==kvp).ToArray();
 }
 public static string ReferenceFamily(DiagnosticMachineType type){
  switch(type){
   case DiagnosticMachineType.GeneralRadiography:return "PRIMARY_RADIOGRAPHIC";
   case DiagnosticMachineType.Mammography:return "PRIMARY_MAMMOGRAPHIC";
   case DiagnosticMachineType.ConventionalCt:return "CT_SECONDARY";
   default:return "";
  }
 }
 public static double[] SupportedTubeVoltages(DiagnosticMachineType type){
  Load();
  if(!DiagnosticData.IsXray(type))return new double[0];
  string family=ReferenceFamily(type);
  var reference=string.IsNullOrEmpty(family)?Enumerable.Empty<double>():CtCoefficientLibrary.SupportedTubeVoltages(family).Select(kvp=>(double)kvp);
  var profiles=installed.Values.Select(JsonUtility.FromJson<DiagnosticProfile>).Where(p=>p.machineType==type).Select(p=>p.tubeVoltageKvp);
  return reference.Concat(profiles).Distinct().OrderBy(kvp=>kvp).ToArray();
 }
 public static double[] SelectableTubeVoltages(DiagnosticMachineType type){
  Load();
  if(!DiagnosticData.IsXray(type))return new double[0];
  var reference=CtCoefficientLibrary.SupportedTubeVoltages("CT_SECONDARY").Select(kvp=>(double)kvp);
  var profiles=installed.Values.Select(JsonUtility.FromJson<DiagnosticProfile>).Select(p=>p.tubeVoltageKvp);
  return reference.Concat(profiles).Distinct().OrderBy(kvp=>kvp).ToArray();
 }
 public static bool SelectTubeVoltage(Design design,DiagnosticMachine machine,double kvp){
  if(design?.diagnosticCalculation?.machines==null||machine==null||!design.diagnosticCalculation.machines.Contains(machine))throw new Exception("Choose a configured diagnostic machine.");
  if(design.items.Find(i=>i.id==machine.machineId)?.locked!=false)throw new Exception("Unlock the machine before changing its tube voltage.");
  if(!SelectableTubeVoltages(machine.machineType).Contains(kvp))throw new Exception("Choose a tube voltage listed in the shared diagnostic dropdown.");
  if(machine.tubeVoltageKvp.TryGet(out double previous)&&previous==kvp)return false;
  machine.tubeVoltageKvp.Set(kvp);
  var profile=Resolve(machine);
  if(profile!=null&&profile.tubeVoltageKvp!=kvp){machine.profileId="";machine.profileVersion="";machine.profileHash="";}
  return true;
 }
 public static void Attach(Design design,DiagnosticMachine machine,DiagnosticProfile profile){
  if(design.items.Find(i=>i.id==machine.machineId)?.locked!=false)throw new Exception("Unlock the machine before changing its profile.");
  if(machine.machineType!=profile.machineType)throw new Exception("Profile family does not match the selected machine.");
  if(!machine.tubeVoltageKvp.TryGet(out double kvp)||kvp!=profile.tubeVoltageKvp)throw new Exception("Profile requires "+profile.tubeVoltageKvp+" kVp. Select that voltage from the dropdown if it describes this acquisition.");
  machine.profileId=profile.id;machine.profileVersion=profile.revision;machine.profileHash=InstalledHash(profile);
  // Input meanings can change between protocols, even when units look alike.
  machine.exposureInputs=profile.inputs.Select(input=>new DiagnosticInput{key=input.key}).ToList();
 }
 static void Require(bool condition,string message){if(!condition)throw new Exception("Invalid machine profile: "+message);}
 static bool Positive(double value)=>CtShieldMath.IsFinite(value)&&value>0;
 public static void Validate(DiagnosticProfile p){
  Require(p!=null&&p.version==1,"unsupported schema.");
  Require(DiagnosticData.IsXray(p.machineType)&&Positive(p.tubeVoltageKvp),"machine family / voltage.");
  Require(!string.IsNullOrWhiteSpace(p.id)&&!string.IsNullOrWhiteSpace(p.revision)&&!string.IsNullOrWhiteSpace(p.machineIdentity)&&!string.IsNullOrWhiteSpace(p.acquisition)&&!string.IsNullOrWhiteSpace(p.spectrumId)&&!string.IsNullOrWhiteSpace(p.provenance),"identity, acquisition, spectrum and provenance are required.");
  Require(p.quantity=="airKerma"&&p.unit=="mGy"&&!p.includesRoomBarriers,"only unshielded air kerma in mGy is supported.");
  Require(new[]{"exposure","exam","min","week"}.Contains(p.basis)&&!string.IsNullOrWhiteSpace(p.exposureDefinition),"exposure basis.");
  Require(p.inputs!=null&&p.components!=null&&p.requiredComponents!=null&&p.components.Count>0&&p.components.Count<=32,"component/exposure definitions.");
  var keys=new HashSet<string>();
  foreach(var input in p.inputs)Require(input!=null&&!string.IsNullOrWhiteSpace(input.key)&&keys.Add(input.key)&&!string.IsNullOrWhiteSpace(input.label)&&!string.IsNullOrWhiteSpace(input.unit)&&CtShieldMath.IsFinite(input.minimum)&&input.minimum>=0&&CtShieldMath.IsFinite(input.maximum)&&input.maximum>=input.minimum,"invalid or duplicate numeric input.");
  Require(p.requiredComponents.Count>0&&p.requiredComponents.Distinct().Count()==p.requiredComponents.Count&&p.requiredComponents.All(kind=>new[]{"Primary","Scatter","Leakage","Secondary"}.Contains(kind)),"component coverage.");
  Require(!p.requiredComponents.Contains("Secondary")||!p.requiredComponents.Any(k=>k=="Scatter"||k=="Leakage"),"secondary overlaps scatter/leakage.");
  Require(p.components.Select(c=>c?.kind).OrderBy(k=>k).SequenceEqual(p.requiredComponents.OrderBy(k=>k)),"every declared component must occur exactly once.");
  var ids=new HashSet<string>();
  foreach(var c in p.components){
   Require(c!=null&&!string.IsNullOrWhiteSpace(c.id)&&ids.Add(c.id)&&c.spectrumId==p.spectrumId,"component identity / spectrum.");
   Require(c.provider=="ReferencePointKerma","provider "+c.provider+" has not been implemented; no point-source substitution is allowed.");
   Require(c.angularModel=="IsotropicValidated"&&!string.IsNullOrWhiteSpace(c.provenance)&&!string.IsNullOrWhiteSpace(c.validityDomain),"a validated angular model and applicability domain are required.");
   Require(CtShieldMath.IsFinite(c.referenceMgy)&&c.referenceMgy>=0&&Positive(c.referenceDistanceMeters)&&Positive(c.minimumDistanceMeters)&&Positive(c.maximumDistanceMeters)&&c.maximumDistanceMeters>=c.minimumDistanceMeters,"reference calibration / distance domain.");
   Require(c.normalization=="ProfileExposure"&&string.IsNullOrEmpty(c.exposureKey)||c.normalization=="PerInput"&&keys.Contains(c.exposureKey),"one declared normalization law per component.");
   if(c.patientInverseSquare)Require(c.kind=="Scatter"&&Positive(c.referencePatientDistanceMeters)&&Positive(c.minimumPatientDistanceMeters)&&c.maximumPatientDistanceMeters>=c.minimumPatientDistanceMeters&&CtShieldMath.IsFinite(c.maximumPatientDistanceMeters),"patient irradiation domain.");
   Require(c.samples!=null&&c.samples.Count>0&&c.samples.Count<=360,"source samples.");
   foreach(var sample in c.samples){
    Require(sample!=null&&Positive(sample.weight)&&sample.offsetMeters!=null&&CtShieldMath.IsFinite(sample.offsetMeters.x)&&CtShieldMath.IsFinite(sample.offsetMeters.y)&&CtShieldMath.IsFinite(sample.offsetMeters.z),"sample position/weight.");
    Require(sample.origin!=DiagnosticPointRole.ROI&&Enum.IsDefined(typeof(DiagnosticPointRole),sample.origin),"sample origin.");
    Require(c.kind!="Scatter"||sample.origin==DiagnosticPointRole.Scatter,"patient scatter must originate at Scatter.");
    Require(c.kind!="Primary"&&c.kind!="Leakage"||sample.origin==DiagnosticPointRole.Target,"primary/leakage must originate at Target.");
   }
   Require(Math.Abs(c.samples.Sum(s=>s.weight)-1)<1e-10,"sample weights must sum to one.");
   Require(c.fits!=null&&c.fits.Count>0&&c.fits.Select(f=>f?.material).Distinct().Count()==c.fits.Count,"material fit coverage.");
   foreach(var fit in c.fits){
    Require(fit!=null&&!string.IsNullOrWhiteSpace(fit.id)&&CtCoefficientLibrary.MaterialIds.Contains(fit.material)&&fit.spectrumId==p.spectrumId&&!string.IsNullOrWhiteSpace(fit.provenance),"fit identity/spectrum/provenance.");
    CtShieldMath.ValidateCoefficients(fit.alpha,fit.beta,fit.gamma);
   }
  }
  Require(keys.All(key=>p.components.Any(c=>c.exposureKey==key)),"unused exposure input.");
 }
}
}
