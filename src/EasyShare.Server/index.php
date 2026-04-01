<?php
/**
 * index.php - EasyShare File Sharing Frontend
 *
 * Share types (determined by meta.json):
 *   file - single file download
 *   zip  - zip archive download
 *   dir  - browsable directory listing
 */

require __DIR__ . '/config.php';
require __DIR__ . '/lang.php';

requireConfig();

// -- Helpers --

function send404() {
    http_response_code(404);
    ?><?php echoPage404(); ?>
<?php
    exit;
}

function echoPage404() {
    ?><!DOCTYPE html>
<html lang="<?= getLangCode() ?>">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title><?= t('not_found_title') ?></title>
<style>
<?php echoStyles(); ?>
</style>
</head>
<body>
<?php echoLangSwitcher(); ?>
<div class="container center">
    <div class="icon">&#x1F517;</div>
    <h1><?= t('not_found_heading') ?></h1>
    <p><?= t('not_found_text') ?></p>
</div>
</body>
</html>
<?php
}

function sendExpired() {
    http_response_code(410);
    ?><!DOCTYPE html>
<html lang="<?= getLangCode() ?>">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title><?= t('expired_title') ?></title>
<style>
<?php echoStyles(); ?>
</style>
</head>
<body>
<?php echoLangSwitcher(); ?>
<div class="container center">
    <div class="icon">&#x23F0;</div>
    <h1><?= t('expired_heading') ?></h1>
    <p><?= t('expired_text') ?></p>
</div>
</body>
</html>
<?php
    exit;
}

function echoLangSwitcher() {
    $html = langSwitcherHtml();
    if ($html) {
        echo '<div style="position:fixed;top:12px;right:16px;z-index:100;font-size:0.85em">' . $html . '</div>';
    }
}

