import assert from 'node:assert/strict'
import fs from 'node:fs'
import vm from 'node:vm'
import { stripTypeScriptTypes } from 'node:module'
let calls=[], fail=true, links=0
class EntryTelemetryClient {
 async submitLandingEntries(body) { calls.push(structuredClone(body)); if(fail) throw {status:503} }
 async linkLandingProfile() { links++ }
}
const storage=new Map()
const source=fs.readFileSync(new URL('../src/app/landing/openobserve.ts', import.meta.url),'utf8').replace(/^import .*\n/gm,'').replaceAll('import.meta.env.PROD','true')
const js=stripTypeScriptTypes(source).replace(/export /g,'')+'\nObject.assign(exports,{recordEntryStep,flushEntryEvents,linkEntryProfile,resetEntryProfileLink})'
const context={exports:{},apiBaseUrl:'https://example.test',window:{location:{origin:'https://example.test'}},URL,EntryTelemetryClient,transport:()=>({}),isLandingPreview:()=>false,landingContext:()=>({landing_variant:'worlds',landing_initial_variant:'worlds',landing_source:'link',landing_switched:'false'}),landingVisitIdentity:()=>({visitorId:'v',entryId:'e'}),localStorage:{getItem:k=>storage.get(k),setItem:(k,v)=>storage.set(k,v)},crypto:globalThis.crypto,structuredClone,Set,Date,Promise}
vm.runInNewContext(js,context)
const api=context.exports
api.recordEntryStep('landing_view')
await api.flushEntryEvents()
assert.equal(calls.length,1)
const id=calls[0].events[0].eventId
fail=false
await api.flushEntryEvents()
assert.equal(calls[1].events[0].eventId,id)
await api.flushEntryEvents()
assert.equal(calls.length,2)
await api.linkEntryProfile(); await api.linkEntryProfile()
assert.equal(links,1)
api.resetEntryProfileLink(); await api.linkEntryProfile()
assert.equal(links,2)
console.log('PASS: anonymous delivery, stable retry ID, queue acknowledgement, authenticated link dedup/reset')

// Exercise the actual assignment used by all five landing URLs, including OAuth state.
const experiment=fs.readFileSync(new URL('../src/app/landing/experiment.ts',import.meta.url),'utf8')
  .replaceAll('import.meta.env?.DEV','false')
const experimentJs=stripTypeScriptTypes(experiment).replace(/export /g,'')+
  '\nObject.assign(exports,{assignLanding,landingContext,landingVisitIdentity,beginLandingAuth,completeLandingAuth,pendingLandingWorld})'
for(const variant of ['worlds','hogwarts','infobiz','evening','houses']) {
 const values=new Map(),location={search:'?landing='+variant+'&utm_campaign=launch'}
 const store={getItem:k=>values.get(k)??null,setItem:(k,v)=>values.set(k,v),removeItem:k=>values.delete(k)}
 const sandbox={exports:{},location,URLSearchParams,crypto:globalThis.crypto,localStorage:store,sessionStorage:store}
 vm.runInNewContext(experimentJs,sandbox)
 const exp=sandbox.exports
 assert.equal(exp.assignLanding(),variant)
 const identity=exp.landingVisitIdentity()
 exp.beginLandingAuth('selected-template')
 assert.equal(exp.pendingLandingWorld(),'selected-template')
 assert.equal(exp.completeLandingAuth(),true)
 assert.equal(exp.landingContext().landing_variant,variant)
 assert.equal(exp.landingContext().utm_campaign,'launch')
 assert.equal(exp.landingVisitIdentity().entryId,identity.entryId)
 location.search='?landing='+(variant==='worlds'?'hogwarts':'worlds')
 exp.assignLanding()
 assert.equal(exp.landingContext().landing_initial_variant,variant)
 assert.equal(exp.landingContext().landing_switched,'true')
}
console.log('PASS: five landing variants, OAuth attribution, template persistence, campaign and switching')
