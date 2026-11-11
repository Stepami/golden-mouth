import { type MouseEvent } from "react";
import { Icon } from "./index";
export function SocialAction({
  kind,
  label,
  count,
  selected,
  onClick,
}: {
  kind: "heart" | "comment" | "quote";
  label: string;
  count?: number;
  selected?: boolean;
  onClick?: () => void;
}) {
  function activate(event: MouseEvent<HTMLButtonElement>) {
    const button = event.currentTarget;
    const visual = button.querySelector<HTMLElement>(".social-glyph");
    // Keyboard activation and reduced motion remain immediate, without movement.
    if (
      event.detail > 0 &&
      !matchMedia("(prefers-reduced-motion: reduce)").matches &&
      visual
    ) {
      visual.getAnimations().forEach((a) => a.cancel());
      if (kind === "heart" && !selected) visual.animate([
        {transform:"scale(.92)"},
        {transform:"scale(1.08)",offset:.55},
        {transform:"scale(1)"},
      ],{duration:180,easing:"cubic-bezier(.23,1,.32,1)"});
    }
    onClick?.();
  }
  return (
    <button
      className={`social-action social-action--${kind}`}
      aria-label={label}
      aria-pressed={kind === "heart" ? !!selected : undefined}
      disabled={!onClick}
      onClick={activate}
    >
      <span className="social-glyph" aria-hidden="true">
        <Icon name={kind} />
      </span>
      {count !== undefined && (
        <span className="social-count">{count || ""}</span>
      )}
    </button>
  );
}
