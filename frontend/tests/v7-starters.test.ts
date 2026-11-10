import test from 'node:test';
import assert from 'node:assert/strict';
import {starterFor} from '../src/v7/storyplay/starters.ts';
test('legacy school has a complete editable opening, ten XP and the original lesson premise',()=>{
 const s=starterFor('aec9494b-4118-4525-8458-e85d759c8dd9',{title:'Школа чародеев',topic:'Урок'});
 assert.equal(s.xp,10);assert.ok(s.draft.length>100&&s.draft.length<2000);assert.match(s.draft,/урок/);assert.doesNotMatch(s.draft,/100 000|галлеон/);assert.notEqual(s.draft,s.prompt);
});
test('custom worlds use their own context instead of guessing a preset from title',()=>{
 const s=starterFor(undefined,{title:'Школа чародеев',topic:'Экспедиция на Марс'});assert.match(s.draft,/Марс/);assert.doesNotMatch(s.draft,/отменённый сто лет/);assert.ok(s.draft.trim());assert.equal(s.xp,10);
});
