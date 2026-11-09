import {readLocal,saveLocal} from './backend';
export type AccountIdentity={name:string;handle:string};
export function accountIdentity(playerId:string,firstWorldName:string):AccountIdentity{
 const key=`reki:v7:${playerId}:account-identity`;
 const saved=readLocal<AccountIdentity|undefined>(key,undefined);
 if(saved)return saved;
 // The complete unique server account ID namespaces the handle; never truncate it.
 const identity={name:firstWorldName,handle:`reki_${playerId.replaceAll('-','')}`};
 saveLocal(key,identity);
 return identity;
}
