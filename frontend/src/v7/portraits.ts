/** Presentation-only fallbacks; never character IDs or backend catalogue entries. */
const photos: Record<string,string> = {
  'гарри поттер':'harry', 'гермиона грейнджер':'hermione', 'рон уизли':'ron', 'драко малфой':'draco',
  'дейл купер':'dale', 'одри хорн':'audrey', 'шелли джонсон':'shelly',
};
export async function defaultPortrait(name: string): Promise<string> {
  const photo = photos[name.trim().toLocaleLowerCase('ru')];
  if(photo) return `/assets/avatars/${photo}.jpg`;
  const { avatarUri } = await import('../ui/avatarRenderer');
  return avatarUri(name.trim() || 'character', undefined, undefined, 'lorelei');
}
