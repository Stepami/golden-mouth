import {readLocal,saveLocal} from './backend';
export type AccountIdentity={name:string;handle:string};

/** A display nickname, not a server-reserved unique account address. */
function gameNickname(playerId:string){
 let hash=2166136261;
 for(const char of playerId){hash^=char.charCodeAt(0);hash=Math.imul(hash,16777619);}
 const value=hash>>>0;
 const adjectives=['tihiy','dikiy','bystriy','ryzhiy','vesyoliy','sonny','lovkiy','smelyy'];
 const animals=['kot','lis','volk','sova','yenot','zhuk','los','kit'];
 return `${adjectives[value%8]}_${animals[(value>>>3)%8]}_${value.toString(36).padStart(4,'0').slice(-4)}`;
}
export function accountIdentity(playerId:string,firstWorldName:string):AccountIdentity{
 const key=`reki:v7:${playerId}:account-identity`;
 const saved=readLocal<AccountIdentity|undefined>(key,undefined);
 if(saved&&!/^reki_[0-9a-f]{32}$/i.test(saved.handle))return saved;
 const identity={name:saved?.name??firstWorldName,handle:gameNickname(playerId)};
 saveLocal(key,identity);
 return identity;
}
