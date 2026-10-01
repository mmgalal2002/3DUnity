using System;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 void WallGenerationSmokeChecks(){
  string original=JsonUtility.ToJson(design);bool oldTop=top,oldCutaway=cutaway;
  try{
   const int width=160,height=120;
   var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
   var pixels=Enumerable.Repeat(new Color32(255,255,255,255),width*height).ToArray();
   Action<int,int,int,int> stroke=(x0,x1,y0,y1)=>{for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)pixels[(height-1-y)*width+x]=new Color32(255,0,255,255);};
   stroke(15,115,15,18);stroke(15,115,85,88);stroke(15,18,15,88);stroke(112,115,15,88);stroke(60,63,15,88);
   texture.SetPixels32(pixels);texture.Apply();
   var source=FloorPlanCodec.Read(texture.EncodeToPNG(),"Req5-connected-fixture.png");
   if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);
   source.authoring=PlanAuthoring.Create(source);
   source.authoring.rules.Add(new PlanColorRule{id="magenta-wall",name="Fixture wall",classification="Wall",target=Color.magenta,display=Color.magenta,
    material="Concrete",densityKgM3=2350,height=3,thicknessMm=150,tolerance=.01f,minAreaPixels=3,minLengthPixels=3});
   PlanAuthoring.Confirm(source,new PlanCalibration{method="Manual",unit="m/px",value=.01f});
   design=Design.Example();design.linkWallsToRoom=false;design.floorPlan=source;ClearSelection();Rebuild();
   string before=JsonUtility.ToJson(design);
   StartConnectedWallGeneration();
   int steps=0;while(generationMaskJob!=null||generationPathJob!=null){WallGenerationUpdate();if(++steps>20000)throw new Exception("Generation processing did not finish cooperatively.");}
   if(generationPreviewPaths==null||generationPreviewPaths.Count<7||generationResult.HasBlockingIssues||generationConnectionPreview==null||!generationConnectionPreview.HasChanges)
    throw new Exception("Clean calibrated fixture did not preview connected walls: "+generationMessage);
   if(!generationPreviewGeometryValid||!world.GetComponentsInChildren<MeshFilter>().Any(f=>f.name=="Joined physical wall preview"))
    throw new Exception("The 3D preview did not build joined physical geometry.");
   if(JsonUtility.ToJson(design)!=before)throw new Exception("Generation preview modified the design.");
   int expected=generationPreviewPaths.Count;
   ClearGenerationPreview();if(JsonUtility.ToJson(design)!=before)throw new Exception("Cancel changed the design.");
   StartConnectedWallGeneration();steps=0;while(generationMaskJob!=null||generationPathJob!=null){WallGenerationUpdate();if(++steps>20000)throw new Exception("Repeat generation processing stalled.");}
   ApplyConnectedWallGeneration();Design.Validate(design);
   if(design.generationBatches.Count!=1||design.items.Count<7+expected||design.wallJunctions.Count==0||world.GetComponentsInChildren<JoinedWallSelection>().Length==0)
    throw new Exception("Apply did not create editable, physically joined wall items.");
   if(design.generationBatches[0].paths.Any(path=>path.manuallyEdited))throw new Exception("Automatic wall joining was mislabeled as a manual edit.");
   var generated=design.items.First(i=>!WallGenerationData.IsEmpty(i.generated));
   var copied=SceneClipboard.PreparePaste(design,SceneClipboard.Copy(design,new[]{generated.id},generated.id),1);
   if(!WallGenerationData.IsEmpty(copied.items[0].generated))throw new Exception("Copying a generated wall reused its source-path identity.");
   int committed=design.items.Count;string saved=JsonUtility.ToJson(design);
   Undo(-1);if(design.items.Count!=7||design.generationBatches.Count!=0)throw new Exception("Wall generation undo failed.");
   Undo(1);if(design.items.Count!=committed||JsonUtility.ToJson(design)!=saved)throw new Exception("Wall generation redo failed.");
   var round=JsonUtility.FromJson<Design>(JsonUtility.ToJson(design));Design.Validate(round);
   if(round.generationBatches.Count!=1||round.wallJunctions.Count!=design.wallJunctions.Count)throw new Exception("Native generation model round-trip failed.");
   StartConnectedWallGeneration();steps=0;while(generationMaskJob!=null||generationPathJob!=null){WallGenerationUpdate();if(++steps>20000)throw new Exception("Regeneration processing stalled.");}
   if(generationDiffPreview==null||generationDiffPreview.HasConflicts||generationDiffPreview.Count("Add")+generationDiffPreview.Count("Update")+generationDiffPreview.Count("Delete")>0)
    throw new Exception("Unchanged regeneration did not show an unchanged diff.");
   ApplyConnectedWallGeneration();if(design.items.Count!=committed)throw new Exception("Unchanged regeneration duplicated walls.");
   Debug.Log("ROOM_STUDIO_WALL_GENERATION_RUNTIME_PASSED: source image -> mask -> paths -> joins -> 2D/3D preview -> apply/cancel, undo/redo, native roundtrip and duplicate-free regeneration");
  }finally{
   generationMaskJob=null;generationPathJob=null;generationPreviewPaths=null;generationResult=null;generationSource=null;generationConnectionPreview=null;generationDiffPreview=null;
   design=JsonUtility.FromJson<Design>(original);ClearSelection();top=oldTop;cutaway=oldCutaway;Commit();Rebuild();
  }
 }
}
}
