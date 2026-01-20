<?php

require_once __DIR__ . '/vendor/autoload.php';

use Basis\Nats\Client;
use Basis\Nats\Configuration;

$natsUrl = getenv('NATS_URL') ?: 'nats://localhost:4222';
$message = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $count = (int) ($_POST['count'] ?? 0);
    $content = $_POST['content'] ?? '';

    if ($count > 0 && $content !== '') {
        try {
            $parsed = parse_url($natsUrl);
            $config = new Configuration([
                'host' => $parsed['host'] ?? 'localhost',
                'port' => $parsed['port'] ?? 4222,
                'user' => isset($parsed['user']) ? urldecode($parsed['user']) : null,
                'pass' => isset($parsed['pass']) ? urldecode($parsed['pass']) : null,
            ]);
            $client = new Client($config);

            for ($i = 0; $i < $count; $i++) {
                $client->publish('notifications', $content);
            }

            $message = "Published {$count} notification(s) successfully!";
        } catch (Exception $e) {
            $message = "Error: " . $e->getMessage();
        }
    } else {
        $message = "Please provide both count and content.";
    }
}

?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Notification Publisher</title>
    <style>
        body { font-family: sans-serif; max-width: 500px; margin: 50px auto; padding: 20px; }
        input, textarea, button { width: 100%; padding: 10px; margin: 10px 0; box-sizing: border-box; }
        button { background: #007bff; color: white; border: none; cursor: pointer; }
        button:hover { background: #0056b3; }
        .message { padding: 10px; margin: 10px 0; background: #d4edda; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; border-color: #f5c6cb; }
    </style>
</head>
<body>
    <h1>Notification Publisher</h1>
    <?php if ($message): ?>
        <div class="message <?= str_starts_with($message, 'Error') ? 'error' : '' ?>">
            <?= htmlspecialchars($message) ?>
        </div>
    <?php endif; ?>
    <form method="POST">
        <label for="count">Number of notifications:</label>
        <input type="number" id="count" name="count" min="1" value="1" required>

        <label for="content">Notification content:</label>
        <textarea id="content" name="content" rows="3" required></textarea>

        <button type="submit">Publish Notifications</button>
    </form>
</body>
</html>
