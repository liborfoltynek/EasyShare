<?php
/**
 * upload.php - HTTP Upload Endpoint
 * 
 * Accepts file uploads via HTTP POST (multipart/form-data).
 * Secured by API key in X-Api-Key header.
 * 
 * Request:
 *   POST /upload.php
 *   Header: X-Api-Key: <secret>
 *   Body: multipart/form-data with "file" field
 *   Optional query/post param: expires (e.g. 7d, 24h, 2026-12-31)
 * 
 * Response (JSON):
 *   200: {"success": true, "url": "https://...", "key": "...", "expires": "..."|null}
 *   400: {"success": false, "error": "..."}
 *   401: {"success": false, "error": "Unauthorized"}
 *   413: {"success": false, "error": "File too large"}
 *   405: {"success": false, "error": "Method not allowed"}
 *   500: {"success": false, "error": "..."}
 */

require __DIR__ . '/config.php';

// Check if configured — for upload endpoint, return JSON error instead of redirect
if (!isConfigured()) {
    header('Content-Type: application/json; charset=utf-8');
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Server not configured. Run setup.php first.']);
    exit;
}

$config = loadConfig();
define('DATA_DIR',      $config['DataDir']);
define('BASE_URL',      $config['BaseUrl']);
define('API_KEY',       $config['ApiKey']);
define('MAX_FILE_SIZE', $config['MaxFileSize'] ?? 1073741824);
define('SHARE_CODE_LENGTH', $config['ShareCodeLength'] ?? 8);

header('Content-Type: application/json; charset=utf-8');

// ── Only allow POST ────────────────────────────────────────────────────

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['success' => false, 'error' => 'Method not allowed']);
    exit;
}

// ── Authenticate ───────────────────────────────────────────────────────

$apiKey = $_SERVER['HTTP_X_API_KEY'] ?? '';
if ($apiKey !== API_KEY) {
    http_response_code(401);
    echo json_encode(['success' => false, 'error' => 'Unauthorized']);
    exit;
}

// ── Validate file ──────────────────────────────────────────────────────

if (!isset($_FILES['file']) || $_FILES['file']['error'] === UPLOAD_ERR_NO_FILE) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'No file provided']);
    exit;
}

$file = $_FILES['file'];

if ($file['error'] !== UPLOAD_ERR_OK) {
    $errorMessages = [
        UPLOAD_ERR_INI_SIZE   => 'File exceeds server upload limit',
        UPLOAD_ERR_FORM_SIZE  => 'File exceeds form upload limit',
        UPLOAD_ERR_PARTIAL    => 'File was only partially uploaded',
        UPLOAD_ERR_NO_TMP_DIR => 'Missing temporary folder',
        UPLOAD_ERR_CANT_WRITE => 'Failed to write file to disk',
        UPLOAD_ERR_EXTENSION  => 'Upload blocked by extension',
    ];
    $msg = $errorMessages[$file['error']] ?? 'Upload error (code ' . $file['error'] . ')';
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => $msg]);
    exit;
}

if ($file['size'] > MAX_FILE_SIZE) {
    http_response_code(413);
    echo json_encode(['success' => false, 'error' => 'File too large (max ' . round(MAX_FILE_SIZE / 1048576) . ' MB)']);
    exit;
}

// ── Sanitize filename ──────────────────────────────────────────────────

$originalName = basename($file['name']);
// Remove any dangerous characters
$originalName = preg_replace('/[^\w\.\-\(\)\[\] ]/', '_', $originalName);
if (empty($originalName) || $originalName === '.' || $originalName === '..') {
    $originalName = 'upload';
}

// ── Parse expiration ───────────────────────────────────────────────────

$expiresParam = $_POST['expires'] ?? $_GET['expires'] ?? null;
$expiresIso = null;

if ($expiresParam) {
    $expiresTime = parseExpiration($expiresParam);
    if ($expiresTime !== null) {
        $expiresIso = date('c', $expiresTime);
    }
}

// ── Generate key and save ──────────────────────────────────────────────

$key = generateKey();
$targetDir = DATA_DIR . '/' . $key;

if (!mkdir($targetDir, 0755, true)) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Failed to create directory']);
    exit;
}

$targetFile = $targetDir . '/' . $originalName;

if (!move_uploaded_file($file['tmp_name'], $targetFile)) {
    // Cleanup
    @rmdir($targetDir);
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Failed to save file']);
    exit;
}

// ── Write meta.json ────────────────────────────────────────────────────

$meta = [
    'type'    => 'file',
    'name'    => $originalName,
    'created' => date('c'),
];
if ($expiresIso !== null) {
    $meta['expires'] = $expiresIso;
}

$metaJson = json_encode($meta, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
file_put_contents($targetDir . '/meta.json', $metaJson);

// ── Return success ─────────────────────────────────────────────────────

$url = BASE_URL . '/' . $key;

$response = [
    'success' => true,
    'url'     => $url,
    'key'     => $key,
    'expires' => $expiresIso,
];

echo json_encode($response, JSON_UNESCAPED_UNICODE);
exit;

// ── Helper functions ───────────────────────────────────────────────────

function generateKey() {
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    $key = '';
    $bytes = random_bytes(SHARE_CODE_LENGTH);
    for ($i = 0; $i < SHARE_CODE_LENGTH; $i++) {
        $key .= $chars[ord($bytes[$i]) % strlen($chars)];
    }
    return $key;
}

/**
 * Parse expiration string. Supports:
 *   Xd = X days, Xh = X hours, Xm = X minutes
 *   yyyy-mm-dd = specific date (end of day)
 * Returns Unix timestamp or null.
 */
function parseExpiration($input) {
    $input = trim($input);
    
    // Relative: Xd, Xh, Xm
    if (preg_match('/^(\d+)([dhm])$/i', $input, $m)) {
        $value = intval($m[1]);
        $unit = strtolower($m[2]);
        $now = time();
        
        switch ($unit) {
            case 'd':
                // End of the Xth day from today
                $target = strtotime('today') + ($value * 86400) + 86400 - 1;
                return $target;
            case 'h':
                // End of the Xth hour
                $hourStart = strtotime(date('Y-m-d H:00:00'));
                return $hourStart + ($value * 3600) + 3600 - 1;
            case 'm':
                // End of the Xth minute
                $minStart = strtotime(date('Y-m-d H:i:00'));
                return $minStart + ($value * 60) + 60 - 1;
        }
    }
    
    // Absolute: yyyy-mm-dd
    $date = date_create_from_format('Y-m-d', $input);
    if ($date !== false) {
        $date->setTime(23, 59, 59);
        return $date->getTimestamp();
    }
    
    return null;
}
