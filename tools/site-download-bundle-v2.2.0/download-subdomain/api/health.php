<?php
declare(strict_types=1);
require __DIR__ . '/bootstrap.php';

$cfg = snapvere_config();
$packages = $cfg['packages'] ?? [];
$found = 0;
$verified = 0;
$items = [];

foreach ($packages as $id => $package) {
    $path = snapvere_package_path($package);
    if ($path !== null && is_file($path)) {
        $found++;
    }

    $result = snapvere_verify_package($package);
    if (($result['ok'] ?? false) === true) {
        $verified++;
    }

    $items[$id] = [
        'filename' => $package['filename'],
        'available' => ($result['ok'] ?? false) === true,
    ];
}

snapvere_json([
    'ok' => $verified === count($packages),
    'product' => 'SNAPVERE',
    'version' => $cfg['product_version'],
    'expected_packages' => count($packages),
    'found_packages' => $found,
    'integrity_verified_packages' => $verified,
    'packages' => $items,
    'mail_function_available' => function_exists('mail'),
]);
