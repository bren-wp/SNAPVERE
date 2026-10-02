<?php
declare(strict_types=1);

const SNAP_ROOT = __DIR__ . '/..';

function snap_config(): array
{
    static $config;
    if ($config === null) {
        $config = require SNAP_ROOT . '/storage/config.php';
    }
    return $config;
}

function snap_security_headers(bool $html = false): void
{
    header_remove('X-Powered-By');
    header('X-Content-Type-Options: nosniff');
    header('Referrer-Policy: strict-origin-when-cross-origin');
    header('Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()');
    header('Cross-Origin-Resource-Policy: same-site');
    header('X-Frame-Options: DENY');
    if ($html) {
        header("Content-Security-Policy: default-src 'self'; base-uri 'self'; form-action 'self' https://download.snapvere.com; frame-ancestors 'none'; img-src 'self' data:; style-src 'self'; script-src 'self'; connect-src 'self' https://download.snapvere.com");
    }
}

function snap_data_path(string $name): string
{
    return SNAP_ROOT . '/storage/data/' . $name;
}

function snap_package_path(string $filename): string
{
    return SNAP_ROOT . '/storage/packages/' . basename($filename);
}

function snap_b64url_encode(string $value): string
{
    return rtrim(strtr(base64_encode($value), '+/', '-_'), '=');
}

function snap_b64url_decode(string $value): string|false
{
    $padding = strlen($value) % 4;
    if ($padding !== 0) {
        $value .= str_repeat('=', 4 - $padding);
    }
    return base64_decode(strtr($value, '-_', '+/'), true);
}

