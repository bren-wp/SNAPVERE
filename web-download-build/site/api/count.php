<?php
declare(strict_types=1);

require __DIR__ . '/../lib/app.php';

snap_security_headers();
snap_apply_cors();

if (($_SERVER['REQUEST_METHOD'] ?? 'GET') !== 'GET') {
    header('Allow: GET');
    snap_json_response(['ok' => false, 'error' => 'method_not_allowed'], 405);
}

$count = snap_counter_value();
snap_json_response([
    'ok' => true,
    'count' => $count['count'],
    'baseline' => $count['baseline'],
    'updated_at' => $count['updated_at'],
]);
