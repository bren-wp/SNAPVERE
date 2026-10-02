<?php
declare(strict_types=1);

require __DIR__ . '/lib/app.php';
snap_security_headers(true);

$queryLang = strtolower((string) ($_GET['lang'] ?? ''));
$accept = strtolower((string) ($_SERVER['HTTP_ACCEPT_LANGUAGE'] ?? ''));
$lang = $queryLang === 'hr' || ($queryLang === '' && str_starts_with($accept, 'hr')) ? 'hr' : 'en';

$copy = [
    'en' => [
        'title' => 'Download SNAPVERE for Windows and browsers',
        'description' => 'Download SNAPVERE for Windows or install the Chrome, Edge, Firefox and Opera browser extensions. Email verification is required before each download.',
        'eyebrow' => 'SNAPVERE downloads',
        'headline' => 'Choose your SNAPVERE package',
        'intro' => 'Windows app and browser extensions are served directly from SNAPVERE infrastructure. Enter a valid email address before downloading.',
        'windows' => 'Windows app',
        'browsers' => 'Browser extensions',
        'download' => 'Download',
        'setup_note' => 'Guided Windows installer',
        'portable_note' => 'Run without installation',
        'extension_note' => 'ZIP package for manual browser installation',
        'email_title' => 'Email required',
        'email_intro' => 'Enter your email address to authorize this download.',
        'email_label' => 'Email address',
        'email_placeholder' => 'name@example.com',
        'continue' => 'Continue to download',
        'cancel' => 'Cancel',
        'privacy' => 'Your email is used to authorize the requested download and maintain an operational security record. It is not a newsletter signup.',
        'privacy_link' => 'Privacy',
        'terms_link' => 'Terms',
        'invalid_email' => 'Enter a valid permanent email address. Disposable email services are not accepted.',
        'generic_error' => 'The download could not be authorized. Please try again.',
        'rate_error' => 'Too many attempts. Please try again later.',
        'version' => 'Package set v0.1.22',
        'count' => 'Authorized downloads',
        'direct' => 'Local protected downloads — no GitHub redirect',
    ],
    'hr' => [
        'title' => 'Preuzmi SNAPVERE za Windows i preglednike',
        'description' => 'Preuzmi SNAPVERE za Windows ili Chrome, Edge, Firefox i Opera ekstenzije. Prije svakog preuzimanja obavezan je unos e-mail adrese.',
        'eyebrow' => 'SNAPVERE preuzimanja',
        'headline' => 'Odaberi SNAPVERE paket',
        'intro' => 'Windows aplikacija i ekstenzije poslužuju se izravno sa SNAPVERE infrastrukture. Prije preuzimanja unesi valjanu e-mail adresu.',
        'windows' => 'Windows aplikacija',
        'browsers' => 'Ekstenzije za preglednike',
        'download' => 'Preuzmi',
        'setup_note' => 'Vođena Windows instalacija',
        'portable_note' => 'Pokretanje bez instalacije',
        'extension_note' => 'ZIP paket za ručnu instalaciju u preglednik',
        'email_title' => 'E-mail je obavezan',
        'email_intro' => 'Unesi svoju e-mail adresu za autorizaciju ovog preuzimanja.',
        'email_label' => 'E-mail adresa',
        'email_placeholder' => 'ime@primjer.hr',
        'continue' => 'Nastavi na preuzimanje',
        'cancel' => 'Odustani',
        'privacy' => 'E-mail se koristi za autorizaciju zatraženog preuzimanja i operativnu sigurnosnu evidenciju. Ovo nije prijava na newsletter.',
        'privacy_link' => 'Privatnost',
        'terms_link' => 'Uvjeti',
        'invalid_email' => 'Unesi valjanu trajnu e-mail adresu. Privremene e-mail usluge nisu dopuštene.',
        'generic_error' => 'Preuzimanje nije moguće autorizirati. Pokušaj ponovno.',
        'rate_error' => 'Previše pokušaja. Pokušaj ponovno kasnije.',
        'version' => 'Set paketa v0.1.22',
        'count' => 'Autorizirana preuzimanja',
        'direct' => 'Lokalna zaštićena preuzimanja — bez GitHub preusmjeravanja',
    ],
][$lang];

