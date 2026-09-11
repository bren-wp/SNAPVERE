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
    private MediaProjection mediaProjection;
    private MediaProjection.Callback projectionCallback;
    private VirtualDisplay virtualDisplay;
    private ImageReader imageReader;

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
        createNotificationChannel();
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

        try {
            startForeground(
                NOTIFICATION_ID,
                buildNotification(getString(R.string.notification_capturing), false),
                ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION);

            int resultCode = intent.getIntExtra(EXTRA_RESULT_CODE, Activity.RESULT_CANCELED);
            Intent resultData = getProjectionData(intent);
            if (resultCode != Activity.RESULT_OK || resultData == null) {
                failCapture("Android did not provide a valid screen-capture token.");
                return START_NOT_STICKY;
            }

            startCapture(resultCode, resultData);
        } catch (RuntimeException exception) {
            failCapture(messageOf(exception));
        }
        return START_NOT_STICKY;
    }

    @Override
    public void onDestroy() {
        if (CAPTURE_ACTIVE.get() && completed.compareAndSet(false, true)) {
            lastError = "Android stopped the capture service before the image was completed.";
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
        mediaProjection = manager.getMediaProjection(resultCode, resultData);
        if (mediaProjection == null) {
            throw new IllegalStateException("Android could not create a MediaProjection session.");
        }

        captureThread = new HandlerThread("SnapvereCapture");
        captureThread.start();
        captureHandler = new Handler(captureThread.getLooper());

        projectionCallback = new MediaProjection.Callback() {
            @Override
            public void onStop() {
                if (!completed.get()) {
                    failCapture("Android ended the screen-capture session before an image was saved.");
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
                failCapture("SNAPVERE did not become fully hidden before capture could start.");
            }
        };
        captureHandler.postDelayed(taskHideTimeout, TASK_HIDE_TIMEOUT_MS);

        if (APP_TASK_HIDDEN.get()) {
            onAppTaskHidden();
        }
    }

    private void onAppTaskHidden() {
        Handler handler = captureHandler;
        if (handler == null || completed.get()) {
            return;
        }
        handler.post(this::beginCaptureAfterTaskHidden);
    }

    private void beginCaptureAfterTaskHidden() {
        if (completed.get() || !APP_TASK_HIDDEN.get() || !captureStarted.compareAndSet(false, true)) {
            return;
        }

        cancelTaskHideTimeout();
        try {
            beginVirtualDisplayCapture();
        } catch (RuntimeException exception) {
            failCapture(messageOf(exception));
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

    private void beginVirtualDisplayCapture() {
        if (mediaProjection == null || captureHandler == null) {
            throw new IllegalStateException("Screen-capture session ended before the display became ready.");
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
            throw new IllegalStateException("Android could not create a virtual display for the capture.");
        }
    }

    private void consumeFirstImage(ImageReader reader, CaptureSize size) {
        if (completed.get()) {
            return;
        }

        Image image = reader.acquireLatestImage();
        if (image == null) {
            return;
        }

        try {
            Bitmap bitmap = bitmapFromImage(image, size.width, size.height);
            try {
                SavedCapture saved = saveBitmap(bitmap);
                finishSuccess(saved);
            } finally {
                bitmap.recycle();
            }
        } catch (IOException | RuntimeException exception) {
            failCapture(messageOf(exception));
        } finally {
            image.close();
        }
    }

    private static Bitmap bitmapFromImage(Image image, int width, int height) {
        Image.Plane plane = image.getPlanes()[0];
        ByteBuffer buffer = plane.getBuffer();
        int pixelStride = plane.getPixelStride();
        int rowStride = plane.getRowStride();
        int rowPadding = rowStride - pixelStride * width;
        int paddedWidth = width + Math.max(0, rowPadding / pixelStride);

        Bitmap padded = Bitmap.createBitmap(paddedWidth, height, Bitmap.Config.ARGB_8888);
        padded.copyPixelsFromBuffer(buffer);
        if (paddedWidth == width) {
            return padded;
        }

        Bitmap cropped = Bitmap.createBitmap(padded, 0, 0, width, height);
        padded.recycle();
        return cropped;
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
            throw new IOException("Android MediaStore did not create a destination for the PNG.");
        }

        boolean committed = false;
        try {
            try (OutputStream output = resolver.openOutputStream(uri, "w")) {
                if (output == null || !bitmap.compress(Bitmap.CompressFormat.PNG, 100, output)) {
                    throw new IOException("The PNG stream could not be written.");
                }
                output.flush();
            }

            ContentValues complete = new ContentValues();
            complete.put(MediaStore.Images.Media.IS_PENDING, 0);
            int updated = resolver.update(uri, complete, null, null);
            if (updated != 1) {
                throw new IOException("Android MediaStore did not finalize the PNG destination.");
            }
            committed = true;
            return new SavedCapture(uri, name);
        } finally {
            if (!committed) {
                resolver.delete(uri, null, null);
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
        sendBroadcast(result);

        cleanupCapture(true);
        releaseCaptureOwnership();
        stopForeground(STOP_FOREGROUND_REMOVE);
        stopSelf();
    }

    private void failCapture(String message) {
        if (!completed.compareAndSet(false, true)) {
            return;
        }

        lastError = message;
        Intent result = new Intent(ACTION_CAPTURE_FAILED)
            .setPackage(getPackageName())
            .putExtra(EXTRA_ERROR_MESSAGE, message);
        sendBroadcast(result);

        cleanupCapture(true);
        releaseCaptureOwnership();
        stopForeground(STOP_FOREGROUND_REMOVE);
        stopSelf();
    }

    private void cleanupAfterDestroy() {
        Handler handler = captureHandler;
        if (handler != null && Looper.myLooper() != handler.getLooper()) {
            boolean posted = handler.post(() -> {
                cleanupCapture(true);
                releaseCaptureOwnership();
            });
            if (posted) {
                return;
            }
        }

        cleanupCapture(true);
        releaseCaptureOwnership();
    }

    private void cleanupCapture(boolean stopProjection) {
        cancelTaskHideTimeout();
        if (captureHandler != null) {
            captureHandler.removeCallbacksAndMessages(null);
        }
        if (virtualDisplay != null) {
            virtualDisplay.release();
            virtualDisplay = null;
        }
        if (imageReader != null) {
            imageReader.setOnImageAvailableListener(null, null);
            imageReader.close();
            imageReader = null;
        }
        if (mediaProjection != null) {
            if (projectionCallback != null) {
                mediaProjection.unregisterCallback(projectionCallback);
                projectionCallback = null;
            }
            if (stopProjection) {
                mediaProjection.stop();
            }
            mediaProjection = null;
        }
        captureHandler = null;
        if (captureThread != null) {
            captureThread.quitSafely();
            captureThread = null;
        }
    }

    private void releaseCaptureOwnership() {
        if (activeService == this) {
            activeService = null;
            APP_TASK_HIDDEN.set(false);
        }
        CAPTURE_ACTIVE.set(false);
    }

    @SuppressLint("deprecation") // Android 10 fallback; API 30+ uses WindowMetrics above.
    @SuppressWarnings("deprecation")
    private CaptureSize getCaptureSize() {
        WindowManager windowManager = getSystemService(WindowManager.class);
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

        if (width <= 0 || height <= 0) {
            throw new IllegalStateException("Android reported an invalid display size.");
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
        NotificationChannel channel = new NotificationChannel(
            CHANNEL_ID,
            getString(R.string.notification_channel_name),
            NotificationManager.IMPORTANCE_LOW);
        channel.setDescription(getString(R.string.notification_channel_description));
        getSystemService(NotificationManager.class).createNotificationChannel(channel);
    }

    @SuppressWarnings("deprecation")
    private static Intent getProjectionData(Intent source) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            return source.getParcelableExtra(EXTRA_RESULT_DATA, Intent.class);
        }
        return source.getParcelableExtra(EXTRA_RESULT_DATA);
    }

    private static String messageOf(Throwable throwable) {
        String message = throwable.getMessage();
        return message == null || message.isBlank()
            ? throwable.getClass().getSimpleName()
            : message;
    }

    private record CaptureSize(int width, int height, int densityDpi) { }

    private record SavedCapture(Uri uri, String name) { }
}
