import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import test from 'node:test';

const source=fs.readFileSync(new URL('../Assets/Plugins/WebGL/RoomStudio.jslib',import.meta.url),'utf8');
function fixture(){
 const responses=[],syncs=[],errors=[],inputs=[];
 const context={
  LibraryManager:{library:{}},Module:{},
  mergeInto:(target,entries)=>Object.assign(target,entries),
  UTF8ToString:value=>String(value),
  SendMessage:(receiver,method,json)=>responses.push({receiver,method,...JSON.parse(json)}),
  FS:{syncfs:(populate,callback)=>{assert.equal(populate,false);syncs.push(callback);}},
  console:{error:(...error)=>errors.push(error)},
  document:{body:{appendChild:()=>{}},createElement:()=>{
   const input={style:{},files:[],removed:false,click:()=>{},remove:()=>{input.removed=true;}};
   inputs.push(input);return input;
  }}
 };
 vm.runInNewContext(source,context);
 return {library:context.LibraryManager.library,context,responses,syncs,errors,inputs};
}

test('storage success is acknowledged only after syncfs completes',()=>{
 const state=fixture();state.library.RSSyncFiles('bridge',1);
 assert.equal(state.context.Module.roomStudioSyncing,true);assert.equal(state.responses.length,0);
 state.syncs.shift()(null);
 assert.equal(state.context.Module.roomStudioSyncing,false);
 assert.equal(state.responses[0].id,1);assert.equal(state.responses[0].ok,true);
 assert.equal(state.responses[0].receiver,'bridge');assert.equal(state.responses[0].method,'OnBrowserResponse');
});
test('writes during a sync get a separate completion after their own flush',()=>{
 const state=fixture();state.library.RSSyncFiles('bridge',1);
 state.library.RSSyncFiles('bridge',2);state.library.RSSyncFiles('bridge',3);
 assert.equal(state.syncs.length,1);state.syncs.shift()(null);
 assert.deepEqual(state.responses.map(response=>response.id),[1]);assert.equal(state.syncs.length,1);
 state.syncs.shift()(null);
 assert.deepEqual(state.responses.map(response=>response.id),[1,2,3]);assert.ok(state.responses.every(response=>response.ok));
});
test('legacy unacknowledged writes still trigger another flush',()=>{
 const state=fixture();state.library.RSSyncFiles('bridge',1);state.library.RSSyncFiles('',0);
 state.syncs.shift()(null);assert.equal(state.syncs.length,1);state.syncs.shift()(null);
 assert.equal(state.responses.length,1);assert.equal(state.context.Module.roomStudioSyncing,false);
});
test('storage failures reject the write and allow a later retry',()=>{
 const state=fixture();state.library.RSSyncFiles('bridge',1);state.syncs.shift()(new Error('Storage quota exceeded'));
 assert.equal(state.responses[0].ok,false);assert.match(state.responses[0].error,/quota/);
 assert.equal(state.context.Module.roomStudioSyncing,false);assert.equal(state.errors.length,1);
 state.library.RSSyncFiles('bridge',2);state.syncs.shift()(null);assert.equal(state.responses[1].ok,true);
});
test('synchronous filesystem failures also complete the callback',()=>{
 const state=fixture();state.context.FS.syncfs=()=>{throw new Error('Filesystem unavailable');};
 state.library.RSSyncFiles('bridge',1);
 assert.equal(state.responses[0].ok,false);assert.match(state.responses[0].error,/unavailable/);
 assert.equal(state.context.Module.roomStudioSyncing,false);
});
test('CT result limit rejects oversized files before reading',()=>{
 const state=fixture();let read=false;state.library.RSChooseFile('bridge',1,0,16*1024*1024);
 const input=state.inputs[0];input.files=[{name:'results.json',size:16*1024*1024+1,text:()=>{read=true;}}];input.onchange();
 assert.equal(read,false);assert.equal(input.removed,true);assert.equal(state.responses[0].ok,false);
 assert.match(state.responses[0].error,/16 MiB/);
});
test('JSON selection preserves Unicode and completes once',async()=>{
 const state=fixture();const json=JSON.stringify({name:'CT \u03b1',schema:'RoomStudio.CT.Results',version:1});
 state.library.RSChooseFile('bridge',1,0,16*1024*1024);
 const input=state.inputs[0];input.files=[{name:'results.json',size:Buffer.byteLength(json),text:()=>Promise.resolve(json)}];input.onchange();
 await Promise.resolve();input.oncancel();
 assert.equal(state.responses.length,1);assert.equal(state.responses[0].payload,json);assert.equal(input.removed,true);
});
test('picker cancellation completes once without a payload',()=>{
 const state=fixture();state.library.RSChooseFile('bridge',1,0,16*1024*1024);
 state.inputs[0].oncancel();state.inputs[0].oncancel();
 assert.equal(state.responses.length,1);assert.equal(state.responses[0].ok,false);
 assert.match(state.responses[0].error,/cancelled/);assert.equal(state.responses[0].payload,undefined);
});