import {validateFloorPlan,isEmptyFloorPlan} from './floor-plan.mjs';
import {generateQAReport} from './radiation-calculations.mjs';
import {EQUIPMENT_DEFAULTS} from './radiation-types.mjs';
import {importWorkspace,mergeWorkspace,generationMetadata} from './workspace.mjs';
export function fromDesign(d) {
 const native=editorRoom(d);if(!d.sourceJson)return native;const doc=mergeWorkspace(d,native);return doc.room??doc;
}
function editorRoom(d) {
 if(d.items?.some(i=>i.kind==='Component'))throw Error('Custom component geometry is not supported by reference QA or canonical ProShield export. Save a native design or export component JSON to preserve these shapes; remove custom components before using the reference engine.');
 if(d.items?.some(i=>Array.isArray(i.doors)&&i.doors.length))throw Error('Door openings are not supported by reference QA or canonical ProShield export. Save a native design to preserve door materials and lead lining; remove doors before using the reference engine.');
 const unsupported=(d.items??[]).filter(i=>i.generated?.unsupportedGeometry).map(i=>i.id);
 if(unsupported.length)throw Error('Generated clipped/custom wall geometry is not supported by reference QA or canonical ProShield export. Wall IDs: '+unsupported.join(', ')+'. Save a native design; do not use an approximate canonical barrier.');
 if(d.wallJunctions?.length){const ids=[...new Set(d.wallJunctions.flatMap(j=>(j.arms??[]).map(a=>a.wallId).filter(Boolean)))];
  throw Error('Joined wall footprints are not supported by reference QA or canonical ProShield export. Wall IDs: '+ids.join(', ')+'. Save a native design until joined geometry is validated against the reference engine.');}
 const ox=d.width/2,oy=d.depth/2;
 const barrier=b=>({thickness:b.thickness,material:b.material.toLowerCase()});
 const room={width:d.width,height:d.depth,roomHeight:d.height,sampleDistance:d.sampleDistance??.3,walls:[],equipment:[],workstations:[],occupiedRegions:[],floor:barrier(d.floor),ceiling:barrier(d.ceiling),floorPlanOpacity:d.floorPlan?.opacity??.5};
 if(!isEmptyFloorPlan(d.floorPlan)){room.floorPlan=validateFloorPlan(d.floorPlan);const generated=generationMetadata(d);if(generated)room.floorPlan.roomStudioGeneration=generated;else delete room.floorPlan.roomStudioGeneration;}
 for(const i of d.items){
  const a=i.angle*Math.PI/180;
  if(i.kind==='Wall')room.walls.push({id:i.id,x1:i.x+ox-Math.cos(a)*i.length/2,y1:i.z+oy+Math.sin(a)*i.length/2,x2:i.x+ox+Math.cos(a)*i.length/2,y2:i.z+oy-Math.sin(a)*i.length/2,...barrier(i.shielding)});
  if(i.kind==='LINAC')room.equipment.push({...structuredClone(EQUIPMENT_DEFAULTS.linac),id:i.id,name:i.name,x:i.x+ox+Math.cos(a)*d.isoOffsetX+Math.sin(a)*d.isoOffsetZ,y:i.z+oy-Math.sin(a)*d.isoOffsetX+Math.cos(a)*d.isoOffsetZ,z:i.y+d.isocentreHeight,planRotationDegrees:((360-i.angle)%360),gantryAngleDegrees:d.gantry,workload:d.workload*100,kVp:d.energy*1000,linacFieldSizeCm:2*d.fieldX*d.fieldY/(d.fieldX+d.fieldY)});
  if(i.kind==='Desk')room.workstations.push({id:i.id,name:i.name,x:i.x+ox,y:i.z+oy,z:i.y+i.assessmentHeight,occupancyFactor:i.occupancy,isControlled:i.isControlled??true});
 }
 for(const r of d.regions??[])room.occupiedRegions.push({id:r.id,name:r.name,scope:r.scope.toLowerCase(),occupancyFactor:r.occupancy,designGoal:r.designGoal,points:r.points.map(p=>({x:p.x+ox,y:p.y+oy}))});
 return room;
}
export function run(input){
 const room=input.room??(input.items?fromDesign(input):input);
 if(!room||!Array.isArray(room.walls)||!Array.isArray(room.equipment)||!Array.isArray(room.workstations)||!Array.isArray(room.occupiedRegions))throw Error('Expected a ProShield room, {room} workspace, or Room Studio design.');
 if(room.walls.length>250||room.equipment.length>100||room.occupiedRegions.length>100)throw Error('Input exceeds desktop calculation limits.');
 for(const key of ['width','height','roomHeight'])if(!Number.isFinite(room[key])||room[key]<=0||room[key]>100)throw Error('Invalid room dimension: '+key);
 const report=generateQAReport(room);
 const rows=[...report.results,...report.floorResults,...report.ceilingResults];
 function requireFinite(value,path='report'){
  if(typeof value==='number'&&!Number.isFinite(value))throw Error('Non-finite calculation result at '+path);
  if(value&&typeof value==='object')for(const [key,child] of Object.entries(value))requireFinite(child,path+'.'+key);
 }
 requireFinite(report);
 return {engine:'ProShield TypeScript reference / c33e41d',notice:'Planning prototype. Not an approval or construction certificate. Air-kerma in mGy/week; no conversion to effective dose. Barrier samples are not direct workstation dose measurements.',summary:rows.length?`${rows.length} barrier assessments; ${rows.filter(r=>!r.passed).length} exceed the source design-goal test.`:'No occupied samples assessed. No compliance conclusion.',report,rows};
}
export function execute(input,mode='qa'){
 if(mode==='import')return importWorkspace(input);
 if(mode==='export'){
  if(generationMetadata(input)&&isEmptyFloorPlan(input.floorPlan))throw Error('Generated-wall provenance has no active floor-plan metadata boundary for canonical export. Save a native design to retain batches, paths and deleted overrides.');
  return {json:JSON.stringify(mergeWorkspace(input,editorRoom(input)),null,2)};
 }
 if(mode==='qa')return run(input);
 throw Error('Unknown reference operation: '+mode);
}
