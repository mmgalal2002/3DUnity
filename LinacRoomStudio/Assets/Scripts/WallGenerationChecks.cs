using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public static class WallGenerationChecks {
 static void Need(bool value,string message){if(!value)throw new Exception("Wall generation model regression: "+message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Need(rejected,message);}
 public static void Run(){
  var guide=PlanAuthoringChecks.Fixture();guide.authoring=PlanAuthoring.Create(guide);
  guide.authoring.rules.Add(new PlanColorRule{id="magenta",name="Concrete partition",material="Concrete",densityKgM3=2350});
  PlanAuthoring.Confirm(guide,new PlanCalibration{method="Manual",unit="m/px",value=.01f});
  var design=Design.Example();design.floorPlan=guide;
  var aPixel=new Vector2Data(40.5f,217.5f);var bPixel=new Vector2Data(599.5f,217.5f);
  Vector3 a=PlanAuthoring.PixelToWorld(guide,new Vector2(aPixel.x,aPixel.y));
  Vector3 b=PlanAuthoring.PixelToWorld(guide,new Vector2(bPixel.x,bPixel.y));
  var wall=Design.Wall("Concrete partition",(a.x+b.x)/2,(a.z+b.z)/2,Vector3.Distance(a,b),0,design.height);
  wall.shielding.material="Concrete";wall.shielding.thickness=150;wall.shielding.density=2350;
  var path=new GeneratedWallPath{id="path-1",ruleId="magenta",itemId=wall.id,categoryName="Concrete partition",material="Concrete",displayColor=Color.magenta,
   sourceAPixel=aPixel,sourceBPixel=bPixel,aPixel=new Vector2Data(aPixel.x,aPixel.y),bPixel=new Vector2Data(bPixel.x,bPixel.y),aWorld=a,bWorld=b,
   sourceComponent=1,areaPixels=2800,lengthPixels=559,height=design.height,baseElevation=0,thicknessMm=150,densityKgM3=2350};
  wall.generated=new GeneratedWallRef{batchId="batch-1",pathId=path.id,ruleId=path.ruleId,categoryName=path.categoryName,displayColor=path.displayColor,
   sourceAPixel=new Vector2Data(aPixel.x,aPixel.y),sourceBPixel=new Vector2Data(bPixel.x,bPixel.y)};
  var batch=new WallGenerationBatch{id="batch-1",sourceFingerprint=PlanAuthoring.Fingerprint(guide),pixelWidth=guide.pixelWidth,pixelHeight=guide.pixelHeight,
   snapshotSignature=WallGenerationData.SnapshotSignature(guide),sourceSnapshot=FloorPlanCodec.Clone(guide)};
  batch.paths.Add(path);design.items.Add(wall);design.generationBatches.Add(batch);
  Design.Validate(design);
  Need(WallGenerationData.MarkManualEdits(design)==0,"unchanged generated wall marked manually edited");
  string json=JsonUtility.ToJson(design);var restored=JsonUtility.FromJson<Design>(json);Design.Validate(restored);
  Need(restored.generationBatches.Count==1&&restored.items.Last().generated.pathId=="path-1"&&restored.generationBatches[0].sourceSnapshot.authoring.rules[0].name=="Concrete partition","native provenance/rule snapshot roundtrip");
  restored.floorPlan=null;Design.Validate(restored);Need(restored.generationBatches[0].sourceSnapshot.imageBase64.Length>0,"guide removal lost batch source image");
  var old=restored.generationBatches[0].sourceSnapshot.authoring.rules[0].name;
  restored.generationBatches[0].sourceSnapshot.authoring.rules[0].name="changed";
  Reject(()=>Design.Validate(restored),"snapshot mutation accepted under old signature");
  restored.generationBatches[0].sourceSnapshot.authoring.rules[0].name=old;
  restored.items.Last().x+=.01f;Need(WallGenerationData.MarkManualEdits(restored)==1&&restored.items.Last().generated.manuallyEdited&&restored.generationBatches[0].paths[0].manuallyEdited,"ordinary wall edit did not set path/item override");
  restored=JsonUtility.FromJson<Design>(json);
  Need(WallGenerationData.MarkDeletedPaths(restored,new[]{restored.items.Last()})==1,"delete did not mark path tombstone");restored.items.RemoveAll(item=>item.id==wall.id);
  Design.Validate(restored);var deleted=JsonUtility.FromJson<Design>(JsonUtility.ToJson(restored));Design.Validate(deleted);
  Need(deleted.generationBatches[0].paths[0].deletedOverride&&deleted.generationBatches[0].paths[0].itemId=="","deleted-path override did not persist");
  restored=JsonUtility.FromJson<Design>(json);restored.items.Last().generated.pathId="missing";Reject(()=>Design.Validate(restored),"orphan generated item accepted");
  restored=JsonUtility.FromJson<Design>(json);restored.generationBatches[0].paths[0].densityKgM3=float.NaN;Reject(()=>Design.Validate(restored),"non-finite physical density accepted");
  var extracted=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(path));extracted.itemId="";
  var fresh=WallGenerationDiffs.ApplyNewBatch(Design.Example(),guide,new[]{extracted});Design.Validate(fresh);
  Need(fresh.generationBatches.Count==1&&fresh.items.Count==8&&Design.Example().items.Count==7,"new batch was not applied atomically to a clone");
  Reject(()=>WallGenerationDiffs.ApplyNewBatch(fresh,guide,new[]{extracted}),"unchanged generation duplicated walls");
  var noChange=WallGenerationDiffs.PreviewRegeneration(fresh,fresh.generationBatches[0].id,guide,new[]{extracted});
  Need(noChange.Count("Unchanged")==1&&!noChange.HasConflicts,"unchanged regeneration did not match stable path ID");
  var repeated=WallGenerationDiffs.ApplyRegeneration(fresh,noChange);Need(repeated.items.Count==8&&JsonUtility.ToJson(fresh)==JsonUtility.ToJson(repeated),"unchanged regeneration altered model");
  var changed=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(extracted));changed.categoryName="Updated category";
  var update=WallGenerationDiffs.PreviewRegeneration(fresh,fresh.generationBatches[0].id,guide,new[]{changed});Need(update.Count("Update")==1,"rule/category change lacked an explicit update preview");
  var updated=WallGenerationDiffs.ApplyRegeneration(fresh,update);Need(updated.items.Last().name=="Updated category"&&fresh.items.Last().name!="Updated category","regeneration mutated original design");
  fresh.items.Last().x+=.01f;var preserved=WallGenerationDiffs.PreviewRegeneration(fresh,fresh.generationBatches[0].id,guide,new[]{extracted});
  var kept=WallGenerationDiffs.ApplyRegeneration(fresh,preserved);Need(kept.items.Last().generated.manuallyEdited&&kept.generationBatches[0].paths[0].manuallyEdited&&Mathf.Abs(kept.items.Last().x-fresh.items.Last().x)<.0001f,"manual movement lost on regeneration");
  var conflict=WallGenerationDiffs.PreviewRegeneration(kept,kept.generationBatches[0].id,guide,new[]{changed});Need(conflict.HasConflicts,"changed rule silently rewrote a manual wall");
  Reject(()=>WallGenerationDiffs.ApplyRegeneration(kept,conflict),"conflicted regeneration applied");
    foreach(string property in new[]{"height","base","thickness","density","display"}){
     var physicalChange=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(extracted));
     if(property=="height")physicalChange.height+=.1f;
     if(property=="base")physicalChange.baseElevation+=.1f;
     if(property=="thickness")physicalChange.thicknessMm+=1;
     if(property=="density")physicalChange.densityKgM3+=1;
     if(property=="display")physicalChange.displayColor=Color.green;
     Need(WallGenerationDiffs.PreviewRegeneration(kept,kept.generationBatches[0].id,guide,new[]{physicalChange}).HasConflicts,"manual wall silently ignored changed "+property);
    }
    foreach(string transform in new[]{"translation","rotation","scale"}){
     var shiftedSource=FloorPlanCodec.Clone(guide);
     if(transform=="translation")shiftedSource.x+=.01f;
     if(transform=="rotation")shiftedSource.rotation+=1;
     if(transform=="scale")PlanAuthoring.Confirm(shiftedSource,new PlanCalibration{method="Manual",unit="m/px",value=.02f});
     Need(WallGenerationDiffs.PreviewRegeneration(kept,kept.generationBatches[0].id,shiftedSource,new[]{extracted}).HasConflicts,"manual wall silently ignored changed source "+transform);
    }
    var corrected=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(extracted));corrected.aWorld.x+=.1f;corrected.aPixel.x+=10;corrected.manuallyEdited=true;
    var correctedDesign=WallGenerationDiffs.ApplyNewBatch(Design.Example(),guide,new[]{corrected});
    Need(!WallGenerationDiffs.PreviewRegeneration(correctedDesign,correctedDesign.generationBatches[0].id,guide,new[]{extracted}).HasConflicts,"unchanged detection lost a manually corrected preview endpoint");
    var first=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(extracted));first.id="joined-left";first.aPixel=new Vector2Data(100,120);first.bPixel=new Vector2Data(300,120);
    first.sourceAPixel=new Vector2Data(100,120);first.sourceBPixel=new Vector2Data(300,120);
    first.aWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(100,120));first.bWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(300,120));
    var second=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(first));second.id="joined-right";
    second.aPixel=second.sourceAPixel=new Vector2Data(301.5f,120);second.bPixel=second.sourceBPixel=new Vector2Data(500,120);
    second.aWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(301.5f,120));second.bWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(500,120));
    var connected=WallGenerationDiffs.ApplyNewBatch(Design.Example(),guide,new[]{first,second});
    var connectedBatch=connected.generationBatches.Single();connectedBatch.connectionOptions=new WallGenerationOptions{version=1,autoConnect=true,tolerance=.02f,allowCrossCategory=true};
    var join=WallConnections.Preview(connected,connectedBatch.paths.Select(value=>value.itemId));WallConnections.Apply(connected,join,false);Design.Validate(connected);
    Need(connected.wallJunctions.Count==1,"generated collinear gap was not physically joined");
    var joinedUnchanged=WallGenerationDiffs.PreviewRegeneration(connected,connectedBatch.id,guide,new[]{first,second});
    Need(!joinedUnchanged.HasConflicts&&joinedUnchanged.Count("Unchanged")==2,"unchanged automatic joins blocked regeneration");
    var connectedCopy=JsonUtility.FromJson<Design>(JsonUtility.ToJson(connected));Design.Validate(connectedCopy);
    Need(connectedCopy.generationBatches[0].connectionOptions.allowCrossCategory&&Mathf.Abs(connectedCopy.generationBatches[0].connectionOptions.tolerance-.02f)<.000001f,"connection settings lost through joining/native roundtrip");
    foreach(float invalid in new[]{0,-1,float.NaN,float.PositiveInfinity})Reject(()=>WallGenerationOptions.Read(new WallGenerationOptions{version=1,tolerance=invalid}),"invalid generation connection tolerance");
    Need(WallGenerationOptions.Read(null).autoConnect&&Mathf.Abs(WallGenerationOptions.Read(new WallGenerationOptions{version=0,tolerance=0}).tolerance-.02f)<.000001f,"absent settings lost documented defaults");
    var crossing=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(first));crossing.id="cross-horizontal";
    crossing.bPixel=crossing.sourceBPixel=new Vector2Data(500,120);crossing.bWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(500,120));
    var branch=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(first));branch.id="cross-vertical";
    branch.aPixel=branch.sourceAPixel=new Vector2Data(300,20);branch.bPixel=branch.sourceBPixel=new Vector2Data(300,220);
    branch.aWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(300,20));branch.bWorld=PlanAuthoring.PixelToWorld(guide,new Vector2(300,220));
    var crossed=WallGenerationDiffs.ApplyNewBatch(Design.Example(),guide,new[]{crossing,branch});var crossedBatch=crossed.generationBatches.Single();
    var crossJoin=WallConnections.Preview(crossed,crossedBatch.paths.Select(value=>value.itemId));WallConnections.Apply(crossed,crossJoin,false);Design.Validate(crossed);
    crossed=JsonUtility.FromJson<Design>(JsonUtility.ToJson(crossed));Design.Validate(crossed);crossedBatch=crossed.generationBatches.Single();
    Need(crossedBatch.paths.Count==4&&crossedBatch.paths.All(value=>!string.IsNullOrEmpty(value.detectionPathId)),"X split lost original detection identity");
    var unchangedCross=WallGenerationDiffs.PreviewRegeneration(crossed,crossedBatch.id,guide,new[]{crossing,branch});
    Need(!unchangedCross.HasConflicts&&unchangedCross.Count("Unchanged")==4,"unchanged X detection blocked regeneration or duplicated split walls");
    var repeatedCross=WallGenerationDiffs.ApplyRegeneration(crossed,unchangedCross);
    Need(JsonUtility.ToJson(crossed)==JsonUtility.ToJson(repeatedCross),"unchanged X regeneration changed split IDs/topology");
    var changedCross=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(crossing));changedCross.thicknessMm+=1;
    Need(WallGenerationDiffs.PreviewRegeneration(crossed,crossedBatch.id,guide,new[]{changedCross,branch}).HasConflicts,"changed physical properties silently rewrote split X walls");
  var tombstone=JsonUtility.FromJson<GeneratedWallPath>(JsonUtility.ToJson(extracted));tombstone.deletedOverride=true;
  var removePreview=WallGenerationDiffs.PreviewRegeneration(repeated,repeated.generationBatches[0].id,guide,new[]{tombstone});Need(removePreview.Count("DeleteOverride")==1,"explicit preview deletion lacked a tombstone action");
  var removed=WallGenerationDiffs.ApplyRegeneration(repeated,removePreview);Need(removed.items.Count==7&&removed.generationBatches[0].paths[0].deletedOverride&&removed.generationBatches[0].paths[0].itemId=="","explicit deletion failed to persist tombstone");
  var onlyDeleted=WallGenerationDiffs.ApplyNewBatch(Design.Example(),guide,new[]{tombstone});Need(onlyDeleted.items.Count==7&&onlyDeleted.generationBatches[0].paths[0].deletedOverride,"new-batch tombstone created a wall");
  var oldDesign=JsonUtility.FromJson<Design>(JsonUtility.ToJson(Design.Example()));oldDesign.generationBatches=null;oldDesign.wallJunctions=null;Design.Validate(oldDesign);
  Debug.Log("ROOM_STUDIO_WALL_GENERATION_MODEL_CHECKS_PASSED: native provenance, source snapshot, immutable signature, manual/deleted overrides, pure apply/regeneration diff, conflict guards and legacy defaults");
 }
}
}
