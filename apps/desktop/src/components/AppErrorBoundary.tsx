import React from "react";

/**
 * Catch render/lifecycle errors in a single WebView without taking down the
 * native recorder process or leaving a completely blank window.
 * This cannot catch native failures, driver crashes or async event handlers.
 */
export default class AppErrorBoundary extends React.Component<
  { children: React.ReactNode },
  { failed: boolean }
> {
  state = { failed: false };

  static getDerivedStateFromError(): { failed: boolean } {
    return { failed: true };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo): void {
    console.error("Snapvere interface error", error, info.componentStack);
  }

  render(): React.ReactNode {
    if (!this.state.failed) return this.props.children;
    return (
      <main
        role="alert"
        className="flex min-h-screen flex-col items-center justify-center gap-5 bg-bg px-8 text-center text-text"
      >
        <div className="rounded-2xl border border-border bg-elevated p-8 shadow-card">
          <div className="text-xs font-semibold uppercase tracking-widest text-accent">SNAPVERE</div>
          <h1 className="mt-3 text-xl font-bold">The interface needs to recover</h1>
          <p className="mt-3 max-w-md text-sm leading-relaxed text-muted">
            An unexpected display error occurred. Your saved screenshots and recordings
            remain in your local folders. Reloading may discard unsaved edits.
          </p>
          <button
            type="button"
            className="mt-5 rounded-full bg-accent px-5 py-2.5 text-sm font-semibold text-white hover:brightness-110 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            onClick={() => window.location.reload()}
          >
            Reload interface
          </button>
        </div>
      </main>
    );
  }
}
