import type { PostDraft } from '../app/postDraft';

/** A saved publication must be reconciled before a new logical request is created. */
export function submissionFor(saved: PostDraft, draft: string, newId: () => string) {
  const recovery = !!saved.publicationId;
  return {
    id: saved.publicationId ?? newId(),
    text: recovery ? saved.submittedText ?? saved.text : draft,
    ownWords: recovery ? saved.submittedOwnWords ?? saved.ownWords : true,
    wantId: recovery ? saved.submittedWantId : undefined,
    recovery,
  };
}
