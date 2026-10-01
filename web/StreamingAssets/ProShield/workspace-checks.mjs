import assert from 'node:assert/strict';
import fs from 'node:fs';
import {importWorkspace,normalizeWorkspace,mergeWorkspace} from './workspace.mjs';
import {fromDesign,run,execute} from './runner.mjs';
import {EQUIPMENT_DEFAULTS,createDefaultLinacMazeAnalysisConfig} from './radiation-types.mjs';
const room={width:8.123456789,height:7,roomHeight:4,floor:{material:'concrete',thickness:200.123456789},ceiling:{material:'lead',thickness:20},floorPlanOpacity:.3,sampleDistance:.3,floorPlanImage:'data:image/png;base64,preserve-me',customMetadata:{review:'unchanged'},linacMazeAnalysis:{...createDefaultLinacMazeAnalysisConfig(),enabled:false},walls:[{id:'wall',x1:8.123456789,y1:7,x2:8.123456789,y2:0,thickness:800,material:'concrete',extra:'wall annotation'}],equipment:[{...structuredClone(EQUIPMENT_DEFAULTS.linac),id:'linac',name:'LINAC',x:3.123456789012345,y:3,z:1.3,kVp:6,planRotationDegrees:15,sourceComponents:[{id:'beam',name:'Explicit beam',type:'primary',x:3.623456789012345,y:3,z:1.3,relativeOutput:1,workloadDistribution:'rad_room_all_other',directionDegrees:0,elevationDegrees:0,beamWidthDegrees:40,enabled:true,extra:'source annotation'}]},{...structuredClone(EQUIPMENT_DEFAULTS.ct),id:'ct',name:'CT',x:2,y:4,z:1}],workstations:[{id:'desk',name:'Desk',x:9,y:3,z:1.2,occupancyFactor:.333333333333,isControlled:false}],occupiedRegions:[]};
const wrapper={room,selectedTool:'wall',schemaVersion:2,editorState:{anything:'preserved'},unknownEnvelope:'keep'};
const guide={kind:'Vector',sourceName:'trace.json',sourcePath:'',widthMeters:12,heightMeters:8,metersPerPixel:.01,pixelWidth:0,pixelHeight:0,imageBase64:'',opacity:.4,x:1,z:-2,rotation:15,visible:true,segments:[{start:{x:-6,y:-4},end:{x:6,y:-4}}]};
function unityRound(d){return JSON.parse(JSON.stringify(d,(_k,v)=>typeof v==='number'?Math.fround(v):v));}
for(const input of [room,wrapper]){
 const imported=importWorkspace(input),d=unityRound(imported.design);
 const out=mergeWorkspace(d,fromDesign(d));
 assert.deepEqual(out,input,'untouched import preserves all JSON fields and exact numeric values');
 assert.deepEqual(run(d).rows,run(input).rows,'imported QA equals canonical QA');
 assert.deepEqual(mergeWorkspace(JSON.parse(JSON.stringify(d)),fromDesign(d)),input,'saved design preserves raw source');
}
const d=unityRound(importWorkspace(wrapper).design),base=structuredClone(d);
const guideInput={...wrapper,room:{...room,floorPlan:unityRound(guide)}};
const guideDesign=unityRound(importWorkspace(guideInput).design);
assert.deepEqual(guideDesign.floorPlan,guideInput.room.floorPlan,'floor-plan metadata imports into the editor without becoming an item');
const guideExport=mergeWorkspace(guideDesign,fromDesign(guideDesign));
assert.deepEqual(guideExport,guideInput,'floor-plan metadata survives canonical round-trip');
assert.equal(fromDesign(guideDesign).walls.length,guideInput.room.walls.length,'floor-plan metadata does not alter canonical walls');
const clearedGuide=structuredClone(guideDesign);clearedGuide.floorPlan=null;
assert.equal(mergeWorkspace(clearedGuide,fromDesign(clearedGuide)).room.floorPlan,undefined,'clearing a guide removes only guide metadata');
const protectedDesign=structuredClone(base);for(const item of protectedDesign.items){item.locked=true;item.groupId='protected-group';item.transparency=.7;}
assert.deepEqual(mergeWorkspace(protectedDesign,fromDesign(protectedDesign)),wrapper,'protection and appearance do not change canonical export');
assert.deepEqual(run(protectedDesign).rows,run(base).rows,'protection and appearance do not change QA');
const visualDesign=structuredClone(base);visualDesign.items.push({id:'planmeca-visual',kind:'Model',model:'PlanmecaViso',name:'Planmeca Viso',x:1,y:0,z:2,angle:0,scale:1});
assert.deepEqual(mergeWorkspace(visualDesign,fromDesign(visualDesign)),wrapper,'Planmeca visual does not become a canonical source');
assert.deepEqual(run(visualDesign).rows,run(base).rows,'Planmeca visual does not change reference QA');
d.items.find(i=>i.id==='linac').x+=1;
const moved=fromDesign(d);assert.ok(Math.abs(moved.equipment[0].x-room.equipment[0].x-1)<1e-6);assert.ok(Math.abs(moved.equipment[0].sourceComponents[0].x-room.equipment[0].sourceComponents[0].x-1)<1e-6);
assert.equal(moved.equipment[0].kVp,6);assert.deepEqual(moved.equipment[1],room.equipment[1]);assert.deepEqual(moved.walls,room.walls);
const edited=structuredClone(base);edited.items.find(i=>i.id==='wall').shielding.thickness=900;assert.equal(fromDesign(edited).walls[0].thickness,900);assert.equal(fromDesign(edited).walls[0].y1,7,'wall endpoint ordering retained');
edited.items=edited.items.filter(i=>i.id!=='ct');assert.equal(fromDesign(edited).equipment.length,1);
const minimal={width:5,height:5,roomHeight:3};const repaired=importWorkspace(minimal);assert.match(repaired.summary,/Added documented defaults/);assert.equal(fromDesign(repaired.design).floor.thickness,150);assert.equal(fromDesign(repaired.design).linacMazeAnalysis.enabled,false);
const missing=structuredClone(room);delete missing.equipment[0].sourceComponents;assert.deepEqual(normalizeWorkspace(missing).room.equipment[0].sourceComponents,[]);
for(const mutate of [r=>r.walls[0].id='ct',r=>r.equipment[0].type='mri',r=>r.floor.material='unknown',r=>r.equipment[0].x=Infinity]){const bad=structuredClone(room);mutate(bad);assert.throws(()=>importWorkspace(bad));}
const shapedImport=structuredClone(base);shapedImport.items.push({id:'custom',kind:'Component'});
assert.throws(()=>run(shapedImport),/Custom component geometry is not supported/);
assert.throws(()=>execute(shapedImport,'export'),/Custom component geometry is not supported/);
fs.writeFileSync(new URL('./import-fixture.json',import.meta.url),JSON.stringify(wrapper,null,2));
fs.writeFileSync(new URL('./imported-design-fixture.json',import.meta.url),JSON.stringify(importWorkspace(wrapper).design,null,2));
console.log('PASS: bare/wrapper import, float32 scene projection with exact double round-trip, unknown fields, saved design round-trip, direct QA parity, Planmeca visual source isolation, source translation, mixed MV storage, wall ordering, deletion, documented repairs, empty components, invalid enums/numbers/IDs.');
