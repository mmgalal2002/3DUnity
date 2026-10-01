import assert from 'node:assert/strict';
import fs from 'node:fs';
import {createHash} from 'node:crypto';
import {fromDesign,run,execute} from './engine.mjs';
import {importWorkspace} from './workspace.mjs';
import {validateFloorPlan} from './floor-plan.mjs';

const fixture=JSON.parse(fs.readFileSync(new URL('import-fixture.json',import.meta.url)));
const png='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aL1sAAAAASUVORK5CYII=';
const guide={kind:'Image',sourceName:'pixel.png',sourcePath:'',pixelWidth:1,pixelHeight:1,widthMeters:1,heightMeters:1,metersPerPixel:1,imageBase64:png,opacity:.35,x:0,z:0,rotation:0,visible:true,segments:[]};
const color={r:1,g:0,b:1,a:1};
guide.authoring={version:1,id:'plan-1',sourceFingerprint:createHash('sha256').update(Buffer.from(png,'base64')).digest('hex'),pixelWidth:1,pixelHeight:1,alphaThreshold:.1,
 calibration:{confirmed:true,method:'Manual',unit:'m/px',value:1,metresPerPixel:1,distanceMetres:5,a:{x:0,y:0},b:{x:1,y:1}},
 rules:[{id:'rule-1',name:'Magenta wall',classification:'Wall',colorSpace:'RGB',material:'Concrete',enabled:true,priority:0,minAreaPixels:4,tolerance:.08,minLengthPixels:5,height:5,baseElevation:0,thicknessMm:150,target:color,display:color}],annotation:{keep:'future extension'}};
const source=structuredClone(fixture);(source.room??source).floorPlan=guide;
const design=importWorkspace(source).design;
assert.deepEqual(run(design).rows,run(fixture).rows,'authoring metadata changed shielding QA');
const geometry=fromDesign(design);delete geometry.floorPlan;
const baseline=fromDesign(importWorkspace(fixture).design);delete baseline.floorPlan;
assert.deepEqual(geometry,baseline,'authoring metadata changed physical geometry');
assert.deepEqual(JSON.parse(execute(design,'export').json),source,'authoring canonical roundtrip');
design.floorPlan.authoring.rules[0].display={r:0,g:1,b:0,a:1};
const exported=JSON.parse(execute(design,'export').json),room=exported.room??exported;
assert.deepEqual(room.floorPlan.authoring.annotation,guide.authoring.annotation,'unknown authoring metadata dropped');
assert.deepEqual(run(design).rows,run(fixture).rows,'rule display color changed QA');
const withDensity=structuredClone(guide);withDensity.authoring.rules[0].densityKgM3=2450;
assert.equal(validateFloorPlan(withDensity).authoring.rules[0].densityKgM3,2450,'explicit physical density lost');
for(const edit of [p=>p.version=2,p=>p.sourceFingerprint='bad',p=>p.pixelWidth=2,p=>p.alphaThreshold=NaN,p=>p.rules.push(structuredClone(p.rules[0])),p=>p.rules[0].material='magenta',p=>p.rules[0].densityKgM3=0,p=>p.rules[0].priority=.5,p=>p.rules[0].target.r=2,p=>p.rules[0].tolerance=-1,p=>p.calibration.metresPerPixel=.5,p=>{p.calibration.method='TwoPoint';p.calibration.b={x:0,y:0};}]){
 const invalid=structuredClone(guide);edit(invalid.authoring);assert.throws(()=>validateFloorPlan(invalid));
}
const empty=structuredClone(guide);empty.authoring={version:0,id:'',sourceFingerprint:'',rules:[],calibration:{confirmed:false}};
assert.equal(validateFloorPlan(empty).authoring,undefined,'empty optional Unity DTO was serialized as active authoring');
console.log('ROOM_STUDIO_PLAN_AUTHORING_BRIDGE_PASSED: metadata/unknown-field roundtrip, geometry/QA invariance, color-only edits, malformed rules/calibration rejection, legacy empty DTO');
