package com.snapvere.android;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.ServiceInfo;
import android.graphics.Bitmap;
import android.graphics.PixelFormat;
import android.graphics.Rect;
import android.hardware.display.DisplayManager;
import android.hardware.display.VirtualDisplay;
import android.media.Image;
import android.media.ImageReader;
import android.media.projection.MediaProjection;
import android.media.projection.MediaProjectionManager;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.IBinder;
import android.os.Looper;
import android.provider.MediaStore;
import android.util.DisplayMetrics;
import android.view.WindowManager;
import android.view.WindowMetrics;

import java.io.IOException;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.concurrent.atomic.AtomicBoolean;

public final class CaptureService extends Service {
    public static final String ACTION_CAPTURE_ONCE = "com.snapvere.android.action.CAPTURE_ONCE";
    public static final String ACTION_CAPTURE_COMPLETED = "com.snapvere.android.action.CAPTURE_COMPLETED";
    public static final String ACTION_CAPTURE_FAILED = "com.snapvere.android.action.CAPTURE_FAILED";
    public static final String EXTRA_RESULT_CODE = "projection_result_code";
    public static final String EXTRA_RESULT_DATA = "projection_result_data";
    public static final String EXTRA_CAPTURE_NAME = "capture_name";
    public static final String EXTRA_CAPTURE_URI = "capture_uri";
    public static final String EXTRA_ERROR_MESSAGE = "error_message";

    private static final String CHANNEL_ID = "snapvere_capture";
    private static final int NOTIFICATION_ID = 42;
    private static final long TASK_HIDE_TIMEOUT_MS = 5000L;
    private static final long FRAME_TIMEOUT_MS = 7000L;
    private static final String PREFS = "snapvere_android";
    private static final String PREF_LATEST_URI = "latest_capture_uri";
    private static final String PREF_LATEST_NAME = "latest_capture_name";
    private static final AtomicBoolean CAPTURE_ACTIVE = new AtomicBoolean();
    private static final AtomicBoolean APP_TASK_HIDDEN = new AtomicBoolean();
    private static volatile CaptureService activeService;
    private static volatile String lastError;

    private final AtomicBoolean completed = new AtomicBoolean();
    private final AtomicBoolean captureStarted = new AtomicBoolean();
    private HandlerThread captureThread;
    private Handler captureHandler;
    private Runnable taskHideTimeout;
    private Runnable frameTimeout;
    private MediaProjection mediaProjection;
    private MediaProjection.Callback projectionCallback;
    private VirtualDisplay virtualDisplay;
    private ImageReader imageReader;
    private String initializationError;

    public static boolean isCaptureActive() {
        return CAPTURE_ACTIVE.get();
    }

    public static String getLastError() {
        return lastError;
    }

    public static void clearLastError() {
        lastError = null;
    }

    public static void prepareForCaptureHandoff() {
        APP_TASK_HIDDEN.set(false);
    }

    public static void cancelCaptureHandoff() {
        APP_TASK_HIDDEN.set(false);
    }

    public static void notifyAppTaskHidden() {
        APP_TASK_HIDDEN.set(true);
        CaptureService service = activeService;
        if (service != null) {
            service.onAppTaskHidden();
        }
    }

    @Override
    public void onCreate() {
        super.onCreate();
        try {
            createNotificationChannel();
        } catch (RuntimeException exception) {
            initializationError = getString(R.string.capture_error_service_unavailable);
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null || !ACTION_CAPTURE_ONCE.equals(intent.getAction())) {
            stopSelf(startId);
            return START_NOT_STICKY;
        }

        // One MediaProjection consent token is one capture session. Ignore any
        // accidental second start while the first session is still active.
        if (!CAPTURE_ACTIVE.compareAndSet(false, true)) {
            return START_NOT_STICKY;
        }

        activeService = this;
        completed.set(false);
        captureStarted.set(false);
        lastError = null;

        if (initializationError != null) {
            failCapture(initializationError);
            return START_NOT_STICKY;
        }

        try {
            startForeground(
                NOTIFICATION_ID,
                buildNotification(getString(R.string.notification_capturing), false),
                ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION);

            int resultCode = intent.getIntExtra(EXTRA_RESULT_CODE, Activity.RESULT_CANCELED);
            Intent resultData = getProjectionData(intent);
            if (resultCode != Activity.RESULT_OK || resultData == null) {
                failCapture(getString(R.string.capture_error_permission));
                return START_NOT_STICKY;
            }

            startCapture(resultCode, resultData);
        } catch (RuntimeException exception) {
            failCapture(getString(R.string.capture_error_service_unavailable));
        }
        return START_NOT_STICKY;
    }

