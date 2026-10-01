import {validateFloorPlan, isEmptyFloorPlan, mergeFloorPlan} from './floor-plan.mjs';
import {createDefaultLinacMazeAnalysisConfig,EQUIPMENT_DEFAULTS} from './radiation-types.mjs';
const materials=['lead','concrete','gypsum','steel','glass','wood'];
const types=['xray','ct','linac','fluoroscopy','mammo','dental'];
const models={xray:'Xray',ct:'CT',linac:'Linac',fluoroscopy:'CathLab',mammo:'Mammography',dental:'Dental'};
const distributions=['rad_room_chest_bucky','rad_room_all_other','rad_room_floor_or_other_barriers','rad_tube_r_and_f','fluoro_tube','chest_room','ct_scanner','mammo_unit','dental_unit'];
const cap=s=>s[0].toUpperCase()+s.slice(1);
const same=(a,b)=>typeof a==='number'&&typeof b==='number'?Math.fround(a)===Math.fround(b):a===b;
const hasGeneratedRef=item=>item?.generated?.batchId&&item.generated.pathId;
export function generationMetadata(d){
 const batches=Array.isArray(d.generationBatches)?d.generationBatches:[];
 const itemRefs=(d.items??[]).filter(hasGeneratedRef).map(item=>({itemId:item.id,generated:structuredClone(item.generated)}));
 return batches.length||itemRefs.length?{version:1,batches:structuredClone(batches),itemRefs}:null;
}
function finite(x,label){if(typeof x!=='number'||!Number.isFinite(x))throw Error('Invalid number: '+label);}
function enumValue(x,values,label){if(!values.includes(x))throw Error('Invalid '+label+': '+x);}
function barrier(b,label){if(!b||typeof b!=='object')throw Error('Missing '+label);finite(b.thickness,label+'.thickness');if(b.thickness<0)throw Error('Negative '+label+' thickness');enumValue(b.material,materials,label+' material');}
export function normalizeWorkspace(input){
 const document=structuredClone(input), room=document?.room??document, repairs=[];
 if(!room||typeof room!=='object'||Array.isArray(room)||'report' in room)throw Error('Expected a bare room or workspace with a room field.');
 function fill(o,key,value,path){if(o[key]===undefined){o[key]=value;repairs.push(path+'.'+key);}}
 for(const k of ['width','height','roomHeight']){finite(room[k],k);if(room[k]<=0||room[k]>100)throw Error('Room dimensions must be > 0 and <= 100 m.');}
 for(const k of ['walls','equipment','workstations','occupiedRegions']){fill(room,k,[],'room');if(!Array.isArray(room[k])||room[k].length>250)throw Error('Invalid/oversized '+k);}
 for(const k of ['floor','ceiling']){fill(room,k,{thickness:150,material:'concrete'},'room');barrier(room[k],k);}
 if(room.floorPlan!==undefined)room.floorPlan=validateFloorPlan(room.floorPlan);
 fill(room,'floorPlanOpacity',.3,'room');fill(room,'sampleDistance',.3,'room');
 fill(room,'linacMazeAnalysis',{...createDefaultLinacMazeAnalysisConfig(),enabled:false},'room');
 const ids=new Set();
 function id(o){if(!o||typeof o.id!=='string'||!o.id||ids.has(o.id))throw Error('Missing or duplicate object ID.');ids.add(o.id);}
 function nums(o,keys){for(const k of keys)finite(o[k],o.id+'.'+k);}
 for(const w of room.walls){id(w);nums(w,['x1','y1','x2','y2']);barrier(w,w.id);}
 for(const e of room.equipment){id(e);enumValue(e.type,types,'equipment type');nums(e,['x','y','z','workload','kVp','K1','nPatients','useFactor']);enumValue(e.workloadDistribution,distributions,'workload distribution');fill(e,'sourceComponents',[],e.id);if(!Array.isArray(e.sourceComponents))throw Error('Invalid sourceComponents');for(const s of e.sourceComponents){enumValue(s.type,['primary','scatter','leakage'],'source type');enumValue(s.workloadDistribution,distributions,'source workload distribution');nums(s,['x','y','z','relativeOutput','directionDegrees','elevationDegrees','beamWidthDegrees']);if(typeof s.enabled!=='boolean')throw Error('Invalid source enabled flag');}}
 for(const w of room.workstations){id(w);nums(w,['x','y','z','occupancyFactor']);if(typeof w.isControlled!=='boolean')throw Error('Invalid controlled-area flag');}
 for(const r of room.occupiedRegions){id(r);enumValue(r.scope,['wall','floor','ceiling'],'region scope');nums(r,['occupancyFactor','designGoal']);if(!Array.isArray(r.points)||r.points.length>100)throw Error('Invalid region points');for(const p of r.points)nums(p,['x','y']);}
 function visit(o,path='room'){if(typeof o==='number')finite(o,path);if(o&&typeof o==='object')for(const [k,v]of Object.entries(o))visit(v,path+'.'+k);}
 visit(room);return {document,room,repairs};
}
export function importWorkspace(input){
 const {document,room:r,repairs}=normalizeWorkspace(input),ox=r.width/2,oy=r.height/2;
 const b=x=>({material:cap(x.material),thickness:x.thickness,density:2350});
 const item=(id,kind,name)=>({id,kind,name:name??id,model:'CT',isControlled:true,x:0,z:0,y:0,angle:0,length:4,height:r.roomHeight,scale:1,occupancy:1,assessmentHeight:1.2,shielding:{material:'Concrete',thickness:150,density:2350}});
 const d={version:2,schemaVersion:2,name:document.name??'Imported ProShield room',width:r.width,depth:r.height,height:r.roomHeight,floor:b(r.floor),ceiling:b(r.ceiling),sampleDistance:r.sampleDistance,items:[],regions:[],energy:6,workload:500,gantry:0,fieldX:20,fieldY:20,isocentreHeight:1.3,isoOffsetX:0,isoOffsetZ:-1.6,sourceDistance:1,selectedModel:'CT',sourceJson:JSON.stringify(document),sourceProjectionJson:'',importSummary:''};
 for(const w of r.walls)d.items.push({...item(w.id,'Wall',w.id),x:(w.x1+w.x2)/2-ox,z:(w.y1+w.y2)/2-oy,angle:-Math.atan2(w.y2-w.y1,w.x2-w.x1)*180/Math.PI,length:Math.hypot(w.x2-w.x1,w.y2-w.y1),shielding:b(w)});
 for(const e of r.equipment)d.items.push({...item(e.id,'Source',e.name),model:models[e.type],x:e.x-ox,z:e.y-oy,y:e.z-EQUIPMENT_DEFAULTS[e.type].z,assessmentHeight:EQUIPMENT_DEFAULTS[e.type].z,angle:-(e.planRotationDegrees??0),scale:e.type==='linac'?.75:1});
 for(const w of r.workstations)d.items.push({...item(w.id,'Desk',w.name),x:w.x-ox,z:w.y-oy,y:w.z,assessmentHeight:0,occupancy:w.occupancyFactor,isControlled:w.isControlled,scale:.33});
 for(const q of r.occupiedRegions)d.regions.push({id:q.id,name:q.name??q.id,scope:cap(q.scope),occupancy:q.occupancyFactor,designGoal:q.designGoal,points:q.points.map(p=>({x:p.x-ox,y:p.y-oy}))});
 if(r.floorPlan!==undefined)d.floorPlan=structuredClone(r.floorPlan);
 const generated=r.floorPlan?.roomStudioGeneration;
 if(generated!==undefined){
  if(generated?.version!==1||!Array.isArray(generated.batches)||generated.batches.length>32||!Array.isArray(generated.itemRefs))throw Error('Unsupported Room Studio generation metadata.');
  d.generationBatches=structuredClone(generated.batches);
  const byId=new Map(d.items.map(item=>[item.id,item]));
  for(const ref of generated.itemRefs){const item=byId.get(ref?.itemId);if(!item||item.kind!=='Wall'||!ref.generated?.batchId||!ref.generated?.pathId)throw Error('Broken Room Studio generated-wall reference.');item.generated=structuredClone(ref.generated);}
 }
 if(r.floorPlan?.opacity!==undefined)d.floorPlanOpacity=r.floorPlan.opacity;
 if(d.items.length>250)throw Error('Visual editor supports at most 250 objects.');
 d.sourceProjectionJson=JSON.stringify({...d,sourceJson:'',sourceProjectionJson:''});
 d.importSummary=`Imported ${r.walls.length} walls, ${r.equipment.length} sources, ${r.workstations.length} workstations. `+(repairs.length?'Added documented defaults: '+repairs.join(', ')+'.':'No field repairs.')+' Original optional values, unknown fields and source components are retained. Source energy/components and maze settings are preserved; their detailed controls are a later step.';
 return {design:d,summary:d.importSummary};
}
export function mergeWorkspace(d,nativeRoom){
 if(!d.sourceJson)return {schemaVersion:2,application:'ProShield',room:nativeRoom};
 const document=JSON.parse(d.sourceJson),r=document.room??document,b=JSON.parse(d.sourceProjectionJson);
 const set=(o,k,v,old,newValue=v)=>{if(!same(v,old))o[k]=newValue;};
 set(r,'width',d.width,b.width);set(r,'height',d.depth,b.depth);set(r,'roomHeight',d.height,b.height);set(r,'sampleDistance',d.sampleDistance,b.sampleDistance);
 for(const k of ['floor','ceiling']){set(r[k],'thickness',d[k].thickness,b[k].thickness);set(r[k],'material',d[k].material,b[k].material,d[k].material.toLowerCase());}
 const now=new Map(d.items.map(i=>[i.id,i])),before=new Map(b.items.map(i=>[i.id,i]));
 // A changed footprint shifts the canonical origin while retaining the centred editor layout.
 const sx=same(d.width,b.width)?0:(d.width-b.width)/2,sy=same(d.depth,b.depth)?0:(d.depth-b.depth)/2;
 function delta(i,a,k){return same(i[k],a[k])?0:i[k]-a[k];}
 r.walls=r.walls.filter(w=>now.has(w.id));
 for(const w of r.walls){const i=now.get(w.id),a=before.get(w.id),dx=delta(i,a,'x')+sx,dy=delta(i,a,'z')+sy;
  if(!same(i.angle,a.angle)||!same(i.length,a.length)){const x=(w.x1+w.x2)/2,y=(w.y1+w.y2)/2,angle=-i.angle*Math.PI/180;w.x1=x-Math.cos(angle)*i.length/2;w.x2=x+Math.cos(angle)*i.length/2;w.y1=y-Math.sin(angle)*i.length/2;w.y2=y+Math.sin(angle)*i.length/2;}
  if(dx){w.x1+=dx;w.x2+=dx;}if(dy){w.y1+=dy;w.y2+=dy;}set(w,'thickness',i.shielding.thickness,a.shielding.thickness);set(w,'material',i.shielding.material,a.shielding.material,i.shielding.material.toLowerCase());
 }
 r.equipment=r.equipment.filter(e=>now.has(e.id));
 for(const e of r.equipment){const i=now.get(e.id),a=before.get(e.id),dx=delta(i,a,'x')+sx,dy=delta(i,a,'z')+sy,dz=delta(i,a,'y'),turn=-delta(i,a,'angle'),rad=turn*Math.PI/180;
  for(const s of e.sourceComponents??[]){if(turn){const x=s.x-e.x,y=s.y-e.y;s.x=e.x+Math.cos(rad)*x-Math.sin(rad)*y;s.y=e.y+Math.sin(rad)*x+Math.cos(rad)*y;s.directionDegrees=((s.directionDegrees+turn)%360+360)%360;}if(dx)s.x+=dx;if(dy)s.y+=dy;if(dz)s.z+=dz;}
  if(dx)e.x+=dx;if(dy)e.y+=dy;if(dz)e.z+=dz;if(turn)e.planRotationDegrees=((e.planRotationDegrees??0)+turn+360)%360;set(e,'name',i.name,a.name);
 }
 r.workstations=r.workstations.filter(w=>now.has(w.id));
 for(const w of r.workstations){const i=now.get(w.id),a=before.get(w.id);const dx=delta(i,a,'x')+sx,dy=delta(i,a,'z')+sy,dz=delta(i,a,'y')+delta(i,a,'assessmentHeight');if(dx)w.x+=dx;if(dy)w.y+=dy;if(dz)w.z+=dz;set(w,'name',i.name,a.name);set(w,'occupancyFactor',i.occupancy,a.occupancy);set(w,'isControlled',i.isControlled,a.isControlled);}
 const regionNow=new Map(d.regions.map(q=>[q.id,q])),regionBefore=new Map(b.regions.map(q=>[q.id,q]));
 r.occupiedRegions=r.occupiedRegions.filter(q=>regionNow.has(q.id));
 for(const q of r.occupiedRegions){const i=regionNow.get(q.id),a=regionBefore.get(q.id);set(q,'name',i.name,a.name);set(q,'scope',i.scope,a.scope,i.scope.toLowerCase());set(q,'occupancyFactor',i.occupancy,a.occupancy);set(q,'designGoal',i.designGoal,a.designGoal);if(i.points.length!==a.points.length)q.points=i.points.map(p=>({x:p.x+d.width/2,y:p.y+d.depth/2}));else q.points.forEach((p,k)=>{const dx=delta(i.points[k],a.points[k],'x')+sx,dy=delta(i.points[k],a.points[k],'y')+sy;if(dx)p.x+=dx;if(dy)p.y+=dy;});}
 for(const w of nativeRoom.walls)if(!before.has(w.id))r.walls.push(w);
 for(const e of nativeRoom.equipment)if(!before.has(e.id))r.equipment.push(e);
 for(const w of nativeRoom.workstations)if(!before.has(w.id))r.workstations.push(w);
 for(const q of nativeRoom.occupiedRegions)if(!regionBefore.has(q.id))r.occupiedRegions.push(q);
 const beforeFloorPlan=b.floorPlan;
 if(!isEmptyFloorPlan(d.floorPlan)){const guide=validateFloorPlan(d.floorPlan),generated=generationMetadata(d);if(generated)guide.roomStudioGeneration=generated;else delete guide.roomStudioGeneration;
  r.floorPlan=mergeFloorPlan(r.floorPlan,guide,b.floorPlan);if(!generated)delete r.floorPlan.roomStudioGeneration;
  if(!same(guide.opacity,b.floorPlan?.opacity))r.floorPlanOpacity=guide.opacity;}
 else if(beforeFloorPlan!==undefined&&beforeFloorPlan!==null)delete r.floorPlan;
 return document;
}
