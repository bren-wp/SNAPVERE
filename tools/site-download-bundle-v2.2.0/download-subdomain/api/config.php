<?php
declare(strict_types=1);

$config = [
    'site_url' => 'https://download.snapvere.com',
    'main_site_url' => 'https://snapvere.com',
    'admin_email' => 'info@brendigo.com',
    'from_email' => 'info@snapvere.com',
    'product_version' => '0.1.22',
    'release_date' => '2026-09-26',
    'verification_ttl' => 900,
    'download_ttl' => 900,
    'max_download_requests' => 32,
    'rate_window' => 3600,
    'rate_limit_ip' => 20,
    'rate_limit_email' => 10,
    'require_origin_for_post' => true,
    'allowed_origins' => [
        'https://snapvere.com',
        'https://www.snapvere.com',
        'https://download.snapvere.com',
    ],
    'packages' => [
        'windows-setup' => [
            'label' => 'Windows Setup',
            'filename' => 'SNAPVERE-Setup.exe',
            'size' => 336526148,
            'sha256' => 'dd08c51953908d43adc2b19357eec2f8ef0b245c3007b4cb7fe36c4a893eeb03',
            'mime' => 'application/vnd.microsoft.portable-executable',
        ],
        'windows-portable' => [
            'label' => 'Windows Portable',
            'filename' => 'SNAPVERE-Portable.exe',
            'size' => 336676694,
            'sha256' => '2866c1a558c1e19aa2329d0c63e4ffff04f5c713cba91ca6a4e9d3c1bcc67db6',
            'mime' => 'application/vnd.microsoft.portable-executable',
        ],
        'chrome' => [
            'label' => 'Chrome extension',
            'filename' => 'SNAPVERE-Chrome.zip',
            'size' => 36483,
            'sha256' => 'c407445edc4db71394488391a60e482b8996d1a0250b56b01e25cd83021cadd6',
            'mime' => 'application/zip',
        ],
        'edge' => [
            'label' => 'Edge extension',
            'filename' => 'SNAPVERE-Edge.zip',
            'size' => 36483,
            'sha256' => 'c407445edc4db71394488391a60e482b8996d1a0250b56b01e25cd83021cadd6',
            'mime' => 'application/zip',
        ],
        'firefox' => [
            'label' => 'Firefox extension',
            'filename' => 'SNAPVERE-Firefox.zip',
            'size' => 36544,
            'sha256' => 'aff90d56bcd3ddc01d334e003f94dcd64022bf6905cedefb59d758da835cb3c9',
            'mime' => 'application/zip',
        ],
        'opera' => [
            'label' => 'Opera extension',
            'filename' => 'SNAPVERE-Opera.zip',
            'size' => 36483,
            'sha256' => 'c407445edc4db71394488391a60e482b8996d1a0250b56b01e25cd83021cadd6',
            'mime' => 'application/zip',
        ],
    ],
];

$local = __DIR__ . '/config.local.php';
if (is_file($local)) {
    $override = require $local;
    if (is_array($override)) {
        $config = array_replace_recursive($config, $override);
    }
}

return $config;
