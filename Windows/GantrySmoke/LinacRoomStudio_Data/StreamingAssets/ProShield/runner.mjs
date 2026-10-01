import fs from 'node:fs';
import {pathToFileURL} from 'node:url';
import {execute} from './engine.mjs';
export {run,fromDesign,execute} from './engine.mjs';
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
 try{const input=JSON.parse(fs.readFileSync(process.argv[2],'utf8'));fs.writeFileSync(process.argv[3],JSON.stringify(execute(input,process.argv[4]??'qa'),null,2));}catch(e){console.error(e.stack);process.exitCode=1;}
}
