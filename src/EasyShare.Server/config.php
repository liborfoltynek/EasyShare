<?php
/**
 * config.php - Central configuration loader
 *
 * Reads settings from config.json (same directory).
 * If config.json doesn't exist, redirects to setup wizard.
 *
 * Exports constants: DATA_DIR, BASE_URL, API_KEY, MAX_FILE_SIZE, SHARE_CODE_LENGTH
 */

function loadConfig(): array {
    $configFile = __DIR__ . '/config.json';

    if (!file_exists($configFile)) {
        return [];
    }

    $json = file_get_contents($configFile);
    if ($json === false) {
        return [];
    }

    $config = json_decode($json, true);
    if (!is_array($config)) {
        return [];
    }

    return $config;
}

function isConfigured(): bool {
    $config = loadConfig();
    // Require at least DataDir, BaseUrl, and ApiKey
    return !empty($config['DataDir'])
        && !empty($config['BaseUrl'])
        && !empty($config['ApiKey']);
}

function requireConfig(): void {
    if (!isConfigured()) {
        // Redirect to setup wizard
        $setupUrl = rtrim(dirname($_SERVER['SCRIPT_NAME']), '/') . '/setup.php';
        header('Location: ' . $setupUrl);
        exit;
    }

    $config = loadConfig();

    // Define constants from config
    if (!defined('DATA_DIR'))      define('DATA_DIR',      $config['DataDir']);
    if (!defined('BASE_URL'))      define('BASE_URL',      $config['BaseUrl']);
    if (!defined('API_KEY'))       define('API_KEY',       $config['ApiKey']);
    if (!defined('MAX_FILE_SIZE')) define('MAX_FILE_SIZE', $config['MaxFileSize'] ?? 1073741824);
    if (!defined('SHARE_CODE_LENGTH')) define('SHARE_CODE_LENGTH', $config['ShareCodeLength'] ?? 8);
}
