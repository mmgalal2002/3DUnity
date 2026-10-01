import assert from 'node:assert/strict';
import fs from 'node:fs';
import {importWorkspace,mergeWorkspace} from './workspace.mjs';
import {fromDesign,run,execute} from './engine.mjs';
import {validateFloorPlan} from './floor-plan.mjs';
const round=d=>JSON.parse(JSON.stringify(d,(_k,v)=>typeof v==='number'?Math.fround(v):v));
const fixture=JSON.parse(fs.readFileSync(new URL('import-fixture.json',import.meta.url)));
const room=fixture.room??fixture;
const vector={kind:'Vector',sourceName:'trace.json',sourcePath:'',widthMeters:12.123456789,heightMeters:8,metersPerPixel:.01,pixelWidth:0,pixelHeight:0,imageBase64:'',opacity:.4567890123,x:1.23456789012,z:-2,rotation:15.123456789,visible:true,segments:[{start:{x:-6.0123456789,y:-4,note:'retain endpoint annotation'},end:{x:6,y:-4}}],annotation:{keep:true}};
const source=structuredClone(fixture),withGuide=source.room??source;withGuide.floorPlan=vector;
const baseline=run(fixture);
const design=round(importWorkspace(source).design);
assert.deepEqual(mergeWorkspace(design,fromDesign(design)),source,'float32 guide must preserve precise source metadata');
assert.deepEqual(run(design).rows,baseline.rows,'guide must not change QA');
const edited=structuredClone(design);edited.floorPlan.x+=1;
const editedDoc=JSON.parse(execute(edited,'export').json),editedRoom=editedDoc.room??editedDoc;
assert.equal(editedRoom.floorPlan.rotation,vector.rotation,'editing X must preserve precise rotation');
assert.deepEqual(editedRoom.floorPlan.annotation,vector.annotation);
for(const key of ['walls','equipment','workstations','occupiedRegions','floor','ceiling'])assert.deepEqual(editedRoom[key],room[key],key+' changed due to guide');
design.floorPlan=null;assert.equal((JSON.parse(execute(design,'export').json).room??{}).floorPlan,undefined,'clear guide');
for(const change of [p=>p.opacity=2,p=>p.x=Infinity,p=>p.widthMeters=0,p=>p.rotation=3601,p=>p.segments=[],p=>p.segments[0].end.x=99,p=>p.segments[0].start.x=NaN,p=>p.sourceName='x'.repeat(261)]){const invalid=structuredClone(vector);change(invalid);assert.throws(()=>validateFloorPlan(invalid));}
const png='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aL1sAAAAASUVORK5CYII=';
const image={kind:'Image',sourceName:'pixel.png',sourcePath:'',pixelWidth:1,pixelHeight:1,widthMeters:1,heightMeters:1,metersPerPixel:1,imageBase64:png,opacity:.35,x:0,z:0,rotation:0,visible:true,segments:[]};
assert.deepEqual(validateFloorPlan(image),image);
for(const change of [p=>p.imageBase64='not-base64',p=>p.imageBase64=btoa('x'.repeat(30)),p=>p.pixelWidth=2,p=>p.metersPerPixel=.5]){const invalid=structuredClone(image);change(invalid);assert.throws(()=>validateFloorPlan(invalid));}
const legacy=round(importWorkspace(fixture).design);legacy.floorPlan={kind:'',pixelWidth:0,pixelHeight:0,imageBase64:'',segments:[]};
assert.deepEqual(JSON.parse(execute(legacy,'export').json),fixture,'Unity empty optional DTO must not introduce a guide');
console.log('ROOM_STUDIO_FLOORPLAN_BRIDGE_PASSED: precision, unknown metadata, edited guide, clear guide, QA/geometry parity, PNG header, invalid input, legacy empty DTO');
