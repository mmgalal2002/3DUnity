import {emptyPlanAuthoring,validatePlanAuthoring} from './plan-authoring.mjs';
// Editor guide metadata only; never projected into shielding geometry.
export function isEmptyFloorPlan(p){return p==null||((!p.kind||p.kind==='Image')&&!p.pixelWidth&&!p.pixelHeight&&!p.imageBase64&&!p.sourceName&&!p.sourcePath&&!p.segments?.length);}
export function validateFloorPlan(value){
 if(isEmptyFloorPlan(value))return null;
 if(typeof value!=='object'||Array.isArray(value))throw Error('Invalid floor-plan guide.');
 const p=structuredClone(value);
 function range(v,min,max,label){if(typeof v!=='number'||!Number.isFinite(v)||v<min||v>max)throw Error('Invalid floor-plan '+label+'.');}
 if(!['Image','Vector'].includes(p.kind))throw Error('Invalid floor-plan kind.');
 for(const [key,max] of [['sourceName',260],['sourcePath',1000]])if(p[key]!==undefined&&(typeof p[key]!=='string'||p[key].length>max))throw Error('Invalid floor-plan '+key+'.');
 range(p.widthMeters,.001,10000,'width');range(p.heightMeters,.001,10000,'height');range(p.opacity,0,1,'opacity');
 range(p.x??0,-10000,10000,'X');range(p.z??0,-10000,10000,'Z');range(p.rotation??0,-3600,3600,'rotation');
 if(p.visible!==undefined&&typeof p.visible!=='boolean')throw Error('Invalid floor-plan visibility.');
 if(p.kind==='Image'){
  range(p.metersPerPixel,.000001,10,'scale');
  if(!Number.isInteger(p.pixelWidth)||!Number.isInteger(p.pixelHeight))throw Error('Invalid floor-plan pixel dimensions.');
  if(typeof p.imageBase64!=='string'||!p.imageBase64.length||p.imageBase64.length>25*1024*1024)throw Error('Invalid floor-plan image data.');
  const base64=p.imageBase64.replace(/\s/g,'');
  if(!/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(base64))throw Error('Invalid floor-plan base64.');
  const bytes=atob(base64);if(bytes.length<24||bytes.length>18*1024*1024)throw Error('Invalid floor-plan image size.');
  const byte=i=>bytes.charCodeAt(i),uint=i=>((byte(i)*16777216)+(byte(i+1)<<16)+(byte(i+2)<<8)+byte(i+3));
  let w=0,h=0;
  if(bytes.slice(0,8)==='\x89PNG\r\n\x1a\n'){
   if(uint(8)!==13||bytes.slice(12,16)!=='IHDR')throw Error('Invalid floor-plan PNG header.');w=uint(16);h=uint(20);
  }else if(byte(0)===255&&byte(1)===216){
   let at=2;
   while(at+3<bytes.length){
    if(byte(at++)!==255)throw Error('Invalid floor-plan JPEG header.');while(at<bytes.length&&byte(at)===255)at++;
    const marker=byte(at++);if(marker===217||marker===218)break;if(marker===1||(marker>=208&&marker<=215))continue;
    const length=(byte(at)<<8)|byte(at+1);if(length<2||length>bytes.length-at)throw Error('Truncated floor-plan JPEG.');
    if(marker>=192&&marker<=207&&![196,200,204].includes(marker)){if(length<8)throw Error('Invalid JPEG frame.');h=(byte(at+3)<<8)|byte(at+4);w=(byte(at+5)<<8)|byte(at+6);break;}at+=length;
   }
  }
  if(w<1||h<1||w>8192||h>8192||w*h>16*1024*1024||w!==p.pixelWidth||h!==p.pixelHeight)throw Error('Invalid/mismatched floor-plan image dimensions.');
  if(Math.abs(p.widthMeters/p.pixelWidth-p.metersPerPixel)>Math.max(.0000001,p.metersPerPixel*.00001))throw Error('Inconsistent floor-plan scale.');
  if(p.segments?.length)throw Error('Image guide cannot contain segments.');
 }else{
  if(p.imageBase64)throw Error('Vector guide cannot contain image data.');
  if(!Array.isArray(p.segments)||p.segments.length<1||p.segments.length>2000)throw Error('Invalid floor-plan segments.');
  for(const s of p.segments){
   if(!s?.start||!s?.end)throw Error('Invalid floor-plan endpoint.');
   for(const point of [s.start,s.end]){range(point.x,-p.widthMeters/2-.0001,p.widthMeters/2+.0001,'segment X');range(point.y,-p.heightMeters/2-.0001,p.heightMeters/2+.0001,'segment Y');}
   if(s.start.x===s.end.x&&s.start.y===s.end.y)throw Error('Empty floor-plan segment.');
  }
 }
 if(emptyPlanAuthoring(p.authoring))delete p.authoring;else p.authoring=validatePlanAuthoring(p.authoring,p);
 return p;
}
// Preserve source precision and unknown annotations when Unity rounded only to float32.
export function mergeFloorPlan(source,current,baseline){
 function merge(original,now,before){
  if(typeof now==='number'&&typeof before==='number'&&Math.fround(now)===Math.fround(before))return original??now;
  if(now===before)return original??now;
  if(Array.isArray(now))return now.map((v,i)=>merge(Array.isArray(original)?original[i]:undefined,v,Array.isArray(before)?before[i]:undefined));
  if(now&&typeof now==='object'){const result={...(original??{})};for(const key of Object.keys(now))result[key]=merge(original?.[key],now[key],before?.[key]);return result;}
  return now;
 }
 return merge(source,current,baseline);
}
