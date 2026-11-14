import { ImageOff } from "lucide-react";

export default function EmptyGallery({ onCapture, hasQuery }: { onCapture: () => void; hasQuery: boolean }) {
  return (
    <div className="grid h-full place-items-center text-center">
      <div className="max-w-sm">
        <div className="mx-auto mb-4 grid h-14 w-14 place-items-center rounded-2xl bg-elevated/60 ring-1 ring-white/10">
          <ImageOff size={26} className="text-muted" />
        </div>
        <h2 className="mb-1 text-lg font-semibold tracking-tight">{hasQuery ? "No results" : "No screenshots yet"}</h2>
        <p className="mb-5 text-sm text-muted">
          {hasQuery ? "Try a different term." : "Press the hotkey or button to capture your first screenshot."}
        </p>
        {!hasQuery && (
          <button onClick={onCapture} className="snapvere-btn-primary rounded-full bg-accent px-5 py-2.5 text-sm font-semibold text-white transition hover:brightness-110 active:scale-95">
            Capture region
          </button>
        )}
      </div>
    </div>
  );
}
