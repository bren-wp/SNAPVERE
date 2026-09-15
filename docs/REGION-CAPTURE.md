# SNAPVERE 0.1.1 Region Capture

Region capture freezes the selected display before the selection overlay is shown. The user can drag, move or resize the region and annotate with Pen, Line, Arrow, Box or Highlight.

Enter/Save writes a PNG; Copy sends the rendered selection to the Windows clipboard; Esc cancels. Geometry is clamped to the frozen frame and empty/invalid selections are rejected before encode.

The browser region flow uses a per-capture token. After the popup closes, the page selection overlay remains responsible for completing the request and now surfaces a localized transient error when the background crop/download fails.
