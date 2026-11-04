import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { createServer } from 'vite';
const server = await createServer({ configFile: false, server: { middlewareMode: true }, appType: 'custom' });
try {
  const { ProfileScore } = await server.ssrLoadModule('/src/v7/ui/index.tsx');
  const { Onboarding } = await server.ssrLoadModule('/src/v7/screens/Onboarding.tsx');
  const { worldView } = await server.ssrLoadModule('/src/v7/backend.ts');
  await test('live skill bars use progress to the next level, not level divided by ten', () => {
    const markup = renderToStaticMarkup(ProfileScore({ score: null, skills: [6, 4, 10], skillLabels: ['Смелость', 'Юмор', 'В тему'], skillProgress: [.25, .5, 1] }));
    assert.match(markup, /aria-valuemax="1" aria-valuenow="0.25"/);
    assert.match(markup, /aria-valuemax="1" aria-valuenow="0.5"/);
    assert.match(markup, /aria-valuemax="1" aria-valuenow="1"/);
    assert.doesNotMatch(markup, /aria-valuemax="10"/);
  });
  await test('reference-only profile keeps its original ten-point display', () => {
    const markup = renderToStaticMarkup(ProfileScore({ score: 33, skills: [2, 3, 4] }));
    assert.match(markup, /aria-valuemax="10" aria-valuenow="2"/);
  });
  await test('editing interests starts on the choices and preserves saved selections', () => {
    const markup = renderToStaticMarkup(createElement(Onboarding, { initialStep: 1, initialQualities: ['Быть собой'], onDone() {}, onBack() {} }));
    assert.doesNotMatch(markup, /onboarding-steps/);
    assert.match(markup, /<button[^>]*aria-pressed="true"[^>]*>Быть собой<\/button>/);
    assert.doesNotMatch(markup, /onboarding-art/);
  });
  await test('the UI adapter keeps the server NPC relation without calculating a new one', () => {
    const mapped = worldView({ id: 'qa-world', title: 'Тест', description: '', summary: '', name: 'Игрок', role: 'Автор', postIdeas: [], firstContext: '', firstPrompt: '', stars: [{ id: 'npc', name: 'Персонаж', avatarSeed: 'npc', role: 'Собеседник', about: '', relation: -2 }] });
    assert.equal(mapped.cast[0].relation, -2);
  });
} finally { await server.close(); }
