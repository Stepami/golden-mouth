/** Never clear local presentation before the server confirms account deletion. */
export async function deleteConfirmedAccount(owner:string,removeRemote:()=>Promise<void>,clearPresentation:(owner:string)=>void,storages:Storage[]){
 await removeRemote();
 clearPresentation(owner);
 for(const storage of storages){try{const keys=Array.from({length:storage.length},(_,i)=>storage.key(i)).filter((key):key is string=>!!key&&key.startsWith(`reki:v7:${owner}:`));for(const key of keys)storage.removeItem(key);}catch{/* A successful remote deletion must not become a retryable DELETE. */}}
}