function echoStyles() {
    ?>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
        font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
        background: #0f0f14;
        color: #e0e0e6;
        min-height: 100vh;
    }
    .container {
        max-width: 800px;
        margin: 0 auto;
        padding: 40px 20px;
    }
    .center { text-align: center; padding-top: 15vh; }
    .icon { font-size: 64px; margin-bottom: 20px; }
    h1 { font-size: 1.8em; margin-bottom: 10px; color: #fff; }
    p { color: #888; font-size: 1.1em; }
    .lang-switcher a { color: #6ea8fe; text-decoration: none; }
    .lang-switcher a:hover { text-decoration: underline; }
    .lang-switcher strong { color: #fff; }
    .lang-sep { color: #555; margin: 0 4px; }
    .breadcrumb {
        background: #1a1a24;
        border-radius: 8px;
        padding: 12px 18px;
        margin-bottom: 24px;
        font-size: 0.9em;
    }
    .breadcrumb a { color: #6ea8fe; text-decoration: none; }
    .breadcrumb a:hover { text-decoration: underline; }
    .breadcrumb .sep { color: #555; margin: 0 6px; }
    .file-list {
        list-style: none;
    }
    .file-list li {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px 16px;
        border-bottom: 1px solid #1e1e2a;
        transition: background 0.15s;
    }
    .file-list li:hover { background: #1a1a24; border-radius: 6px; }
    .file-list .file-icon { font-size: 1.3em; flex-shrink: 0; }
    .file-list a {
        color: #e0e0e6;
        text-decoration: none;
        flex: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .file-list a:hover { color: #6ea8fe; }
    .file-list .size {
        color: #666;
        font-size: 0.85em;
        white-space: nowrap;
    }
    .dl-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 8px;
        border-radius: 5px;
        background: transparent;
        color: #6ea8fe;
        text-decoration: none;
        font-size: 1.1em;
        flex-shrink: 0;
        align-self: center;
        margin-left: auto;
        transition: color 0.2s, background 0.2s;
    }
    .dl-btn:hover {
        background: rgba(110,168,254,0.12);
        color: #8cbbff;
    }
    .download-box {
        background: #1a1a24;
        border: 1px solid #2a2a3a;
        border-radius: 12px;
        padding: 40px;
        text-align: center;
        margin-top: 30px;
    }
    .download-box .file-name {
        font-size: 1.2em;
        color: #fff;
        margin: 16px 0 8px;
        word-break: break-all;
    }
    .download-box .file-size {
        color: #888;
        margin-bottom: 24px;
    }
    .btn {
        display: inline-block;
        background: #6ea8fe;
        color: #000;
        padding: 12px 32px;
        border-radius: 8px;
        text-decoration: none;
        font-weight: 600;
        font-size: 1em;
        transition: background 0.2s;
    }
    .btn:hover { background: #8cbbff; }
    .expiry-banner {
        background: #1c1c2e;
        border: 1px solid #2a2a40;
        border-left: 3px solid #f0ad4e;
        border-radius: 6px;
        padding: 10px 16px;
        margin-bottom: 20px;
        font-size: 0.9em;
        color: #ccc;
    }
    .file-list li.has-thumb {
        align-items: flex-start;
        padding: 8px 16px;
    }
    .thumb {
        width: 80px;
        height: 60px;
        object-fit: cover;
        border-radius: 6px;
        flex-shrink: 0;
        background: #1a1a24;
        cursor: pointer;
        transition: transform 0.2s, box-shadow 0.2s;
    }
    .thumb:hover {
        transform: scale(1.05);
        box-shadow: 0 4px 16px rgba(110,168,254,0.3);
    }
    .file-info {
        display: flex;
        flex-direction: column;
        gap: 2px;
        flex: 1;
        min-width: 0;
    }
    .file-info a {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .file-info .size {
        color: #666;
        font-size: 0.85em;
    }
    /* Lightbox */
    .lightbox {
        display: none;
        position: fixed;
        inset: 0;
        background: rgba(0,0,0,0.9);
        z-index: 1000;
        justify-content: center;
        align-items: center;
        cursor: pointer;
    }
    .lightbox.active { display: flex; }
    .lightbox img {
        max-width: 95vw;
        max-height: 95vh;
        object-fit: contain;
        border-radius: 8px;
        box-shadow: 0 0 40px rgba(0,0,0,0.5);
    }
    .preview-img {
        max-width: 100%;
        max-height: 400px;
        object-fit: contain;
        border-radius: 10px;
        margin-bottom: 8px;
        cursor: pointer;
        transition: transform 0.2s, box-shadow 0.2s;
    }
    .preview-img:hover {
        transform: scale(1.02);
        box-shadow: 0 8px 32px rgba(110,168,254,0.25);
    }
    <?php
}

function isImage($name) {
    $ext = strtolower(pathinfo($name, PATHINFO_EXTENSION));
    return in_array($ext, ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg', 'ico']);
}

function formatSize($bytes) {
    if ($bytes < 1024) return $bytes . ' B';
    if ($bytes < 1048576) return round($bytes / 1024, 1) . ' KB';
    if ($bytes < 1073741824) return round($bytes / 1048576, 1) . ' MB';
    return round($bytes / 1073741824, 2) . ' GB';
}

function formatExpiry($expiresTime) {
    $diff = $expiresTime - time();
    if ($diff <= 0) return t('expiry_expired');
    if ($diff < 60) return t('expiry_less_minute');
    if ($diff < 3600) return str_replace('{n}', (string)ceil($diff / 60), t('expiry_minutes'));
    if ($diff < 86400) return str_replace('{n}', (string)ceil($diff / 3600), t('expiry_hours'));
    return str_replace('{n}', (string)ceil($diff / 86400), t('expiry_days'));
}

function expiryBannerHtml($meta) {
    if (!isset($meta['expires'])) return '';
    $expiresTime = strtotime($meta['expires']);
    if ($expiresTime === false) return '';
    $dateStr = date('j.n.Y H:i', $expiresTime);
    $relStr = formatExpiry($expiresTime);
    $text = str_replace(['{date}', '{relative}'], [$dateStr, $relStr], t('expiry_banner'));
    return '<div class="expiry-banner">' . $text . '</div>';
}

function getFileIcon($name, $isDir) {
    if ($isDir) return '&#x1F4C1;';
    $ext = strtolower(pathinfo($name, PATHINFO_EXTENSION));
    $icons = [
        'pdf' => '&#x1F4D5;', 'doc' => '&#x1F4C4;', 'docx' => '&#x1F4C4;',
        'xls' => '&#x1F4CA;', 'xlsx' => '&#x1F4CA;',
        'txt' => '&#x1F4DD;', 'csv' => '&#x1F4CA;', 'log' => '&#x1F4DD;',
        'zip' => '&#x1F4E6;', 'rar' => '&#x1F4E6;', '7z' => '&#x1F4E6;',
        'tar' => '&#x1F4E6;', 'gz' => '&#x1F4E6;',
        'jpg' => '&#x1F5BC;', 'jpeg' => '&#x1F5BC;', 'png' => '&#x1F5BC;',
        'gif' => '&#x1F5BC;', 'bmp' => '&#x1F5BC;', 'svg' => '&#x1F5BC;',
        'webp' => '&#x1F5BC;', 'ico' => '&#x1F5BC;',
        'mp4' => '&#x1F3AC;', 'avi' => '&#x1F3AC;', 'mkv' => '&#x1F3AC;',
        'mp3' => '&#x1F3B5;', 'wav' => '&#x1F3B5;',
        'exe' => '&#x2699;', 'msi' => '&#x2699;',
        'html' => '&#x1F310;', 'htm' => '&#x1F310;',
        'json' => '&#x1F4CB;', 'xml' => '&#x1F4CB;',
    ];
    return $icons[$ext] ?? '&#x1F4C4;';
}

function getMimeType($file) {
    $ext = strtolower(pathinfo($file, PATHINFO_EXTENSION));
    $mimes = [
        'pdf' => 'application/pdf',
        'zip' => 'application/zip',
        'doc' => 'application/msword',
        'docx' => 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'xls' => 'application/vnd.ms-excel',
        'xlsx' => 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'jpg' => 'image/jpeg', 'jpeg' => 'image/jpeg',
        'png' => 'image/png', 'gif' => 'image/gif', 'svg' => 'image/svg+xml',
        'webp' => 'image/webp', 'bmp' => 'image/bmp', 'ico' => 'image/x-icon',
        'mp4' => 'video/mp4', 'avi' => 'video/x-msvideo', 'mkv' => 'video/x-matroska',
        'mp3' => 'audio/mpeg', 'wav' => 'audio/wav', 'ogg' => 'audio/ogg',
        'txt' => 'text/plain', 'csv' => 'text/csv', 'log' => 'text/plain',
        'html' => 'text/html', 'htm' => 'text/html', 'css' => 'text/css',
        'js' => 'application/javascript', 'json' => 'application/json',
        'xml' => 'application/xml',
        'exe' => 'application/octet-stream',
        '7z' => 'application/x-7z-compressed',
        'rar' => 'application/x-rar-compressed',
        'tar' => 'application/x-tar',
        'gz' => 'application/gzip',
    ];
    return $mimes[$ext] ?? 'application/octet-stream';
}

function getSiteName() {
    return parse_url(BASE_URL, PHP_URL_HOST) ?? 'EasyShare';
}

// -- Main Logic --

$key = $_GET['key'] ?? '';
$subpath = $_GET['path'] ?? '';

if (!preg_match('/^[a-zA-Z0-9]{4,32}$/', $key)) {
    send404();
}

$shareDir = DATA_DIR . '/' . $key;
$metaFile = $shareDir . '/meta.json';

if (!is_dir($shareDir) || !is_file($metaFile)) {
    send404();
}

$meta = json_decode(file_get_contents($metaFile), true);
if (!$meta || !isset($meta['type'])) {
    send404();
}

$type = $meta['type'];
$originalName = $meta['name'] ?? 'file';
$siteName = getSiteName();

// Check expiration
if (isset($meta['expires'])) {
    $expiresTime = strtotime($meta['expires']);
    if ($expiresTime !== false && time() > $expiresTime) {
        sendExpired();
    }
}

// -- File / Zip download --

if ($type === 'file' || $type === 'zip') {
    $files = array_diff(scandir($shareDir), ['.', '..', 'meta.json']);
    if (empty($files)) {
        send404();
    }
    $fileName = reset($files);
    $filePath = $shareDir . '/' . $fileName;

    if (!is_file($filePath)) {
        send404();
    }

    // Serve image inline for preview
    if (isset($_GET['preview']) && isImage($fileName)) {
        header('Content-Type: ' . getMimeType($fileName));
        header('Content-Disposition: inline; filename="' . addslashes($originalName) . '"');
        header('Content-Length: ' . filesize($filePath));
        header('Cache-Control: public, max-age=86400');
        readfile($filePath);
        exit;
    }

    // Show download page
    if (!isset($_GET['download'])) {
        $fileSize = filesize($filePath);
        $isImg = isImage($fileName);
        $previewUrl = '?key=' . urlencode($key) . '&preview=1';
        ?><!DOCTYPE html>
<html lang="<?= getLangCode() ?>">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title><?= htmlspecialchars($originalName) ?> &ndash; <?= htmlspecialchars($siteName) ?></title>
<?php if ($isImg): ?>
<?php
    $ogImageUrl = BASE_URL . '/' . urlencode($key) . '?preview=1';
    $ogTitle = htmlspecialchars($originalName);
    $ogPageUrl = BASE_URL . '/' . urlencode($key);
?>
<meta property="og:type" content="website">
<meta property="og:title" content="<?= $ogTitle ?>">
<meta property="og:description" content="<?= $ogTitle ?> (<?= formatSize($fileSize) ?>)">
<meta property="og:image" content="<?= $ogImageUrl ?>">
<meta property="og:image:type" content="<?= getMimeType($fileName) ?>">
<meta property="og:url" content="<?= $ogPageUrl ?>">
<meta property="og:site_name" content="<?= htmlspecialchars($siteName) ?>">
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="<?= $ogTitle ?>">
<meta name="twitter:image" content="<?= $ogImageUrl ?>">
<?php else: ?>
<?php
    $fileExt = strtolower(pathinfo($originalName, PATHINFO_EXTENSION));
    $ogImageUrl = BASE_URL . '/og_icon.php?mode=file&ext=' . urlencode($fileExt) . '&name=' . urlencode($originalName);
    $ogTitle = htmlspecialchars($originalName);
    $ogPageUrl = BASE_URL . '/' . urlencode($key);
?>
<meta property="og:type" content="website">
<meta property="og:title" content="<?= $ogTitle ?>">
<meta property="og:description" content="<?= $ogTitle ?> (<?= formatSize($fileSize) ?>)">
<meta property="og:image" content="<?= $ogImageUrl ?>">
<meta property="og:image:type" content="image/png">
<meta property="og:image:width" content="1200">
<meta property="og:image:height" content="630">
<meta property="og:url" content="<?= $ogPageUrl ?>">
<meta property="og:site_name" content="<?= htmlspecialchars($siteName) ?>">
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="<?= $ogTitle ?>">
<meta name="twitter:image" content="<?= $ogImageUrl ?>">
<?php endif; ?>
<style>
<?php echoStyles(); ?>
</style>
</head>
<body>
<?php echoLangSwitcher(); ?>
<div class="container">
    <div class="download-box">
<?php if ($isImg): ?>
        <img class="preview-img" src="<?= $previewUrl ?>" alt="<?= htmlspecialchars($originalName) ?>" onclick="openLightbox('<?= $previewUrl ?>')">
<?php else: ?>
        <div style="font-size:48px"><?= getFileIcon($fileName, false) ?></div>
<?php endif; ?>
        <div class="file-name"><?= htmlspecialchars($originalName) ?></div>
        <div class="file-size"><?= formatSize($fileSize) ?></div>
        <a class="btn" href="?key=<?= urlencode($key) ?>&download=1"><?= t('download_btn') ?></a>
    </div>
    <?= expiryBannerHtml($meta) ?>
</div>

<?php if ($isImg): ?>
<div class="lightbox" id="lightbox" onclick="closeLightbox()">
    <img id="lightboxImg" src="" alt="Preview">
</div>
<script>
function openLightbox(src) {
    document.getElementById('lightboxImg').src = src;
    document.getElementById('lightbox').classList.add('active');
}
function closeLightbox() {
    document.getElementById('lightbox').classList.remove('active');
}
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') closeLightbox();
});
</script>
<?php endif; ?>

</body>
</html>
<?php
        exit;
    }

    // Send file for download
    header('Content-Type: ' . getMimeType($fileName));
    header('Content-Disposition: attachment; filename="' . addslashes($originalName) . '"');
    header('Content-Length: ' . filesize($filePath));
    header('Cache-Control: no-cache, must-revalidate');
    readfile($filePath);
    exit;
}

// -- Directory browsing --

if ($type === 'dir') {
    $subpath = str_replace('\\', '/', $subpath);
    $subpath = trim($subpath, '/');

    $parts = explode('/', $subpath);
    $clean = [];
    foreach ($parts as $part) {
        if ($part === '' || $part === '.') continue;
        if ($part === '..') continue;
        $clean[] = $part;
    }
    $subpath = implode('/', $clean);

    $browsePath = $shareDir . ($subpath ? '/' . $subpath : '');

    $realBrowse = realpath($browsePath);
    $realShare = realpath($shareDir);
    if ($realBrowse === false || strpos($realBrowse, $realShare) !== 0) {
        send404();
    }

    if (is_file($realBrowse)) {
        $fileName = basename($realBrowse);
        $mime = getMimeType($fileName);
        header('Content-Type: ' . $mime);
        if (isImage($fileName)) {
            header('Content-Disposition: inline; filename="' . addslashes($fileName) . '"');
            header('Cache-Control: public, max-age=86400');
        } else {
            header('Content-Disposition: attachment; filename="' . addslashes($fileName) . '"');
            header('Cache-Control: no-cache, must-revalidate');
        }
        header('Content-Length: ' . filesize($realBrowse));
        readfile($realBrowse);
        exit;
    }

    if (!is_dir($realBrowse)) {
        send404();
    }

    $entries = array_diff(scandir($realBrowse), ['.', '..']);
    if ($subpath === '') {
        $entries = array_diff($entries, ['meta.json']);
    }

    $dirs = [];
    $files = [];
    foreach ($entries as $entry) {
        $fullPath = $realBrowse . '/' . $entry;
        if (is_dir($fullPath)) {
            $dirs[] = $entry;
        } else {
            $files[] = ['name' => $entry, 'size' => filesize($fullPath)];
        }
    }
    sort($dirs);
    usort($files, fn($a, $b) => strcasecmp($a['name'], $b['name']));

    $breadcrumbParts = $subpath ? explode('/', $subpath) : [];
    $baseUrl = BASE_URL . '/' . $key;

    ?><!DOCTYPE html>
<html lang="<?= getLangCode() ?>">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title><?= htmlspecialchars($originalName) ?><?= $subpath ? ' / ' . htmlspecialchars($subpath) : '' ?> &ndash; <?= htmlspecialchars($siteName) ?></title>
<?php
    $ogDirImageUrl = BASE_URL . '/og_icon.php?mode=dir&name=' . urlencode($originalName);
    $ogDirTitle = htmlspecialchars($originalName) . ($subpath ? ' / ' . htmlspecialchars($subpath) : '');
    $ogDirPageUrl = BASE_URL . '/' . urlencode($key) . ($subpath ? '/' . htmlspecialchars($subpath) : '');
    $ogDirDesc = str_replace('{site}', $siteName, t('og_dir_desc'));
?>
<meta property="og:type" content="website">
<meta property="og:title" content="<?= $ogDirTitle ?>">
<meta property="og:description" content="<?= htmlspecialchars($ogDirDesc) ?>">
<meta property="og:image" content="<?= $ogDirImageUrl ?>">
<meta property="og:image:type" content="image/png">
<meta property="og:image:width" content="1200">
<meta property="og:image:height" content="630">
<meta property="og:url" content="<?= $ogDirPageUrl ?>">
<meta property="og:site_name" content="<?= htmlspecialchars($siteName) ?>">
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="<?= $ogDirTitle ?>">
<meta name="twitter:image" content="<?= $ogDirImageUrl ?>">
<meta name="twitter:description" content="<?= htmlspecialchars($ogDirDesc) ?>">
<style>
<?php echoStyles(); ?>
</style>
</head>
<body>
<?php echoLangSwitcher(); ?>
<div class="container">
    <div class="breadcrumb">
        <a href="<?= $baseUrl ?>"><?= htmlspecialchars($originalName) ?></a>
<?php
    $crumbPath = '';
    foreach ($breadcrumbParts as $crumb) {
        $crumbPath .= ($crumbPath ? '/' : '') . $crumb;
        echo '        <span class="sep">/</span>';
        echo '<a href="' . $baseUrl . '/' . htmlspecialchars($crumbPath) . '">' . htmlspecialchars($crumb) . '</a>';
        echo "\n";
    }
?>
    </div>

    <?= expiryBannerHtml($meta) ?>

    <ul class="file-list">
<?php if ($subpath): ?>
        <li>
            <span class="file-icon">&#x2B06;&#xFE0F;</span>
            <a href="<?php
                $parentPath = dirname($subpath);
                echo $parentPath === '.' ? $baseUrl : $baseUrl . '/' . htmlspecialchars($parentPath);
            ?>">.. </a>
        </li>
<?php endif; ?>
<?php foreach ($dirs as $dir): ?>
        <li>
            <span class="file-icon">&#x1F4C1;</span>
            <a href="<?= $baseUrl . '/' . htmlspecialchars(($subpath ? $subpath . '/' : '') . $dir) ?>"><?= htmlspecialchars($dir) ?></a>
        </li>
<?php endforeach; ?>
<?php foreach ($files as $file):
    $fileUrl = $baseUrl . '/' . htmlspecialchars(($subpath ? $subpath . '/' : '') . $file['name']);
    $isImg = isImage($file['name']);
?>
        <li<?= $isImg ? ' class="has-thumb"' : '' ?>>
<?php if ($isImg): ?>
            <img class="thumb" src="<?= $fileUrl ?>" alt="<?= htmlspecialchars($file['name']) ?>" loading="lazy" onclick="openLightbox(this.src)">
            <div class="file-info">
                <a href="<?= $fileUrl ?>"><?= htmlspecialchars($file['name']) ?></a>
                <span class="size"><?= formatSize($file['size']) ?></span>
            </div>
            <a class="dl-btn" href="<?= $fileUrl ?>" download="<?= htmlspecialchars($file['name']) ?>" title="<?= t('download_btn') ?>">&#x2B07;</a>
<?php else: ?>
            <span class="file-icon"><?= getFileIcon($file['name'], false) ?></span>
            <a href="<?= $fileUrl ?>"><?= htmlspecialchars($file['name']) ?></a>
            <span class="size"><?= formatSize($file['size']) ?></span>
            <a class="dl-btn" href="<?= $fileUrl ?>" download="<?= htmlspecialchars($file['name']) ?>" title="<?= t('download_btn') ?>">&#x2B07;</a>
<?php endif; ?>
        </li>
<?php endforeach; ?>
    </ul>

<?php if (empty($dirs) && empty($files)): ?>
    <p style="text-align:center; color:#555; margin-top:40px"><?= t('dir_empty') ?></p>
<?php endif; ?>
</div>

<div class="lightbox" id="lightbox" onclick="closeLightbox()">
    <img id="lightboxImg" src="" alt="Preview">
</div>
<script>
function openLightbox(src) {
    document.getElementById('lightboxImg').src = src;
    document.getElementById('lightbox').classList.add('active');
}
function closeLightbox() {
    document.getElementById('lightbox').classList.remove('active');
}
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') closeLightbox();
});
</script>
</body>
</html>
<?php
    exit;
}

// Unknown type
send404();
