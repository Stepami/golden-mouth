/** Presentation-only fallbacks; never character IDs or backend catalogue entries. */
const photos: Record<string,string> = {
  'гарри поттер':'harry', 'гермиона грейнджер':'hermione', 'рон уизли':'ron', 'драко малфой':'draco',
  'дейл купер':'dale', 'одри хорн':'audrey', 'шелли джонсон':'shelly',
  'вера воронина':'vera', 'костя воронин':'kostya', 'николай петрович':'nikolai', 'кирилл':'kirill',
};
export const roleLooks:Record<string,string>={isabella:'muse',eduard:'scout',yakov:'sage',lida:'spark',diana:'spark',jon:'sage',james:'scout',greedy_wolf:'sage','2kotleti_news':'scout',alexey_nl:'muse'};
const namedLooks:Record<string,string>={'изабелла лебедева':'muse','эдуард калинин':'scout','яков чернов':'sage','лида':'spark','диана шторморождённая':'spark','джон винтер':'sage','джеймс левлистер':'scout','меркантил':'sage','бургер в декабре':'scout','куляш ai':'muse'};
const itPortraits:Record<string,string>={'меркантил':'merkantil','бургер в декабре':'burger','куляш ai':'kulyash','куляш ии':'kulyash'};
export function defaultPortrait(name: string): string | undefined {
  const key = name.trim().toLocaleLowerCase('ru');
  if(itPortraits[key])return `/assets/avatars/3d/${itPortraits[key]}-v9.png`;
  if(namedLooks[key])return characterPortrait(namedLooks[key]);
  const photo = photos[key];
  if(photo) return `/assets/avatars/${photo}.jpg`;
  if(key === 'катя миронова') return '/assets/s1-imgPhoto.png';
  if(key === 'макс лесной') return '/assets/s1-imgPhoto1.png';
  // Fictional characters without supplied portraits share the generated avatar family.
  return characterPortrait(name);
}

export const characterLooks = ['scout', 'muse', 'sage', 'spark'] as const;
export function characterPortrait(seed: string) {
  const look = characterLooks.find(value => value === seed) ?? characterLooks[Array.from(seed).reduce((sum, c) => sum + c.charCodeAt(0), 0) % characterLooks.length];
  return `/assets/avatars/3d/${look}-v8.png`;
}
/** Retire the old generated SVGs without replacing a player's uploaded photo. */
export function displayPortrait(src: string | undefined, name: string) {
  if(src==='')return undefined;
  const fixed=itPortraits[name.trim().toLocaleLowerCase('ru')];
  if(fixed&&(!src||src.startsWith('/assets/avatars/3d/')))return `/assets/avatars/3d/${fixed}-v9.png`;
  return src?.startsWith('data:image/svg+xml') ? defaultPortrait(name) : src || defaultPortrait(name);
}
