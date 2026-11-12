import React from "react";
import BrandMark from "./BrandMark";

/**
 * Shared brand shell for Home, Settings and Editor.
 * Transparent record/overlay windows are deliberately excluded so their
 * coordinate spaces and click-through behaviour cannot change.
 */
export default function WindowFrame({
  title, children,
}: { title?: string; children: React.ReactNode }) {
  const context = title?.includes("Editor") ? "EDITOR"
    : title === "Settings" ? "PREFERENCES" : "CAPTURE LIBRARY";

  return (
    <div className="snapvere-app-shell flex h-full w-full min-h-0 flex-col bg-bg text-text">
      <div className="snapvere-app-brandbar" aria-label="Snapvere application header">
        <div className="snapvere-app-identity">
          <BrandMark className="snapvere-app-logo" />
          <div className="snapvere-app-identity-text">
            <span className="snapvere-app-wordmark">SNAPVERE</span>
            <span className="snapvere-app-tagline">Capture. Edit. Done.</span>
          </div>
        </div>
        <div className="snapvere-app-brandbar-end">
          <span className="snapvere-app-context">{context}</span>
          <span className="snapvere-app-local">
            <span className="snapvere-app-local-dot" aria-hidden="true" />
            LOCAL-FIRST
          </span>
        </div>
      </div>
      {children}
    </div>
  );
}
