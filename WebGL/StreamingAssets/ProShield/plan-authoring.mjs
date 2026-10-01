// Optional editor metadata under room.floorPlan; never shielding geometry.
export const emptyPlanAuthoring=p=>p==null||(p.version===0&&!p.id&&!p.sourceFingerprint&&!p.rules?.length&&!p.calibration?.confirmed);
export function validatePlanAuthoring(p,guide){
 if(emptyPlanAuthoring(p))return null;
 const fail=label=>{throw Error('Invalid plan authoring '+label+'.');};
 const range=(v,min,max,label)=>{if(typeof v!=='number'||!Number.isFinite(v)||v<min||v>max)fail(label);};
 const text=(v,max,label)=>{if(typeof v!=='string'||!v.trim()||v.length>max)fail(label);};
 if(p.version!==1||guide.kind!=='Image')fail('version/image');
 text(p.id,128,'ID');if(!/^[0-9a-f]{64}$/.test(p.sourceFingerprint))fail('source fingerprint');
 if(p.pixelWidth!==guide.pixelWidth||p.pixelHeight!==guide.pixelHeight)fail('source dimensions');
 range(p.alphaThreshold,0,1,'alpha threshold');if(!Array.isArray(p.rules)||p.rules.length>32)fail('rule count');
 const ids=new Set();
 for(const r of p.rules){
  if(!r)fail('missing rule');text(r.id,128,'rule ID');text(r.name,100,'rule name');if(ids.has(r.id))fail('duplicate rule ID');ids.add(r.id);
  if(typeof r.enabled!=='boolean'||!['Wall','Opening','Reference','Ignore'].includes(r.classification)||!['RGB','HSV'].includes(r.colorSpace))fail('classification/color space');
  if(!Number.isInteger(r.priority)||r.priority<0||r.priority>9999||!Number.isInteger(r.minAreaPixels)||r.minAreaPixels<1||r.minAreaPixels>65536)fail('priority/area');
  for(const color of [r.target,r.display]){if(!color)fail('color');for(const ch of ['r','g','b','a'])range(color[ch],0,1,'color channel');}
  range(r.tolerance,0,1,'tolerance');range(r.minLengthPixels,1,8192,'minimum length');range(r.height,.001,100,'height');range(r.baseElevation,-100,100,'base elevation');range(r.thicknessMm,.01,3000,'thickness');
  // Older saved rules predate an explicit density field; Unity supplies the
  // legacy material default when one is absent.
  if(r.densityKgM3!==undefined)range(r.densityKgM3,100,25000,'density');
    if(r.material!==null&&r.material!==''&&!['Concrete','Steel','Lead','Gypsum','Glass','PlateGlass','Wood'].includes(r.material))fail('physical material');
 }
 const c=p.calibration;if(!c||typeof c.confirmed!=='boolean'||!['Manual','TwoPoint'].includes(c.method)||!['m/px','mm/px','px/m'].includes(c.unit))fail('calibration');
 range(c.value,.000001,1000000,'calibration value');range(c.metresPerPixel,.000001,10,'calibration scale');range(c.distanceMetres,.000001,10000,'calibration distance');
 for(const point of [c.a,c.b]){if(!point)fail('calibration point');range(point.x,0,p.pixelWidth,'pixel X');range(point.y,0,p.pixelHeight,'pixel Y');}
 if(c.confirmed){
  const px=Math.hypot(c.b.x-c.a.x,c.b.y-c.a.y);
  const expected=c.method==='TwoPoint'?c.distanceMetres/px:c.unit==='m/px'?c.value:c.unit==='mm/px'?c.value/1000:1/c.value;
  range(expected,.000001,10,'confirmed scale');if(c.method==='TwoPoint'&&px<.000001)fail('coincident points');
  if(Math.abs(expected-c.metresPerPixel)>Math.max(.00000001,c.metresPerPixel*.00001))fail('inconsistent calibration');
 }
 return structuredClone(p);
}
