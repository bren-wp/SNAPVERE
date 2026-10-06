# SNAPVERE 0.1.30 Privacy

SNAPVERE is local-first. Core Windows and browser capture processing does not require a SNAPVERE account, automatic cloud service, first-party analytics or capture telemetry.

## Windows

Captured pixels are processed locally. PNG/MP4 files are saved only through user-requested capture workflows.

Startup diagnostics are local, size-bounded and rotated. Before persisted exception text is written, SNAPVERE redacts absolute Windows/UNC paths, HTTP/HTTPS/file URIs, e-mail addresses and common credential/token patterns. User-facing failure copy does not expose raw exception text or local diagnostic paths. Diagnostics are not intended to contain screenshot pixels, clipboard contents, credentials or private signing material.

## Browsers

Browser extensions process screenshot pixels locally and use the browser download flow for saved PNG files. They do not request broad host access and do not include a first-party network client, analytics SDK, advertising SDK or automatic screenshot uploader.

Persistent browser storage is used for user settings. Short-lived capture ownership metadata (random session token, tab/window identifiers and start time) is kept in session-scoped extension storage when the browser supports it, with a local-storage fallback only for compatibility. Recent captures queries a bounded SNAPVERE candidate set from the browser download API and then locally revalidates filename/state before presenting or opening an item.

External applications, browser sync features or synced folders chosen by the user have their own privacy behavior.

Support: info@snapvere.com
