<?php
/**
 * setup.php - WordPress-style configuration wizard
 * 
 * Displayed on first run when config.json doesn't exist or is incomplete.
 * Creates config.json with user-provided values.
 */

require __DIR__ . '/lang.php';

$configFile = __DIR__ . '/config.json';
$message = '';
$messageType = '';

// If already configured, show info and allow reconfiguration
$existingConfig = [];
if (file_exists($configFile)) {
    $existingConfig = json_decode(file_get_contents($configFile), true) ?: [];
}

// ── Handle client config download ──────────────────────────────────────

if (isset($_GET['action']) && $_GET['action'] === 'download_client_config' && !empty($existingConfig)) {
    $baseUrl = rtrim($existingConfig['BaseUrl'] ?? '', '/');
    $clientConfig = [
        'UploadEndpoint'  => $baseUrl . '/upload.php',
        'ApiKey'          => $existingConfig['ApiKey'] ?? '',
        'ShareCodeLength' => $existingConfig['ShareCodeLength'] ?? 8,
    ];
    $json = json_encode($clientConfig, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
    
    $host = parse_url($baseUrl, PHP_URL_HOST) ?? 'easyshare';
    $filename = $host . '_client.json';
    
    header('Content-Type: application/json; charset=utf-8');
    header('Content-Disposition: attachment; filename="' . $filename . '"');
    header('Content-Length: ' . strlen($json));
    echo $json;
    exit;
}

// Handle form submission
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $dataDir     = trim($_POST['data_dir'] ?? '');
    $baseUrl     = rtrim(trim($_POST['base_url'] ?? ''), '/');
    $apiKey      = trim($_POST['api_key'] ?? '');
    $maxFileSize = intval($_POST['max_file_size'] ?? 1073741824);
    $codeLength   = intval($_POST['share_code_length'] ?? 8);
    
    if ($maxFileSize < 1024) $maxFileSize = 1073741824;
    if ($codeLength < 4) $codeLength = 4;
    if ($codeLength > 32) $codeLength = 32;
    
    $errors = [];
    
    if (empty($dataDir)) {
        $errors[] = t('setup_data_dir') . ' is required.';
    }
    if (empty($baseUrl)) {
        $errors[] = t('setup_base_url') . ' is required.';
    }
    if (empty($apiKey)) {
        $errors[] = t('setup_api_key') . ' is required.';
    }
    
    // Try to create data directory if it doesn't exist
    if (!empty($dataDir) && !is_dir($dataDir)) {
        @mkdir($dataDir, 0755, true);
    }
    
    if (!empty($dataDir) && !is_dir($dataDir)) {
        $errors[] = t('setup_error_dir');
    }
    
    if (empty($errors)) {
        $config = [
            'DataDir'     => $dataDir,
            'BaseUrl'     => $baseUrl,
            'ApiKey'      => $apiKey,
            'MaxFileSize' => $maxFileSize,
            'ShareCodeLength'   => $codeLength,
        ];
        
        $json = json_encode($config, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
        
        if (file_put_contents($configFile, $json) !== false) {
            $message = t('setup_success');
            $messageType = 'success';
            $existingConfig = $config;
        } else {
            $message = t('setup_error_write');
            $messageType = 'error';
        }
    } else {
        $message = implode('<br>', $errors);
        $messageType = 'error';
    }
}

// Values for form (POST data > existing config > empty)
$val = [
    'data_dir'      => $_POST['data_dir']      ?? ($existingConfig['DataDir'] ?? ''),
    'base_url'      => $_POST['base_url']       ?? ($existingConfig['BaseUrl'] ?? ''),
    'api_key'       => $_POST['api_key']        ?? ($existingConfig['ApiKey'] ?? ''),
    'max_file_size' => $_POST['max_file_size']  ?? ($existingConfig['MaxFileSize'] ?? 1073741824),
    'share_code_length' => $_POST['share_code_length'] ?? ($existingConfig['ShareCodeLength'] ?? 8),
];

$siteName = !empty($existingConfig['BaseUrl']) 
    ? parse_url($existingConfig['BaseUrl'], PHP_URL_HOST) ?? t('site_name') 
    : t('site_name');

?><!DOCTYPE html>
<html lang="<?= getLangCode() ?>">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title><?= t('setup_title') ?> &ndash; <?= htmlspecialchars($siteName) ?></title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body {
    font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
    background: #0f0f14;
    color: #e0e0e6;
    min-height: 100vh;
}
.container {
    max-width: 600px;
    margin: 0 auto;
    padding: 40px 20px;
}
.lang-switcher {
    text-align: right;
    margin-bottom: 20px;
    font-size: 0.85em;
}
.lang-switcher a { color: #6ea8fe; text-decoration: none; }
.lang-switcher a:hover { text-decoration: underline; }
.lang-switcher strong { color: #fff; }
.lang-sep { color: #555; margin: 0 4px; }
.logo {
    text-align: center;
    margin-bottom: 32px;
}
.logo .icon { font-size: 56px; margin-bottom: 12px; }
.logo h1 { font-size: 1.6em; color: #fff; margin-bottom: 4px; }
.logo p { color: #888; font-size: 0.95em; }
.card {
    background: #1a1a24;
    border: 1px solid #2a2a3a;
    border-radius: 12px;
    padding: 28px;
    margin-bottom: 20px;
}
.form-group {
    margin-bottom: 20px;
}
.form-group:last-child {
    margin-bottom: 0;
}
label {
    display: block;
    font-size: 0.9em;
    color: #ccc;
    margin-bottom: 6px;
    font-weight: 600;
}
.help-text {
    font-size: 0.8em;
    color: #666;
    margin-top: 4px;
}
input[type="text"],
input[type="number"],
input[type="password"] {
    width: 100%;
    padding: 10px 14px;
    background: #12121c;
    border: 1px solid #2a2a3a;
    border-radius: 8px;
    color: #e0e0e6;
    font-size: 0.95em;
    font-family: 'Consolas', 'Courier New', monospace;
    outline: none;
    transition: border-color 0.2s;
}
input:focus {
    border-color: #6ea8fe;
}
.input-group {
    display: flex;
    gap: 8px;
}
.input-group input { flex: 1; }
.btn {
    display: inline-block;
    padding: 10px 20px;
    border-radius: 8px;
    border: none;
    font-size: 0.95em;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.2s;
}
.btn-primary {
    background: #6ea8fe;
    color: #000;
    width: 100%;
    padding: 14px;
    font-size: 1em;
    margin-top: 8px;
}
.btn-primary:hover { background: #8cbbff; }
.btn-secondary {
    background: #2a2a3a;
    color: #e0e0e6;
    padding: 10px 16px;
    white-space: nowrap;
}
.btn-secondary:hover { background: #3a3a4a; }
.message {
    padding: 14px 18px;
    border-radius: 8px;
    margin-bottom: 20px;
    font-size: 0.9em;
}
.message.success {
    background: #1a2e1a;
    border: 1px solid #4caf50;
    color: #81c784;
}
.message.error {
    background: #2e1a1a;
    border: 1px solid #ef5350;
    color: #ef9a9a;
}
.message.success a { color: #81c784; font-weight: 600; }
</style>
</head>
<body>
<div class="container">
    <?= langSwitcherHtml() ?>
    
    <div class="logo">
        <div class="icon">⚙️</div>
        <h1><?= t('setup_heading') ?></h1>
        <p><?= t('setup_desc') ?></p>
    </div>
    
    <?php if ($message): ?>
    <div class="message <?= $messageType ?>">
        <?= $message ?>
        <?php if ($messageType === 'success'): ?>
        <br><br><a href="<?= htmlspecialchars($existingConfig['BaseUrl'] ?? '/') ?>">&rarr; <?= htmlspecialchars($siteName) ?></a>
        <?php endif; ?>
    </div>
    <?php endif; ?>
    
    <?php if (!empty($existingConfig['BaseUrl']) && !empty($existingConfig['ApiKey'])): ?>
    <div class="card" style="margin-bottom:20px">
        <div style="display:flex;align-items:center;gap:12px;margin-bottom:12px">
            <span style="font-size:24px">📲</span>
            <div>
                <div style="font-weight:600;color:#fff"><?= t('setup_client_config_title') ?></div>
                <div style="font-size:0.85em;color:#888"><?= t('setup_client_config_desc') ?></div>
            </div>
        </div>
        <a class="btn btn-secondary" href="?action=download_client_config" 
           style="display:inline-block;text-decoration:none;text-align:center">
            ⬇ <?= t('setup_client_config_download') ?>
        </a>
        <p class="help-text" style="margin-top:8px"><?= t('setup_client_config_help') ?></p>
    </div>
    <?php endif; ?>
    
    <form method="post" action="">
        <div class="card">
            <div class="form-group">
                <label for="data_dir"><?= t('setup_data_dir') ?></label>
                <input type="text" id="data_dir" name="data_dir" 
                       value="<?= htmlspecialchars($val['data_dir']) ?>"
                       placeholder="C:/web/mysite/data">
                <p class="help-text"><?= t('setup_data_dir_help') ?></p>
            </div>
            
            <div class="form-group">
                <label for="base_url"><?= t('setup_base_url') ?></label>
                <input type="text" id="base_url" name="base_url" 
                       value="<?= htmlspecialchars($val['base_url']) ?>"
                       placeholder="https://files.example.com">
                <p class="help-text"><?= t('setup_base_url_help') ?></p>
            </div>
            
            <div class="form-group">
                <label for="api_key"><?= t('setup_api_key') ?></label>
                <div class="input-group">
                    <input type="text" id="api_key" name="api_key" 
                           value="<?= htmlspecialchars($val['api_key']) ?>"
                           placeholder="">
                    <button type="button" class="btn btn-secondary" onclick="generateApiKey()"><?= t('setup_generate_key') ?></button>
                </div>
                <p class="help-text"><?= t('setup_api_key_help') ?></p>
            </div>
            
            <div class="form-group">
                <label for="max_file_size"><?= t('setup_max_file_size') ?></label>
                <input type="number" id="max_file_size" name="max_file_size" 
                       value="<?= htmlspecialchars($val['max_file_size']) ?>"
                       min="1024">
            </div>
            
            <div class="form-group">
                <label for="share_code_length"><?= t('setup_share_code_length') ?></label>
                <input type="number" id="share_code_length" name="share_code_length" 
                       value="<?= htmlspecialchars($val['share_code_length']) ?>"
                       min="4" max="32">
                <p class="help-text"><?= t('setup_share_code_length_help') ?></p>
            </div>
        </div>
        
        <button type="submit" class="btn btn-primary"><?= t('setup_save') ?></button>
    </form>
</div>

<script>
function generateApiKey() {
    const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let key = '';
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    for (let i = 0; i < 32; i++) {
        key += chars[array[i] % chars.length];
    }
    document.getElementById('api_key').value = key;
}
</script>
</body>
</html>
