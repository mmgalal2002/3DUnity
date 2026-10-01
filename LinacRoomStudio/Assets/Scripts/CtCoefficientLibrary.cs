using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RoomStudio {
[Serializable] sealed class CtDatasetDto {
 public int schemaVersion;
 public string datasetId,thicknessUnit,coefficientInverseLengthUnit,beamFamily,specificationSha256,reportedPdfSha256;
 public bool interpolationEnabled,extrapolationEnabled;
 public CtFitDto[] fits;
}
[Serializable] sealed class CtFitDto {public string id,material,figure;public int kvp,pdfPage,reportPage;public double alpha,beta,gamma;}
[Serializable] sealed class PrimaryDatasetDto {
 public int schemaVersion;
 public string datasetId,thicknessUnit,coefficientInverseLengthUnit,specificationSha256;
 public bool ctFallbackEnabled;
 public PrimaryFitDto[] rows;
}
[Serializable] sealed class PrimaryFitDto {public string material,anode,beamFamily,sourceState;public int kvp,pdfPage,reportPage;public bool provided;public double alpha,beta,gamma;}
public sealed class CtCoefficient {
 public readonly string Id,Material,Figure;
 public readonly int Kvp,ReportPage;
 public readonly ArcherFit Fit;
 internal CtCoefficient(CtFitDto record){Id=record.id;Material=record.material;Figure=record.figure;Kvp=record.kvp;ReportPage=record.reportPage;Fit=new ArcherFit(record.alpha,record.beta,record.gamma);}
}
public static class CtCoefficientLibrary {
 static CtCoefficient[] coefficients;
 static PrimaryDatasetDto archive;
 static string specificationHash,datasetHash,reportedPdfHash;
 public static string DatasetHash {get{Load();return datasetHash;}}
 public static string SpecificationHash {get{Load();return specificationHash;}}
 public static string ReportedPdfHash {get{Load();return reportedPdfHash;}}
 public static int PrimaryProvidedCount {get{Load();return archive.rows.Count(row=>row.provided);}}
 public static int PrimaryMissingCount {get{Load();return archive.rows.Count(row=>!row.provided);}}
 public static int PrimaryNegativeBetaCount {get{Load();return archive.rows.Count(row=>row.provided&&row.beta<0);}}
 public static readonly string[] MaterialIds={"Lead","Concrete","Gypsum","Steel","PlateGlass","Wood"};
 public static string MaterialLabel(string id)=>id=="Gypsum"?"Gypsum wallboard":id=="PlateGlass"?"Plate glass":id=="Glass"?"Lead glass (legacy)":id;
 static string Read(string name){var asset=Resources.Load<TextAsset>("CT/"+name);if(asset==null)throw new Exception("Missing CT coefficient resource: "+name);return asset.text;}
 static void Load(){
  if(coefficients!=null)return;
    string datasetJson=Read("ct-secondary");var dataset=JsonUtility.FromJson<CtDatasetDto>(datasetJson);
  if(dataset==null||dataset.schemaVersion!=1||dataset.datasetId!="NCRP147_APPENDIX_A_CT_SECONDARY"||dataset.beamFamily!="CT_SECONDARY"||dataset.thicknessUnit!="mm"||dataset.coefficientInverseLengthUnit!="1/mm"||dataset.interpolationEnabled||dataset.extrapolationEnabled||dataset.fits==null||dataset.fits.Length!=4)throw new Exception("Invalid CT coefficient dataset.");
  var expected=new[]{new CtFitDto{id="CT_PB_120",material="lead",kvp=120,alpha=2.246,beta=5.73,gamma=.547},new CtFitDto{id="CT_PB_140",material="lead",kvp=140,alpha=2.009,beta=3.99,gamma=.342},new CtFitDto{id="CT_CONCRETE_120",material="concrete",kvp=120,alpha=.0383,beta=.0142,gamma=.658},new CtFitDto{id="CT_CONCRETE_140",material="concrete",kvp=140,alpha=.0336,beta=.0122,gamma=.519}};
  foreach(var known in expected){var matching=dataset.fits.Where(row=>row.id==known.id).ToArray();if(matching.Length!=1)throw new Exception("Missing/duplicate CT fit.");var record=matching[0];if(record.material!=known.material||record.kvp!=known.kvp||record.alpha!=known.alpha||record.beta!=known.beta||record.gamma!=known.gamma)throw new Exception("CT fit does not match the supplied reference: "+known.id);}
  var primary=JsonUtility.FromJson<PrimaryDatasetDto>(Read("primary-archive"));
  if(primary==null||primary.schemaVersion!=1||primary.ctFallbackEnabled||primary.datasetId!="NCRP147_APPENDIX_A_TABLE_A1_PRIMARY"||primary.rows==null||primary.rows.Length!=156||primary.rows.Count(row=>row.provided)!=146||primary.rows.Count(row=>row.provided&&row.beta<0)!=8)throw new Exception("Invalid separate primary coefficient archive.");
  foreach(var row in primary.rows){if(row.provided){CtShieldMath.ValidateCoefficients(row.alpha,row.beta,row.gamma);if(row.beamFamily!="PRIMARY_RADIOGRAPHIC"&&row.beamFamily!="PRIMARY_MAMMOGRAPHIC")throw new Exception("Invalid primary beam family.");}else if(row.alpha!=0||row.beta!=0||row.gamma!=0)throw new Exception("Missing primary rows cannot supply fits.");}
  if(primary.rows.Single(row=>row.material=="wood"&&row.kvp==110).gamma!=3.309)throw new Exception("Primary wood gamma mismatch.");
    archive=primary;specificationHash=dataset.specificationSha256;reportedPdfHash=dataset.reportedPdfSha256;using(var sha=SHA256.Create())datasetHash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(datasetJson))).Replace("-","").ToLowerInvariant();coefficients=dataset.fits.Select(record=>new CtCoefficient(record)).ToArray();
 }
 public static bool TryGet(string family,string material,int kvp,out CtCoefficient coefficient,out string status){
  Load();coefficient=null;
  if(family!="CT_SECONDARY"||kvp!=120&&kvp!=140){status="UnsupportedSpectrum";return false;}
  string normalized=material=="Lead"?"lead":material=="Concrete"?"concrete":material;
  coefficient=coefficients.FirstOrDefault(row=>row.Material==normalized&&row.Kvp==kvp);
  status=coefficient==null?"UnsupportedMaterialSpectrumCombination":"ValidCalculation";return coefficient!=null;
 }
 public static int[] SupportedTubeVoltages(string family){
  Load();
  if(family=="CT_SECONDARY")return coefficients.Select(row=>row.Kvp).Distinct().OrderBy(kvp=>kvp).ToArray();
  if(family=="PRIMARY_RADIOGRAPHIC"||family=="PRIMARY_MAMMOGRAPHIC")
   return archive.rows.Where(row=>row.provided&&row.beamFamily==family).Select(row=>row.kvp).Distinct().OrderBy(kvp=>kvp).ToArray();
  throw new ArgumentException("Unknown attenuation reference family: "+family,nameof(family));
 }
 public static double PlottedMaximumMm(CtCoefficient coefficient)=>coefficient.Material=="lead"?3:300;
}
}