$config = snap_config();
$packages = $config['packages'];
?>
<!doctype html>
<html lang="<?= htmlspecialchars($lang, ENT_QUOTES, 'UTF-8') ?>">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <title><?= htmlspecialchars($copy['title'], ENT_QUOTES, 'UTF-8') ?></title>
    <meta name="description" content="<?= htmlspecialchars($copy['description'], ENT_QUOTES, 'UTF-8') ?>">
    <meta name="robots" content="index,follow,max-image-preview:large">
    <meta name="theme-color" content="#0B0D12">
    <link rel="canonical" href="https://download.snapvere.com/">
    <link rel="alternate" hreflang="en" href="https://download.snapvere.com/?lang=en">
    <link rel="alternate" hreflang="hr" href="https://download.snapvere.com/?lang=hr">
    <link rel="alternate" hreflang="x-default" href="https://download.snapvere.com/">
    <link rel="stylesheet" href="/assets/css/download.css?v=3.0.0">
    <script src="/assets/js/download.js?v=3.0.0" defer></script>
</head>
<body>
<header class="site-header">
    <a class="brand" href="https://snapvere.com/" aria-label="SNAPVERE">
        <span class="brand-mark" aria-hidden="true">S</span>
        <span>SNAPVERE</span>
    </a>
    <nav class="language-nav" aria-label="Language">
        <a href="/?lang=en" lang="en"<?= $lang === 'en' ? ' aria-current="page"' : '' ?>>EN</a>
        <a href="/?lang=hr" lang="hr"<?= $lang === 'hr' ? ' aria-current="page"' : '' ?>>HR</a>
    </nav>
</header>

<main>
    <section class="hero">
        <p class="eyebrow"><?= htmlspecialchars($copy['eyebrow'], ENT_QUOTES, 'UTF-8') ?></p>
        <h1><?= htmlspecialchars($copy['headline'], ENT_QUOTES, 'UTF-8') ?></h1>
        <p class="hero-copy"><?= htmlspecialchars($copy['intro'], ENT_QUOTES, 'UTF-8') ?></p>
        <div class="status-row">
            <span class="status-pill"><?= htmlspecialchars($copy['version'], ENT_QUOTES, 'UTF-8') ?></span>
            <span class="status-pill status-pill-success"><?= htmlspecialchars($copy['direct'], ENT_QUOTES, 'UTF-8') ?></span>
            <span class="status-pill"><span id="download-count">—</span> <?= htmlspecialchars($copy['count'], ENT_QUOTES, 'UTF-8') ?></span>
        </div>
    </section>

    <section class="download-section" aria-labelledby="windows-heading">
        <div class="section-heading">
            <span class="section-icon" aria-hidden="true">▦</span>
            <div>
                <p class="section-kicker">Windows</p>
                <h2 id="windows-heading"><?= htmlspecialchars($copy['windows'], ENT_QUOTES, 'UTF-8') ?></h2>
            </div>
        </div>
        <div class="download-grid download-grid-two">
            <?php foreach ($packages as $id => $package): ?>
                <?php if ($package['group'] !== 'windows') continue; ?>
                <article class="download-card">
                    <div class="card-top">
                        <span class="package-badge">Windows</span>
                        <span class="package-size"><?= htmlspecialchars(snap_human_size((int) $package['size']), ENT_QUOTES, 'UTF-8') ?></span>
                    </div>
                    <h3><?= htmlspecialchars((string) $package[$lang === 'hr' ? 'label_hr' : 'label_en'], ENT_QUOTES, 'UTF-8') ?></h3>
                    <p><?= htmlspecialchars($id === 'windows-setup' ? $copy['setup_note'] : $copy['portable_note'], ENT_QUOTES, 'UTF-8') ?></p>
                    <button
                        class="download-button"
                        type="button"
                        data-package="<?= htmlspecialchars((string) $id, ENT_QUOTES, 'UTF-8') ?>"
                        data-label="<?= htmlspecialchars((string) $package[$lang === 'hr' ? 'label_hr' : 'label_en'], ENT_QUOTES, 'UTF-8') ?>"
                    >
                        <?= htmlspecialchars($copy['download'], ENT_QUOTES, 'UTF-8') ?>
                    </button>
                </article>
            <?php endforeach; ?>
        </div>
    </section>

    <section class="download-section" aria-labelledby="browser-heading">
        <div class="section-heading">
            <span class="section-icon" aria-hidden="true">◎</span>
            <div>
                <p class="section-kicker">Web</p>
                <h2 id="browser-heading"><?= htmlspecialchars($copy['browsers'], ENT_QUOTES, 'UTF-8') ?></h2>
            </div>
        </div>
        <div class="download-grid">
            <?php foreach ($packages as $id => $package): ?>
                <?php if ($package['group'] !== 'browser') continue; ?>
                <article class="download-card">
                    <div class="card-top">
                        <span class="package-badge"><?= htmlspecialchars(ucfirst((string) $id), ENT_QUOTES, 'UTF-8') ?></span>
                        <span class="package-size"><?= htmlspecialchars(snap_human_size((int) $package['size']), ENT_QUOTES, 'UTF-8') ?></span>
                    </div>
                    <h3><?= htmlspecialchars((string) $package[$lang === 'hr' ? 'label_hr' : 'label_en'], ENT_QUOTES, 'UTF-8') ?></h3>
                    <p><?= htmlspecialchars($copy['extension_note'], ENT_QUOTES, 'UTF-8') ?></p>
                    <button
                        class="download-button"
                        type="button"
                        data-package="<?= htmlspecialchars((string) $id, ENT_QUOTES, 'UTF-8') ?>"
                        data-label="<?= htmlspecialchars((string) $package[$lang === 'hr' ? 'label_hr' : 'label_en'], ENT_QUOTES, 'UTF-8') ?>"
                    >
                        <?= htmlspecialchars($copy['download'], ENT_QUOTES, 'UTF-8') ?>
                    </button>
                </article>
            <?php endforeach; ?>
        </div>
    </section>
