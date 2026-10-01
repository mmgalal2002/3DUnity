import {execute} from './engine.mjs';
self.onmessage=({data})=>{
 try{self.postMessage({ok:true,payload:JSON.stringify(execute(JSON.parse(data.json),data.mode))});}
 catch(error){self.postMessage({ok:false,error:error.message||String(error)});}
};
