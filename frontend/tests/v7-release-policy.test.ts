import test from 'node:test';
import assert from 'node:assert/strict';
import {receiptForRelease} from '../src/v7/releasePolicy.ts';
import {deleteConfirmedAccount} from '../src/v7/accountDeletion.ts';
import type {PublicationReceipt} from '../src/v7/publication/PublicationCards';
const receipt={worldId:'world',result:{postId:'post',delta:3},questReward:{amount:10,title:'quest',questId:'q'},questOutcome:{source:'local',status:'success',title:'quest',reason:'local',xp:10}} as PublicationReceipt;
test('production removes cached preview grants without discarding the real publication',()=>{const value=receiptForRelease(receipt,false)!;assert.equal(value.result,receipt.result);assert.equal(value.questReward,undefined);assert.equal(value.questOutcome,undefined);assert.equal(receiptForRelease(receipt,true),receipt);});
test('production preserves an explicit server grade and removes the local grant override',()=>{const value=receiptForRelease({...receipt,questOutcome:{...receipt.questOutcome!,source:'server'}},false)!;assert.equal(value.questOutcome?.source,'server');assert.equal(value.questReward,undefined);});
function storage(){const m=new Map([['reki:v7:a:persona:w','mine'],['reki:v7:a:result:w','mine'],['reki:v7:ab:result:w','other'],['other-setting','keep']]);return {m,length:m.size,key:(i:number)=>[...m.keys()][i]??null,removeItem:(k:string)=>m.delete(k)} as unknown as Storage&{m:Map<string,string>};}
test('failed account deletion retains all cached data and never calls presentation cleanup',async()=>{const s=storage();let cleared=false;await assert.rejects(deleteConfirmedAccount('a',async()=>{throw Error('network');},()=>{cleared=true;},[s]));assert.equal(cleared,false);assert.equal(s.m.size,4);});
test('confirmed deletion cleans only the deleted account and follows the remote acknowledgement',async()=>{const s=storage(),order:string[]=[];await deleteConfirmedAccount('a',async()=>{order.push('remote');},()=>{order.push('presentation');},[s]);assert.deepEqual(order,['remote','presentation']);assert.equal(s.m.has('reki:v7:a:persona:w'),false);assert.equal(s.m.get('reki:v7:ab:result:w'),'other');assert.equal(s.m.get('other-setting'),'keep');});
