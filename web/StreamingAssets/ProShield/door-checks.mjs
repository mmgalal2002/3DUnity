import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {importWorkspace} from './workspace.mjs';
import {execute} from './engine.mjs';

const fixture=JSON.parse(readFileSync(fileURLToPath(new URL('./import-fixture.json',import.meta.url)),'utf8'));
const design=importWorkspace(fixture).design;
const wall=design.items.find(item=>item.kind==='Wall');
assert.ok(wall,'fixture must contain a wall');
wall.doors=[{id:'door-qa-guard',name:'Entry',center:0,width:1,height:2.1,panel:{material:'Steel',thickness:45,density:7850},leadLiningMm:3}];
const saved=JSON.parse(JSON.stringify(design));
for(const mode of ['qa','export']){
 assert.throws(()=>execute(saved,mode),error=>/Door openings are not supported/.test(error.message)&&/native design/.test(error.message),mode+' silently ignored a door');
}
assert.equal(saved.items.find(item=>item.id===wall.id).doors[0].leadLiningMm,3,'guard modified native door');
console.log('ROOM_STUDIO_DOOR_REFERENCE_GUARD_PASSED: QA and canonical export reject apertures; native source retained');
