<?php
declare(strict_types=1);

require __DIR__ . '/../lib/app.php';

snap_security_headers();

$method = strtoupper((string) ($_SERVER['REQUEST_METHOD'] ?? 'GET'));
if (!in_array($method, ['GET', 'HEAD'], true)) {
    header('Allow: GET, HEAD');
    http_response_code(405);
    exit;
}

$packageId = trim((string) ($_GET['package'] ?? ''));
$token = trim((string) ($_GET['token'] ?? ''));
$package = snap_package($packageId);
$payload = $package === null ? null : snap_verify_token($token, $packageId);

if ($package === null || $payload === null) {
    http_response_code(403);
    header('Content-Type: text/plain; charset=UTF-8');
    header('Cache-Control: no-store, max-age=0');
    echo 'Download authorization is invalid or expired.';
    exit;
}

$lead = snap_find_lead((string) $payload['lead']);
if ($lead === null || (string) ($lead['package'] ?? '') !== $packageId) {
    http_response_code(403);
    header('Content-Type: text/plain; charset=UTF-8');
    header('Cache-Control: no-store, max-age=0');
    echo 'Download authorization could not be resolved.';
    exit;
}

$file = snap_package_path((string) $package['file']);
if (!is_file($file) || !is_readable($file)) {
    http_response_code(503);
    header('Content-Type: text/plain; charset=UTF-8');
    header('Cache-Control: no-store, max-age=0');
    echo 'The selected package is temporarily unavailable.';
    exit;
}

$size = filesize($file);
if ($size === false || $size !== (int) $package['size']) {
    http_response_code(503);
    header('Content-Type: text/plain; charset=UTF-8');
    header('Cache-Control: no-store, max-age=0');
    echo 'Package integrity metadata does not match the local file.';
    exit;
}

$start = 0;
$end = $size - 1;
$status = 200;
$range = trim((string) ($_SERVER['HTTP_RANGE'] ?? ''));

if ($range !== '') {
    if (!preg_match('/^bytes=(\d*)-(\d*)$/', $range, $m)) {
        http_response_code(416);
        header('Content-Range: bytes */' . $size);
        exit;
    }

    if ($m[1] === '' && $m[2] === '') {
        http_response_code(416);
        header('Content-Range: bytes */' . $size);
        exit;
    }

    if ($m[1] === '') {
        $suffix = (int) $m[2];
        if ($suffix <= 0) {
            http_response_code(416);
            header('Content-Range: bytes */' . $size);
            exit;
        }
        $start = max(0, $size - $suffix);
    } else {
        $start = (int) $m[1];
    }

    if ($m[2] !== '') {
        $end = min($end, (int) $m[2]);
    }

    if ($start > $end || $start >= $size) {
        http_response_code(416);
        header('Content-Range: bytes */' . $size);
        exit;
    }
    $status = 206;
}

$length = $end - $start + 1;
http_response_code($status);
header('Content-Type: ' . (string) $package['mime']);
header('Content-Disposition: attachment; filename="' . addcslashes((string) $package['file'], '"\\') . '"');
header('Content-Length: ' . $length);
header('Accept-Ranges: bytes');
header('Cache-Control: private, no-store, max-age=0');
header('Pragma: no-cache');
header('X-Content-Type-Options: nosniff');
header('X-SNAPVERE-SHA256: ' . (string) $package['sha256']);

if ($status === 206) {
    header('Content-Range: bytes ' . $start . '-' . $end . '/' . $size);
}

if ($method === 'HEAD') {
    exit;
}

if (!snap_is_prefetch()) {
    try {
        snap_record_download_once($lead, $packageId, $package);
    } catch (Throwable $e) {
        error_log('SNAPVERE download event logging failed: ' . $e->getMessage());
    }
}

@set_time_limit(0);
while (ob_get_level() > 0) {
    @ob_end_clean();
}

$fh = fopen($file, 'rb');
if ($fh === false) {
    exit;
}

if ($start > 0) {
    fseek($fh, $start);
}

$remaining = $length;
$chunkSize = 1024 * 1024;

while ($remaining > 0 && !feof($fh)) {
    $read = min($chunkSize, $remaining);
    $buffer = fread($fh, $read);
    if ($buffer === false || $buffer === '') {
        break;
    }

    echo $buffer;
    $remaining -= strlen($buffer);

    if (function_exists('ob_flush')) {
        @ob_flush();
    }
    @flush();

    if (connection_aborted()) {
        break;
    }
}

fclose($fh);
exit;