    @Override
    public void onDestroy() {
        if (CAPTURE_ACTIVE.get() && activeService == this && completed.compareAndSet(false, true)) {
            lastError = getString(R.string.capture_error_stopped);
            sendFailureBroadcastBestEffort(lastError);
        }
        cleanupAfterDestroy();
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    private void startCapture(int resultCode, Intent resultData) {
        MediaProjectionManager manager =
            (MediaProjectionManager) getSystemService(Context.MEDIA_PROJECTION_SERVICE);
        if (manager == null) {
            throw new IllegalStateException("MediaProjectionManager unavailable");
        }

        mediaProjection = manager.getMediaProjection(resultCode, resultData);
        if (mediaProjection == null) {
            throw new IllegalStateException("MediaProjection session unavailable");
        }

        captureThread = new HandlerThread("SnapvereCapture");
        captureThread.start();
        captureHandler = new Handler(captureThread.getLooper());

        projectionCallback = new MediaProjection.Callback() {
            @Override
            public void onStop() {
                if (!completed.get()) {
                    failCapture(getString(R.string.capture_error_stopped));
                }
            }
        };
        mediaProjection.registerCallback(projectionCallback, captureHandler);

        // MainActivity starts this foreground service while it is still visible,
        // then moves its task behind the previously visible task. Capture starts
        // only after MainActivity.onStop() confirms that SNAPVERE is no longer
        // visible; the timeout is failure protection, not a transition delay.
        taskHideTimeout = () -> {
            if (!completed.get() && !captureStarted.get()) {
                failCapture(getString(R.string.capture_error_hidden));
            }
        };
        if (!captureHandler.postDelayed(taskHideTimeout, TASK_HIDE_TIMEOUT_MS)) {
            throw new IllegalStateException("Capture handler rejected task-hide timeout");
        }

        if (APP_TASK_HIDDEN.get()) {
            onAppTaskHidden();
        }
    }

    private void onAppTaskHidden() {
        Handler handler = captureHandler;
        if (handler == null || completed.get()) {
            return;
        }
        try {
            if (!handler.post(this::beginCaptureAfterTaskHidden)) {
                failCapture(getString(R.string.capture_error_service_unavailable));
            }
        } catch (RuntimeException exception) {
            failCapture(getString(R.string.capture_error_service_unavailable));
        }
    }

    private void beginCaptureAfterTaskHidden() {
        if (completed.get() || !APP_TASK_HIDDEN.get() || !captureStarted.compareAndSet(false, true)) {
            return;
        }

        cancelTaskHideTimeout();
        try {
            beginVirtualDisplayCapture();
        } catch (RuntimeException exception) {
            failCapture(getString(R.string.capture_error_frame));
        }
    }

    private void cancelTaskHideTimeout() {
        Handler handler = captureHandler;
        Runnable timeout = taskHideTimeout;
        if (handler != null && timeout != null) {
            handler.removeCallbacks(timeout);
        }
        taskHideTimeout = null;
    }

    private void scheduleFrameTimeout() {
        Handler handler = captureHandler;
        if (handler == null) {
            throw new IllegalStateException("Capture handler unavailable");
        }

        frameTimeout = () -> {
            if (!completed.get()) {
                failCapture(getString(R.string.capture_error_frame_timeout));
            }
        };
        if (!handler.postDelayed(frameTimeout, FRAME_TIMEOUT_MS)) {
            throw new IllegalStateException("Capture handler rejected frame timeout");
        }
    }

    private void cancelFrameTimeout() {
        Handler handler = captureHandler;
        Runnable timeout = frameTimeout;
        if (handler != null && timeout != null) {
            handler.removeCallbacks(timeout);
        }
        frameTimeout = null;
    }

    private void beginVirtualDisplayCapture() {
        if (mediaProjection == null || captureHandler == null) {
            throw new IllegalStateException("Capture session ended before display setup");
        }

        CaptureSize size = getCaptureSize();
        imageReader = ImageReader.newInstance(
            size.width,
            size.height,
            PixelFormat.RGBA_8888,
            2);
        imageReader.setOnImageAvailableListener(reader -> consumeFirstImage(reader, size), captureHandler);

        virtualDisplay = mediaProjection.createVirtualDisplay(
            "SNAPVERE",
            size.width,
            size.height,
            size.densityDpi,
            DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
            imageReader.getSurface(),
            null,
            captureHandler);
        if (virtualDisplay == null) {
            throw new IllegalStateException("Virtual display unavailable");
        }

        scheduleFrameTimeout();
    }

    private void consumeFirstImage(ImageReader reader, CaptureSize size) {
        if (completed.get()) {
            return;
        }

        Image image;
        try {
            image = reader.acquireLatestImage();
        } catch (RuntimeException exception) {
            failCapture(getString(R.string.capture_error_frame));
            return;
        }

        if (image == null) {
            return;
        }

        cancelFrameTimeout();
        Bitmap bitmap = null;
        SavedCapture saved = null;
        String failure = null;
        try {
            bitmap = bitmapFromImage(image, size.width, size.height);
            saved = saveBitmap(bitmap);
        } catch (IOException exception) {
            failure = getString(R.string.capture_error_save);
        } catch (RuntimeException | OutOfMemoryError exception) {
            failure = getString(R.string.capture_error_frame);
        } finally {
            if (bitmap != null) {
                try {
                    bitmap.recycle();
                } catch (RuntimeException ignored) {
                }
            }
            try {
                image.close();
            } catch (RuntimeException ignored) {
                // Completion/failure must not be replaced by an image-close failure.
            }
        }

        if (saved != null) {
            finishSuccess(saved);
        } else {
            failCapture(failure == null ? getString(R.string.unknown_error) : failure);
        }
    }

    private static Bitmap bitmapFromImage(Image image, int width, int height) {
        Image.Plane[] planes = image.getPlanes();
        if (planes.length == 0) {
            throw new IllegalStateException("Capture image has no pixel plane");
        }

        Image.Plane plane = planes[0];
        ByteBuffer buffer = plane.getBuffer();
        int pixelStride = plane.getPixelStride();
        int rowStride = plane.getRowStride();
        int paddedWidth = CaptureBufferLayout.paddedWidth(width, pixelStride, rowStride);

        long requiredBytes;
        try {
            requiredBytes = Math.multiplyExact((long) rowStride, (long) height);
        } catch (ArithmeticException exception) {
            throw new IllegalArgumentException("Capture buffer size overflow", exception);
        }

        buffer.rewind();
        if (requiredBytes > buffer.remaining()) {
            throw new IllegalArgumentException("Capture buffer is smaller than the declared row layout");
        }

        Bitmap padded = Bitmap.createBitmap(paddedWidth, height, Bitmap.Config.ARGB_8888);
        try {
            padded.copyPixelsFromBuffer(buffer);
            if (paddedWidth == width) {
                return padded;
            }

            return Bitmap.createBitmap(padded, 0, 0, width, height);
        } finally {
            if (paddedWidth != width) {
                padded.recycle();
            }
        }
    }

    private SavedCapture saveBitmap(Bitmap bitmap) throws IOException {
        String name = "SNAPVERE_" +
            new SimpleDateFormat("yyyy-MM-dd_HHmmss_SSS", Locale.US).format(new Date()) +
            ".png";

        ContentValues values = new ContentValues();
        values.put(MediaStore.Images.Media.DISPLAY_NAME, name);
        values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
        values.put(MediaStore.Images.Media.RELATIVE_PATH, Environment.DIRECTORY_PICTURES + "/SNAPVERE");
        values.put(MediaStore.Images.Media.IS_PENDING, 1);

        ContentResolver resolver = getContentResolver();
        Uri uri = resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
        if (uri == null) {
            throw new IOException("MediaStore destination unavailable");
        }

        boolean committed = false;
        try {
            try (OutputStream output = resolver.openOutputStream(uri, "w")) {
                if (output == null || !bitmap.compress(Bitmap.CompressFormat.PNG, 100, output)) {
                    throw new IOException("PNG stream unavailable");
                }
                output.flush();
            }

            ContentValues complete = new ContentValues();
            complete.put(MediaStore.Images.Media.IS_PENDING, 0);
            int updated = resolver.update(uri, complete, null, null);
            if (updated != 1) {
                throw new IOException("MediaStore did not finalize capture");
            }
            committed = true;
            return new SavedCapture(uri, name);
        } finally {
            if (!committed) {
                try {
                    resolver.delete(uri, null, null);
                } catch (RuntimeException ignored) {
                    // Preserve the original save failure if MediaStore cleanup also fails.
                }
            }
        }
    }

    private void finishSuccess(SavedCapture saved) {
        if (!completed.compareAndSet(false, true)) {
            return;
        }

        lastError = null;
        SharedPreferences.Editor editor = getSharedPreferences(PREFS, MODE_PRIVATE).edit();
        editor.putString(PREF_LATEST_URI, saved.uri.toString());
        editor.putString(PREF_LATEST_NAME, saved.name);
        editor.apply();

        Intent result = new Intent(ACTION_CAPTURE_COMPLETED)
            .setPackage(getPackageName())
            .putExtra(EXTRA_CAPTURE_NAME, saved.name)
            .putExtra(EXTRA_CAPTURE_URI, saved.uri.toString());
        sendBroadcastBestEffort(result);

        finishService();
    }

    private void failCapture(String message) {
        if (!completed.compareAndSet(false, true)) {
            return;
        }

        lastError = message;
        sendFailureBroadcastBestEffort(message);
        finishService();
    }

    private void sendFailureBroadcastBestEffort(String message) {
        Intent result = new Intent(ACTION_CAPTURE_FAILED)
            .setPackage(getPackageName())
            .putExtra(EXTRA_ERROR_MESSAGE, message);
        sendBroadcastBestEffort(result);
    }

    private void sendBroadcastBestEffort(Intent intent) {
        try {
            sendBroadcast(intent);
        } catch (RuntimeException ignored) {
            // Broadcast delivery must never block cleanup of capture resources.
        }
    }

    private void finishService() {
        cleanupCapture(true);
        releaseCaptureOwnership();
        try {
            stopForeground(STOP_FOREGROUND_REMOVE);
        } catch (RuntimeException ignored) {
        }
        stopSelf();
    }

    private void cleanupAfterDestroy() {
        Handler handler = captureHandler;
        if (handler != null && Looper.myLooper() != handler.getLooper()) {
            boolean posted;
            try {
                posted = handler.post(() -> {
                    cleanupCapture(true);
                    releaseCaptureOwnership();
                });
            } catch (RuntimeException exception) {
                posted = false;
            }
            if (posted) {
                return;
            }
        }

        cleanupCapture(true);
        releaseCaptureOwnership();
    }

    private void cleanupCapture(boolean stopProjection) {
        cancelTaskHideTimeout();
        cancelFrameTimeout();

        Handler handler = captureHandler;
        captureHandler = null;
        if (handler != null) {
            try {
                handler.removeCallbacksAndMessages(null);
            } catch (RuntimeException ignored) {
            }
        }

        VirtualDisplay display = virtualDisplay;
        virtualDisplay = null;
        if (display != null) {
            try {
                display.release();
            } catch (RuntimeException ignored) {
            }
        }

        ImageReader reader = imageReader;
        imageReader = null;
        if (reader != null) {
            try {
                reader.setOnImageAvailableListener(null, null);
            } catch (RuntimeException ignored) {
            }
            try {
                reader.close();
            } catch (RuntimeException ignored) {
            }
        }

        MediaProjection projection = mediaProjection;
        MediaProjection.Callback callback = projectionCallback;
        mediaProjection = null;
        projectionCallback = null;
        if (projection != null) {
            if (callback != null) {
                try {
                    projection.unregisterCallback(callback);
                } catch (RuntimeException ignored) {
                }
            }
            if (stopProjection) {
                try {
                    projection.stop();
                } catch (RuntimeException ignored) {
                }
            }
        }

        HandlerThread thread = captureThread;
        captureThread = null;
        if (thread != null) {
            try {
                thread.quitSafely();
            } catch (RuntimeException ignored) {
            }
        }
    }

    private void releaseCaptureOwnership() {
        if (activeService == this) {
            activeService = null;
            APP_TASK_HIDDEN.set(false);
            CAPTURE_ACTIVE.set(false);
        }
    }

    @SuppressLint("deprecation") // Android 10 fallback; API 30+ uses WindowMetrics above.
    @SuppressWarnings("deprecation")
    private CaptureSize getCaptureSize() {
        WindowManager windowManager = getSystemService(WindowManager.class);
        if (windowManager == null) {
            throw new IllegalStateException("WindowManager unavailable");
        }

        int width;
        int height;
        int densityDpi = getResources().getDisplayMetrics().densityDpi;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            WindowMetrics metrics = windowManager.getMaximumWindowMetrics();
            Rect bounds = metrics.getBounds();
            width = bounds.width();
            height = bounds.height();
        } else {
            DisplayMetrics metrics = new DisplayMetrics();
            windowManager.getDefaultDisplay().getRealMetrics(metrics);
            width = metrics.widthPixels;
            height = metrics.heightPixels;
            densityDpi = metrics.densityDpi;
        }

        if (width <= 0 || height <= 0 || densityDpi <= 0) {
            throw new IllegalStateException("Invalid display metrics");
        }
        return new CaptureSize(width, height, densityDpi);
    }

