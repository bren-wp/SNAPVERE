package com.snapvere.android;

import android.app.Activity;
import android.app.ActivityNotFoundException;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.media.projection.MediaProjectionManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

public final class MainActivity extends Activity {
    private static final int REQUEST_SCREEN_CAPTURE = 1001;
    private static final String PREFS = "snapvere_android";
    private static final String PREF_LATEST_URI = "latest_capture_uri";
    private static final String PREF_LATEST_NAME = "latest_capture_name";

    private TextView statusText;
    private Button openLatestButton;
    private Button shareLatestButton;
    private boolean receiverRegistered;

    private final BroadcastReceiver captureReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            if (CaptureService.ACTION_CAPTURE_COMPLETED.equals(intent.getAction())) {
                String name = intent.getStringExtra(CaptureService.EXTRA_CAPTURE_NAME);
                statusText.setText(getString(R.string.capture_saved, name == null ? "PNG" : name));
                refreshLatestState();
            } else if (CaptureService.ACTION_CAPTURE_FAILED.equals(intent.getAction())) {
                String message = intent.getStringExtra(CaptureService.EXTRA_ERROR_MESSAGE);
                statusText.setText(getString(R.string.capture_failed, message == null ? "Unknown error" : message));
            }
        }
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(Color.rgb(7, 8, 13));
        getWindow().setNavigationBarColor(Color.rgb(7, 8, 13));
        setContentView(buildContent());
        refreshLatestState();
    }

    @Override
    protected void onStart() {
        super.onStart();
        registerCaptureReceiver();
        refreshLatestState();
    }

    @Override
    protected void onStop() {
        if (receiverRegistered) {
            unregisterReceiver(captureReceiver);
            receiverRegistered = false;
        }
        super.onStop();
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQUEST_SCREEN_CAPTURE) {
            return;
        }

        if (resultCode != RESULT_OK || data == null) {
            statusText.setText(R.string.capture_cancelled);
            return;
        }

        Intent serviceIntent = new Intent(this, CaptureService.class)
            .setAction(CaptureService.ACTION_CAPTURE_ONCE)
            .putExtra(CaptureService.EXTRA_RESULT_CODE, resultCode)
            .putExtra(CaptureService.EXTRA_RESULT_DATA, data);

        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(serviceIntent);
            } else {
                startService(serviceIntent);
            }
        } catch (RuntimeException exception) {
            statusText.setText(getString(R.string.capture_failed, exception.getMessage()));
        }
    }

    private View buildContent() {
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setBackgroundColor(Color.rgb(7, 8, 13));

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(dp(24), dp(30), dp(24), dp(30));
        scroll.addView(root, new ScrollView.LayoutParams(
            ScrollView.LayoutParams.MATCH_PARENT,
            ScrollView.LayoutParams.WRAP_CONTENT));

        TextView brand = text("SNAPVERE", 28, 0xFFF6F5FB, true);
        root.addView(brand);
        TextView tagline = text(getString(R.string.tagline), 14, 0xFF8E96AA, false);
        tagline.setPadding(0, dp(2), 0, dp(24));
        root.addView(tagline);

        LinearLayout privacyCard = card();
        TextView localFirst = text(getString(R.string.local_first), 11, 0xFF72D8B4, true);
        privacyCard.addView(localFirst);
        TextView privacy = text(getString(R.string.local_first_description), 14, 0xFFB8C0D1, false);
        privacy.setPadding(0, dp(6), 0, 0);
        privacyCard.addView(privacy);
        root.addView(privacyCard, marginBottom(dp(18)));

        LinearLayout captureCard = card();
        captureCard.addView(text(getString(R.string.capture_screen), 21, 0xFFF6F5FB, true));
        TextView captureDescription = text(getString(R.string.capture_screen_description), 13, 0xFFAEB5C6, false);
        captureDescription.setPadding(0, dp(6), 0, dp(14));
        captureCard.addView(captureDescription);

        Button captureButton = actionButton(getString(R.string.capture_screen), true);
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
        statusText.setText(R.string.capture_requesting);
        MediaProjectionManager manager =
            (MediaProjectionManager) getSystemService(Context.MEDIA_PROJECTION_SERVICE);
        startActivityForResult(manager.createScreenCaptureIntent(), REQUEST_SCREEN_CAPTURE);
    }

    private void refreshLatestState() {
        if (openLatestButton == null || shareLatestButton == null) {
            return;
        }
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        boolean hasLatest = !prefs.getString(PREF_LATEST_URI, "").isBlank();
        openLatestButton.setEnabled(hasLatest);
        shareLatestButton.setEnabled(hasLatest);
        openLatestButton.setAlpha(hasLatest ? 1.0f : 0.5f);
        shareLatestButton.setAlpha(hasLatest ? 1.0f : 0.5f);
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
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            registerReceiver(captureReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
        } else {
            registerReceiver(captureReceiver, filter);
        }
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