function snap_runtime_secrets(): array
{
    $dir = SNAP_ROOT . '/storage/data';
    if (!is_dir($dir) && !mkdir($dir, 0700, true) && !is_dir($dir)) {
        throw new RuntimeException('Storage directory is unavailable.');
    }

    $path = snap_data_path('.secrets.json');
    $lockPath = snap_data_path('.secrets.lock');
    $lock = fopen($lockPath, 'c+');
    if ($lock === false) {
        throw new RuntimeException('Unable to open secrets lock.');
    }

    try {
        if (!flock($lock, LOCK_EX)) {
            throw new RuntimeException('Unable to lock runtime secrets.');
        }

        if (is_file($path)) {
            $decoded = json_decode((string) file_get_contents($path), true);
            if (
                is_array($decoded)
                && isset($decoded['token_key'], $decoded['ip_key'])
                && is_string($decoded['token_key'])
                && is_string($decoded['ip_key'])
                && strlen($decoded['token_key']) >= 64
                && strlen($decoded['ip_key']) >= 64
            ) {
                return $decoded;
            }
        }

        $secrets = [
            'token_key' => bin2hex(random_bytes(32)),
            'ip_key' => bin2hex(random_bytes(32)),
            'created_at' => gmdate(DATE_ATOM),
        ];

        $tmp = $path . '.tmp-' . bin2hex(random_bytes(6));
        $json = json_encode($secrets, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
        if ($json === false || file_put_contents($tmp, $json . PHP_EOL, LOCK_EX) === false) {
            throw new RuntimeException('Unable to write runtime secrets.');
        }
        @chmod($tmp, 0600);
        if (!rename($tmp, $path)) {
            @unlink($tmp);
            throw new RuntimeException('Unable to publish runtime secrets.');
        }
        @chmod($path, 0600);
        return $secrets;
    } finally {
        flock($lock, LOCK_UN);
        fclose($lock);
    }
}

function snap_ip_hash(): string
{
    $ip = (string) ($_SERVER['REMOTE_ADDR'] ?? '');
    $secrets = snap_runtime_secrets();
    $key = hex2bin($secrets['ip_key']);
    if ($key === false) {
        throw new RuntimeException('Invalid IP hashing key.');
    }
    return hash_hmac('sha256', $ip, $key);
}

function snap_safe_text(string $value, int $max = 600): string
{
    $value = trim(preg_replace('/[\x00-\x1F\x7F]+/u', ' ', $value) ?? '');
    if (function_exists('mb_substr')) {
        return mb_substr($value, 0, $max, 'UTF-8');
    }
    return substr($value, 0, $max);
}

function snap_origin_value(): string
{
    $origin = trim((string) ($_SERVER['HTTP_ORIGIN'] ?? ''));
    if ($origin !== '') {
        return rtrim($origin, '/');
    }
    $referer = trim((string) ($_SERVER['HTTP_REFERER'] ?? ''));
    if ($referer === '') {
        return '';
    }
    $parts = parse_url($referer);
    if (!is_array($parts) || empty($parts['scheme']) || empty($parts['host'])) {
        return '';
    }
    $port = isset($parts['port']) ? ':' . (int) $parts['port'] : '';
    return strtolower($parts['scheme']) . '://' . strtolower($parts['host']) . $port;
}

function snap_origin_allowed(string $origin): bool
{
    if ($origin === '') {
        return false;
    }
    $allowed = array_map(
        static fn (string $item): string => rtrim(strtolower($item), '/'),
        snap_config()['allowed_origins']
    );
    return in_array(rtrim(strtolower($origin), '/'), $allowed, true);
}

function snap_apply_cors(): void
{
    $origin = trim((string) ($_SERVER['HTTP_ORIGIN'] ?? ''));
    if ($origin !== '' && snap_origin_allowed($origin)) {
        header('Access-Control-Allow-Origin: ' . $origin);
        header('Vary: Origin');
        header('Access-Control-Allow-Methods: POST, OPTIONS');
        header('Access-Control-Allow-Headers: Content-Type, Accept');
        header('Access-Control-Max-Age: 600');
    }
}

function snap_verify_post_origin(): bool
{
    $config = snap_config();
    if (empty($config['require_origin_for_post'])) {
        return true;
    }
    return snap_origin_allowed(snap_origin_value());
}

function snap_append_jsonl(string $filename, array $record): void
{
    $path = snap_data_path($filename);
    $fh = fopen($path, 'ab');
    if ($fh === false) {
        throw new RuntimeException('Unable to open storage file.');
    }
    try {
        if (!flock($fh, LOCK_EX)) {
            throw new RuntimeException('Unable to lock storage file.');
        }
        $json = json_encode($record, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
        if ($json === false || fwrite($fh, $json . PHP_EOL) === false) {
            throw new RuntimeException('Unable to persist storage record.');
        }
        fflush($fh);
    } finally {
        flock($fh, LOCK_UN);
        fclose($fh);
    }
}

function snap_find_lead(string $leadId): ?array
{
    $path = snap_data_path('download-leads.jsonl');
    if (!is_file($path)) {
        return null;
    }

    $fh = fopen($path, 'rb');
    if ($fh === false) {
        return null;
    }

    $found = null;
    while (($line = fgets($fh)) !== false) {
        $row = json_decode($line, true);
        if (is_array($row) && isset($row['lead_id']) && hash_equals((string) $row['lead_id'], $leadId)) {
            $found = $row;
        }
    }
    fclose($fh);
    return $found;
}

function snap_rate_limit_ok(string $key): bool
{
    $config = snap_config()['rate_limit'];
    $window = max(60, (int) $config['window_seconds']);
    $max = max(1, (int) $config['max_attempts']);
    $path = snap_data_path('rate-limit.json');
    $lockPath = snap_data_path('rate-limit.lock');
    $fh = fopen($lockPath, 'c+');
    if ($fh === false) {
        return false;
    }

    try {
        if (!flock($fh, LOCK_EX)) {
            return false;
        }
        $now = time();
        $data = [];
        if (is_file($path)) {
            $decoded = json_decode((string) file_get_contents($path), true);
            if (is_array($decoded)) {
                $data = $decoded;
            }
        }

        foreach ($data as $storedKey => $entry) {
            if (!is_array($entry) || ($now - (int) ($entry['window_start'] ?? 0)) > ($window * 2)) {
                unset($data[$storedKey]);
            }
        }

        $entry = $data[$key] ?? ['window_start' => $now, 'count' => 0];
        if (($now - (int) $entry['window_start']) >= $window) {
            $entry = ['window_start' => $now, 'count' => 0];
        }
        $entry['count'] = (int) $entry['count'] + 1;
        $data[$key] = $entry;

        $tmp = $path . '.tmp-' . bin2hex(random_bytes(4));
        file_put_contents($tmp, json_encode($data, JSON_UNESCAPED_SLASHES), LOCK_EX);
        rename($tmp, $path);
        return $entry['count'] <= $max;
    } finally {
        flock($fh, LOCK_UN);
        fclose($fh);
    }
}

function snap_email_domain(string $email): string
{
    $at = strrpos($email, '@');
    return $at === false ? '' : strtolower(substr($email, $at + 1));
}

function snap_email_valid(string $email): bool
{
    if ($email === '' || strlen($email) > 254 || filter_var($email, FILTER_VALIDATE_EMAIL) === false) {
        return false;
    }

    $domain = snap_email_domain($email);
    if ($domain === '' || str_ends_with($domain, '.invalid') || str_ends_with($domain, '.test') || str_ends_with($domain, '.example')) {
        return false;
    }

    if (function_exists('idn_to_ascii')) {
        $ascii = idn_to_ascii($domain, IDNA_DEFAULT, INTL_IDNA_VARIANT_UTS46);
        if (is_string($ascii) && $ascii !== '') {
            $domain = strtolower($ascii);
        }
    }

    $blocked = array_map('strtolower', snap_config()['blocked_email_domains']);
    foreach ($blocked as $item) {
        if ($domain === $item || str_ends_with($domain, '.' . $item)) {
            return false;
        }
    }
    return true;
}

function snap_package(string $packageId): ?array
{
    $packages = snap_config()['packages'];
    return isset($packages[$packageId]) && is_array($packages[$packageId]) ? $packages[$packageId] : null;
}

function snap_issue_token(string $leadId, string $packageId): string
{
    $payload = [
        'v' => 1,
        'lead' => $leadId,
        'pkg' => $packageId,
        'exp' => time() + (int) snap_config()['token_ttl'],
        'nonce' => bin2hex(random_bytes(8)),
    ];
    $json = json_encode($payload, JSON_UNESCAPED_SLASHES);
    if ($json === false) {
        throw new RuntimeException('Unable to encode access token.');
    }
    $encoded = snap_b64url_encode($json);
    $secrets = snap_runtime_secrets();
    $key = hex2bin($secrets['token_key']);
    if ($key === false) {
        throw new RuntimeException('Invalid token key.');
    }
    $signature = hash_hmac('sha256', $encoded, $key, true);
    return $encoded . '.' . snap_b64url_encode($signature);
}

function snap_verify_token(string $token, string $packageId): ?array
{
    if (!str_contains($token, '.')) {
        return null;
    }
    [$encoded, $signatureEncoded] = explode('.', $token, 2);
    $signature = snap_b64url_decode($signatureEncoded);
    $payloadJson = snap_b64url_decode($encoded);
    if ($signature === false || $payloadJson === false) {
        return null;
    }

    $secrets = snap_runtime_secrets();
    $key = hex2bin($secrets['token_key']);
    if ($key === false) {
        return null;
    }
    $expected = hash_hmac('sha256', $encoded, $key, true);
    if (!hash_equals($expected, $signature)) {
        return null;
    }

    $payload = json_decode($payloadJson, true);
    if (
        !is_array($payload)
        || (int) ($payload['v'] ?? 0) !== 1
        || (string) ($payload['pkg'] ?? '') !== $packageId
        || (int) ($payload['exp'] ?? 0) < time()
        || !preg_match('/^[a-f0-9]{32}$/', (string) ($payload['lead'] ?? ''))
    ) {
        return null;
    }
    return $payload;
}

function snap_is_prefetch(): bool
{
    $purpose = strtolower((string) ($_SERVER['HTTP_SEC_PURPOSE'] ?? $_SERVER['HTTP_PURPOSE'] ?? ''));
    return str_contains($purpose, 'prefetch') || str_contains($purpose, 'prerender');
}

function snap_increment_counter(): int
{
    $config = snap_config();
    $path = snap_data_path('download-count.json');
    $lockPath = snap_data_path('download-count.lock');
    $lock = fopen($lockPath, 'c+');
    if ($lock === false) {
        return (int) $config['counter_baseline'];
    }

    try {
        flock($lock, LOCK_EX);
        $count = (int) $config['counter_baseline'];
        if (is_file($path)) {
            $decoded = json_decode((string) file_get_contents($path), true);
            if (is_array($decoded) && isset($decoded['count'])) {
                $count = max($count, (int) $decoded['count']);
            }
        }
        $count++;
        $payload = [
            'count' => $count,
            'updated_at' => gmdate(DATE_ATOM),
        ];
        $tmp = $path . '.tmp-' . bin2hex(random_bytes(4));
        file_put_contents($tmp, json_encode($payload, JSON_UNESCAPED_SLASHES) . PHP_EOL, LOCK_EX);
        rename($tmp, $path);
        return $count;
    } finally {
        flock($lock, LOCK_UN);
        fclose($lock);
    }
}

function snap_counter_value(): array
{
    $baseline = (int) snap_config()['counter_baseline'];
    $path = snap_data_path('download-count.json');
    if (is_file($path)) {
        $decoded = json_decode((string) file_get_contents($path), true);
        if (is_array($decoded)) {
            return [
                'count' => max($baseline, (int) ($decoded['count'] ?? $baseline)),
                'baseline' => $baseline,
                'updated_at' => (string) ($decoded['updated_at'] ?? ''),
            ];
        }
    }
    return ['count' => $baseline, 'baseline' => $baseline, 'updated_at' => ''];
}

function snap_mail_download(array $lead, string $packageId, array $package, string $eventId): bool
{
    $config = snap_config();
    $to = (string) $config['admin_email'];
    $label = (string) ($package['label_hr'] ?? $package['file']);
    $subjectText = '[SNAPVERE] Preuzimanje: ' . $label;
    $subject = function_exists('mb_encode_mimeheader')
        ? mb_encode_mimeheader($subjectText, 'UTF-8')
        : $subjectText;

    $body = implode(PHP_EOL, [
        'SNAPVERE — novo autorizirano preuzimanje',
        '',
        'E-mail: ' . (string) ($lead['email'] ?? ''),
        'Paket: ' . $label,
        'Datoteka: ' . (string) $package['file'],
        'Verzija: ' . (string) $config['version'],
        'Vrijeme (UTC): ' . gmdate(DATE_ATOM),
        'Jezik: ' . (string) ($lead['lang'] ?? ''),
        'IP hash: ' . substr((string) ($lead['ip_hash'] ?? ''), 0, 32),
        'User-Agent: ' . (string) ($lead['user_agent'] ?? ''),
        'Referer: ' . (string) ($lead['referer'] ?? ''),
        'Origin: ' . (string) ($lead['origin'] ?? ''),
        'Lead ID: ' . (string) ($lead['lead_id'] ?? ''),
        'Event ID: ' . $eventId,
        '',
        'Ova poruka je generirana nakon prvog stvarnog autoriziranog GET preuzimanja. HEAD/prefetch i ponovljeni Range zahtjevi ne šalju dodatne poruke.',
    ]);

    $from = preg_replace('/[\r\n]+/', '', (string) $config['mail_from']) ?: 'no-reply@snapvere.com';
    $reply = preg_replace('/[\r\n]+/', '', (string) ($lead['email'] ?? ''));
    $headers = [
        'From: SNAPVERE Downloads <' . $from . '>',
        'MIME-Version: 1.0',
        'Content-Type: text/plain; charset=UTF-8',
        'X-Auto-Response-Suppress: All',
    ];
    if ($reply !== '' && filter_var($reply, FILTER_VALIDATE_EMAIL) !== false) {
        $headers[] = 'Reply-To: ' . $reply;
    }

    $ok = false;
    if (function_exists('mail')) {
        $ok = @mail($to, $subject, $body, implode("\r\n", $headers));
    }

    if (!$ok) {
        snap_append_jsonl('mail-spool.jsonl', [
            'event_id' => $eventId,
            'created_at' => gmdate(DATE_ATOM),
            'to' => $to,
            'subject' => $subjectText,
            'body' => $body,
            'status' => 'mail_transport_failed',
        ]);
    }
    return $ok;
}

function snap_record_download_once(array $lead, string $packageId, array $package): bool
{
    $eventKey = hash('sha256', (string) $lead['lead_id'] . '|' . $packageId);
    $path = snap_data_path('download-events.jsonl');
    $lockPath = snap_data_path('download-events.lock');
    $lock = fopen($lockPath, 'c+');
    if ($lock === false) {
        throw new RuntimeException('Unable to open download event lock.');
    }

    try {
        if (!flock($lock, LOCK_EX)) {
            throw new RuntimeException('Unable to lock download events.');
        }

        if (is_file($path)) {
            $fh = fopen($path, 'rb');
            if ($fh !== false) {
                while (($line = fgets($fh)) !== false) {
                    $row = json_decode($line, true);
                    if (is_array($row) && isset($row['event_key']) && hash_equals((string) $row['event_key'], $eventKey)) {
                        fclose($fh);
                        return false;
                    }
                }
                fclose($fh);
            }
        }

        $eventId = bin2hex(random_bytes(16));
        $record = [
            'event_id' => $eventId,
            'event_key' => $eventKey,
            'created_at' => gmdate(DATE_ATOM),
            'lead_id' => (string) $lead['lead_id'],
            'package' => $packageId,
            'file' => (string) $package['file'],
            'email' => (string) $lead['email'],
            'ip_hash' => (string) $lead['ip_hash'],
            'user_agent' => snap_safe_text((string) ($_SERVER['HTTP_USER_AGENT'] ?? ''), 500),
            'range' => snap_safe_text((string) ($_SERVER['HTTP_RANGE'] ?? ''), 120),
        ];
        $json = json_encode($record, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
        if ($json === false) {
            throw new RuntimeException('Unable to encode download event.');
        }

        $fh = fopen($path, 'ab');
        if ($fh === false) {
            throw new RuntimeException('Unable to open download event file.');
        }
        fwrite($fh, $json . PHP_EOL);
        fflush($fh);
        fclose($fh);

        snap_increment_counter();
        $mailOk = snap_mail_download($lead, $packageId, $package, $eventId);

        snap_append_jsonl('download-mail-status.jsonl', [
            'event_id' => $eventId,
            'created_at' => gmdate(DATE_ATOM),
            'mail_sent' => $mailOk,
        ]);
        return true;
    } finally {
        flock($lock, LOCK_UN);
        fclose($lock);
    }
}

function snap_json_response(array $payload, int $status = 200): never
{
    http_response_code($status);
    snap_security_headers();
    snap_apply_cors();
    header('Content-Type: application/json; charset=UTF-8');
    header('Cache-Control: no-store, max-age=0');
    echo json_encode($payload, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
    exit;
}

function snap_human_size(int $bytes): string
{
    if ($bytes >= 1073741824) {
        return number_format($bytes / 1073741824, 2) . ' GB';
    }
    if ($bytes >= 1048576) {
        return number_format($bytes / 1048576, 1) . ' MB';
    }
    if ($bytes >= 1024) {
        return number_format($bytes / 1024, 1) . ' KB';
    }
    return $bytes . ' B';
}