    private Notification buildNotification(String text, boolean autoCancel) {
        Intent openApp = new Intent(this, MainActivity.class)
            .addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        PendingIntent contentIntent = PendingIntent.getActivity(
            this,
            0,
            openApp,
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);

        return new Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(getString(R.string.app_name))
            .setContentText(text)
            .setContentIntent(contentIntent)
            .setOngoing(!autoCancel)
            .setAutoCancel(autoCancel)
            .setCategory(Notification.CATEGORY_SERVICE)
            .build();
    }

    private void createNotificationChannel() {
        NotificationManager manager = getSystemService(NotificationManager.class);
        if (manager == null) {
            throw new IllegalStateException("NotificationManager unavailable");
        }

        NotificationChannel channel = new NotificationChannel(
            CHANNEL_ID,
            getString(R.string.notification_channel_name),
            NotificationManager.IMPORTANCE_LOW);
        channel.setDescription(getString(R.string.notification_channel_description));
        manager.createNotificationChannel(channel);
    }

    @SuppressWarnings("deprecation")
    private static Intent getProjectionData(Intent source) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            return source.getParcelableExtra(EXTRA_RESULT_DATA, Intent.class);
        }
        return source.getParcelableExtra(EXTRA_RESULT_DATA);
    }

    private record CaptureSize(int width, int height, int densityDpi) { }

    private record SavedCapture(Uri uri, String name) { }
}
