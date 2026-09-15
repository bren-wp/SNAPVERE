package com.snapvere.android;

import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertThrows;

import java.nio.ByteBuffer;

import org.junit.Test;

public final class CaptureBufferLayoutTest {
    @Test
    public void compactVisibleRgbaCopiesTightRows() {
        byte[] pixels = new byte[] {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        };

        byte[] compact = CaptureBufferLayout.compactVisibleRgba(
            ByteBuffer.wrap(pixels), 2, 2, 4, 8);

        assertArrayEquals(pixels, compact);
    }

    @Test
    public void compactVisibleRgbaSkipsRowPaddingWithoutRequiringFinalPadding() {
        byte[] source = new byte[] {
            1, 2, 3, 4, 5, 6, 7, 8,
            90, 91, 92, 93,
            9, 10, 11, 12, 13, 14, 15, 16
        };

        byte[] compact = CaptureBufferLayout.compactVisibleRgba(
            ByteBuffer.wrap(source), 2, 2, 4, 12);

        assertArrayEquals(
            new byte[] {
                1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 13, 14, 15, 16
            },
            compact);
    }

    @Test
    public void compactVisibleRgbaRejectsBufferMissingVisibleFinalRowBytes() {
        byte[] source = new byte[19];

        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.wrap(source), 2, 2, 4, 12));
    }

    @Test
    public void compactVisibleRgbaRejectsInvalidDimensionsAndStrides() {
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.allocate(8), 0, 1, 4, 8));
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.allocate(8), 2, 0, 4, 8));
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.allocate(8), 2, 1, 8, 16));
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.allocate(8), 2, 1, 4, 7));
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.allocate(12), 2, 1, 4, 9));
    }

    @Test
    public void compactVisibleRgbaRejectsOversizedOutput() {
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.compactVisibleRgba(
                ByteBuffer.allocate(4), Integer.MAX_VALUE, 1, 4, Integer.MAX_VALUE));
    }
}
