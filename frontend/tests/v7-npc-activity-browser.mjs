// Run from frontend/: node tests/v7-npc-activity-browser.mjs
import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import fs from 'node:fs/promises';
import {createServer} from 'vite';
import react from '@vitejs/plugin-react';
const delay=ms=>new Promise(r=>setTimeout(r,ms));
const html='<html><head><meta name="viewport" content="width=device-width,initial-scale=1"></head><body><div id="root"></div><script>window.__REKI_CONFIG__={apiBaseUrl:"/api/v1"}</script><script type="module" src="/tests/fixtures/npc-activity.tsx"></script></body></html>';
const server=await createServer({configFile:false,root:process.env.REKI_NPC_QA_ROOT??process.cwd(),cacheDir:'/private/tmp/reki-npc-vite-cache',plugins:[react(),{name:'npc-qa',configureServer(s){s.middlewares.use('/__npc-qa',async(req,res)=>{res.setHeader('Content-Type','text/html');res.end(await s.transformIndexHtml('/__npc-qa',html));});}}],server:{host:'127.0.0.1',port:0}});
await server.listen();
const url=server.resolvedUrls.local[0]+'__npc-qa';
const profile=await fs.mkdtemp('/private/tmp/reki-npc-browser-');
const proc=spawn(process.env.CHROME_BIN??'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',['--headless=new','--disable-gpu','--no-first-run','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:'ignore'});
let socket;
try{
 let port;for(let i=0;i<80;i++){try{port=(await fs.readFile(profile+'/DevToolsActivePort','utf8')).split('\n')[0];break;}catch{await delay(100);}}assert.ok(port,'Chrome started');
 const target=await(await fetch('http://127.0.0.1:'+port+'/json/new?about:blank',{method:'PUT'})).json();
 socket=new WebSocket(target.webSocketDebuggerUrl);await new Promise((r,j)=>{socket.onopen=r;socket.onerror=j;});
 let id=0;const waiting=new Map();socket.onmessage=e=>{const v=JSON.parse(e.data);if(v.method==='Runtime.exceptionThrown')console.error(JSON.stringify(v.params));if(v.id){const q=waiting.get(v.id);waiting.delete(v.id);v.error?q.reject(Error(JSON.stringify(v.error))):q.resolve(v.result);}};
 const rpc=(method,params={})=>new Promise((resolve,reject)=>{const n=++id;waiting.set(n,{resolve,reject});socket.send(JSON.stringify({id:n,method,params}));});
 const evaluate=async expression=>{const result=await rpc('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(result.exceptionDetails)throw Error(JSON.stringify(result.exceptionDetails));return result.result.value;};
 const until=async (expr,attempts=100)=>{for(let i=0;i<attempts;i++){if(await evaluate(expr))return;await delay(100);}throw Error('Timed out: '+expr+' DOM: '+await evaluate('document.body.innerText'));};
 const click=async text=>{assert.ok(await evaluate(`(()=>{const b=[...document.querySelectorAll('button')].find(b=>(b.textContent+' '+b.getAttribute('aria-label')).includes(${JSON.stringify(text)}));if(!b)return false;b.click();return true;})()`),text);await delay(100);};
 await rpc('Runtime.enable');
 const inbox="JSON.parse(localStorage.getItem('reki:v7:qa-npc-inbox:activity')||'[]')";
 await rpc('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});
 await rpc('Page.navigate',{url});await until("document.body?.textContent.includes('QA-мир')");await click('QA-мир');
 await until(`${inbox}.some(a=>a.kind==='npc-post')`);
 assert.equal(await evaluate(`${inbox}.filter(a=>a.kind==='npc-post').length`),1,'own post excluded');
 await click('Уведомления');await until("!!document.querySelector('.activity-open')");
 assert.ok(await evaluate("document.querySelector('.activity-open').textContent.includes('новая публикация')"));
 // A failed destination must remain unread.
 await evaluate('qa.failOpen=true');await click('новая публикация');await until("document.body?.textContent.includes('Не получилось')");
 assert.equal(await evaluate(`${inbox}.find(a=>a.kind==='npc-post').read`),false);
 await click('Назад');await evaluate('qa.failOpen=false');await click('новая публикация');
 await until("document.body?.textContent.includes('Полный текст npc-1')");
 assert.equal(await evaluate(`${inbox}.find(a=>a.kind==='npc-post').read`),true);
 await click('Назад');
 // Focus polling finds a new asynchronous post, while preserving the read one.
 await evaluate("qa.add('npc-2');qa.resume()");await until(`${inbox}.filter(a=>a.kind==='npc-post').length===2`);
 assert.equal(await evaluate(`${inbox}.find(a=>a.postId==='npc-1').read`),true);
 await evaluate('qa.resume()');await delay(300);
 assert.equal(await evaluate(`${inbox}.filter(a=>a.kind==='npc-post').length`),2,'no duplicates');
 // Failure is retried rather than recorded as seen.
 await evaluate("qa.failDetail=true;qa.add('npc-3');qa.resume()");await delay(300);
 assert.equal(await evaluate(`${inbox}.filter(a=>a.kind==='npc-post').length`),2);
 await evaluate('qa.failDetail=false;qa.resume()');await until(`${inbox}.filter(a=>a.kind==='npc-post').length===3`);
 await evaluate("qa.add('npc-4')");await until(`${inbox}.filter(a=>a.kind==='npc-post').length===4`,350);
 for(const width of [320,390,430,1280]){
  await rpc('Emulation.setDeviceMetricsOverride',{width,height:844,deviceScaleFactor:1,mobile:false});
  for(const scale of [1,2]){
   await evaluate(`document.documentElement.style.setProperty('--text-scale','${scale}');document.documentElement.dataset.largeText='${scale>1}'`);await delay(100);
   assert.equal(await evaluate('document.documentElement.scrollWidth>innerWidth'),false,`overflow ${width}/${scale}`);
  }
 }
 await evaluate("document.documentElement.style.setProperty('--text-scale','1')");
 await rpc('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});
 const shot=await rpc('Page.captureScreenshot',{format:'png'});await fs.writeFile('/private/tmp/reki-npc-inbox.png',Buffer.from(shot.data,'base64'));
 await rpc('Page.reload');await until("document.body?.textContent.includes('QA-мир')");await click('QA-мир');await delay(500);
 assert.equal(await evaluate(`${inbox}.filter(a=>a.kind==='npc-post').length`),4);
 assert.equal(await evaluate(`${inbox}.find(a=>a.postId==='npc-1').read`),true,'read state persists');
 await click('Написать');await until("!!document.querySelector('textarea')");
 await evaluate(`(()=>{const t=document.querySelector('textarea');Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype,'value').set.call(t,'QA draft intact');t.dispatchEvent(new Event('input',{bubbles:true}));})()`);
 await evaluate("qa.add('npc-5');qa.resume()");await until(`${inbox}.filter(a=>a.kind==='npc-post').length===5`);
 assert.equal(await evaluate("document.querySelector('textarea')?.value"),'QA draft intact','polling preserves editor/draft');
 console.log('PASS: real BackendApp inbox, exact destination, failed-open unread, focus and 30s timer delivery, retry, dedup, reload, draft preservation, 4 widths × 2 text sizes');
}finally{socket?.close();proc.kill();await server.close();await delay(300);await fs.rm(profile,{recursive:true,force:true,maxRetries:5,retryDelay:100});}