</main>

<footer>
    <span>© <?= date('Y') ?> SNAPVERE · Brendigo</span>
    <div>
        <a href="https://snapvere.com/privacy"><?= htmlspecialchars($copy['privacy_link'], ENT_QUOTES, 'UTF-8') ?></a>
        <a href="https://snapvere.com/terms"><?= htmlspecialchars($copy['terms_link'], ENT_QUOTES, 'UTF-8') ?></a>
        <a href="mailto:info@snapvere.com">info@snapvere.com</a>
    </div>
</footer>

<div class="modal" id="download-modal" hidden>
    <div class="modal-backdrop" data-close-modal></div>
    <section class="modal-panel" role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <button class="modal-close" type="button" data-close-modal aria-label="<?= htmlspecialchars($copy['cancel'], ENT_QUOTES, 'UTF-8') ?>">×</button>
        <p class="eyebrow"><?= htmlspecialchars($copy['eyebrow'], ENT_QUOTES, 'UTF-8') ?></p>
        <h2 id="modal-title"><?= htmlspecialchars($copy['email_title'], ENT_QUOTES, 'UTF-8') ?></h2>
        <p class="modal-package" id="modal-package-label"></p>
        <p><?= htmlspecialchars($copy['email_intro'], ENT_QUOTES, 'UTF-8') ?></p>

        <form id="download-form" method="post" action="/api/access.php">
            <input type="hidden" name="package" id="package-input" value="">
            <input type="hidden" name="lang" value="<?= htmlspecialchars($lang, ENT_QUOTES, 'UTF-8') ?>">
            <input type="hidden" name="_format" value="json">
            <div class="honeypot" aria-hidden="true">
                <label>Website <input type="text" name="website" tabindex="-1" autocomplete="off"></label>
            </div>
            <label class="field-label" for="email-input"><?= htmlspecialchars($copy['email_label'], ENT_QUOTES, 'UTF-8') ?></label>
            <input
                class="email-input"
                id="email-input"
                type="email"
                name="email"
                required
                maxlength="254"
                autocomplete="email"
                inputmode="email"
                placeholder="<?= htmlspecialchars($copy['email_placeholder'], ENT_QUOTES, 'UTF-8') ?>"
            >
            <p class="form-error" id="form-error" role="alert" hidden></p>
            <button class="download-button download-button-full" id="submit-download" type="submit">
                <?= htmlspecialchars($copy['continue'], ENT_QUOTES, 'UTF-8') ?>
            </button>
        </form>

        <p class="privacy-note">
            <?= htmlspecialchars($copy['privacy'], ENT_QUOTES, 'UTF-8') ?>
            <a href="https://snapvere.com/privacy"><?= htmlspecialchars($copy['privacy_link'], ENT_QUOTES, 'UTF-8') ?></a>.
        </p>
    </section>
</div>

<div
    id="download-copy"
    hidden
    data-invalid-email="<?= htmlspecialchars($copy['invalid_email'], ENT_QUOTES, 'UTF-8') ?>"
    data-generic-error="<?= htmlspecialchars($copy['generic_error'], ENT_QUOTES, 'UTF-8') ?>"
    data-rate-error="<?= htmlspecialchars($copy['rate_error'], ENT_QUOTES, 'UTF-8') ?>"
></div>
</body>
</html>
