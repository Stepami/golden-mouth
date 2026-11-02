import { useRef, type MouseEvent } from "react";
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
  const actionRef = useRef<HTMLButtonElement>(null);
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
      const frames =
        kind === "heart"
          ? [
              { transform: "scale(.78) rotate(-12deg)" },
              { transform: "scale(1.24) rotate(6deg)", offset: 0.55 },
              { transform: "scale(1) rotate(0)" },
            ]
          : kind === "comment"
            ? [
                { transform: "translateY(0) scale(.85)" },
                { transform: "translateY(-3px) scale(1.12)", offset: 0.6 },
                { transform: "translateY(0) scale(1)" },
              ]
            : [
                { transform: "rotate(-18deg) scale(.88)" },
                { transform: "rotate(10deg) scale(1.12)", offset: 0.6 },
                { transform: "rotate(0) scale(1)" },
              ];
      visual.animate(frames, {
        duration: 260,
        easing: "cubic-bezier(.23,1,.32,1)",
      });
      if (kind === "heart")
        button.querySelector(".social-count")?.animate(
          [
            { opacity: 0.4, transform: "translateY(3px)" },
            { opacity: 1, transform: "translateY(0)" },
          ],
          { duration: 180, easing: "ease-out" },
        );
    }
    onClick?.();
  }
  return (
    <button
      ref={actionRef}
      className={`social-action social-action--${kind}`}
      aria-label={label}
      aria-pressed={kind === "heart" ? !!selected : undefined}
      onClick={activate}
    >
      <span className="social-glyph" aria-hidden="true">
        {kind === "heart" ? (
          <svg width="19" height="19" viewBox="0 0 19 19" className="heart-svg">
            <path
              d="M15.0417 11.0833C16.2212 9.9275 17.4167 8.54208 17.4167 6.72917C17.4167 5.57437 16.9579 4.46687 16.1414 3.65031C15.3248 2.83374 14.2173 2.375 13.0625 2.375C11.6692 2.375 10.6875 2.77083 9.5 3.95833C8.3125 2.77083 7.33083 2.375 5.9375 2.375C4.7827 2.375 3.6752 2.83374 2.85864 3.65031C2.04207 4.46687 1.58333 5.57437 1.58333 6.72917C1.58333 8.55 2.77083 9.93542 3.95833 11.0833L9.5 16.625L15.0417 11.0833Z"
              strokeWidth="1.65"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        ) : (
          <Icon name={kind} />
        )}
      </span>
      {count !== undefined && (
        <span className="social-count">{count || ""}</span>
      )}
    </button>
  );
}
