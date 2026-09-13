package com.snapvere.android;

import java.nio.ByteBuffer;

final class CaptureBufferLayout {
    private static final int RGBA_BYTES_PER_PIXEL = 4;

    private CaptureBufferLayout() {
    }

    static int paddedWidth(int width, int pixelStride, int rowStride) {
        validateRowLayout(width, pixelStride, rowStride);

        long minimumRowBytes = (long) width * pixelStride;
        int rowPadding = rowStride - (int) minimumRowBytes;
        int extraPixels = rowPadding / pixelStride;
        try {
            return Math.addExact(width, extraPixels);
        } catch (ArithmeticException exception) {
            throw new IllegalArgumentException("Padded capture width exceeds the supported bitmap range.", exception);
        }
    }

    static byte[] compactVisibleRgba(
        ByteBuffer source,
        int width,
        int height,
        int pixelStride,
        int rowStride) {
        if (source == null) {
            throw new IllegalArgumentException("Capture buffer is unavailable.");
        }
        if (height <= 0) {
            throw new IllegalArgumentException("Capture height must be positive.");
        }

        int visibleRowBytes = validateRowLayout(width, pixelStride, rowStride);
        long requiredSourceBytes;
        long compactBytes;
        try {
            requiredSourceBytes = Math.addExact(
                Math.multiplyExact((long) (height - 1), (long) rowStride),
                (long) visibleRowBytes);
            compactBytes = Math.multiplyExact((long) visibleRowBytes, (long) height);
        } catch (ArithmeticException exception) {
            throw new IllegalArgumentException("Capture buffer size overflow.", exception);
        }

        if (compactBytes > Integer.MAX_VALUE) {
            throw new IllegalArgumentException("Capture bitmap exceeds the supported buffer range.");
        }

        ByteBuffer input = source.duplicate();
        input.rewind();
        if (requiredSourceBytes > input.remaining()) {
            throw new IllegalArgumentException("Capture buffer is smaller than the visible row layout.");
        }

        byte[] compact = new byte[(int) compactBytes];
        if (rowStride == visibleRowBytes) {
            input.get(compact);
            return compact;
        }

        for (int row = 0; row < height; row++) {
            long sourceOffset = (long) row * rowStride;
            if (sourceOffset > Integer.MAX_VALUE) {
                throw new IllegalArgumentException("Capture row offset exceeds the supported buffer range.");
            }
            input.position((int) sourceOffset);
            input.get(compact, row * visibleRowBytes, visibleRowBytes);
        }
        return compact;
    }

    private static int validateRowLayout(int width, int pixelStride, int rowStride) {
        if (width <= 0) {
            throw new IllegalArgumentException("Capture width must be positive.");
        }
        if (pixelStride != RGBA_BYTES_PER_PIXEL) {
            throw new IllegalArgumentException("Unexpected RGBA pixel stride.");
        }
        if (rowStride <= 0) {
            throw new IllegalArgumentException("Row stride must be positive.");
        }

        long minimumRowBytes = (long) width * pixelStride;
        if (minimumRowBytes > Integer.MAX_VALUE || rowStride < minimumRowBytes) {
            throw new IllegalArgumentException("Row stride is smaller than the visible capture row.");
        }

        int rowPadding = rowStride - (int) minimumRowBytes;
        if (rowPadding % pixelStride != 0) {
            throw new IllegalArgumentException("Row padding is not aligned to the pixel stride.");
        }
        return (int) minimumRowBytes;
    }
}
