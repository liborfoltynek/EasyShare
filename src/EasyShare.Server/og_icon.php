<?php
/**
 * og_icon.php - Dynamic OG preview image generator
 * Generates 1200x630 PNG images for social media link previews (WhatsApp, Telegram, etc.)
 *
 * Usage:
 *   og_icon.php?mode=file&ext=pdf&name=document.pdf
 *   og_icon.php?mode=dir&name=MyFolder
 */

if (!function_exists('imagecreatetruecolor')) {
    http_response_code(500);
    exit;
}

require __DIR__ . '/lang.php';

// -- Parameters --
$mode = $_GET['mode'] ?? 'file';
$ext  = strtoupper(trim($_GET['ext'] ?? ''));
$name = $_GET['name'] ?? '';

$W = 1200;
$H = 630;

// -- Font setup (Windows/IIS) --
$fontReg  = 'C:/Windows/Fonts/segoeui.ttf';
$fontBold = 'C:/Windows/Fonts/segoeuib.ttf';
$useTTF   = file_exists($fontReg) && function_exists('imagettftext');
if ($useTTF && !file_exists($fontBold)) {
    $fontBold = $fontReg;
}

// -- Create canvas --
$img = imagecreatetruecolor($W, $H);
imagealphablending($img, true);
imagesavealpha($img, true);

// -- Palette --
$cBg      = imagecolorallocate($img, 15, 15, 20);       // #0f0f14
$cCard    = imagecolorallocate($img, 26, 26, 36);        // card background
$cBorder  = imagecolorallocate($img, 42, 42, 58);
$cWhite   = imagecolorallocate($img, 255, 255, 255);
$cLight   = imagecolorallocate($img, 210, 210, 220);
$cMuted   = imagecolorallocate($img, 120, 120, 140);
$cAccent  = imagecolorallocate($img, 110, 168, 254);     // site accent

// Extension → accent colour
$extColors = [
    'PDF'  => [220, 53, 69],   'DOC'  => [40, 100, 210],  'DOCX' => [40, 100, 210],
    'ODT'  => [40, 100, 210],  'RTF'  => [40, 100, 210],
    'XLS'  => [25, 135, 84],   'XLSX' => [25, 135, 84],   'CSV'  => [25, 135, 84],
    'ZIP'  => [200, 160, 20],  'RAR'  => [200, 160, 20],  '7Z'   => [200, 160, 20],
    'TAR'  => [200, 160, 20],  'GZ'   => [200, 160, 20],
    'MP4'  => [111, 66, 193],  'AVI'  => [111, 66, 193],  'MKV'  => [111, 66, 193],
    'MOV'  => [111, 66, 193],
    'MP3'  => [214, 51, 132],  'WAV'  => [214, 51, 132],  'OGG'  => [214, 51, 132],
    'FLAC' => [214, 51, 132],
    'TXT'  => [108, 117, 135], 'LOG'  => [108, 117, 135], 'MD'   => [108, 117, 135],
    'HTML' => [253, 126, 20],  'JSON' => [253, 126, 20],  'XML'  => [253, 126, 20],
    'JS'   => [253, 126, 20],  'CSS'  => [253, 126, 20],
    'EXE'  => [13, 202, 240],  'MSI'  => [13, 202, 240],
];
$rgb = $extColors[$ext] ?? [110, 168, 254];
$cExt     = imagecolorallocate($img, $rgb[0], $rgb[1], $rgb[2]);
$cExtDark = imagecolorallocate($img, (int)($rgb[0]*0.55), (int)($rgb[1]*0.55), (int)($rgb[2]*0.55));

// -- Background --
imagefill($img, 0, 0, $cBg);

// Subtle vignette (top & bottom)
for ($i = 0; $i < 50; $i++) {
    $a = 127 - (int)($i * 1.8);
    if ($a < 0) break;
    $gc = imagecolorallocatealpha($img, 0, 0, 0, $a);
    imageline($img, 0, $i, $W, $i, $gc);
    imageline($img, 0, $H - 1 - $i, $W, $H - 1 - $i, $gc);
}

