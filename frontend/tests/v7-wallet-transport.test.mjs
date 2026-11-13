import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'vite';
const storage=()=>{const m=new Map();return {getItem:k=>m.get(k)??null,setItem:(k,v)=>m.set(k,v),removeItem:k=>m.delete(k)};};
const previous={document:globalThis.document,window:globalThis.window,location:globalThis.location,localStorage:globalThis.localStorage,sessionStorage:globalThis.sessionStorage,fetch:globalThis.fetch};
globalThis.window={__REKI_CONFIG__:{apiBaseUrl:'/api/v1'},location:{origin:'http://wallet-fixture.invalid',hash:''}};
globalThis.document={visibilityState:'hidden'};
globalThis.location=globalThis.window.location;globalThis.localStorage=storage();globalThis.sessionStorage=storage();
const calls=[];globalThis.fetch=async(url)=>{calls.push(String(url));return String(url).includes('/auth/external/tg/')?new Response(JSON.stringify({access_token:'fake',refresh_token:'fake'}),{status:200,headers:{'Content-Type':'application/json'}}):new Response('{}',{status:401,headers:{'Content-Type':'application/json'}});};
const server=await createServer({configFile:false,server:{middlewareMode:true},appType:'custom'});
try{
 const auth=await server.ssrLoadModule('/src/api/auth.ts');const {transport}=await server.ssrLoadModule('/src/api/transport.ts');
 await test('exchange HTTP transport never replays POST or refreshes credentials after401',async()=>{auth.setInitData('controlled-fixture');await auth.loginMiniApp();await assert.rejects(transport().fetch('http://wallet-fixture.invalid/api/v1/me/energy/exchange',{method:'POST'}),e=>e.status===401);assert.equal(calls.filter(x=>x.endsWith('/me/energy/exchange')).length,1);assert.equal(calls.filter(x=>x.includes('/auth/refresh')).length,0);});
}finally{await server.close();Object.assign(globalThis,previous);}
