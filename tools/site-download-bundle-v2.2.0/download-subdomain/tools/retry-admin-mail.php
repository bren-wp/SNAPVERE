<?php
declare(strict_types=1);
require dirname(__DIR__) . '/api/bootstrap.php';

$path = snapvere_data_dir() . '/pending-admin-mail.jsonl';

if (!is_file($path)) {
    echo "No pending admin mail.\n";
    exit(0);
}

$lines = file($path, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES) ?: [];
$remaining = [];
$sent = 0;

foreach ($lines as $line) {
    $record = json_decode($line, true);
    if (!is_array($record)) {
        continue;
    }

    $ok = snapvere_send_mail(
        (string) ($record['to'] ?? snapvere_config()['admin_email']),
        (string) ($record['subject'] ?? '[SNAPVERE] Download'),
        (string) ($record['body'] ?? ''),
        isset($record['reply_to']) ? (string) $record['reply_to'] : null
    );

    if ($ok) {
        $sent++;
    } else {
        $remaining[] = $line;
    }
}

file_put_contents($path, $remaining ? implode(PHP_EOL, $remaining) . PHP_EOL : '', LOCK_EX);
echo "Sent: {$sent}; remaining: " . count($remaining) . "\n";