// -- Render --
if ($mode === 'dir') {
    renderDirIcon($img, $W, $H, $name);
} else {
    renderFileIcon($img, $W, $H, $ext, $name, $cExt, $cExtDark);
}

// Site branding bottom-centre
// Site branding — use configured base URL hostname or 'EasyShare'
$brandHost = 'EasyShare';
if (file_exists(__DIR__ . '/config.php')) {
    require_once __DIR__ . '/config.php';
    if (isConfigured()) {
        $cfg = loadConfig();
        $brandHost = parse_url($cfg['BaseUrl'] ?? '', PHP_URL_HOST) ?: 'EasyShare';
    }
}
ttfText($img, $brandHost, $W / 2, $H - 28, $cMuted, 16, true);

// -- Output --
header('Content-Type: image/png');
header('Cache-Control: public, max-age=604800');
imagepng($img, null, 7);
imagedestroy($img);
exit;

// ============================================================
// Helper: draw text (TTF with fallback)
// ============================================================
function ttfText($img, $text, $x, $y, $color, $size = 20, $center = false, $bold = false) {
    global $useTTF, $fontReg, $fontBold;
    $font = $bold ? $fontBold : $fontReg;

    if ($useTTF) {
        $bb = imagettfbbox($size, 0, $font, $text);
        $tw = $bb[2] - $bb[0];
        $th = $bb[1] - $bb[7];
        if ($center) $x -= $tw / 2;
        imagettftext($img, $size, 0, (int)$x, (int)($y + $th / 2), $color, $font, $text);
    } else {
        $fs = min(5, max(1, (int)($size / 4)));
        $tw = imagefontwidth($fs) * strlen($text);
        if ($center) $x -= $tw / 2;
        imagestring($img, $fs, (int)$x, (int)($y - imagefontheight($fs) / 2), $text, $color);
    }
}

// Rounded-rect fill
function roundedRect($img, $x1, $y1, $x2, $y2, $r, $c) {
    imagefilledrectangle($img, $x1 + $r, $y1, $x2 - $r, $y2, $c);
    imagefilledrectangle($img, $x1, $y1 + $r, $x2, $y2 - $r, $c);
    imagefilledellipse($img, $x1 + $r, $y1 + $r, $r * 2, $r * 2, $c);
    imagefilledellipse($img, $x2 - $r, $y1 + $r, $r * 2, $r * 2, $c);
    imagefilledellipse($img, $x1 + $r, $y2 - $r, $r * 2, $r * 2, $c);
    imagefilledellipse($img, $x2 - $r, $y2 - $r, $r * 2, $r * 2, $c);
}

// ============================================================
// FILE icon
// ============================================================
function renderFileIcon($img, $W, $H, $ext, $name, $cExt, $cExtDark) {
    global $cWhite, $cLight, $cMuted, $cCard, $cBorder;

    $cx = $W / 2;

    // -- Document shape --
    $icoW = 200;
    $icoH = 260;
    $fold = 50;
    $ix = (int)($cx - $icoW / 2);
    $iy = 70;

    // Shadow
    $sh = imagecolorallocatealpha($img, 0, 0, 0, 80);
    imagefilledrectangle($img, $ix + 8, $iy + 8, $ix + $icoW + 8, $iy + $icoH + 8, $sh);

    // Body
    $body = imagecolorallocate($img, 50, 50, 72);
    imagefilledrectangle($img, $ix, $iy + $fold, $ix + $icoW, $iy + $icoH, $body);
    imagefilledrectangle($img, $ix, $iy, $ix + $icoW - $fold, $iy + $fold, $body);

    // Folded corner
    $foldC = imagecolorallocate($img, 65, 65, 92);
    $pts = [$ix + $icoW - $fold, $iy,  $ix + $icoW, $iy + $fold,  $ix + $icoW - $fold, $iy + $fold];
    imagefilledpolygon($img, $pts, 3, $foldC);

    // Fake text lines
    $lineC = imagecolorallocate($img, 60, 60, 82);
    for ($i = 0; $i < 3; $i++) {
        $ly = $iy + $fold + 22 + $i * 20;
        $lw = ($i === 2) ? (int)($icoW * 0.45) : (int)($icoW * 0.65);
        imagefilledrectangle($img, $ix + 24, $ly, $ix + 24 + $lw, $ly + 7, $lineC);
    }

    // Extension badge
    if ($ext) {
        $bh = 56;
        $bw = max(100, strlen($ext) * 28 + 40);
        $bx1 = (int)($cx - $bw / 2);
        $by1 = (int)($iy + $icoH / 2 + 10);
        roundedRect($img, $bx1, $by1, $bx1 + $bw, $by1 + $bh, 10, $cExt);
        ttfText($img, $ext, $cx, $by1 + $bh / 2, $cWhite, 28, true, true);
    }

    // File name below icon
    if ($name) {
        $maxLen = 55;
        $dn = mb_strlen($name) > $maxLen ? mb_substr($name, 0, $maxLen - 3) . '...' : $name;
        ttfText($img, $dn, $cx, $iy + $icoH + 55, $cLight, 26, true);
    }

    // Subtitle
    $label = $ext ? str_replace('{ext}', $ext, t('og_file_label')) : t('og_file_download');
    ttfText($img, $label, $cx, $iy + $icoH + 95, $cMuted, 18, true);
}

