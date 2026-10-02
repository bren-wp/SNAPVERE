<?php
declare(strict_types=1);
require dirname(__DIR__) . '/api/bootstrap.php';

$failures = [];
$cfg = snapvere_config();

if (PHP_VERSION_ID < 80100) {
    $failures[] = 'PHP 8.1+ is required.';
}

if (!is_writable(snapvere_data_dir())) {
    $failures[] = 'storage/data must be writable by PHP.';
}

foreach ($cfg['packages'] as $id => $package) {
    $result = snapvere_verify_package($package);
    if (($result['ok'] ?? false) !== true) {
        $failures[] = $id . ': integrity check failed (' . ($result['reason'] ?? 'unknown') . ').';
    }
}

if ($failures) {
    fwrite(STDERR, "SNAPVERE download backend self-test FAILED\n- " . implode("\n- ", $failures) . "\n");
    exit(1);
}

echo "SNAPVERE download backend self-test PASS\n";
echo "Version: " . $cfg['product_version'] . "\n";
echo "Packages: " . count($cfg['packages']) . "/6 verified\n";
echo "Admin notification: " . $cfg['admin_email'] . "\n";
