package com.snapvere.android;

final class CaptureBufferLayout {
    private CaptureBufferLayout() {
    }

    static int paddedWidth(int width, int pixelStride, int rowStride) {
        if (width <= 0) {
            throw new IllegalArgumentException("Capture width must be positive.");
        }
        if (pixelStride <= 0) {
            throw new IllegalArgumentException("Pixel stride must be positive.");
        }

        long minimumRowBytes = (long) width * pixelStride;
        if (minimumRowBytes > Integer.MAX_VALUE || rowStride < minimumRowBytes) {
            throw new IllegalArgumentException("Row stride is smaller than the visible capture row.");
        }

        int rowPadding = rowStride - (int) minimumRowBytes;
        int extraPixels = rowPadding / pixelStride;
        return Math.addExact(width, extraPixels);
    }
}
