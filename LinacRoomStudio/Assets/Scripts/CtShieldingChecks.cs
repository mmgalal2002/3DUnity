using System;
using UnityEngine;

namespace RoomStudio {
public static class CtShieldingChecks {
 static int assertions;
 static void Require(bool condition,string message){assertions++;if(!condition)throw new Exception("CT integration: "+message);}
 static void Reject(Action operation,string message){bool rejected=false;try{operation();}catch(Exception){rejected=true;}Require(rejected,message);}
 [Serializable] class LegacyBarrier {public string material="Lead";public float thickness=2,density=11340;}
 public static void Run(){
    assertions=0;CtShieldMathChecks.Run();
   foreach(string incomplete in new[]{"","+","-",".","1.","1e","1e+","1e-","NaN","Infinity","1e309","1,237"})
    Require(!CtShieldMath.TryNumber(incomplete,-10000,10000,out _),"incomplete or invalid CT numeric entry: "+incomplete);
   Require(CtShieldMath.TryNumber("1.237000000001",-10000,10000,out double exact)&&Math.Abs(exact-1.237000000001)<1e-14,"CT numeric entry retains double coordinates");
   Require(CtShieldMath.TryNumber("1e-8",1e-8,1e8,out exact)&&exact==1e-8,"complete CT exponent entry");
   Require(!CtShieldMath.TryNumber("-1",0,1e12,out _),"CT source input bounds");
    Require(CtCoefficientLibrary.PrimaryProvidedCount==146&&CtCoefficientLibrary.PrimaryMissingCount==10&&CtCoefficientLibrary.PrimaryNegativeBetaCount==8,"separate source archive counts and signs");
    Require(CtCoefficientLibrary.TryGet("CT_SECONDARY","Lead",120,out var lead,out var fitStatus)&&lead.Fit.BetaPerMm==5.73,"exact CT-secondary selection");
    Require(!CtCoefficientLibrary.TryGet("PRIMARY_RADIOGRAPHIC","Lead",120,out _,out fitStatus)&&fitStatus=="UnsupportedSpectrum","primary family is not CT fallback");
    Require(!CtCoefficientLibrary.TryGet("CT_SECONDARY","Lead",130,out _,out fitStatus)&&fitStatus=="UnsupportedSpectrum","no energy interpolation");
    Require(!CtCoefficientLibrary.TryGet("CT_SECONDARY","Steel",120,out _,out fitStatus)&&fitStatus=="UnsupportedMaterialSpectrumCombination","unsupported CT material");
    Require(CtCoefficientLibrary.MaterialIds.Length==6&&Array.IndexOf(CtCoefficientLibrary.MaterialIds,"PlateGlass")>=0&&Array.IndexOf(CtCoefficientLibrary.MaterialIds,"Glass")<0,"new catalogue keeps plate glass distinct from legacy lead glass");
  var design=Design.Example();Design.Validate(design);
  var legacy=JsonUtility.FromJson<Barrier>(JsonUtility.ToJson(new LegacyBarrier()));
  Require(legacy.shieldingEnabled,"missing shielding field defaults enabled");Require(!legacy.ctApplicabilityReviewed,"legacy CT material applicability is unknown");
  var oldClone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(oldClone);Require(!CtShieldingData.Active(oldClone.ct),"old native has no active CT defaults");
  var scanner=EquipmentPalette.Create("CT",new Vector3(2,0,3));design.items.Add(scanner);CtShieldingData.Ensure(design);
  var scatter=CtShieldingData.CreatePoint("Scatter",new CtVector(2,0,3),scanner.id);scatter.ctPoint.localX=1;scatter.ctPoint.localY=.5;design.items.Add(scatter);
  var roi=CtShieldingData.CreatePoint("ROI",new CtVector(5,1,3));design.items.Add(roi);
  var source=new CtSourceData{deviceId=scanner.id,scatterPointId=scatter.id};design.ct.sources.Add(source);
  roi.ctPoint.roi.contributions.Add(new CtRoiContribution{sourceId=source.id});Design.Validate(design);
  var position=CtShieldingData.Position(design,scatter);Require(Math.Abs(position.x-3)<1e-12&&Math.Abs(position.y-.5)<1e-12,"attached local anchor");
  scanner.angle=90;position=CtShieldingData.Position(design,scatter);Require(Math.Abs(position.x-2)<1e-12&&Math.Abs(position.z-2)<1e-12,"owner rotation follows in double geometry");
  Require(CtShieldingData.Position(design,roi).x==5,"independent ROI does not move");
  var clone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(clone);
  Require(!clone.ct.sources[0].hasWorkload&&!clone.items.Find(item=>item.id==roi.id).ctPoint.roi.hasGoal,"missing scientific fields remain missing");
  Require(clone.ct.sources[0].scatterPointId==scatter.id,"native role and source pairing roundtrip");
  design.ct.sources.Add(source);Reject(()=>Design.Validate(design),"duplicate source rejected");design.ct.sources.RemoveAt(1);
  roi.ctPoint.roi.hasBestFactor=true;roi.ctPoint.roi.bestFactor=double.NaN;Reject(()=>Design.Validate(design),"nonfinite CT input rejected before calculation");roi.ctPoint.roi.bestFactor=.5;
  roi.ctPoint.roi.hasWorstFactor=true;roi.ctPoint.roi.worstFactor=.5;Reject(()=>Design.Validate(design),"inverted scenario bounds rejected");roi.ctPoint.roi.worstFactor=2;
  scatter.locked=true;Reject(()=>CtShieldingData.SetPointPosition(design,scatter,new CtVector(1,1,1)),"protected scatter cannot move");scatter.locked=false;
  scanner.locked=true;Reject(()=>CtShieldingData.SetPointPosition(design,scatter,new CtVector(1,1,1)),"protected owner guards its scatter anchor");scanner.locked=false;
  source.deviceId=design.items[5].id;Reject(()=>Design.Validate(design),"LINAC cannot acquire CT scatter calibration");source.deviceId=scanner.id;
  Design.Validate(design);
  SourceConfigurationChecks();
  CalculationChecks();
  Debug.Log("ROOM_STUDIO_CT_DATA_CHECKS_PASSED: "+assertions+" assertions; legacy defaults, typed points, anchors, missing inputs and protected contracts");
 }
 static void SourceConfigurationChecks(){
  var design=new Design{linkWallsToRoom=false,name="CT source setup - DEMO_ONLY"};CtShieldingData.Ensure(design);
  var roi=CtShieldingData.CreatePoint("ROI",new CtVector(3,1,0));var patient=CtShieldingData.CreatePoint("Patient",new CtVector(4,1,0));var protectedRoi=CtShieldingData.CreatePoint("ROI",new CtVector(5,1,0));protectedRoi.locked=true;
  design.items.Add(roi);design.items.Add(patient);design.items.Add(protectedRoi);design.ct.selectedRoiId=roi.id;
  var missing=CtShieldingCalculation.Calculate(design);
  Require(missing.rows.Count==3&&missing.rows.TrueForAll(row=>!row.hasPhysical&&!row.hasVerdict&&row.status=="MissingRequiredInput"),"ROI-first workflow reports missing sources without zero-dose results");
  Require(missing.rows[0].errors[0].Contains("CT source / energy settings"),"missing-source results explain the configuration route");
  var scanner=EquipmentPalette.Create("CT",Vector3.zero);design.items.Add(scanner);
  var other=EquipmentPalette.Create("MRI",Vector3.zero);design.items.Add(other);
  string before=JsonUtility.ToJson(design);
  Reject(()=>CtShieldingData.ConfigureSource(design,other),"only CT scanners can configure a CT source");
  Reject(()=>CtShieldingData.ConfigureSource(design,EquipmentPalette.Create("CT",Vector3.zero)),"source setup requires a scanner in the current design");
  Require(JsonUtility.ToJson(design)==before,"rejected source ownership leaves the design unchanged");
  scanner.locked=true;before=JsonUtility.ToJson(design);
  Reject(()=>CtShieldingData.ConfigureSource(design,scanner),"protected scanner cannot acquire a source");
  Require(JsonUtility.ToJson(design)==before,"protected source setup is atomic");scanner.locked=false;
  var source=CtShieldingData.ConfigureSource(design,scanner);var scatter=design.items.Find(item=>item.id==source.scatterPointId);Design.Validate(design);
  Require(source.deviceId==scanner.id&&scatter.ctPoint.ownerId==scanner.id&&scatter.ctPoint.role=="Scatter","point-first source setup creates an owned scatter marker");
  Require(source.kvp==0&&!source.hasReferenceKerma&&!source.hasWorkload&&!source.applicabilityReviewed&&!scatter.ctPoint.anchorConfirmed,"source setup invents no energy, strength, workload or calibration");
  Require(design.ct.selectedRoiId==roi.id&&design.ct.selectedSourceId==source.id,"source setup retains the evaluation point and selects its source");
  Require(roi.ctPoint.roi.contributions.Count==1&&patient.ctPoint.roi.contributions.Count==1,"source setup links existing editable ROI and patient points");
  Require(protectedRoi.ctPoint.roi.contributions.Count==0,"source setup does not edit a protected evaluation point");
  before=JsonUtility.ToJson(design);
  Require(CtShieldingData.ConfigureSource(design,scanner)==source&&JsonUtility.ToJson(design)==before,"reopening configuration does not duplicate a source or workload");
  CtShieldingData.SetTubePotential(design,source,120);
  Require(CtShieldingCalculation.Calculate(design).rows[0].status=="MissingRequiredInput","selecting energy alone cannot fabricate absolute kerma");
  source.scannerId="DEMO_ONLY";source.protocolId="DEMO_ONLY reference exam";source.citation="DEMO_ONLY specification section 14.1";source.validityDomain="DEMO_ONLY isotropic effective point model 1-10 m";source.includedComponents="DEMO_ONLY total secondary";
  source.componentsConfirmed=source.applicabilityReviewed=source.isotropicReferenceAccepted=true;source.mode="ReferenceExams";
  source.hasReferenceKerma=source.hasReferenceDistance=source.hasWorkload=source.hasMinimumDistance=source.hasMaximumDistance=true;
  source.referenceMgyPerExam=.2;source.referenceDistanceMeters=1;source.examsPerWeek=200;source.minimumDistanceMeters=1;source.maximumDistanceMeters=10;
  scatter.ctPoint.localY=1;scatter.ctPoint.anchorConfirmed=true;CtShieldingData.SynchronizeAnchors(design);
  var calculated=CtShieldingCalculation.Calculate(design,"Nominal","AllOff");
  Require(calculated.rows[0].hasPhysical&&Math.Abs(calculated.rows[0].physicalMgyPerWeek-40.0/9)<1e-12,"fresh source configuration reaches ROI reference normalization");
  Require(calculated.rows[1].hasPhysical&&Math.Abs(calculated.rows[1].physicalMgyPerWeek-2.5)<1e-12,"fresh source configuration reaches patient reference normalization");
  Require(!calculated.rows[2].hasPhysical&&calculated.rows[2].status=="IncompleteContributions","unlinked protected points cannot silently pass");
  var clone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(clone);
  Require(clone.ct.sources.Count==1&&clone.ct.sources[0].kvp==120&&clone.items.Find(item=>item.id==roi.id).ctPoint.roi.contributions[0].sourceId==source.id,"point-first source and energy survive native persistence");
  Require(Math.Abs(CtShieldingCalculation.Calculate(clone,"Nominal","AllOff").rows[0].physicalMgyPerWeek-40.0/9)<1e-12,"native-restored source produces the same ROI math");
  var full=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  while(full.ct.sources.Count<CtShieldingData.MaxSources){var added=EquipmentPalette.Create("CT",Vector3.zero);full.items.Add(added);CtShieldingData.ConfigureSource(full,added);}
  var extra=EquipmentPalette.Create("CT",Vector3.zero);full.items.Add(extra);before=JsonUtility.ToJson(full);
  Reject(()=>CtShieldingData.ConfigureSource(full,extra),"source setup enforces the source limit");
  Require(JsonUtility.ToJson(full)==before,"source-limit failure does not leave a scatter marker or contribution");
  full=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));
  while(full.items.FindAll(CtShieldingData.IsPoint).Count<CtShieldingData.MaxPoints)full.items.Add(CtShieldingData.CreatePoint("ROI",new CtVector(3,1,0)));
  extra=EquipmentPalette.Create("CT",Vector3.zero);full.items.Add(extra);before=JsonUtility.ToJson(full);
  Reject(()=>CtShieldingData.ConfigureSource(full,extra),"source setup enforces the calculation-point limit");
  Require(JsonUtility.ToJson(full)==before,"point-limit failure leaves the source ledger unchanged");
  full=new Design{linkWallsToRoom=false};extra=EquipmentPalette.Create("CT",Vector3.zero);full.items.Add(extra);
  while(full.items.Count<250)full.items.Add(EquipmentPalette.Create("Chair",Vector3.zero));before=JsonUtility.ToJson(full);
  Reject(()=>CtShieldingData.ConfigureSource(full,extra),"source setup enforces the project object limit");
  Require(JsonUtility.ToJson(full)==before&&full.ct==null,"object-limit failure does not activate or partially configure CT metadata");
  Debug.Log("ROOM_STUDIO_CT_SOURCE_SETUP_CHECKS_PASSED: ROI-first configuration, explicit energy/source inputs, protected points, native persistence and atomic limits");
 }
 public static Design DemoFixture(){
  var design=new Design{linkWallsToRoom=false,name="CT arithmetic demo - NOT CLINICAL"};var scanner=EquipmentPalette.Create("CT",Vector3.zero);design.items.Add(scanner);CtShieldingData.Ensure(design);design.ct.scenarioName="DEMO_ONLY";
  var source=CtShieldingData.ConfigureSource(design,scanner);var scatter=design.items.Find(item=>item.id==source.scatterPointId);scatter.ctPoint.localY=1;scatter.ctPoint.anchorConfirmed=true;CtShieldingData.SynchronizeAnchors(design);
  var roi=CtShieldingData.CreatePoint("ROI",new CtVector(3,1,0));design.items.Add(roi);design.ct.selectedRoiId=roi.id;
  CtShieldingData.SetTubePotential(design,source,120);source.scannerId="DEMO_ONLY";source.protocolId="Reference example";source.citation="DEMO_ONLY specification section 14.1";source.validityDomain="DEMO_ONLY isotropic effective point model 1-10 m";source.includedComponents="Total secondary; no extra leakage";source.componentsConfirmed=source.applicabilityReviewed=source.pathApproximationAccepted=true;
  source.mode="ReferenceExams";source.isotropicReferenceAccepted=true;source.hasReferenceKerma=source.hasReferenceDistance=source.hasWorkload=source.hasMinimumDistance=source.hasMaximumDistance=true;source.referenceMgyPerExam=.2;source.referenceDistanceMeters=1;source.examsPerWeek=200;source.minimumDistanceMeters=1;source.maximumDistanceMeters=10;
  var data=roi.ctPoint.roi;data.contributions.Add(new CtRoiContribution{sourceId=source.id});data.hasOccupancy=true;data.occupancy=.25;data.occupancyConvention="DEMO_ONLY exposure occupancy";data.hasGoal=true;data.goalMgyPerWeek=.002;data.goalSource="DEMO_ONLY invented goal";data.hasBestFactor=true;data.bestFactor=.5;data.hasWorstFactor=true;data.worstFactor=2;
  var wall=Design.Wall("Lead",1.5f,0,4,90,3);wall.shielding.material="Lead";wall.shielding.thickness=2;wall.shielding.ctApplicabilityReviewed=true;design.items.Add(wall);
  return design;
 }
 static void CalculationChecks(){
  var design=DemoFixture();var source=design.ct.sources[0];var scanner=design.items.Find(item=>item.id==source.deviceId);var scatter=design.items.Find(item=>item.id==source.scatterPointId);
  var roi=design.items.Find(CtShieldingData.IsRoi);var data=roi.ctPoint.roi;var wall=design.items.Find(item=>item.kind=="Wall");
  CtShieldingData.SetPointPosition(design,roi,new CtVector(1,1,0));
  Require(CtShieldingCalculation.DistanceMeters(design,scatter,roi)*100==100&&StudioViewportLayout.DistanceText(100,false)=="100 cm","one metre label converts once");
  CtShieldingData.SetPointPosition(design,roi,new CtVector(3,5,0));
  Require(CtShieldingCalculation.DistanceMeters(design,scatter,roi)*100==500&&StudioViewportLayout.DistanceText(500,true)=="500 cm (3D)","3-4-5 full spatial distance identified in plan projection");
  CtShieldingData.SetPointPosition(design,roi,new CtVector(3,1,0));
  var current=CtShieldingCalculation.Calculate(design);var row=current.rows[0];
  Require(row.hasVerdict&&row.passed&&Math.Abs(row.physicalMgyPerWeek-.005511067464392423)<1e-11,"reference physical/occupied verdict: "+row.status+"; K="+row.physicalMgyPerWeek.ToString("R")+"; "+string.Join("; ",row.errors));
  Require(Math.Abs(row.occupiedMgyPerWeek-.001377766866098106)<1e-11,"occupancy does not change physical field");
  Require(Math.Abs(row.contributions[0].segments[0].pathThicknessMm-2)<1e-9,"finite slab mm conversion");
  string energyFingerprint=CtShieldingCalculation.InputFingerprint(design);
  CtShieldingData.SetTubePotential(design,source,140);
  Require(source.kvp==140&&!source.applicabilityReviewed,"changing CT energy requires renewed source applicability review");
  var unreviewedEnergy=CtShieldingCalculation.Calculate(design);
  Require(!unreviewedEnergy.rows[0].hasVerdict&&unreviewedEnergy.rows[0].status=="MissingRequiredInput","unreviewed changed spectrum cannot pass");
  Require(CtShieldingCalculation.InputFingerprint(design)!=energyFingerprint&&!CtShieldingCalculation.SameInputs(current,unreviewedEnergy),"CT energy changes invalidate input comparisons");
  Reject(()=>CtShieldingData.SetTubePotential(design,source,130),"energy controls cannot interpolate unsupported CT kVp");
  scanner.locked=true;Reject(()=>CtShieldingData.SetTubePotential(design,source,120),"locked scanner protects CT energy");scanner.locked=false;
  scatter.locked=true;Reject(()=>CtShieldingData.SetTubePotential(design,source,120),"locked scatter protects CT energy");scatter.locked=false;
  var energyClone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(energyClone);
  Require(energyClone.ct.sources[0].kvp==140&&!energyClone.ct.sources[0].applicabilityReviewed,"native energy selection and review state roundtrip");
  foreach(int potential in new[]{120,140})foreach(string material in new[]{"Lead","Concrete"}){
   CtShieldingData.SetTubePotential(design,source,potential);source.applicabilityReviewed=true;
   wall.shielding.material=material;wall.shielding.thickness=material=="Lead"?2:100;
   var mapped=CtShieldingCalculation.Calculate(design).rows[0];var contribution=mapped.contributions[0];
   string fitId=(material=="Lead"?"CT_PB_":"CT_CONCRETE_")+potential;
   double transmission=material=="Lead"?(potential==120?.001239990179488295:.001258504786528333):(potential==120?.01390034709918690:.02096420076642798);
   Require(mapped.hasPhysical&&contribution.hasResult&&contribution.segments[0].fitId==fitId,"POI material/energy maps to "+fitId);
   Require(Math.Abs(contribution.transmission-transmission)<transmission*1e-10,"POI transmission uses selected CT coefficients: "+fitId);
   Require(Math.Abs(mapped.physicalMgyPerWeek-40.0/9*transmission)<1e-11,"selected CT energy drives absolute POI kerma: "+fitId);
  }
  CtShieldingData.SetTubePotential(design,source,120);source.applicabilityReviewed=true;wall.shielding.material="Lead";wall.shielding.thickness=2;
  var off=CtShieldingCalculation.Calculate(design,"Nominal","AllOff");Require(off.rows[0].hasPhysical&&Math.Abs(off.rows[0].physicalMgyPerWeek-40.0/9)<1e-12,"unshielded baseline");
  Require(CtShieldingCalculation.SameInputs(off,current),"shielding-only snapshot comparison");
  var worst=CtShieldingCalculation.Calculate(design,"Worst");Require(Math.Abs(worst.rows[0].physicalMgyPerWeek-2*row.physicalMgyPerWeek)<1e-11&&!worst.rows[0].passed,"worst-case workload changes criterion");
  var best=CtShieldingCalculation.Calculate(design,"Best");Require(Math.Abs(best.rows[0].physicalMgyPerWeek-.5*row.physicalMgyPerWeek)<1e-11,"best-case workload scaling");
  wall.shielding.shieldingEnabled=false;var wallOff=CtShieldingCalculation.Calculate(design);Require(Math.Abs(wallOff.rows[0].physicalMgyPerWeek-40.0/9)<1e-12,"per-wall toggle leaves geometry intact");Require(CtShieldingCalculation.SameInputs(wallOff,current),"wall-toggle-only snapshots retain matched comparison");wall.shielding.shieldingEnabled=true;
  data.occupancy=0;row=CtShieldingCalculation.Calculate(design).rows[0];Require(row.hasPhysical&&!row.hasVerdict&&row.status=="NotEvaluatedZeroOccupancy","zero occupancy retains field without pass");data.occupancy=.25;
  source.hasWorkload=false;row=CtShieldingCalculation.Calculate(design).rows[0];Require(!row.hasPhysical&&!row.hasVerdict,"missing source input is not zero");source.hasWorkload=true;
  source.isotropicReferenceAccepted=false;Require(!CtShieldingCalculation.Calculate(design).rows[0].hasVerdict,"isotropy must not be inferred from reference-point input");source.isotropicReferenceAccepted=true;
  source.kvp=130;Require(CtShieldingCalculation.Calculate(design).rows[0].status=="UnsupportedSpectrum","unsupported kVp cannot pass");source.kvp=120;
  source.mode="DirectWeekly";data.contributions[0].hasDirectKerma=true;data.contributions[0].directMgyPerWeek=4;source.examsPerWeek=10000;
  CtShieldingData.ConfirmDirectPosition(design,roi,data.contributions[0]);
  Require(Math.Abs(CtShieldingCalculation.Calculate(design,"Nominal","AllOff").rows[0].physicalMgyPerWeek-4)<1e-12,"direct weekly field is not normalized twice");
  CtShieldingData.SetTubePotential(design,source,140);source.applicabilityReviewed=true;
  Require(CtShieldingCalculation.Calculate(design,"Nominal","AllOff").rows[0].status=="SourceModelOutsideValidityDomain","changed CT energy invalidates direct POI kerma calibration");
  CtShieldingData.SetTubePotential(design,source,120);source.applicabilityReviewed=true;
  roi.x=4;Require(CtShieldingCalculation.Calculate(design,"Nominal","AllOff").rows[0].status=="SourceModelOutsideValidityDomain","moved direct ROI needs a new field input");roi.x=3;
  scanner.x=1;Require(CtShieldingCalculation.Calculate(design,"Nominal","AllOff").rows[0].status=="SourceModelOutsideValidityDomain","moved scanner invalidates direct ROI calibration");scanner.x=0;
  CtShieldingData.SetPointPosition(design,roi,new CtVector(1.237000000001,1,0));Require(Math.Abs(CtShieldingData.Position(design,roi).x-1.237000000001)<1e-14,"typed CT coordinates retain double precision");CtShieldingData.SetPointPosition(design,roi,new CtVector(3,1,0));
  source.mode="ReferenceExams";source.examsPerWeek=200;
  source.referenceMgyPerExam=double.Epsilon;data.goalMgyPerWeek=0;var tiny=CtShieldingCalculation.Calculate(design).rows[0];
  Require(tiny.hasPhysical&&!tiny.isZero&&!tiny.passed&&tiny.underflow,"tiny positive source cannot turn into exact zero or pass a zero goal");source.referenceMgyPerExam=.2;data.goalMgyPerWeek=.002;
  var duplicate=JsonUtility.FromJson<Item>(JsonUtility.ToJson(wall));duplicate.id=Guid.NewGuid().ToString();design.items.Add(duplicate);
  Require(Math.Abs(CtShieldingCalculation.Calculate(design).rows[0].physicalMgyPerWeek-current.rows[0].physicalMgyPerWeek)<1e-11,"redundant same-material overlap does not double mass");
  duplicate.shielding.material="Concrete";Require(CtShieldingCalculation.Calculate(design).rows[0].status=="UnsupportedCompositeBarrier","mixed-material overlap rejected");design.items.Remove(duplicate);
  roi.x=1.5005f;Require(CtShieldingCalculation.Calculate(design).rows[0].hasPhysical,"ROI inside solid clips path");roi.x=3;
  wall.z=10;Require(CtShieldingCalculation.Calculate(design).rows[0].contributions[0].transmission==1,"finite panel miss is unobstructed");wall.z=0;
  wall.x=4;Require(CtShieldingCalculation.Calculate(design).rows[0].contributions[0].transmission==1,"barrier beyond ROI ignored");wall.x=1.5f;
  scatter.ctPoint.localX=1.5;Require(CtShieldingCalculation.Calculate(design).rows[0].contributions[0].segments[0].pathThicknessMm<1.000001,"source inside solid clips initial entry");scatter.ctPoint.localX=0;
  scatter.ctPoint.localZ=2;roi.z=2;Require(CtShieldingCalculation.Calculate(design).rows[0].status=="InvalidGeometry","coplanar panel-edge path is ambiguous, not huge attenuation");scatter.ctPoint.localZ=0;roi.z=0;
  wall.doors=new System.Collections.Generic.List<DoorOpening>{new DoorOpening()};Require(CtShieldingCalculation.Calculate(design).rows[0].status=="InvalidGeometry","door geometry cannot be silently replaced with a closed slab");wall.doors=null;
  CtShieldingCalculation.SaveResult(design,current);source.examsPerWeek=400;Require(design.ct.results[0].rows[0].physicalMgyPerWeek==current.rows[0].physicalMgyPerWeek,"saved result remains immutable");
  Require(!CtShieldingCalculation.SameInputs(current,CtShieldingCalculation.Calculate(design)),"changed workload is not shielding-only comparison");
  var empty=new Design();CtShieldingCalculation.ImportResults(empty,CtShieldingCalculation.ExportResults(design.ct.results));Require(empty.ct.results.Count==1,"portable CT result import");
  Reject(()=>CtShieldingCalculation.ImportResults(empty,CtShieldingCalculation.ExportResults(design.ct.results)),"duplicate import atomic rejection");Require(empty.ct.results.Count==1,"failed import preserves history");
  var tampered=JsonUtility.FromJson<CtResultSnapshot>(JsonUtility.ToJson(current));tampered.rows[0].physicalMgyPerWeek=0;Reject(()=>CtShieldingData.ValidateResult(tampered),"altered historical result integrity rejected");
  Reject(()=>CtShieldingCalculation.ImportResults(new Design(),"{}"),"missing result format cannot inherit DTO defaults");
  var clone=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(clone);Require(clone.ct.results.Count==1,"native history roundtrip");
  scatter.locked=true;Reject(()=>SelectionEditing.Move(design,new[]{scanner},Vector3.right),"locked scatter protects owner transform");Reject(()=>SelectionEditing.Remove(design,new[]{scanner.id}),"locked dependent prevents partial scanner deletion");scatter.locked=false;
  Reject(()=>SelectionEditing.Move(design,new[]{scatter},Vector3.right),"attached anchor avoids generic scene movement");Reject(()=>SceneClipboard.Copy(design,new[]{scatter.id},scatter.id),"attached source cannot be copied into duplicate workload");
  SelectionEditing.Remove(clone,new[]{scanner.id});Design.Validate(clone);Require(clone.ct.sources.Count==0&&!clone.items.Exists(item=>item.id==scatter.id),"scanner deletion removes owned anchor and provider atomically");
 }
}
public partial class StudioApp {
 void CtRuntimeChecks(){
  string original=JsonUtility.ToJson(design),originalFile=fileName,originalDirectory=runtimeTestDirectory,originalTab=tab;
  var originalLatest=ctLatest;bool originalPopup=ctResultsPopup,originalShowQa=showQa;
  string directory=System.IO.Path.Combine(Application.temporaryCachePath,"CTRuntime-"+Guid.NewGuid().ToString("N"));
  try{
   runtimeTestDirectory=directory;System.IO.Directory.CreateDirectory(directory);design=CtShieldingChecks.DemoFixture();ClearSelection();Commit();Rebuild();
   var source=design.ct.sources[0];var scanner=design.items.Find(item=>item.id==source.deviceId);var scatter=design.items.Find(item=>item.id==source.scatterPointId);var roi=design.items.Find(CtShieldingData.IsRoi);
   if(!objects.TryGetValue(scatter.id,out var scatterObject)||scatterObject.GetComponent<Collider>()==null||scatterObject.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount!=107414)throw new Exception("Dot resource or selection collider missing.");
   if(scatterObject.GetComponentInChildren<Renderer>().sharedMaterial.shader.name!="RoomStudio/CTMarker")throw new Exception("Scatter point annotation shader was stripped.");
   if(Vector3.Distance(scatterObject.transform.position,new Vector3(0,1,0))>.00001f||ctMeasurements.Count!=1||Math.Abs(CtShieldingCalculation.DistanceMeters(design,scatter,roi)*100-300)>.00001)throw new Exception("CT point/line/cm alignment failed.");
   roi.x=3;roi.y=5;roi.z=0;scatter.ctPoint.localZ=-4;Rebuild();
   if(Math.Abs(CtShieldingCalculation.DistanceMeters(design,scatter,roi)-Math.Sqrt(41))>.00001)throw new Exception("CT distance lost elevation.");
   roi.y=1;scatter.ctPoint.localZ=0;Commit();Rebuild();
   Choose(scanner.id);SelectionEditing.Move(design,SelectedItems,Vector3.right);Commit();Rebuild();
   if(Vector3.Distance(objects[scatter.id].transform.position,new Vector3(1,1,0))>.00001f||design.items.Find(item=>item.id==roi.id).x!=3)throw new Exception("Attached scatter or independent ROI movement failed.");
   Undo(-1);scanner=design.items.Find(item=>item.id==source.deviceId);scatter=design.items.Find(item=>item.id==source.scatterPointId);roi=design.items.Find(item=>item.id==roi.id);source=design.ct.sources[0];
   if(Math.Abs(CtShieldingCalculation.DistanceMeters(design,scatter,roi)-3)>.00001)throw new Exception("CT point transform undo failed.");
   Choose(roi.id);string before=JsonUtility.ToJson(design);HandleShortcut(KeyCode.RightArrow,false,false,false,true);if(JsonUtility.ToJson(design)!=before)throw new Exception("CT nudge captured numeric/text focus.");
   HandleShortcut(KeyCode.L,true);string protectedJson=JsonUtility.ToJson(design);HandleShortcut(KeyCode.Delete);HandleShortcut(KeyCode.R);if(JsonUtility.ToJson(design)!=protectedJson)throw new Exception("Locked ROI edited through shortcut.");HandleShortcut(KeyCode.L,true);
   tab="CT";CalculateCtScenario();if(ctLatest==null||!ctLatest.rows[0].passed||design.ct.results.Count!=1)throw new Exception("CT calculate/save/modal workflow failed.");
   var wall=design.items.Find(item=>item.kind=="Wall");wall.shielding.shieldingEnabled=false;Commit();Rebuild();CalculateCtScenario();
   if(design.ct.results.Count!=2||ctLatest.rows[0].passed||!CtShieldingCalculation.SameInputs(design.ct.results[0],design.ct.results[1]))throw new Exception("CT on/off saved comparison failed.");
   wall.shielding.shieldingEnabled=true;Commit();CalculateCtCases();if(design.ct.results.Count!=5||ctLatest.scenarioCase!="Nominal")throw new Exception("CT scenario batch is incomplete.");
   fileName="CT-runtime-DEMO_ONLY";Save();string path=System.IO.Path.Combine(directory,SafeName()+".json");if(!System.IO.File.Exists(path))throw new Exception("CT native save failed.");
   string frozen=JsonUtility.ToJson(design.ct.results);Load(path);if(design.ct.results.Count!=5||!design.items.Find(item=>item.kind=="Wall").shielding.shieldingEnabled)throw new Exception("CT native reload lost history or wall toggle.");
   string exported=CtShieldingCalculation.ExportResults(design.ct.results);WriteOutput("ct-runtime-results.json",exported);var fresh=new Design();CtShieldingCalculation.ImportResults(fresh,System.IO.File.ReadAllText(System.IO.Path.Combine(directory,"ct-runtime-results.json")));if(fresh.ct.results.Count!=5)throw new Exception("CT results file roundtrip failed.");
   Choose(design.items.Find(CtShieldingData.IsRoi).id);var clipboard=SceneClipboard.Copy(design,selection,selected);var pasted=SceneClipboard.Paste(design,clipboard,1);var copy=pasted.items[0];if(copy.id==roi.id||copy.ctPoint.roi.contributions.Exists(contribution=>contribution.hasDirectKerma))throw new Exception("Copied ROI reused direct source calibration.");Commit();Rebuild();Undo(-1);
   var owner=design.items.Find(item=>item.model=="CT"&&!CtShieldingData.IsPoint(item));Choose(owner.id);Delete();if(design.ct.sources.Count!=0||design.items.Exists(item=>CtShieldingData.IsPoint(item)&&item.ctPoint.role=="Scatter"))throw new Exception("CT owner deletion left dangling scientific records.");Undo(-1);Design.Validate(design);
   Debug.Log("ROOM_STUDIO_CT_RUNTIME_CHECKS_PASSED: Dot mesh/annotation/collider, 3D cm distance, source ownership, independent ROI, guards/history, on/off and best/nominal/worst snapshots, native/results files and atomic deletion");
  }finally{
   design=JsonUtility.FromJson<Design>(original);runtimeTestDirectory=originalDirectory;fileName=originalFile;tab=originalTab;ctLatest=originalLatest;ctResultsPopup=originalPopup;showQa=originalShowQa;ctInputDrafts.Clear();ClearSelection();Commit();Rebuild();
   if(System.IO.Directory.Exists(directory))System.IO.Directory.Delete(directory,true);
  }
 }
 System.Collections.IEnumerator CtOnlySmokeTest(){
  yield return null;
  try{CtShieldingChecks.Run();CtRuntimeChecks();Debug.Log("ROOM_STUDIO_CT_PLAYER_SMOKE_PASSED");Application.Quit(0);}
  catch(Exception error){Debug.LogError("ROOM_STUDIO_CT_PLAYER_SMOKE_FAILED: "+error);Application.Quit(1);}
 }
 System.Collections.IEnumerator CtBrowserSmokeTest(){
  yield return null;
  try{CtShieldingChecks.Run();CtRuntimeChecks();status="CT feature checks passed.";Debug.Log("ROOM_STUDIO_CT_BROWSER_SMOKE_PASSED");}
  catch(Exception error){status="CT feature checks failed: "+error.Message;Debug.LogError("ROOM_STUDIO_CT_BROWSER_SMOKE_FAILED: "+error);}
 }
}
}