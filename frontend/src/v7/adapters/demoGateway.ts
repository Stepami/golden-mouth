import { simulateResponse, validatePost } from "../domain/game";
import type { SocialGateway } from "../domain/types";
export const demoGateway: SocialGateway = {
  async publish(command, world, _save, signal) {
    const error = validatePost(command.text);
    if (error) throw new Error(error);
    await new Promise<void>((resolve, reject) => {
      const timer = setTimeout(resolve, 900);
      signal?.addEventListener(
        "abort",
        () => {
          clearTimeout(timer);
          reject(new DOMException("Cancelled", "AbortError"));
        },
        { once: true },
      );
    });
    return simulateResponse(command, world);
  },
};
