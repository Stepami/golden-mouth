import type { PostDraft } from '../app/postDraft';

/** A saved publication must be reconciled before a new logical request is created. */
export function submissionFor(saved: PostDraft, draft: string, newId: () => string) {
  const recovery = !!saved.publicationId;
  return {
    id: saved.publicationId ?? newId(),
    text: recovery ? saved.submittedText ?? saved.text : draft,
    ownWords: recovery ? saved.submittedOwnWords ?? saved.ownWords : saved.ownWords,
    wantId: recovery ? saved.submittedWantId : undefined,
    recovery,
  };
}

/** A technical/model failure is not a rejection of the player's text. */
export function publicationError(state: 'failed' | 'unknown', reason?: { code: string }): string {
  if (state === 'unknown') return 'Исход отправки пока неизвестен. Проверь статус — повторный пост не создаётся.';
  switch (reason?.code) {
    case 'technical_failure': return 'Не удалось подготовить реакцию персонажей. Текст сохранён — можно попробовать ещё раз.';
    case 'unsafe_generated_content': return 'Не удалось подготовить безопасную реакцию персонажей. Текст сохранён — можно попробовать ещё раз.';
    case 'content_rejected': return 'Публикация не прошла проверку содержания. Текст сохранён — его можно изменить.';
    case 'crisis': return 'Публикация остановлена проверкой безопасности. Текст сохранён.';
    default: return 'Не получилось опубликовать. Текст сохранён.';
  }
}
