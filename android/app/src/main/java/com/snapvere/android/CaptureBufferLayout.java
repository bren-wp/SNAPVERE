package com.snapvere.android;

final class CaptureBufferLayout {
    private static final int RGBA_BYTES_PER_PIXEL = 4;

    private CaptureBufferLayout() {
    }

    static int paddedWidth(int width, int pixelStride, int rowStride) {
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

        int extraPixels = rowPadding / pixelStride;
        try {
            return Math.addExact(width, extraPixels);
        } catch (ArithmeticException exception) {
            throw new IllegalArgumentException("Padded capture width exceeds the supported bitmap range.", exception);
        }
    }
}
