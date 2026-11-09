import {GameArt} from './GameArt';
/** Illustrative rewards use dimensional art; navigation keeps its compact glyphs. */
export function RewardArt({kind,className=''}:{kind:'energy'|'crystal'|'followers'|'flame'|'gift'|'booster';className?:string}) {
 return <GameArt kind={kind} className={`reward-art ${className}`}/>;
}
