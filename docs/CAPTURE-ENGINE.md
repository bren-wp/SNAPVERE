# SNAPVERE 0.1.2 Capture Engine

Windows screen capture prefers Windows.Graphics.Capture when supported. Expected platform, timeout or native acquisition failures can fall back to the compatibility monitor backend; caller cancellation is never converted into fallback work.

Captured frames use validated physical-pixel dimensions, stride and BGRA8 data. Region workflows crop a frozen frame before annotation/encoding. PNG encoding performs RGB channel conversion and DEFLATE compression off the WinUI thread and writes the compressed backing memory without an additional full-buffer copy.

File output uses temporary staging and final move semantics so incomplete output is not intentionally exposed as a completed PNG.
