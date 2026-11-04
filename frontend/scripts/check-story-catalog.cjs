// Run after compiling src/v7/ui/index.tsx and src/v7/catalog.ts to CommonJS.
const assert = require('node:assert/strict');
const path = require('node:path');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const fs = require('node:fs');
const os = require('node:os');
async function main() {
const out = fs.mkdtempSync(path.join(os.tmpdir(), 'reki-story-catalog-'));
fs.symlinkSync(path.resolve(__dirname, '../node_modules'), path.join(out, 'node_modules'), 'dir');
const { build } = await import('vite');
const { default: react } = await import('@vitejs/plugin-react');
try {
for (const [entry, output] of [['src/v7/ui/index.tsx', 'ui/index.js'], ['src/v7/catalog.ts', 'catalog.js']]) {
  await build({ configFile: false, plugins: [react()], logLevel: 'error', build: {
    ssr: entry, outDir: out, emptyOutDir: false,
    rollupOptions: { output: { format: 'cjs', entryFileNames: output } },
  } });
}
const { WorldCard } = require(path.join(out, 'ui/index.js'));
const { catalogEntries, filterCatalog } = require(path.join(out, 'catalog.js'));
const world = (id, title, genre = 'Фэнтези') => ({ id, title, genre, cover: 'cover.webp', hero: 'cover.webp', members: '2 341', tagline: '', description: 'Описание мира', goal: '', role: '', playerName: '', cast: [], quests: [], seedPost: '', topic: '' });
const real = [world('1', 'Вечерело'), world('2', 'Школа чародеев'), world('3', 'Игра домов'), world('4', 'Айти-тусовка')];
const planned = [world('hogwarts', 'Harry Potter'), world('twin-peaks', 'Twin Peaks'), world('gorpcore', 'Gorpcore'), world('voroniny', 'Воронины')];
const entries = catalogEntries(real, planned);
assert.equal(entries.length, 8);
assert.equal(entries.filter(x => x.available).length, 4);
assert.equal(entries.filter(x => !x.available).length, 4);
assert.equal(new Set(entries.map(x => x.key)).size, 8);
assert.equal(filterCatalog(entries, ' TWIN ', 'Все')[0].available, false);
assert.equal(filterCatalog(entries, 'школа', 'Все')[0].available, true);
assert.equal(filterCatalog(catalogEntries([], planned), '', 'Все').some(x => x.available), false);
const noAction = WorldCard({ world: planned[0], disabled: true, onClick: () => { throw Error('Must not be attached'); } });
assert.equal(noAction.props.disabled, true);
assert.equal(noAction.props.onClick, undefined);
const disabled = renderToStaticMarkup(noAction);
assert.match(disabled, /disabled=""/);
assert.match(disabled, /Скоро/);
assert.doesNotMatch(disabled, /2 341/);
assert.doesNotMatch(disabled, /Открыть мир/);
const open = () => {};
const active = WorldCard({ world: real[0], onClick: open });
assert.equal(active.props.disabled, false);
assert.equal(active.props.onClick, open);
assert.match(renderToStaticMarkup(active), /Открыть мир Вечерело/);
console.log('Catalog verified: 4 active + 4 disabled; search, native disabled semantics and event wiring passed.');
} finally { fs.rmSync(out, { recursive: true, force: true }); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