// ============================================================
// DIRECTORY tree icon
// ============================================================
function renderDirIcon($img, $W, $H, $name) {
    global $cWhite, $cLight, $cMuted, $cAccent;

    $folderY = imagecolorallocate($img, 255, 200, 40);
    $folderD = imagecolorallocate($img, 195, 150, 15);
    $lineC   = imagecolorallocate($img, 60, 95, 160);
    $fileC   = imagecolorallocate($img, 90, 90, 120);
    $fileC2  = imagecolorallocate($img, 120, 120, 150);

    $sx   = (int)($W / 2 - 220);
    $rowH = 58;
    $ind  = 56;

    // Tree data: [level, isFolder, label]
    $tree = [
        [0, true,  $name ?: t('og_shared_folder')],
        [1, true,  'Dokumenty'],
        [2, false, 'report.pdf'],
        [2, false, 'zadani.docx'],
        [1, true,  'Fotky'],
        [2, false, 'IMG_001.jpg'],
        [1, false, 'readme.md'],
    ];

    foreach ($tree as $idx => $node) {
        [$lv, $isDir, $label] = $node;
        $x  = $sx + $lv * $ind;
        $cy = 55 + $idx * $rowH;

        // Connector lines
        if ($lv > 0) {
            $px = $x - $ind + 18;
            imagesetthickness($img, 2);
            // vertical
            imageline($img, $px, $cy - $rowH + 32, $px, $cy + 16, $lineC);
            // horizontal
            imageline($img, $px, $cy + 16, $x - 2, $cy + 16, $lineC);
            imagesetthickness($img, 1);

            // Extend vertical line for siblings below at same parent
            for ($j = $idx + 1; $j < count($tree); $j++) {
                if ($tree[$j][0] < $lv) break;
                if ($tree[$j][0] === $lv) {
                    imagesetthickness($img, 2);
                    imageline($img, $px, $cy + 16, $px, 55 + $j * $rowH + 16, $lineC);
                    imagesetthickness($img, 1);
                    break;
                }
            }
        }

        if ($isDir) {
            // Folder icon (30×24)
            imagefilledrectangle($img, $x, $cy + 6, $x + 30, $cy + 28, $folderY);
            imagefilledrectangle($img, $x, $cy, $x + 14, $cy + 9, $folderD);
        } else {
            // File icon (20×26)
            imagefilledrectangle($img, $x + 4, $cy + 1, $x + 24, $cy + 27, $fileC);
            $fp = [$x + 17, $cy + 1,  $x + 24, $cy + 8,  $x + 17, $cy + 8];
            imagefilledpolygon($img, $fp, 3, $fileC2);
        }

        $tc = ($lv === 0) ? $cAccent : ($isDir ? $cWhite : $cMuted);
        $fs = ($lv === 0) ? 24 : 20;
        ttfText($img, $label, $x + 38, $cy + 14, $tc, $fs, false, $isDir);
    }

    // Bottom label
    ttfText($img, t('og_shared_folder'), $W / 2, $H - 65, $cMuted, 18, true);
}
