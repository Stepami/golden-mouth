import type {AvatarProfile} from '../ui/avatarProfile';
const prefix='reki-avatar:';
export function generatedAvatarSource(seed:string,style:string,profile?:AvatarProfile){
 return prefix+JSON.stringify({seed,style,profile});
}
export const isGeneratedAvatar=(src:string|undefined)=>!!src?.startsWith(prefix);
export async function resolveGeneratedAvatar(src:string){
 const {seed,style,profile}=JSON.parse(src.slice(prefix.length));
 const {avatarUri}=await import('../ui/avatarRenderer');
 return avatarUri(seed,undefined,profile,style);
}
