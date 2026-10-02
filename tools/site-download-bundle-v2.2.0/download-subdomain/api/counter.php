<?php
declare(strict_types=1);
require __DIR__ . '/bootstrap.php';

snapvere_json([
    'ok' => true,
    'count' => snapvere_current_counter(),
]);
