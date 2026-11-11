import {useEffect,useState} from 'react';
import type {AvatarProfile} from '../../ui/avatarProfile';
import {Button} from '../ui';
import {generatedAvatarSource,resolveGeneratedAvatar} from '../generatedAvatar';

export function AvatarGenerator({seed,style,profile,onSeed}:{seed:string;style:string;profile?:AvatarProfile;onSeed:(seed:string)=>void}){
 const source=generatedAvatarSource(seed,style,profile);
 const [image,setImage]=useState<{source:string;uri:string}>(),[error,setError]=useState(false);
 useEffect(()=>{let active=true;setError(false);void resolveGeneratedAvatar(source).then(uri=>{if(active)setImage({source,uri});}).catch(()=>{if(active)setError(true);});return()=>{active=false;};},[source]);
 return <div className="world-avatar-generator"><div className="world-avatar-preview">{image?.source===source?<img src={image.uri} alt="Предпросмотр аватара"/>:<span>{error?'Не удалось собрать аватар':'Собираем аватар…'}</span>}</div><Button type="button" variant="secondary" onClick={()=>onSeed(`generated-${crypto.randomUUID()}`)}>Другой вариант</Button></div>;
}
