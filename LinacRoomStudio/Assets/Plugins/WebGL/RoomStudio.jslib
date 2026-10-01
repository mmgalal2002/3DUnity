mergeInto(LibraryManager.library, {
 RSExecute__deps: ['$SendMessage'],
 RSExecute: function(receiverPtr,id,pathPtr,jsonPtr,modePtr) {
  var receiver=UTF8ToString(receiverPtr),path=UTF8ToString(pathPtr),json=UTF8ToString(jsonPtr),mode=UTF8ToString(modePtr);
  var worker=null,timer=null,finished=false;
  function finish(result){
   if(finished)return;finished=true;clearTimeout(timer);if(worker)worker.terminate();
   result.id=id;SendMessage(receiver,'OnBrowserResponse',JSON.stringify(result));
  }
  try{
   worker=new Worker(new URL(path.replace(/\/$/,'')+'/browser-worker.mjs',document.baseURI),{type:'module'});
   timer=setTimeout(function(){finish({ok:false,error:'Calculation timed out. Please try again.'});},120000);
   worker.onmessage=function(event){finish(event.data);};
   worker.onerror=function(event){event.preventDefault();finish({ok:false,error:event.message||'Could not load the calculation engine.'});};
   worker.postMessage({json:json,mode:mode});
  }catch(error){finish({ok:false,error:error.message});}
 },
 RSChooseFile__deps: ['$SendMessage'],
 RSChooseFile: function(receiverPtr,id,floorPlan,maxJsonBytes){
  var receiver=UTF8ToString(receiverPtr),input=document.createElement('input');
  input.type='file';input.accept=floorPlan?'.json,application/json,image/png,image/jpeg,.png,.jpg,.jpeg':'.json,application/json';input.style.display='none';document.body.appendChild(input);
  var finished=false;
  function finish(result){if(finished)return;finished=true;input.remove();result.id=id;SendMessage(receiver,'OnBrowserResponse',JSON.stringify(result));}
  input.oncancel=function(){finish({ok:false,error:'File selection cancelled.'});};
  input.onchange=function(){
   var file=input.files[0];if(!file){finish({ok:false,error:'No file selected.'});return;}
   var isImage=floorPlan&&((file.type||'').indexOf('image/')===0||/\.(png|jpe?g)$/i.test(file.name));
    var limit=isImage?18*1024*1024:Math.min(maxJsonBytes||25*1024*1024,25*1024*1024);
    if(file.size===0||file.size>limit){finish({ok:false,error:'Choose a non-empty file no larger than '+(limit/1024/1024)+' MiB.'});return;}
   function loaded(text){finish({ok:true,payload:floorPlan?JSON.stringify({name:file.name,payload:text}):text});}
   if(isImage){var reader=new FileReader();reader.onload=function(){loaded(reader.result);};reader.onerror=function(){finish({ok:false,error:'Could not read the selected image.'});};reader.readAsDataURL(file);}
   else file.text().then(loaded,function(error){finish({ok:false,error:error.message});});
  };
 input.click();
 },
 RSShowCopyableHelp: function(textPtr){
  var text=UTF8ToString(textPtr),existing=document.getElementById('room-studio-copy-help');if(existing)existing.remove();
  // A same-origin frame isolates native copy/selection events from Unity's canvas input.
  // It needs no clipboard permission or additional managed engine modules.
  var overlay=document.createElement('iframe');overlay.id='room-studio-copy-help';overlay.title='Copy keyboard help';
  overlay.style.cssText='position:fixed;left:5%;top:5%;width:90%;height:90%;z-index:10000;border:1px solid #4cbfb5';document.body.appendChild(overlay);
  var doc=overlay.contentDocument;doc.body.style.cssText='box-sizing:border-box;margin:0;height:100vh;background:#142130;color:#eef6ff;padding:24px;display:flex;flex-direction:column;gap:12px;font:16px system-ui';
  var label=doc.createElement('label');label.textContent='Select text and press Ctrl+C to copy. Escape, F1 or Close returns to the app.';
  var area=doc.createElement('textarea');area.readOnly=true;area.value=text;area.setAttribute('aria-label','Keyboard help text');area.style.cssText='flex:1;min-height:100px;background:#0b1521;color:#eef6ff;font:16px/1.6 system-ui;padding:16px';
  var close=doc.createElement('button');close.textContent='Close copyable help';close.style.cssText='padding:10px;font:16px system-ui';
  function dismiss(){overlay.remove();Module.canvas.focus();}
  close.onclick=dismiss;doc.addEventListener('keydown',function(event){if(event.key==='Escape'||event.key==='F1'){event.preventDefault();dismiss();}});
  doc.body.appendChild(label);doc.body.appendChild(area);doc.body.appendChild(close);area.focus();area.select();
 },
 RSCTDiagnostics: function(jsonPtr){
  if(!/[?&]ct-(tests|demo)=1(?:&|$)/.test(location.search))return;
  var state=JSON.parse(UTF8ToString(jsonPtr));state.filesSyncing=!!Module.roomStudioSyncing;window.roomStudioCtDiagnostics=state;
 },
 RSDownload: function(namePtr,textPtr){
  var blob=new Blob([UTF8ToString(textPtr)],{type:'text/plain;charset=utf-8'}),url=URL.createObjectURL(blob),link=document.createElement('a');
  link.href=url;link.download=UTF8ToString(namePtr);document.body.appendChild(link);link.click();link.remove();setTimeout(function(){URL.revokeObjectURL(url);},60000);
 },
 RSSyncFiles__deps: ['$SendMessage'],
 RSSyncFiles: function(receiverPtr,id){
  var receiver=UTF8ToString(receiverPtr);
  if(!Module.roomStudioSyncRequests)Module.roomStudioSyncRequests=[];
  if(id)Module.roomStudioSyncRequests.push({receiver:receiver,id:id});
  if(Module.roomStudioSyncing){Module.roomStudioSyncAgain=true;return;}
  function sync(){
   var requests=Module.roomStudioSyncRequests;Module.roomStudioSyncRequests=[];
   Module.roomStudioSyncing=true;Module.roomStudioSyncAgain=false;
   function finished(error){
    Module.roomStudioSyncing=false;if(error)console.error('Could not persist local designs:',error);
    requests.forEach(function(request){SendMessage(request.receiver,'OnBrowserResponse',JSON.stringify({id:request.id,ok:!error,payload:error?null:'Browser storage synchronized.',error:error?(error.message||String(error)):null}));});
    if(Module.roomStudioSyncAgain||Module.roomStudioSyncRequests.length)sync();
   }
   try{FS.syncfs(false,finished);}catch(error){finished(error);}
  }
  sync();
 }
});
