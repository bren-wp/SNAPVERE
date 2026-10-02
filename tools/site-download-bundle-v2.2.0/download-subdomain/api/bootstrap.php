<?php
declare(strict_types=1);

ini_set('display_errors', '0');
ini_set('log_errors', '1');
header_remove('X-Powered-By');

$config = require __DIR__ . '/config.php';

function snapvere_config(): array
{
    global $config;
    return $config;
}

function snapvere_root(): string
{
    return dirname(__DIR__);
}

function snapvere_data_dir(): string
{
    $dir = snapvere_root() . '/storage/data';
    if (!is_dir($dir)) {
        @mkdir($dir, 0700, true);
    }
    return $dir;
}

function snapvere_packages_dir(): string
{
    return snapvere_root() . '/storage/packages';
}

function snapvere_security_headers(): void
{
    header('X-Content-Type-Options: nosniff');
    header('X-Frame-Options: DENY');
    header('Referrer-Policy: strict-origin-when-cross-origin');
    header('Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()');
    header("Content-Security-Policy: default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'");
    header('Cache-Control: no-store, max-age=0');
}

function snapvere_json(array $payload, int $status = 200): never
{
    snapvere_security_headers();
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    echo json_encode($payload, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
    exit;
}

function snapvere_random_token(int $bytes = 32): string
{
    return rtrim(strtr(base64_encode(random_bytes($bytes)), '+/', '-_'), '=');
}

function snapvere_token_hash(string $token): string
{
    return hash('sha256', $token);
}

function snapvere_runtime_secret(): string
{
    $path = snapvere_data_dir() . '/.runtime-secret';
    if (is_file($path)) {
        $value = trim((string) @file_get_contents($path));
        if (preg_match('/^[a-f0-9]{64}$/', $value)) {
            return $value;
        }
    }

    $value = bin2hex(random_bytes(32));
    $tmp = $path . '.tmp-' . bin2hex(random_bytes(6));
    if (@file_put_contents($tmp, $value, LOCK_EX) === false) {
        throw new RuntimeException('Unable to initialise runtime secret.');
    }
    @chmod($tmp, 0600);

    if (!@rename($tmp, $path)) {
        @unlink($tmp);
        if (is_file($path)) {
            $existing = trim((string) @file_get_contents($path));
            if (preg_match('/^[a-f0-9]{64}$/', $existing)) {
                return $existing;
            }
        }
        throw new RuntimeException('Unable to persist runtime secret.');
    }

    @chmod($path, 0600);
    return $value;
}

function snapvere_client_ip(): string
{
    return (string) ($_SERVER['REMOTE_ADDR'] ?? '0.0.0.0');
}

function snapvere_hash_private(string $value): string
{
    return hash_hmac('sha256', $value, snapvere_runtime_secret());
}

function snapvere_user_agent(): string
{
    return mb_substr((string) ($_SERVER['HTTP_USER_AGENT'] ?? ''), 0, 1000);
}

function snapvere_referer(): string
{
    return mb_substr((string) ($_SERVER['HTTP_REFERER'] ?? ''), 0, 1000);
}

function snapvere_read_json_file(string $path): array
{
    if (!is_file($path)) {
        return [];
    }

    $raw = @file_get_contents($path);
    if (!is_string($raw) || $raw === '') {
        return [];
    }

    $decoded = json_decode($raw, true);
    return is_array($decoded) ? $decoded : [];
}

function snapvere_mutate_json(string $name, callable $callback): mixed
{
    $path = snapvere_data_dir() . '/' . $name;
    $lockPath = $path . '.lock';
    $lock = @fopen($lockPath, 'c+');
    if ($lock === false) {
        throw new RuntimeException('Unable to open state lock.');
    }

    try {
        if (!flock($lock, LOCK_EX)) {
            throw new RuntimeException('Unable to lock state.');
        }

        $state = snapvere_read_json_file($path);
        $result = $callback($state);

        $encoded = json_encode($state, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
        if (!is_string($encoded)) {
            throw new RuntimeException('Unable to encode state.');
        }

        $tmp = $path . '.tmp-' . bin2hex(random_bytes(6));
        if (@file_put_contents($tmp, $encoded, LOCK_EX) === false) {
            throw new RuntimeException('Unable to write state.');
        }
        @chmod($tmp, 0600);

        if (!@rename($tmp, $path)) {
            @unlink($tmp);
            throw new RuntimeException('Unable to publish state.');
        }
        @chmod($path, 0600);

        flock($lock, LOCK_UN);
        return $result;
    } finally {
        fclose($lock);
    }
}

function snapvere_append_jsonl(string $name, array $record): void
{
    $path = snapvere_data_dir() . '/' . $name;
    $fh = @fopen($path, 'ab');
    if ($fh === false) {
        return;
    }

    try {
        if (flock($fh, LOCK_EX)) {
            fwrite($fh, json_encode($record, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) . PHP_EOL);
            fflush($fh);
            flock($fh, LOCK_UN);
        }
    } finally {
        fclose($fh);
    }

    @chmod($path, 0600);
}

function snapvere_cleanup_tokens(array &$state): void
{
    $now = time();
    foreach ($state as $key => $record) {
        if (!is_array($record) || (int) ($record['expires_at'] ?? 0) < $now - 3600) {
            unset($state[$key]);
        }
    }
}

function snapvere_package(string $id): ?array
{
    $packages = snapvere_config()['packages'] ?? [];
    return isset($packages[$id]) && is_array($packages[$id]) ? $packages[$id] : null;
}

function snapvere_package_path(array $package): ?string
{
    $filename = (string) ($package['filename'] ?? '');
    if ($filename === '' || basename($filename) !== $filename) {
        return null;
    }

    $base = realpath(snapvere_packages_dir());
    $candidate = realpath(snapvere_packages_dir() . '/' . $filename);
    if ($base === false || $candidate === false) {
        return null;
    }

    $prefix = rtrim($base, DIRECTORY_SEPARATOR) . DIRECTORY_SEPARATOR;
    if (!str_starts_with($candidate, $prefix)) {
        return null;
    }

    return $candidate;
}

function snapvere_verify_package(array $package): array
{
    $path = snapvere_package_path($package);
    if ($path === null || !is_file($path)) {
        return ['ok' => false, 'reason' => 'missing'];
    }

    $expectedSize = (int) ($package['size'] ?? 0);
    $actualSize = filesize($path);
    if ($actualSize === false || $actualSize !== $expectedSize) {
        return ['ok' => false, 'reason' => 'size'];
    }

    $expectedHash = strtolower((string) ($package['sha256'] ?? ''));
    $actualHash = hash_file('sha256', $path);
    if (!is_string($actualHash) || !hash_equals($expectedHash, strtolower($actualHash))) {
        return ['ok' => false, 'reason' => 'sha256'];
    }

    return ['ok' => true, 'path' => $path, 'size' => $actualSize, 'sha256' => $actualHash];
}

function snapvere_normalize_email(string $email): string
{
    return strtolower(trim($email));
}

function snapvere_email_is_disposable_or_invalid(string $email): bool
{
    if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
        return true;
    }

    [$local, $domain] = array_pad(explode('@', $email, 2), 2, '');
    if ($local === '' || $domain === '') {
        return true;
    }

    $domain = strtolower($domain);
    $blocked = [
        '10minutemail.com', '10minutemail.net', 'dispostable.com', 'fakeinbox.com',
        'getnada.com', 'grr.la', 'guerrillamail.com', 'guerrillamailblock.com',
        'maildrop.cc', 'mailinator.com', 'mailnesia.com', 'moakt.com',
        'sharklasers.com', 'temp-mail.org', 'tempail.com', 'tempmail.com',
        'tempmail.net', 'throwawaymail.com', 'trashmail.com', 'yopmail.com',
        'example.com', 'example.net', 'example.org',
    ];

    if (in_array($domain, $blocked, true)) {
        return true;
    }

    if ($domain === 'localhost' || str_ends_with($domain, '.test') || str_ends_with($domain, '.invalid')) {
        return true;
    }

    if (preg_match('/^(test|fake|spam|noreply|no-reply)([0-9._+-]*)$/i', $local)) {
        return true;
    }

    return false;
}

function snapvere_origin_allowed(): bool
{
    $cfg = snapvere_config();
    if (($cfg['require_origin_for_post'] ?? true) !== true) {
        return true;
    }

    $origin = trim((string) ($_SERVER['HTTP_ORIGIN'] ?? ''));
    $referer = trim((string) ($_SERVER['HTTP_REFERER'] ?? ''));
    $allowed = array_map(static fn ($value) => rtrim((string) $value, '/'), $cfg['allowed_origins'] ?? []);

    if ($origin !== '') {
        return in_array(rtrim($origin, '/'), $allowed, true);
    }

    if ($referer !== '') {
        $parts = parse_url($referer);
        if (is_array($parts) && isset($parts['scheme'], $parts['host'])) {
            $candidate = $parts['scheme'] . '://' . $parts['host'];
            return in_array(rtrim($candidate, '/'), $allowed, true);
        }
    }

    return false;
}

function snapvere_start_session(): void
{
    if (PHP_SAPI === 'cli' || session_status() === PHP_SESSION_ACTIVE) {
        return;
    }

    session_name('snapvere_download');
    session_set_cookie_params([
        'lifetime' => 0,
        'path' => '/',
        'secure' => (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off'),
        'httponly' => true,
        'samesite' => 'Lax',
    ]);
    session_start();
}

function snapvere_csrf_token(): string
{
    snapvere_start_session();
    if (!isset($_SESSION['csrf']) || !is_string($_SESSION['csrf']) || strlen($_SESSION['csrf']) < 32) {
        $_SESSION['csrf'] = snapvere_random_token(24);
    }
    return $_SESSION['csrf'];
}

function snapvere_validate_csrf(string $token): bool
{
    snapvere_start_session();
    $expected = $_SESSION['csrf'] ?? '';
    return is_string($expected) && $expected !== '' && hash_equals($expected, $token);
}

function snapvere_rate_limit(string $email): bool
{
    $cfg = snapvere_config();
    $window = (int) ($cfg['rate_window'] ?? 3600);
    $now = time();
    $ipKey = 'ip:' . snapvere_hash_private(snapvere_client_ip());
    $emailKey = 'mail:' . hash('sha256', $email);

    return (bool) snapvere_mutate_json('rate-limit.json', function (&$state) use ($cfg, $window, $now, $ipKey, $emailKey) {
        foreach ($state as $key => $value) {
            if (!is_array($value) || (int) ($value['window_start'] ?? 0) < $now - ($window * 2)) {
                unset($state[$key]);
            }
        }

        $checks = [
            [$ipKey, (int) ($cfg['rate_limit_ip'] ?? 20)],
            [$emailKey, (int) ($cfg['rate_limit_email'] ?? 10)],
        ];

        foreach ($checks as [$key, $limit]) {
            $record = $state[$key] ?? ['window_start' => $now, 'count' => 0];
            if ((int) ($record['window_start'] ?? 0) <= $now - $window) {
                $record = ['window_start' => $now, 'count' => 0];
            }
            if ((int) ($record['count'] ?? 0) >= $limit) {
                return false;
            }
        }

        foreach ($checks as [$key, $limit]) {
            $record = $state[$key] ?? ['window_start' => $now, 'count' => 0];
            if ((int) ($record['window_start'] ?? 0) <= $now - $window) {
                $record = ['window_start' => $now, 'count' => 0];
            }
            $record['count'] = (int) ($record['count'] ?? 0) + 1;
            $state[$key] = $record;
        }

        return true;
    });
}

function snapvere_send_mail(string $to, string $subject, string $body, ?string $replyTo = null): bool
{
    $cfg = snapvere_config();
    $from = (string) ($cfg['from_email'] ?? 'info@snapvere.com');

    $headers = [
        'From: SNAPVERE <' . $from . '>',
        'MIME-Version: 1.0',
        'Content-Type: text/plain; charset=UTF-8',
        'Content-Transfer-Encoding: 8bit',
        'X-Mailer: SNAPVERE Website',
    ];

    if ($replyTo !== null && filter_var($replyTo, FILTER_VALIDATE_EMAIL)) {
        $headers[] = 'Reply-To: ' . $replyTo;
    }

    return @mail($to, $subject, $body, implode("\r\n", $headers));
}

function snapvere_increment_counter(): int
{
    return (int) snapvere_mutate_json('download-count.json', function (&$state) {
        $current = isset($state['count']) ? (int) $state['count'] : 1795;
        $current++;
        $state = ['count' => $current, 'updated_at' => gmdate('c')];
        return $current;
    });
}

function snapvere_current_counter(): int
{
    $state = snapvere_read_json_file(snapvere_data_dir() . '/download-count.json');
    return isset($state['count']) ? (int) $state['count'] : 1795;
}

function snapvere_is_prefetch(): bool
{
    $purpose = strtolower((string) ($_SERVER['HTTP_PURPOSE'] ?? ''));
    $secPurpose = strtolower((string) ($_SERVER['HTTP_SEC_PURPOSE'] ?? ''));
    return str_contains($purpose, 'prefetch')
        || str_contains($purpose, 'prerender')
        || str_contains($secPurpose, 'prefetch')
        || str_contains($secPurpose, 'prerender');
}
