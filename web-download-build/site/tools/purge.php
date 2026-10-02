<?php
declare(strict_types=1);

require __DIR__ . '/../lib/app.php';

$days = max(1, (int) (snap_config()['lead_retention_days'] ?? 90));
$cutoff = time() - ($days * 86400);
$targets = [
    'download-leads.jsonl',
    'download-events.jsonl',
    'download-mail-status.jsonl',
    'mail-spool.jsonl',
];

$maintenance = fopen(snap_data_path('maintenance.lock'), 'c+');
if ($maintenance === false || !flock($maintenance, LOCK_EX)) {
    fwrite(STDERR, "Unable to acquire maintenance lock.\n");
    exit(1);
}

try {
    foreach ($targets as $name) {
        $path = snap_data_path($name);
        if (!is_file($path)) {
            echo "SKIP — {$name} does not exist\n";
            continue;
        }

        $source = fopen($path, 'rb');
        if ($source === false) {
            fwrite(STDERR, "Unable to open {$name}\n");
            continue;
        }

        $tmp = $path . '.purge-' . bin2hex(random_bytes(4));
        $dest = fopen($tmp, 'wb');
        if ($dest === false) {
            fclose($source);
            fwrite(STDERR, "Unable to create temporary file for {$name}\n");
            continue;
        }

        $kept = 0;
        $removed = 0;

        while (($line = fgets($source)) !== false) {
            $row = json_decode($line, true);
            $createdAt = is_array($row) ? (string) ($row['created_at'] ?? '') : '';
            $timestamp = $createdAt !== '' ? strtotime($createdAt) : false;

            if ($timestamp !== false && $timestamp < $cutoff) {
                $removed++;
                continue;
            }

            fwrite($dest, $line);
            $kept++;
        }

        fclose($source);
        fflush($dest);
        fclose($dest);
        @chmod($tmp, 0600);

        if (!rename($tmp, $path)) {
            @unlink($tmp);
            fwrite(STDERR, "Unable to replace {$name}\n");
            continue;
        }

        echo "OK — {$name}: kept {$kept}, removed {$removed}\n";
    }
} finally {
    flock($maintenance, LOCK_UN);
    fclose($maintenance);
}
