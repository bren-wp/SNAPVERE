package com.snapvere.android;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertThrows;

import org.junit.Test;

public final class CaptureBufferLayoutTest {
    @Test
    public void paddedWidthReturnsVisibleWidthWhenRowsAreTight() {
        assertEquals(1080, CaptureBufferLayout.paddedWidth(1080, 4, 4320));
    }

    @Test
    public void paddedWidthIncludesWholePaddingPixels() {
        assertEquals(1088, CaptureBufferLayout.paddedWidth(1080, 4, 4352));
    }

    @Test
    public void paddedWidthRejectsInvalidDimensionsAndStrides() {
        assertThrows(IllegalArgumentException.class, () -> CaptureBufferLayout.paddedWidth(0, 4, 4));
        assertThrows(IllegalArgumentException.class, () -> CaptureBufferLayout.paddedWidth(10, 0, 40));
        assertThrows(IllegalArgumentException.class, () -> CaptureBufferLayout.paddedWidth(10, 8, 80));
        assertThrows(IllegalArgumentException.class, () -> CaptureBufferLayout.paddedWidth(10, 4, 0));
        assertThrows(IllegalArgumentException.class, () -> CaptureBufferLayout.paddedWidth(10, 4, 39));
    }

    @Test
    public void paddedWidthRejectsPartialPaddingPixels() {
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.paddedWidth(10, 4, 41));
    }

    @Test
    public void paddedWidthRejectsOverflowingVisibleRows() {
        assertThrows(
            IllegalArgumentException.class,
            () -> CaptureBufferLayout.paddedWidth(Integer.MAX_VALUE, 4, Integer.MAX_VALUE));
    }
}
