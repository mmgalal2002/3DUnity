import assert from 'node:assert/strict';
import fs from 'node:fs';
import {createHash} from 'node:crypto';
import {run,execute} from './engine.mjs';
import {importWorkspace} from './workspace.mjs';

const fixture=JSON.parse(fs.readFileSync(new URL('import-fixture.json',import.meta.url)));
const png='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aL1sAAAAASUVORK5CYII=';
const fingerprint=createHash('sha256').update(Buffer.from(png,'base64')).digest('hex');
const design=importWorkspace(fixture).design,wall=design.items.find(item=>item.kind==='Wall');
const guide={kind:'Image',sourceName:'one-pixel.png',sourcePath:'',pixelWidth:1,pixelHeight:1,widthMeters:1,heightMeters:1,metersPerPixel:1,imageBase64:png,opacity:.35,x:0,z:0,rotation:0,visible:true,segments:[]};
design.floorPlan=guide;
const pixel={x:.5,y:.5},color={r:1,g:0,b:1,a:1};
wall.generated={batchId:'batch-1',pathId:'path-1',ruleId:'rule-1',categoryName:'Partition',displayColor:color,sourceAPixel:pixel,sourceBPixel:pixel,manuallyEdited:false,unsupportedGeometry:false};
design.generationBatches=[{version:1,id:'batch-1',sourceFingerprint:fingerprint,snapshotSignature:'a'.repeat(64),pixelWidth:1,pixelHeight:1,sourceSnapshot:null,paths:[
 {id:'path-1',ruleId:'rule-1',itemId:wall.id,categoryName:'Partition',material:'Concrete',displayColor:color,sourceAPixel:pixel,sourceBPixel:pixel,aPixel:pixel,bPixel:pixel,
  aWorld:{x:0,y:0,z:0},bWorld:{x:1,y:0,z:0},sourceComponent:0,areaPixels:1,lengthPixels:1,height:5,baseElevation:0,thicknessMm:150,densityKgM3:2350,manuallyEdited:false,deletedOverride:false,unsupportedGeometry:false}]}];
const baseline=importWorkspace(fixture).design;
assert.deepEqual(run(design).rows,run(baseline).rows,'straight generated-wall metadata changed reference QA');
const canonical=JSON.parse(execute(design,'export').json),room=canonical.room??canonical;
assert.equal(room.floorPlan.roomStudioGeneration.batches[0].paths[0].id,'path-1','canonical editor metadata lost the source path');
const restored=importWorkspace(canonical).design;
assert.equal(restored.items.find(item=>item.id===wall.id).generated.pathId,'path-1','workspace import lost item provenance');
assert.equal(restored.generationBatches[0].paths[0].itemId,wall.id,'workspace import lost batch paths');
const withoutGuide=structuredClone(design);withoutGuide.floorPlan=null;
assert.deepEqual(run(withoutGuide).rows,run(baseline).rows,'source guide removal changed straight-wall QA');
assert.throws(()=>execute(withoutGuide,'export'),/metadata boundary.*native design/i,'canonical export silently dropped orphaned provenance');
const clipped=structuredClone(design);clipped.items.find(item=>item.id===wall.id).generated.unsupportedGeometry=true;
for(const mode of ['qa','export'])assert.throws(()=>execute(clipped,mode),error=>error.message.includes(wall.id)&&/clipped\/custom wall geometry/.test(error.message),mode+' flattened unsupported geometry');
const joined=structuredClone(design);joined.wallJunctions=[{id:'join-1',kind:'T',x:0,z:0,arms:[{wallId:wall.id,endpoint:'End'}]}];
for(const mode of ['qa','export'])assert.throws(()=>execute(joined,mode),error=>error.message.includes(wall.id)&&/Joined wall footprints/.test(error.message),mode+' ignored joined footprint');
console.log('ROOM_STUDIO_WALL_GENERATION_BRIDGE_PASSED: straight-wall QA parity, canonical metadata roundtrip, guide-removal export guard and fail-closed clipped/joined geometry');
