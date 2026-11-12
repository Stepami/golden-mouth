import {useSyncExternalStore} from 'react'

type InstallEvent = Event & {prompt:()=>Promise<{outcome:'accepted'|'dismissed'}>;userChoice:Promise<{outcome:'accepted'|'dismissed'}>}
const dismissedKey='reki:install:dismissed:v1'
const read=(key:string)=>{try{return localStorage.getItem(key)==='1'}catch{return false}}
const display=matchMedia('(display-mode: standalone)')
let state={prompt:null as InstallEvent|null,installed:display.matches||!!(navigator as Navigator&{standalone?:boolean}).standalone,dismissed:read(dismissedKey)}
const listeners=new Set<()=>void>()
const change=(patch:Partial<typeof state>)=>{state={...state,...patch};listeners.forEach(fn=>fn())}
const remember=(key:string)=>{try{localStorage.setItem(key,'1')}catch{/* Keep the choice for this page session. */}}
window.addEventListener('beforeinstallprompt',event=>{event.preventDefault();change({prompt:event as InstallEvent})})
window.addEventListener('appinstalled',()=>{remember(dismissedKey);change({installed:true,prompt:null,dismissed:true})})
display.addEventListener('change',()=>change({installed:display.matches||!!(navigator as Navigator&{standalone?:boolean}).standalone}))
window.addEventListener('storage',event=>{if(event.key===dismissedKey)change({dismissed:read(dismissedKey)})})
export function dismissInstall(){remember(dismissedKey);change({dismissed:true})}
export function useInstall(){return useSyncExternalStore(fn=>{listeners.add(fn);return()=>{listeners.delete(fn)}},()=>state)}
export async function requestInstall(){
 const event=state.prompt
 if(!event)return null
 change({prompt:null})
 await event.prompt()
 return (await event.userChoice).outcome
}
