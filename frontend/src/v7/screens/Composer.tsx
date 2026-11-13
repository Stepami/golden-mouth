import { Avatar, Button, Chip, Notice, QuoteBlock, ScreenHeading } from "../ui";
import { approachLabels } from "../domain/worlds";
import type { Approach, Quest } from "../domain/types";
export function Composer({
  name,
  avatar,
  draft,
  onDraft,
  approach,
  onApproach,
  quest,
  context,
  error,
  pending,
  onPublish,
  onBack,
}: {
  name: string;
  avatar?: string;
  draft: string;
  onDraft: (text: string, fromIdea?: boolean) => void;
  approach: Approach;
  onApproach: (a: Approach) => void;
  quest?: Quest;
  context?: { name: string; text: string };
  error?: string;
  pending: boolean;
  onPublish: () => void;
  onBack: () => void;
}) {
  return (
    <>
      <ScreenHeading
        title={context ? "Продолжить разговор" : "Новая публикация"}
        back={onBack}
      />
      <div className="section stack composer-body">
        {quest && (
          <div className="composer-quest">
            <h2>{quest.title}</h2>
            <p>{quest.prompt}</p>
            <button
              className="link-button"
              onClick={() => onDraft(quest.draft, true)}
            >
              Помочь начать
            </button>
          </div>
        )}
        <div className="row">
          <Avatar name={name} src={avatar} />
          <div>
            <strong>{name}</strong>
            <span className="caption muted block">Публично</span>
          </div>
        </div>
        {context && <QuoteBlock {...context} />}
        <label className="sr-only" htmlFor="post-text">
          Текст публикации
        </label>
        <textarea
          id="post-text"
          className="composer-text"
          placeholder="Что хочешь сказать?"
          value={draft}
          onChange={(e) => onDraft(e.target.value)}
          maxLength={2000}
          aria-describedby="draft-hint"
          autoFocus
        />
        <div id="draft-hint" className="composer-count caption muted" aria-label="Символов в публикации">
          {draft.length} / 2 000
        </div>
        <div className="stack">
          <div className="approaches">
            {(Object.keys(approachLabels) as Approach[]).map((a) => (
              <Chip
                key={a}
                selected={approach === a}
                onClick={() => onApproach(a)}
              >
                {approachLabels[a]}
              </Chip>
            ))}
          </div>
        </div>
        {error && (
          <Notice title="Не удалось опубликовать" tone="error">
            {error}
          </Notice>
        )}
        <Button
          className="full"
          size="lg"
          onClick={onPublish}
          disabled={!draft.trim()}
          busy={pending}
        >
          Опубликовать <span className="caption">· 1 энергия</span>
        </Button>
      </div>
    </>
  );
}
