<?php
declare(strict_types=1);

require __DIR__ . '/../lib/app.php';

snap_security_headers();
snap_apply_cors();

if (($_SERVER['REQUEST_METHOD'] ?? '') === 'OPTIONS') {
    header('Access-Control-Allow-Headers: Content-Type, Accept, X-Requested-With');
    http_response_code(204);
    exit;
}

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST, OPTIONS');
    snap_json_response(['ok' => false, 'error' => 'method_not_allowed'], 405);
}

if (!snap_verify_post_origin()) {
    snap_json_response(['ok' => false, 'error' => 'origin_not_allowed'], 403);
}

if (!snap_rate_limit_ok(snap_ip_hash())) {
    snap_json_response(['ok' => false, 'error' => 'too_many_requests'], 429);
}

$packageId = trim((string) ($_POST['package'] ?? ''));
$email = strtolower(trim((string) ($_POST['email'] ?? '')));
$lang = strtolower(trim((string) ($_POST['lang'] ?? 'en')));
$honeypot = trim((string) ($_POST['website'] ?? ''));

if ($honeypot !== '') {
    snap_json_response(['ok' => false, 'error' => 'invalid_request'], 422);
}

$package = snap_package($packageId);
if ($package === null) {
    snap_json_response(['ok' => false, 'error' => 'invalid_package'], 422);
}

if (!snap_email_valid($email)) {
    snap_json_response(['ok' => false, 'error' => 'invalid_email'], 422);
}

$leadId = bin2hex(random_bytes(16));
$lead = [
    'lead_id' => $leadId,
    'created_at' => gmdate(DATE_ATOM),
    'email' => $email,
    'package' => $packageId,
    'file' => (string) $package['file'],
    'version' => (string) snap_config()['version'],
    'lang' => in_array($lang, ['hr', 'en'], true) ? $lang : 'en',
    'ip_hash' => snap_ip_hash(),
    'user_agent' => snap_safe_text((string) ($_SERVER['HTTP_USER_AGENT'] ?? ''), 500),
    'referer' => snap_safe_text((string) ($_SERVER['HTTP_REFERER'] ?? ''), 500),
    'origin' => snap_safe_text(snap_origin_value(), 250),
];

snap_append_jsonl('download-leads.jsonl', $lead);
$token = snap_issue_token($leadId, $packageId);
$url = '/api/file.php?package=' . rawurlencode($packageId) . '&token=' . rawurlencode($token);

$wantsJson =
    str_contains(strtolower((string) ($_SERVER['HTTP_ACCEPT'] ?? '')), 'application/json')
    || strtolower((string) ($_SERVER['HTTP_X_REQUESTED_WITH'] ?? '')) === 'xmlhttprequest'
    || (string) ($_POST['_format'] ?? '') === 'json';

if (!$wantsJson) {
    http_response_code(303);
    header('Location: ' . $url);
    exit;
}

snap_json_response([
    'ok' => true,
    'download_url' => $url,
    'expires_in' => (int) snap_config()['token_ttl'],
]);
