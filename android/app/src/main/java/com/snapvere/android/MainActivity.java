package com.snapvere.android;

import android.app.AlertDialog;
import android.content.BroadcastReceiver;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.content.res.ColorStateList;
import android.content.res.Configuration;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.RippleDrawable;
import android.media.projection.MediaProjectionManager;
import android.net.Uri;
import android.os.Bundle;
import android.os.ParcelFileDescriptor;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.ImageView;
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

import java.io.IOException;

public final class MainActivity extends ComponentActivity {
    private static final String PREFS = "snapvere_android";
    private static final String PREF_LATEST_URI = "latest_capture_uri";
    private static final String PREF_LATEST_NAME = "latest_capture_name";

    private static final String PRODUCT_WEBSITE = "https://snapvere.com";
    private static final String PRIVACY_URL = "https://snapvere.com/privacy";
    private static final String TERMS_URL = "https://snapvere.com/terms";
    private static final String SUPPORT_EMAIL = "info@snapvere.com";

    private TextView statusText;
    private TextView latestNameText;
    private TextView latestDetailText;
    private Button captureButton;
    private Button openLatestButton;
    private Button shareLatestButton;
    private Button deleteLatestButton;
    private boolean receiverRegistered;
    private boolean captureTaskHidePending;
    private ActivityResultLauncher<Intent> captureLauncher;

