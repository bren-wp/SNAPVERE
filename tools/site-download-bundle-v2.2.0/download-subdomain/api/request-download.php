<?php
declare(strict_types=1);
require __DIR__ . '/bootstrap.php';

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    snapvere_json(['ok' => false, 'error' => 'method_not_allowed'], 405);
}

if (!snapvere_origin_allowed()) {
    snapvere_json(['ok' => false, 'error' => 'origin_rejected'], 403);
}

if (!snapvere_validate_csrf((string) ($_POST['csrf'] ?? ''))) {
    snapvere_json(['ok' => false, 'error' => 'csrf_rejected'], 403);
}

if (trim((string) ($_POST['website'] ?? '')) !== '') {
    snapvere_json(['ok' => true, 'message' => 'Verification email sent.']);
}

$email = snapvere_normalize_email((string) ($_POST['email'] ?? ''));
$packageId = trim((string) ($_POST['package'] ?? ''));

if (snapvere_email_is_disposable_or_invalid($email)) {
    snapvere_json(['ok' => false, 'error' => 'email_invalid_or_disposable'], 422);
}

$package = snapvere_package($packageId);
if ($package === null) {
    snapvere_json(['ok' => false, 'error' => 'package_invalid'], 422);
}

if (!snapvere_rate_limit($email)) {
    snapvere_json(['ok' => false, 'error' => 'rate_limited'], 429);
}

$integrity = snapvere_verify_package($package);
if (($integrity['ok'] ?? false) !== true) {
    snapvere_json(['ok' => false, 'error' => 'package_unavailable'], 503);
}

$token = snapvere_random_token();
$tokenHash = snapvere_token_hash($token);
$now = time();
$expires = $now + (int) (snapvere_config()['verification_ttl'] ?? 900);

snapvere_mutate_json('verify-tokens.json', function (&$state) use ($tokenHash, $email, $packageId, $now, $expires) {
    snapvere_cleanup_tokens($state);
    $state[$tokenHash] = [
        'email' => $email,
        'package' => $packageId,
        'created_at' => $now,
        'expires_at' => $expires,
        'used' => false,
        'ip_hash' => snapvere_hash_private(snapvere_client_ip()),
        'ua_hash' => snapvere_hash_private(snapvere_user_agent()),
    ];
    return true;
});

$cfg = snapvere_config();
$link = rtrim((string) $cfg['site_url'], '/') . '/api/verify.php?t=' . rawurlencode($token);
$label = (string) ($package['label'] ?? $packageId);
$filename = (string) ($package['filename'] ?? '');

$body = "Confirm your SNAPVERE download\n\n"
    . "Package: {$label}\n"
    . "File: {$filename}\n"
    . "Version: " . $cfg['product_version'] . "\n\n"
    . "Open this link to confirm the email address and start the protected download:\n{$link}\n\n"
    . "The link expires in 15 minutes. If you did not request this download, ignore this message.\n";

if (!snapvere_send_mail($email, 'Confirm your SNAPVERE download', $body)) {
    snapvere_mutate_json('verify-tokens.json', function (&$state) use ($tokenHash) {
        unset($state[$tokenHash]);
        return true;
    });
    snapvere_json(['ok' => false, 'error' => 'verification_mail_failed'], 503);
}

snapvere_append_jsonl('download-leads.jsonl', [
    'time' => gmdate('c'),
    'email' => $email,
    'package' => $packageId,
    'filename' => $filename,
    'ip_hash' => snapvere_hash_private(snapvere_client_ip()),
    'ua_hash' => snapvere_hash_private(snapvere_user_agent()),
]);

snapvere_json([
    'ok' => true,
    'message' => 'Verification email sent. Open the link in your email to continue.',
]);
