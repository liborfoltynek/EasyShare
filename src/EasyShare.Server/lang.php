<?php
/**
 * lang.php - Translation system
 *
 * Loads translations from lang/*.json files.
 * Language detection: ?lang= query -> cookie -> Accept-Language -> fallback 'en'
 *
 * Usage:
 *   require 'lang.php';
 *   echo t('download');     // translated string
 *   echo langSwitcherHtml(); // language switcher HTML
 */

$_LANG = [];
$_LANG_CODE = 'en';

/**
 * Get list of available languages by scanning lang/ directory.
 */
function getAvailableLanguages(): array {
    $dir = __DIR__ . '/lang';
    if (!is_dir($dir)) {
        return ['en'];
    }

    $langs = [];
    foreach (glob($dir . '/*.json') as $file) {
        $code = pathinfo($file, PATHINFO_FILENAME);
        $langs[] = $code;
    }
    sort($langs);
    return $langs ?: ['en'];
}

/**
 * Detect current language from query, cookie, or Accept-Language header.
 */
function detectLanguage(): string {
    $available = getAvailableLanguages();

    // 1. Query parameter ?lang=xx
    if (isset($_GET['lang']) && in_array($_GET['lang'], $available)) {
        $lang = $_GET['lang'];
        setcookie('lang', $lang, time() + 365 * 86400, '/');
        return $lang;
    }

    // 2. Cookie
    if (isset($_COOKIE['lang']) && in_array($_COOKIE['lang'], $available)) {
        return $_COOKIE['lang'];
    }

    // 3. Accept-Language header
    $header = $_SERVER['HTTP_ACCEPT_LANGUAGE'] ?? '';
    if ($header) {
        preg_match_all('/([a-z]{2})(?:-[A-Z]{2})?(?:;q=[\d.]+)?/', $header, $matches);
        if (!empty($matches[1])) {
            foreach ($matches[1] as $code) {
                if (in_array($code, $available)) {
                    return $code;
                }
            }
        }
    }

    // 4. Fallback
    return in_array('en', $available) ? 'en' : $available[0];
}

/**
 * Load translations for the detected language.
 */
function initLang(): void {
    global $_LANG, $_LANG_CODE;

    $_LANG_CODE = detectLanguage();
    $file = __DIR__ . '/lang/' . $_LANG_CODE . '.json';

    if (file_exists($file)) {
        $json = file_get_contents($file);
        $_LANG = json_decode($json, true) ?: [];
    }

    // Fallback to English if key missing
    if ($_LANG_CODE !== 'en') {
        $enFile = __DIR__ . '/lang/en.json';
        if (file_exists($enFile)) {
            $enLang = json_decode(file_get_contents($enFile), true) ?: [];
            $_LANG = array_merge($enLang, $_LANG);
        }
    }
}

/**
 * Translate a key. Returns the key itself if no translation found.
 */
function t(string $key): string {
    global $_LANG;
    return $_LANG[$key] ?? $key;
}

/**
 * Get current language code.
 */
function getLangCode(): string {
    global $_LANG_CODE;
    return $_LANG_CODE;
}

/**
 * Language names for display.
 */
function getLangName(string $code): string {
    $names = [
        'cs' => 'Čeština',
        'en' => 'English',
        'de' => 'Deutsch',
        'sk' => 'Slovenčina',
        'pl' => 'Polski',
        'fr' => 'Français',
        'es' => 'Español',
    ];
    return $names[$code] ?? strtoupper($code);
}

/**
 * Generate HTML for language switcher.
 */
function langSwitcherHtml(): string {
    $available = getAvailableLanguages();
    if (count($available) <= 1) {
        return '';
    }

    $current = getLangCode();
    $html = '<div class="lang-switcher">';

    foreach ($available as $i => $code) {
        if ($i > 0) {
            $html .= ' <span class="lang-sep">|</span> ';
        }

        if ($code === $current) {
            $html .= '<strong>' . htmlspecialchars(getLangName($code)) . '</strong>';
        } else {
            $url = '?' . http_build_query(array_merge($_GET, ['lang' => $code]));
            $html .= '<a href="' . htmlspecialchars($url) . '">' . htmlspecialchars(getLangName($code)) . '</a>';
        }
    }

    $html .= '</div>';
    return $html;
}

// Auto-initialize on include
initLang();
