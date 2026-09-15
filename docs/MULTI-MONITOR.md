# SNAPVERE 0.1.2 Multi-monitor Behavior

Windows capture works in physical-pixel coordinates and discovers the target display before capture. Mixed DPI and negative desktop coordinates are treated as normal monitor topology rather than being coerced to a primary-monitor model.

Window and region overlays are positioned against the selected display bounds. Tests cover geometry and architecture-level behavior; real hardware combinations can still expose driver/platform limitations and should fail in a controlled way rather than crash the process.
