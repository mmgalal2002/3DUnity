import assert from 'node:assert/strict';
import fs from 'node:fs';
import {fromDesign,run,execute} from './engine.mjs';
import {importWorkspace} from './workspace.mjs';

const fixture=JSON.parse(fs.readFileSync(new URL('import-fixture.json',import.meta.url)));
const design=importWorkspace(fixture).design;
const geometry=fromDesign(design),report=run(design).rows;
design.editing={moveStep:.001,resizeStep:.017,thicknessStepMm:.1,scaleStep:.01,gridSnap:true,wallSnap:true,gridVisible:false,gridSpacing:.125,wallSnapTolerance:.02,lengthAnchor:'End'};
assert.deepEqual(fromDesign(design),geometry,'editor preferences changed shielding geometry');
assert.deepEqual(run(design).rows,report,'editor preferences changed reference QA');
assert.deepEqual(JSON.parse(execute(design,'export').json),fixture,'preferences rewrote precise canonical source fields');
console.log('ROOM_STUDIO_PRECISION_BRIDGE_PASSED: native editing preferences leave geometry, reference QA and canonical source export unchanged');
