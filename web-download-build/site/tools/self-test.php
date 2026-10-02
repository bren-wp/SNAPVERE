<?php
declare(strict_types=1);

require __DIR__ . '/../lib/app.php';

$failures = [];
$passes = [];

$check = static function (bool $ok, string $label) use (&$failures, &$passes): void {
    if ($ok) {
        $passes[] = $label;
    } else {
        $failures[] = $label;
    }
};

$check(PHP_VERSION_ID >= 80100, 'PHP 8.1 or newer');
$check(is_writable(SNAP_ROOT . '/storage/data'), 'storage/data is writable');

try {
    $secrets = snap_runtime_secrets();
    $check(isset($secrets['token_key'], $secrets['ip_key']), 'runtime secrets are available');
} catch (Throwable $e) {
    $check(false, 'runtime secrets are available');
}

$config = snap_config();
$check((string) ($config['admin_email'] ?? '') === 'info@brendigo.com', 'admin notification email is configured');
$check(count($config['packages'] ?? []) === 6, 'exactly six public package IDs are configured');

foreach ($config['packages'] as $id => $package) {
    $file = snap_package_path((string) $package['file']);
    $check(is_file($file), $id . ': local package exists');
    if (!is_file($file)) {
        continue;
    }

    $size = filesize($file);
    $check($size === (int) $package['size'], $id . ': size matches');

    $sha = hash_file('sha256', $file);
    $check(is_string($sha) && hash_equals((string) $package['sha256'], $sha), $id . ': SHA-256 matches');
}

$blockedExample = snap_email_valid('user@mailinator.com') === false;
$validExample = snap_email_valid('user@example.org') === true;
$check($blockedExample, 'disposable email denylist works');
$check($validExample, 'normal permanent email syntax is accepted');

echo "SNAPVERE download deployment self-test\n";
echo "=====================================\n";
foreach ($passes as $pass) {
    echo "PASS — " . $pass . "\n";
}
foreach ($failures as $failure) {
    echo "FAIL — " . $failure . "\n";
}

if ($failures !== []) {
    exit(1);
}
echo "\nAll checks passed.\n";
