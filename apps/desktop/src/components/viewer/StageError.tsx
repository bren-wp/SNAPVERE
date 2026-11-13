import { AlertTriangle } from "lucide-react";

export default function StageError({
  video,
  detail,
  onOpenExternal,
}: {
  video: boolean;
  /** Tehnički razlog (MediaError) — mali red za bug reportove. */
  detail?: string;
  onOpenExternal: () => void;
}) {
  return (
    <div className="grid h-full place-items-center p-8 text-center">
      <div className="max-w-sm">
        <div className="mx-auto mb-4 grid h-12 w-12 place-items-center rounded-2xl bg-white/5 ring-1 ring-white/10">
          <AlertTriangle size={22} className="text-warn" />
        </div>
        <h2 className="mb-1 text-base font-semibold">
          {video ? "This video can’t be played here" : "This image can’t be shown"}
        </h2>
        <p className="mb-5 text-sm text-muted">The file may have been moved, renamed or deleted.</p>
        {detail && <p className="-mt-3 mb-5 font-mono text-[10px] text-white/35">{detail}</p>}
        <button
          onClick={onOpenExternal}
          className="rounded-full px-4 py-2 text-sm ring-1 ring-white/15 transition hover:bg-white/10 active:scale-95"
        >
          Open in default app
        </button>
      </div>
    </div>
  );
}

