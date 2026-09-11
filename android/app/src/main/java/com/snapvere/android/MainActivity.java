package com.snapvere.android;

import android.content.ActivityNotFoundException;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.graphics.drawable.GradientDrawable;
import android.media.projection.MediaProjectionManager;
import android.net.Uri;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import androidx.activity.ComponentActivity;
import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.core.content.ContextCompat;
import androidx.core.graphics.Insets;
import androidx.core.view.ViewCompat;
import androidx.core.view.WindowInsetsCompat;

public final class MainActivity extends ComponentActivity {
    private static final String PREFS = "snapvere_android";
    private static final String PREF_LATEST_URI = "latest_capture_uri";
    private static final String PREF_LATEST_NAME = "latest_capture_name";

    private TextView statusText;
    private Button captureButton;
    private Button openLatestButton;
    private Button shareLatestButton;
    private boolean receiverRegistered;
    private boolean captureTaskHidePending;
    private ActivityResultLauncher<Intent> captureLauncher;

    private final BroadcastReceiver captureReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            refreshCaptureState();
            if (CaptureService.ACTION_CAPTURE_COMPLETED.equals(intent.getAction())) {
                String name = intent.getStringExtra(CaptureService.EXTRA_CAPTURE_NAME);
                statusText.setText(getString(R.string.capture_saved, name == null ? "PNG" : name));
                refreshLatestState();
            } else if (CaptureService.ACTION_CAPTURE_FAILED.equals(intent.getAction())) {
                String message = intent.getStringExtra(CaptureService.EXTRA_ERROR_MESSAGE);
                statusText.setText(getString(
                    R.string.capture_failed,
                    message == null || message.isBlank() ? getString(R.string.unknown_error) : message));
            }
        }
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        captureLauncher = registerForActivityResult(
            new ActivityResultContracts.StartActivityForResult(),
            result -> handleCaptureResult(result.getResultCode(), result.getData()));

        setContentView(buildContent());
        refreshCaptureState();
        refreshLatestState();
    }

    @Override
    protected void onStart() {
        super.onStart();
        registerCaptureReceiver();
        refreshCaptureState();
        refreshLatestState();
        refreshStatusFromLastCapture();
    }

    @Override
    protected void onStop() {
        if (captureTaskHidePending) {
            captureTaskHidePending = false;
            CaptureService.notifyAppTaskHidden();
        }
        if (receiverRegistered) {
            unregisterReceiver(captureReceiver);
            receiverRegistered = false;
        }
        super.onStop();
    }

    private View buildContent() {
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setBackgroundColor(0xFF07080D);
        ViewCompat.setOnApplyWindowInsetsListener(scroll, (view, windowInsets) -> {
            Insets bars = windowInsets.getInsets(WindowInsetsCompat.Type.systemBars());
            view.setPadding(bars.left, bars.top, bars.right, bars.bottom);
            return windowInsets;
        });

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(dp(24), dp(30), dp(24), dp(30));
        scroll.addView(root, new ScrollView.LayoutParams(
            ScrollView.LayoutParams.MATCH_PARENT,
            ScrollView.LayoutParams.WRAP_CONTENT));

        root.addView(text("SNAPVERE", 28, 0xFFF6F5FB, true));
        TextView tagline = text(getString(R.string.tagline), 14, 0xFF8E96AA, false);
        tagline.setPadding(0, dp(2), 0, dp(24));
        root.addView(tagline);

        LinearLayout privacyCard = card();
        privacyCard.addView(text(getString(R.string.local_first), 11, 0xFF72D8B4, true));
        TextView privacy = text(getString(R.string.local_first_description), 14, 0xFFB8C0D1, false);
        privacy.setPadding(0, dp(6), 0, 0);
        privacyCard.addView(privacy);
        root.addView(privacyCard, marginBottom(dp(18)));

        LinearLayout captureCard = card();
        captureCard.addView(text(getString(R.string.capture_screen), 21, 0xFFF6F5FB, true));
        TextView captureDescription = text(getString(R.string.capture_screen_description), 13, 0xFFAEB5C6, false);
        captureDescription.setPadding(0, dp(6), 0, dp(14));
        captureCard.addView(captureDescription);

        captureButton = actionButton(getString(R.string.capture_screen), true);
        captureButton.setOnClickListener(view -> requestScreenCapture());
        captureCard.addView(captureButton);
        root.addView(captureCard, marginBottom(dp(14)));

        LinearLayout recentCard = card();
        openLatestButton = actionButton(getString(R.string.open_latest), false);
        openLatestButton.setOnClickListener(view -> openLatestCapture());
        recentCard.addView(openLatestButton);

        shareLatestButton = actionButton(getString(R.string.share_latest), false);
        LinearLayout.LayoutParams shareParams = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT);
        shareParams.topMargin = dp(8);
        shareLatestButton.setLayoutParams(shareParams);
        shareLatestButton.setOnClickListener(view -> shareLatestCapture());
        recentCard.addView(shareLatestButton);
        root.addView(recentCard, marginBottom(dp(14)));

        statusText = text(getString(R.string.capture_ready), 13, 0xFF72D8B4, false);
        statusText.setPadding(dp(4), dp(4), dp(4), dp(14));
        statusText.setAccessibilityLiveRegion(View.ACCESSIBILITY_LIVE_REGION_POLITE);
        root.addView(statusText);

        TextView privacyNote = text(getString(R.string.privacy_note), 12, 0xFF7D879E, false);
        privacyNote.setPadding(dp(4), dp(4), dp(4), 0);
        root.addView(privacyNote);

        return scroll;
    }

    private void requestScreenCapture() {
        if (CaptureService.isCaptureActive()) {
            refreshCaptureState();
            return;
        }

        CaptureService.clearLastError();
        captureButton.setEnabled(false);
        captureButton.setAlpha(0.65f);
        statusText.setText(R.string.capture_requesting);
        MediaProjectionManager manager =
            (MediaProjectionManager) getSystemService(Context.MEDIA_PROJECTION_SERVICE);
        captureLauncher.launch(manager.createScreenCaptureIntent());
    }

    private void handleCaptureResult(int resultCode, Intent data) {
        if (resultCode != RESULT_OK || data == null) {
            refreshCaptureState();
            statusText.setText(R.string.capture_cancelled);
            return;
        }

        Intent serviceIntent = new Intent(this, CaptureService.class)
            .setAction(CaptureService.ACTION_CAPTURE_ONCE)
            .putExtra(CaptureService.EXTRA_RESULT_CODE, resultCode)
            .putExtra(CaptureService.EXTRA_RESULT_DATA, data);

        CaptureService.prepareForCaptureHandoff();
        captureTaskHidePending = true;
        try {
            ContextCompat.startForegroundService(this, serviceIntent);
            if (!moveTaskToBack(true)) {
                captureTaskHidePending = false;
                CaptureService.cancelCaptureHandoff();
                stopService(new Intent(this, CaptureService.class));
                refreshCaptureState();
                statusText.setText(R.string.capture_background_failed);
            }
        } catch (RuntimeException exception) {
            captureTaskHidePending = false;
            CaptureService.cancelCaptureHandoff();
            refreshCaptureState();
            String message = exception.getMessage();
            statusText.setText(getString(
                R.string.capture_failed,
                message == null || message.isBlank() ? getString(R.string.unknown_error) : message));
        }
    }

    private void refreshCaptureState() {
        if (captureButton == null) {
            return;
        }
        boolean active = CaptureService.isCaptureActive();
        captureButton.setEnabled(!active);
        captureButton.setAlpha(active ? 0.65f : 1.0f);
    }

    private void refreshLatestState() {
        if (openLatestButton == null || shareLatestButton == null) {
            return;
        }
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String latest = prefs.getString(PREF_LATEST_URI, "");
        boolean hasLatest = latest != null && !latest.isBlank();
        openLatestButton.setEnabled(hasLatest);
        shareLatestButton.setEnabled(hasLatest);
        openLatestButton.setAlpha(hasLatest ? 1.0f : 0.5f);
        shareLatestButton.setAlpha(hasLatest ? 1.0f : 0.5f);
    }

    private void refreshStatusFromLastCapture() {
        if (statusText == null || CaptureService.isCaptureActive()) {
            return;
        }

        String lastError = CaptureService.getLastError();
        if (lastError != null && !lastError.isBlank()) {
            statusText.setText(getString(R.string.capture_failed, lastError));
            return;
        }

        String latestName = getSharedPreferences(PREFS, MODE_PRIVATE)
            .getString(PREF_LATEST_NAME, "");
        if (latestName != null && !latestName.isBlank()) {
            statusText.setText(getString(R.string.capture_saved, latestName));
        }
    }

    private void openLatestCapture() {
        Uri uri = getLatestCaptureUri();
        if (uri == null) {
            statusText.setText(R.string.no_capture);
            return;
        }

        Intent viewIntent = new Intent(Intent.ACTION_VIEW)
            .setDataAndType(uri, "image/png")
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try {
            startActivity(viewIntent);
        } catch (ActivityNotFoundException | SecurityException exception) {
            statusText.setText(R.string.open_failed);
        }
    }

    private void shareLatestCapture() {
        Uri uri = getLatestCaptureUri();
        if (uri == null) {
            statusText.setText(R.string.no_capture);
            return;
        }

        Intent share = new Intent(Intent.ACTION_SEND)
            .setType("image/png")
            .putExtra(Intent.EXTRA_STREAM, uri)
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try {
            startActivity(Intent.createChooser(share, getString(R.string.share_latest)));
        } catch (ActivityNotFoundException | SecurityException exception) {
            statusText.setText(R.string.share_failed);
        }
    }

    private Uri getLatestCaptureUri() {
        String value = getSharedPreferences(PREFS, MODE_PRIVATE)
            .getString(PREF_LATEST_URI, "");
        return value == null || value.isBlank() ? null : Uri.parse(value);
    }

    private void registerCaptureReceiver() {
        if (receiverRegistered) {
            return;
        }
        IntentFilter filter = new IntentFilter();
        filter.addAction(CaptureService.ACTION_CAPTURE_COMPLETED);
        filter.addAction(CaptureService.ACTION_CAPTURE_FAILED);
        ContextCompat.registerReceiver(
            this,
            captureReceiver,
            filter,
            ContextCompat.RECEIVER_NOT_EXPORTED);
        receiverRegistered = true;
    }

    private LinearLayout card() {
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        layout.setPadding(dp(18), dp(18), dp(18), dp(18));
        GradientDrawable background = new GradientDrawable();
        background.setColor(0xFF0F1320);
        background.setCornerRadius(dp(18));
        background.setStroke(dp(1), 0xFF303A50);
        layout.setBackground(background);
        return layout;
    }

    private Button actionButton(String label, boolean primary) {
        Button button = new Button(this);
        button.setText(label);
        button.setTextSize(14);
        button.setTextColor(0xFFF6F5FB);
        button.setAllCaps(false);
        button.setGravity(Gravity.CENTER);
        button.setPadding(dp(16), dp(10), dp(16), dp(10));
        GradientDrawable background = new GradientDrawable();
        background.setColor(primary ? 0xFF6847D8 : 0xFF171C2A);
        background.setCornerRadius(dp(12));
        background.setStroke(dp(1), primary ? 0xFF9D86FF : 0xFF344057);
        button.setBackground(background);
        return button;
    }

    private TextView text(String value, float sizeSp, int color, boolean bold) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(sizeSp);
        view.setTextColor(color);
        view.setLineSpacing(0, 1.12f);
        if (bold) {
            view.setTypeface(view.getTypeface(), android.graphics.Typeface.BOLD);
        }
        return view;
    }

    private LinearLayout.LayoutParams marginBottom(int marginBottom) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT);
        params.bottomMargin = marginBottom;
        return params;
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }
}