    private final BroadcastReceiver captureReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            refreshCaptureState();
            if (CaptureService.ACTION_CAPTURE_COMPLETED.equals(intent.getAction())) {
                String name = intent.getStringExtra(CaptureService.EXTRA_CAPTURE_NAME);
                showStatus(
                    getString(R.string.capture_saved, name == null ? "PNG" : name),
                    R.color.snapvere_success);
                refreshLatestState();
            } else if (CaptureService.ACTION_CAPTURE_FAILED.equals(intent.getAction())) {
                String message = intent.getStringExtra(CaptureService.EXTRA_ERROR_MESSAGE);
                showStatus(
                    getString(
                        R.string.capture_failed,
                        message == null || message.isBlank()
                            ? getString(R.string.unknown_error)
                            : message),
                    R.color.snapvere_danger);
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
            try {
                unregisterReceiver(captureReceiver);
            } catch (IllegalArgumentException ignored) {
                // Android may already have removed the receiver during teardown.
            }
            receiverRegistered = false;
        }
        super.onStop();
    }

    private View buildContent() {
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setClipToPadding(false);
        scroll.setVerticalScrollBarEnabled(false);
        scroll.setBackgroundColor(getColor(R.color.snapvere_canvas));
        ViewCompat.setOnApplyWindowInsetsListener(scroll, (view, windowInsets) -> {
            Insets bars = windowInsets.getInsets(WindowInsetsCompat.Type.systemBars());
            view.setPadding(bars.left, bars.top, bars.right, bars.bottom);
            return windowInsets;
        });

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        int horizontalPadding = getResources().getConfiguration().smallestScreenWidthDp >= 600
            ? dp(48)
            : dp(20);
        root.setPadding(horizontalPadding, dp(24), horizontalPadding, dp(32));
        scroll.addView(root, new ScrollView.LayoutParams(
            ScrollView.LayoutParams.MATCH_PARENT,
            ScrollView.LayoutParams.WRAP_CONTENT));

        root.addView(buildHeader(), marginBottom(dp(22)));
        root.addView(buildCaptureCard(), marginBottom(dp(14)));
        root.addView(buildLatestCard(), marginBottom(dp(14)));
        root.addView(buildPrivacyCard(), marginBottom(dp(14)));
        root.addView(buildAboutCard(), marginBottom(dp(18)));

        TextView footer = text(
            getString(R.string.version_format, resolveVersionName()),
            11,
            R.color.snapvere_text_muted,
            false);
        footer.setGravity(Gravity.CENTER);
        footer.setPadding(dp(4), dp(4), dp(4), 0);
        root.addView(footer);

        return scroll;
    }

    private View buildHeader() {
        LinearLayout header = new LinearLayout(this);
        header.setOrientation(LinearLayout.HORIZONTAL);
        header.setGravity(Gravity.CENTER_VERTICAL);

        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.ic_snapvere);
        logo.setContentDescription(getString(R.string.app_name));
        LinearLayout.LayoutParams logoParams = new LinearLayout.LayoutParams(dp(56), dp(56));
        logoParams.setMarginEnd(dp(14));
        header.addView(logo, logoParams);

        LinearLayout copy = new LinearLayout(this);
        copy.setOrientation(LinearLayout.VERTICAL);
        copy.setGravity(Gravity.START);
        copy.addView(text(getString(R.string.app_name), 27, R.color.snapvere_text_primary, true));

        TextView tagline = text(getString(R.string.tagline), 13, R.color.snapvere_text_secondary, false);
        tagline.setPadding(0, dp(1), 0, dp(8));
        copy.addView(tagline);
        copy.addView(badge(getString(R.string.android_local_badge)));

        header.addView(copy, new LinearLayout.LayoutParams(
            0,
            LinearLayout.LayoutParams.WRAP_CONTENT,
            1f));
        return header;
    }

    private View buildCaptureCard() {
        LinearLayout card = card(true);
        card.addView(sectionLabel(getString(R.string.capture_section), R.color.snapvere_cyan));

        TextView title = text(getString(R.string.capture_screen), 22, R.color.snapvere_text_primary, true);
        title.setPadding(0, dp(9), 0, 0);
        card.addView(title);

        TextView description = text(
            getString(R.string.capture_screen_description),
            13,
            R.color.snapvere_text_secondary,
            false);
        description.setPadding(0, dp(7), 0, dp(14));
        card.addView(description);

        LinearLayout statusPanel = new LinearLayout(this);
        statusPanel.setOrientation(LinearLayout.VERTICAL);
        statusPanel.setPadding(dp(14), dp(12), dp(14), dp(12));
        statusPanel.setBackground(shape(
            R.color.snapvere_surface_high,
            R.color.snapvere_border,
            1,
            12));

        statusText = text(getString(R.string.capture_ready), 13, R.color.snapvere_success, true);
        statusText.setAccessibilityLiveRegion(View.ACCESSIBILITY_LIVE_REGION_POLITE);
        statusPanel.addView(statusText);
        card.addView(statusPanel, marginBottom(dp(12)));

        captureButton = actionButton(getString(R.string.capture_screen), ButtonStyle.PRIMARY);
        captureButton.setOnClickListener(view -> requestScreenCapture());
        card.addView(captureButton);

        TextView consent = text(
            getString(R.string.capture_consent_note),
            12,
            R.color.snapvere_text_muted,
            false);
        consent.setPadding(dp(2), dp(10), dp(2), 0);
        card.addView(consent);
        return card;
    }

    private View buildLatestCard() {
        LinearLayout card = card(false);
        card.addView(sectionLabel(getString(R.string.latest_section), R.color.snapvere_accent_strong));

        latestNameText = text(getString(R.string.latest_empty), 18, R.color.snapvere_text_primary, true);
        latestNameText.setPadding(0, dp(9), 0, 0);
        card.addView(latestNameText);

        latestDetailText = text(
            getString(R.string.latest_empty_description),
            13,
            R.color.snapvere_text_muted,
            false);
        latestDetailText.setPadding(0, dp(5), 0, dp(14));
        card.addView(latestDetailText);

        openLatestButton = actionButton(getString(R.string.open_latest), ButtonStyle.SECONDARY);
        openLatestButton.setOnClickListener(view -> openLatestCapture());
        shareLatestButton = actionButton(getString(R.string.share_latest), ButtonStyle.SECONDARY);
        shareLatestButton.setOnClickListener(view -> shareLatestCapture());
        card.addView(buildActionPair(openLatestButton, shareLatestButton));

        deleteLatestButton = actionButton(getString(R.string.delete_latest), ButtonStyle.DESTRUCTIVE);
        LinearLayout.LayoutParams deleteParams = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT);
        deleteParams.topMargin = dp(8);
        deleteLatestButton.setLayoutParams(deleteParams);
        deleteLatestButton.setOnClickListener(view -> confirmDeleteLatestCapture());
        card.addView(deleteLatestButton);
        return card;
    }

    private View buildPrivacyCard() {
        LinearLayout card = card(false);
        card.addView(sectionLabel(getString(R.string.privacy_section), R.color.snapvere_success));

        TextView title = text(getString(R.string.local_first), 18, R.color.snapvere_text_primary, true);
        title.setPadding(0, dp(9), 0, 0);
        card.addView(title);

        TextView description = text(
            getString(R.string.local_first_description),
            13,
            R.color.snapvere_text_secondary,
            false);
        description.setPadding(0, dp(6), 0, dp(10));
        card.addView(description);

        TextView note = text(getString(R.string.privacy_note), 12, R.color.snapvere_text_muted, false);
        card.addView(note);
        return card;
    }

    private View buildAboutCard() {
        LinearLayout card = card(false);
        card.addView(sectionLabel(getString(R.string.about_section), R.color.snapvere_cyan));

        TextView description = text(
            getString(R.string.about_description),
            13,
            R.color.snapvere_text_secondary,
            false);
        description.setPadding(0, dp(9), 0, dp(14));
        card.addView(description);

        Button website = actionButton(getString(R.string.website), ButtonStyle.TERTIARY);
        website.setOnClickListener(view -> openExternal(PRODUCT_WEBSITE));
        Button support = actionButton(getString(R.string.support), ButtonStyle.TERTIARY);
        support.setOnClickListener(view -> openSupport());
        card.addView(buildActionPair(website, support));

        Button privacy = actionButton(getString(R.string.privacy_policy), ButtonStyle.TERTIARY);
        privacy.setOnClickListener(view -> openExternal(PRIVACY_URL));
        Button terms = actionButton(getString(R.string.terms_of_use), ButtonStyle.TERTIARY);
        terms.setOnClickListener(view -> openExternal(TERMS_URL));
        LinearLayout secondRow = buildActionPair(privacy, terms);
        LinearLayout.LayoutParams secondRowParams = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT);
        secondRowParams.topMargin = dp(8);
        card.addView(secondRow, secondRowParams);
        return card;
    }

    private LinearLayout buildActionPair(Button first, Button second) {
        LinearLayout actions = new LinearLayout(this);
        boolean stacked = shouldStackActionPairs();
        actions.setOrientation(stacked ? LinearLayout.VERTICAL : LinearLayout.HORIZONTAL);

        if (stacked) {
            actions.addView(first, fullWidthButtonParams(false));
            actions.addView(second, fullWidthButtonParams(true));
        } else {
            actions.addView(first, weightedButtonParams(true));
            actions.addView(second, weightedButtonParams(false));
        }
        return actions;
    }

    private boolean shouldStackActionPairs() {
        Configuration configuration = getResources().getConfiguration();
        int widthDp = configuration.screenWidthDp;
        return (widthDp > 0 && widthDp < 360) || configuration.fontScale >= 1.25f;
    }

    private void requestScreenCapture() {
        if (CaptureService.isCaptureActive()) {
            refreshCaptureState();
            return;
        }

        CaptureService.clearLastError();
        setCaptureButtonEnabled(false);
        showStatus(getString(R.string.capture_requesting), R.color.snapvere_warning);

        try {
            MediaProjectionManager manager =
                (MediaProjectionManager) getSystemService(Context.MEDIA_PROJECTION_SERVICE);
            if (manager == null) {
                throw new IllegalStateException("Android screen-capture service is unavailable.");
            }
            captureLauncher.launch(manager.createScreenCaptureIntent());
        } catch (RuntimeException exception) {
            setCaptureButtonEnabled(true);
            showStatus(
                getString(R.string.capture_failed, messageOf(exception)),
                R.color.snapvere_danger);
        }
    }

    private void handleCaptureResult(int resultCode, Intent data) {
        if (resultCode != RESULT_OK || data == null) {
            refreshCaptureState();
            showStatus(getString(R.string.capture_cancelled), R.color.snapvere_text_secondary);
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
            showStatus(getString(R.string.capture_in_progress), R.color.snapvere_warning);
            if (!moveTaskToBack(true)) {
                captureTaskHidePending = false;
                CaptureService.cancelCaptureHandoff();
                stopService(new Intent(this, CaptureService.class));
                refreshCaptureState();
                showStatus(getString(R.string.capture_background_failed), R.color.snapvere_danger);
            }
        } catch (RuntimeException exception) {
            captureTaskHidePending = false;
            CaptureService.cancelCaptureHandoff();
            refreshCaptureState();
            showStatus(
                getString(R.string.capture_failed, messageOf(exception)),
                R.color.snapvere_danger);
        }
    }

    private void refreshCaptureState() {
        if (captureButton == null) {
            return;
        }
        boolean active = CaptureService.isCaptureActive();
        setCaptureButtonEnabled(!active);
        if (active && statusText != null) {
            showStatus(getString(R.string.capture_in_progress), R.color.snapvere_warning);
        }
    }

    private void refreshLatestState() {
        if (openLatestButton == null || shareLatestButton == null || deleteLatestButton == null) {
            return;
        }

        LatestCapture latest = getLatestCapture();
        boolean hasLatest = latest.uri != null && isReadableCapture(latest.uri);
        if (!hasLatest && latest.uri != null) {
            clearLatestCapturePreference();
        }

        if (hasLatest) {
            latestNameText.setText(latest.name == null || latest.name.isBlank() ? "PNG" : latest.name);
            latestDetailText.setText(R.string.latest_location);
        } else {
            latestNameText.setText(R.string.latest_empty);
            latestDetailText.setText(R.string.latest_empty_description);
        }

        setButtonEnabled(openLatestButton, hasLatest);
        setButtonEnabled(shareLatestButton, hasLatest);
        setButtonEnabled(deleteLatestButton, hasLatest);
    }

    private void refreshStatusFromLastCapture() {
        if (statusText == null || CaptureService.isCaptureActive()) {
            return;
        }

        String lastError = CaptureService.getLastError();
        if (lastError != null && !lastError.isBlank()) {
            showStatus(getString(R.string.capture_failed, lastError), R.color.snapvere_danger);
            return;
        }

        LatestCapture latest = getLatestCapture();
        if (latest.name != null && !latest.name.isBlank() && latest.uri != null && isReadableCapture(latest.uri)) {
            showStatus(getString(R.string.capture_saved, latest.name), R.color.snapvere_success);
        } else {
            showStatus(getString(R.string.capture_ready), R.color.snapvere_success);
        }
    }

    private void openLatestCapture() {
        LatestCapture latest = getLatestCapture();
        if (latest.uri == null || !isReadableCapture(latest.uri)) {
            clearLatestCapturePreference();
            refreshLatestState();
            showStatus(getString(R.string.no_capture), R.color.snapvere_text_secondary);
            return;
        }

        Intent viewIntent = new Intent(Intent.ACTION_VIEW)
            .setDataAndType(latest.uri, "image/png")
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try {
            startActivity(viewIntent);
        } catch (RuntimeException exception) {
            showStatus(getString(R.string.open_failed), R.color.snapvere_danger);
        }
    }

    private void shareLatestCapture() {
        LatestCapture latest = getLatestCapture();
        if (latest.uri == null || !isReadableCapture(latest.uri)) {
            clearLatestCapturePreference();
            refreshLatestState();
            showStatus(getString(R.string.no_capture), R.color.snapvere_text_secondary);
            return;
        }

        Intent share = new Intent(Intent.ACTION_SEND)
            .setType("image/png")
            .putExtra(Intent.EXTRA_STREAM, latest.uri)
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try {
            startActivity(Intent.createChooser(share, getString(R.string.share_latest)));
        } catch (RuntimeException exception) {
            showStatus(getString(R.string.share_failed), R.color.snapvere_danger);
        }
    }

    private void confirmDeleteLatestCapture() {
        LatestCapture latest = getLatestCapture();
        if (latest.uri == null || !isReadableCapture(latest.uri)) {
            clearLatestCapturePreference();
            refreshLatestState();
            showStatus(getString(R.string.no_capture), R.color.snapvere_text_secondary);
            return;
        }

        new AlertDialog.Builder(this)
            .setTitle(R.string.delete_capture_title)
            .setMessage(R.string.delete_capture_message)
            .setNegativeButton(R.string.delete_capture_cancel, null)
            .setPositiveButton(R.string.delete_capture_confirm, (dialog, which) -> deleteLatestCapture(latest.uri))
            .show();
    }

    private void deleteLatestCapture(Uri uri) {
        try {
            int deleted = getContentResolver().delete(uri, null, null);
            if (deleted > 0 || !isReadableCapture(uri)) {
                clearLatestCapturePreference();
                refreshLatestState();
                showStatus(getString(R.string.delete_capture_done), R.color.snapvere_success);
                return;
            }
        } catch (RuntimeException exception) {
            // A stale MediaStore provider/URI must not crash the application.
        }
        showStatus(getString(R.string.delete_capture_failed), R.color.snapvere_danger);
    }

    private void openExternal(String url) {
        Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse(url))
            .addCategory(Intent.CATEGORY_BROWSABLE);
        try {
            startActivity(intent);
        } catch (RuntimeException exception) {
            showStatus(getString(R.string.external_link_failed), R.color.snapvere_danger);
        }
    }

    private void openSupport() {
        Intent intent = new Intent(Intent.ACTION_SENDTO, Uri.parse("mailto:" + SUPPORT_EMAIL));
        try {
            startActivity(intent);
            return;
        } catch (RuntimeException ignored) {
        }

        try {
            ClipboardManager clipboard = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
            if (clipboard == null) {
                throw new IllegalStateException("Android clipboard service is unavailable.");
            }
            clipboard.setPrimaryClip(ClipData.newPlainText(SUPPORT_EMAIL, SUPPORT_EMAIL));
            showStatus(getString(R.string.support_copied), R.color.snapvere_success);
        } catch (RuntimeException exception) {
            showStatus(getString(R.string.support_unavailable), R.color.snapvere_danger);
        }
    }

    private LatestCapture getLatestCapture() {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE);
        String uriValue = prefs.getString(PREF_LATEST_URI, "");
        String name = prefs.getString(PREF_LATEST_NAME, "");
        Uri uri = uriValue == null || uriValue.isBlank() ? null : Uri.parse(uriValue);
        return new LatestCapture(uri, name);
    }

    private boolean isReadableCapture(Uri uri) {
        try (ParcelFileDescriptor descriptor = getContentResolver().openFileDescriptor(uri, "r")) {
            return descriptor != null;
        } catch (IOException | RuntimeException exception) {
            return false;
        }
    }

    private void clearLatestCapturePreference() {
        getSharedPreferences(PREFS, MODE_PRIVATE)
            .edit()
            .remove(PREF_LATEST_URI)
            .remove(PREF_LATEST_NAME)
            .apply();
    }

    private void registerCaptureReceiver() {
        if (receiverRegistered) {
            return;
        }
        IntentFilter filter = new IntentFilter();
        filter.addAction(CaptureService.ACTION_CAPTURE_COMPLETED);
        filter.addAction(CaptureService.ACTION_CAPTURE_FAILED);
        try {
            ContextCompat.registerReceiver(
                this,
                captureReceiver,
                filter,
                ContextCompat.RECEIVER_NOT_EXPORTED);
            receiverRegistered = true;
        } catch (RuntimeException exception) {
            receiverRegistered = false;
            showStatus(getString(R.string.capture_receiver_failed), R.color.snapvere_danger);
        }
    }

    private LinearLayout card(boolean emphasized) {
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        layout.setPadding(dp(18), dp(18), dp(18), dp(18));
        layout.setBackground(shape(
            emphasized ? R.color.snapvere_surface_high : R.color.snapvere_surface,
            emphasized ? R.color.snapvere_border_strong : R.color.snapvere_border,
            1,
            18));
        return layout;
    }

    private TextView sectionLabel(String value, int colorRes) {
        TextView label = text(value, 11, colorRes, true);
        label.setLetterSpacing(0.1f);
        return label;
    }

    private TextView badge(String value) {
        TextView badge = text(value, 10, R.color.snapvere_accent_strong, true);
        badge.setLetterSpacing(0.08f);
        badge.setPadding(dp(10), dp(5), dp(10), dp(5));
        badge.setBackground(shape(R.color.snapvere_surface_high, R.color.snapvere_border_strong, 1, 999));
        return badge;
    }

    private Button actionButton(String label, ButtonStyle style) {
        Button button = new Button(this);
        button.setText(label);
        button.setTextSize(14);
        button.setTypeface(button.getTypeface(), Typeface.BOLD);
        button.setTextColor(getColor(style.textColor));
        button.setAllCaps(false);
        button.setGravity(Gravity.CENTER);
        button.setMinHeight(dp(52));
        button.setMinWidth(0);
        button.setPadding(dp(14), dp(10), dp(14), dp(10));

        GradientDrawable content = shape(style.backgroundColor, style.borderColor, 1, 12);
        button.setBackground(new RippleDrawable(
            ColorStateList.valueOf(getColor(R.color.snapvere_ripple)),
            content,
            null));
        return button;
    }

    private GradientDrawable shape(int fillColorRes, int strokeColorRes, int strokeDp, int radiusDp) {
        GradientDrawable drawable = new GradientDrawable();
        drawable.setColor(getColor(fillColorRes));
        drawable.setCornerRadius(dp(radiusDp));
        drawable.setStroke(dp(strokeDp), getColor(strokeColorRes));
        return drawable;
    }

    private TextView text(String value, float sizeSp, int colorRes, boolean bold) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(sizeSp);
        view.setTextColor(getColor(colorRes));
        view.setLineSpacing(0, 1.14f);
        if (bold) {
            view.setTypeface(view.getTypeface(), Typeface.BOLD);
        }
        return view;
    }

    private void showStatus(String value, int colorRes) {
        if (statusText == null) {
            return;
        }
        statusText.setText(value);
        statusText.setTextColor(getColor(colorRes));
    }

    private void setCaptureButtonEnabled(boolean enabled) {
        setButtonEnabled(captureButton, enabled);
    }

    private static void setButtonEnabled(Button button, boolean enabled) {
        if (button == null) {
            return;
        }
        button.setEnabled(enabled);
        button.setAlpha(enabled ? 1.0f : 0.45f);
    }

    private LinearLayout.LayoutParams weightedButtonParams(boolean addEndMargin) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
            0,
            LinearLayout.LayoutParams.WRAP_CONTENT,
            1f);
        if (addEndMargin) {
            params.setMarginEnd(dp(8));
        }
        return params;
    }

    private LinearLayout.LayoutParams fullWidthButtonParams(boolean addTopMargin) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT);
        if (addTopMargin) {
            params.topMargin = dp(8);
        }
        return params;
    }

    private LinearLayout.LayoutParams marginBottom(int marginBottom) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT);
        params.bottomMargin = marginBottom;
        return params;
    }

    @SuppressWarnings("deprecation")
    private String resolveVersionName() {
        try {
            String value = getPackageManager().getPackageInfo(getPackageName(), 0).versionName;
            return value == null || value.isBlank() ? "0.1.0" : value;
        } catch (PackageManager.NameNotFoundException exception) {
            return "0.1.0";
        }
    }

    private String messageOf(Throwable throwable) {
        String message = throwable.getMessage();
        return message == null || message.isBlank()
            ? getString(R.string.unknown_error)
            : message;
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }

    private static final class LatestCapture {
        final Uri uri;
        final String name;

        LatestCapture(Uri uri, String name) {
            this.uri = uri;
            this.name = name;
        }
    }

    private enum ButtonStyle {
        PRIMARY(R.color.snapvere_accent, R.color.snapvere_accent_strong, R.color.snapvere_text_primary),
        SECONDARY(R.color.snapvere_surface_high, R.color.snapvere_border_strong, R.color.snapvere_text_primary),
        TERTIARY(R.color.snapvere_surface, R.color.snapvere_border, R.color.snapvere_text_secondary),
        DESTRUCTIVE(R.color.snapvere_surface, R.color.snapvere_border, R.color.snapvere_danger);

        final int backgroundColor;
        final int borderColor;
        final int textColor;

        ButtonStyle(int backgroundColor, int borderColor, int textColor) {
            this.backgroundColor = backgroundColor;
            this.borderColor = borderColor;
            this.textColor = textColor;
        }
    }
}