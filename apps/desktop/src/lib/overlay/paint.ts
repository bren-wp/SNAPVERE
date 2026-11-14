//! Wait for the compositor before revealing protected screen overlays.
/** Koliko cloakan overlay čeka nakon iscrtanog framea da ga DWM preuzme (ms). */
export const REVEAL_DELAY_MS = 34;
export const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));
/** Dva animation framea = DOM i canvas su predani kompozitoru. Timeout je osigurač:
 *  cloakan prozor ne smije ostati nevidljiv ako rAF iz nekog razloga ne stigne. */
export const painted = () =>
  new Promise<void>((resolve) => {
    let done = false;
    const finish = () => {
      if (!done) { done = true; resolve(); }
    };
    requestAnimationFrame(() => requestAnimationFrame(finish));
    setTimeout(finish, 200);
  });
