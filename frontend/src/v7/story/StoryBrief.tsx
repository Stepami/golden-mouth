export function StoryBrief({ goal }: { goal: string }) {
  return <section className="section stack story-brief">
    <article className="story-step stack"><p className="caption muted">Твой персонаж · Главная цель</p><h2>К чему ты идёшь</h2><p>{goal}</p></article>
    <article className="story-step stack"><p className="caption muted">Развитие персонажа</p><h2>Какие навыки развивать</h2><ul className="publication-changes"><li><strong>Смелость</strong> — задавать прямые вопросы и проявлять инициативу.</li><li><strong>Юмор</strong> — находить общий язык и снимать напряжение.</li><li><strong>В тему</strong> — замечать важное и убеждать собеседников.</li></ul><p>Чем выше навыки, тем острее квесты и сложнее решения.</p><p className="caption muted">Следующие уровни сюжетных квестов появятся позже. Высокий навык не гарантирует согласие персонажа.</p></article>
  </section>;
}
